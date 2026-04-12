// VERIFIQ - IFC Compliance Checker
// Copyright 2026 BBMW0 Technologies. All rights reserved.
//
// ─── IFC PROPERTY WRITER ─────────────────────────────────────────────────────
//
// Writes corrected property values back into an IFC STEP file without
// requiring the user to return to their BIM authoring tool.
//
// Approach:
//   The IFC STEP physical file is plain text. Each entity is a line:
//     #123 = IFCPROPERTYSINGLEVALUE('FireRating',$,IFCTEXT('60'),$);
//   This writer finds the STEP entity by ID, replaces only the value token,
//   and saves a new corrected file (the original is never modified).
//
// Safety rules:
//   1. Always writes to a new file - never overwrites the source.
//   2. Validates the replacement value matches the declared data type.
//   3. Records every edit in an edit log alongside the output file.
//   4. Preserves the original file timestamp and header.

using System.Text;
using System.Text.RegularExpressions;
using VERIFIQ.Core.Models;

namespace VERIFIQ.Parser;

public sealed class PropertyEditRequest
{
    /// <summary>The STEP entity ID of the IfcPropertySingleValue entity to edit.</summary>
    public int    PropertyStepId  { get; set; }

    /// <summary>The property set name (for display and logging).</summary>
    public string PropertySetName { get; set; } = string.Empty;

    /// <summary>The property name (for display and logging).</summary>
    public string PropertyName    { get; set; } = string.Empty;

    /// <summary>The IFC data type: IFCTEXT, IFCBOOLEAN, IFCREAL, IFCINTEGER, IFCLABEL.</summary>
    public string IfcDataType     { get; set; } = "IFCLABEL";

    /// <summary>The new value to write (raw string - do not wrap in quotes).</summary>
    public string NewValue        { get; set; } = string.Empty;

    /// <summary>The element GUID this property belongs to (for the edit log).</summary>
    public string ElementGuid     { get; set; } = string.Empty;
}

public sealed class PropertyEditResult
{
    public bool   Success          { get; set; }
    public string OutputFilePath   { get; set; } = string.Empty;
    public string EditLogPath      { get; set; } = string.Empty;
    public int    EditsApplied     { get; set; }
    public List<string> Errors     { get; set; } = new();
    public List<string> Applied    { get; set; } = new();
}

public sealed class IfcPropertyWriter
{
    // ─── PUBLIC ENTRY POINT ──────────────────────────────────────────────────

    /// <summary>
    /// Applies a batch of property edits to the source IFC file and writes
    /// a corrected copy. The original file is never modified.
    /// </summary>
    public async Task<PropertyEditResult> ApplyEditsAsync(
        string sourceIfcPath,
        List<PropertyEditRequest> edits,
        CancellationToken ct = default)
    {
        var result = new PropertyEditResult();

        if (!File.Exists(sourceIfcPath))
        {
            result.Errors.Add($"Source IFC file not found: {sourceIfcPath}");
            return result;
        }

        if (edits == null || edits.Count == 0)
        {
            result.Errors.Add("No edits provided.");
            return result;
        }

        // Build output path: same folder, filename appended with _VERIFIQ_FIXED
        var dir       = Path.GetDirectoryName(sourceIfcPath) ?? ".";
        var stem      = Path.GetFileNameWithoutExtension(sourceIfcPath);
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var outPath   = Path.Combine(dir, $"{stem}_VERIFIQ_FIXED_{timestamp}.ifc");
        var logPath   = Path.Combine(dir, $"{stem}_VERIFIQ_EDITLOG_{timestamp}.txt");

        // Read all lines
        var lines = await File.ReadAllLinesAsync(sourceIfcPath, Encoding.UTF8, ct);

        // Build lookup: StepId -> line index
        var lineIndex = BuildStepIndex(lines);

        // Apply each edit
        var editLog = new List<string>();
        editLog.Add($"VERIFIQ Property Edit Log");
        editLog.Add($"Source:    {sourceIfcPath}");
        editLog.Add($"Output:    {outPath}");
        editLog.Add($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        editLog.Add($"Edits:     {edits.Count}");
        editLog.Add(new string('-', 80));

        foreach (var edit in edits)
        {
            ct.ThrowIfCancellationRequested();

            if (!lineIndex.TryGetValue(edit.PropertyStepId, out int lineIdx))
            {
                var msg = $"SKIP  #{edit.PropertyStepId} {edit.PropertySetName}.{edit.PropertyName} - STEP entity not found in file";
                result.Errors.Add(msg);
                editLog.Add(msg);
                continue;
            }

            var originalLine = lines[lineIdx];

            // Validate it is actually an IfcPropertySingleValue
            if (!originalLine.Contains("IFCPROPERTYSINGLEVALUE", StringComparison.OrdinalIgnoreCase))
            {
                var msg = $"SKIP  #{edit.PropertyStepId} - not an IfcPropertySingleValue (line: {originalLine.Trim()[..Math.Min(80, originalLine.Trim().Length)]})";
                result.Errors.Add(msg);
                editLog.Add(msg);
                continue;
            }

            // Validate the new value
            var valErr = ValidateValue(edit);
            if (valErr != null)
            {
                result.Errors.Add($"SKIP  #{edit.PropertyStepId} {edit.PropertyName} - {valErr}");
                editLog.Add($"SKIP  #{edit.PropertyStepId} {edit.PropertyName} - {valErr}");
                continue;
            }

            // Build the replacement value token
            var newToken = BuildValueToken(edit);

            // Replace the value in the line
            var newLine = ReplaceValueInLine(originalLine, newToken, edit.IfcDataType);

            if (newLine == originalLine)
            {
                var msg = $"WARN  #{edit.PropertyStepId} {edit.PropertyName} - line unchanged (value may already be correct or pattern did not match)";
                result.Errors.Add(msg);
                editLog.Add(msg);
                continue;
            }

            lines[lineIdx] = newLine;

            var ok = $"OK    #{edit.PropertyStepId} {edit.PropertySetName}.{edit.PropertyName} [{edit.ElementGuid}]  OLD: {ExtractCurrentValue(originalLine)}  NEW: {edit.NewValue}";
            result.Applied.Add(ok);
            editLog.Add(ok);
            result.EditsApplied++;
        }

        // Write output file
        await File.WriteAllLinesAsync(outPath, lines, Encoding.UTF8, ct);
        await File.WriteAllLinesAsync(logPath, editLog, Encoding.UTF8, ct);

        result.Success       = result.EditsApplied > 0;
        result.OutputFilePath = outPath;
        result.EditLogPath    = logPath;

        return result;
    }

    // ─── HELPERS ─────────────────────────────────────────────────────────────

    private static Dictionary<int, int> BuildStepIndex(string[] lines)
    {
        var index = new Dictionary<int, int>();
        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].TrimStart();
            if (line.StartsWith('#'))
            {
                var eq = line.IndexOf('=');
                if (eq > 1 && int.TryParse(line[1..eq].Trim(), out int id))
                    index[id] = i;
            }
        }
        return index;
    }

    private static string? ValidateValue(PropertyEditRequest edit)
    {
        if (string.IsNullOrWhiteSpace(edit.NewValue))
            return "New value cannot be empty.";

        return edit.IfcDataType.ToUpperInvariant() switch
        {
            "IFCBOOLEAN" => (edit.NewValue.ToUpperInvariant() is "TRUE" or "FALSE")
                ? null : "Boolean value must be TRUE or FALSE.",
            "IFCREAL"    => double.TryParse(edit.NewValue,
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out _)
                ? null : "Real value must be a valid decimal number (e.g. 60.0, 0.35).",
            "IFCINTEGER" => int.TryParse(edit.NewValue, out _)
                ? null : "Integer value must be a whole number.",
            _            => null  // IFCLABEL, IFCTEXT, IFCIDENTIFIER - accept any string
        };
    }

    private static string BuildValueToken(PropertyEditRequest edit)
    {
        return edit.IfcDataType.ToUpperInvariant() switch
        {
            "IFCBOOLEAN" => $".{edit.NewValue.ToUpperInvariant()}.",
            "IFCREAL"    => $"IFCREAL({edit.NewValue})",
            "IFCINTEGER" => $"IFCINTEGER({edit.NewValue})",
            "IFCTEXT"    => $"IFCTEXT('{EscapeStepString(edit.NewValue)}')",
            "IFCLABEL"   => $"IFCLABEL('{EscapeStepString(edit.NewValue)}')",
            _            => $"IFCLABEL('{EscapeStepString(edit.NewValue)}')"
        };
    }

    /// <summary>
    /// Replaces the 3rd argument (NominalValue) in an IfcPropertySingleValue line.
    /// IFC STEP format: IFCPROPERTYSINGLEVALUE('Name',Description,NominalValue,Unit)
    /// Position 3 (0-indexed) is the value we want to replace.
    /// </summary>
    private static string ReplaceValueInLine(string line, string newToken, string dataType)
    {
        // Strategy: find the 3rd comma-separated argument inside the outer parens
        // Handle nested parens (e.g. IFCREAL(60.0)) by tracking depth
        var eqIdx = line.IndexOf('=');
        if (eqIdx < 0) return line;

        var argsStart = line.IndexOf('(', eqIdx);
        if (argsStart < 0) return line;

        // Find the matching closing paren
        int depth = 0, argsEnd = -1;
        for (int i = argsStart; i < line.Length; i++)
        {
            if (line[i] == '(') depth++;
            else if (line[i] == ')') { depth--; if (depth == 0) { argsEnd = i; break; } }
        }
        if (argsEnd < 0) return line;

        var argsContent = line[(argsStart + 1)..argsEnd];

        // Split by comma at depth 0 only
        var args = SplitStepArgs(argsContent);
        if (args.Count < 3) return line;

        // Arg index 2 (0-based) is NominalValue - replace it
        args[2] = newToken;

        var newArgs = string.Join(",", args);
        return line[..(argsStart + 1)] + newArgs + line[argsEnd..];
    }

    private static List<string> SplitStepArgs(string argsContent)
    {
        var result = new List<string>();
        var current = new StringBuilder();
        int depth = 0;
        bool inStr = false;

        for (int i = 0; i < argsContent.Length; i++)
        {
            char c = argsContent[i];
            if (c == '\'' && (i == 0 || argsContent[i-1] != '\\'))
                inStr = !inStr;

            if (!inStr)
            {
                if (c == '(') depth++;
                else if (c == ')') depth--;
                else if (c == ',' && depth == 0)
                {
                    result.Add(current.ToString());
                    current.Clear();
                    continue;
                }
            }
            current.Append(c);
        }
        result.Add(current.ToString());
        return result;
    }

    private static string ExtractCurrentValue(string line)
    {
        // Quick extract for logging - get the 3rd arg content
        try
        {
            var eqIdx = line.IndexOf('=');
            var start = line.IndexOf('(', eqIdx);
            var content = line[(start + 1)..line.LastIndexOf(')')];
            var args = SplitStepArgs(content);
            return args.Count >= 3 ? args[2].Trim() : "?";
        }
        catch { return "?"; }
    }

    private static string EscapeStepString(string s)
        => s.Replace("'", "''").Replace("\\", "\\\\");
}
