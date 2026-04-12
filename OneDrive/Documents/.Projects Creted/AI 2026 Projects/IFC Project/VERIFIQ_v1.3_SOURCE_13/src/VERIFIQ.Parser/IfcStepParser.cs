// VERIFIQ - IFC Compliance Checker
// Copyright 2026 BBMW0 Technologies. Developed by Jia Wen Gan.
// IFC STEP Physical File Parser - reads IFC2x3 and IFC4 .ifc files

using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using VERIFIQ.Core.Enums;
using VERIFIQ.Core.Models;

namespace VERIFIQ.Parser;

/// <summary>
/// Progress callback delegate for large file parsing.
/// </summary>
public delegate void ParseProgressCallback(int percentComplete, string currentStep);

/// <summary>
/// Primary IFC STEP file parser.
/// Reads raw .ifc STEP physical files and constructs an IfcFile object
/// containing all entities, relationships, properties, and classifications.
/// Supports IFC2x3 and IFC4 Reference View (required for IFC+SG / CORENET-X).
/// </summary>
public sealed class IfcStepParser
{
    private readonly ParseProgressCallback? _progress;

    // Raw entity store: StepId -> (ClassName, RawArgs)
    private readonly Dictionary<int, (string ClassName, string RawArgs)> _entities = new();

    // Relationship lookups built after initial parse
    private readonly Dictionary<int, List<int>> _relDefinesByProps  = new(); // elementId -> [psetIds]
    private readonly Dictionary<int, List<int>> _relAssocClassif    = new(); // elementId -> [classifRefIds]
    private readonly Dictionary<int, List<int>> _relContainedInStorey = new(); // elementId -> storeyId
    private readonly Dictionary<int, List<int>> _relAggregate       = new(); // containerId -> [childIds]

    public IfcStepParser(ParseProgressCallback? progress = null)
    {
        _progress = progress;
    }

    // ─── PUBLIC ENTRY POINT ──────────────────────────────────────────────────

    public async Task<IfcFile> ParseAsync(string filePath, CancellationToken ct = default)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"IFC file not found: {filePath}");

        var result = new IfcFile
        {
            FilePath = filePath,
            FileSizeBytes = new FileInfo(filePath).Length,
            Format = DetectFormat(filePath)
        };

        // Handle compressed .ifczip
        string workingPath = filePath;
        if (result.Format == InputFileFormat.IFCZIP)
        {
            workingPath = await ExtractIfcZipAsync(filePath, ct);
        }

        Report(5, "Reading file...");
        var lines = await ReadAllLinesAsync(workingPath, ct);

        Report(15, "Parsing header...");
        ParseHeader(lines, result);

        Report(25, "Indexing entities...");
        await Task.Run(() => IndexEntities(lines), ct);

        Report(45, "Resolving relationships...");
        await Task.Run(() => ResolveRelationships(), ct);

        Report(60, "Building spatial hierarchy...");
        BuildSpatialHierarchy(result);

        Report(75, "Extracting elements...");
        ExtractElements(result);

        Report(90, "Extracting property sets...");
        AssignPropertySets(result);

        Report(93, "Extracting bounding boxes and geometry...");
        AssignBoundingBoxes(result);

        Report(94, "Extracting triangle meshes for 3D viewer...");
        ExtractMeshes(result);

        Report(95, "Resolving classifications...");
        AssignClassifications(result);

        Report(98, "Extracting georeferencing...");
        ExtractGeoreference(result);

        Report(100, "Complete.");

        return result;
    }

    // ─── FORMAT DETECTION ────────────────────────────────────────────────────

    private static InputFileFormat DetectFormat(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return ext switch
        {
            ".ifc"    => InputFileFormat.IFC,
            ".ifcxml" => InputFileFormat.IFCXML,
            ".ifczip" => InputFileFormat.IFCZIP,
            ".dwg"    => InputFileFormat.DWG,
            ".dxf"    => InputFileFormat.DXF,
            ".dwf"    => InputFileFormat.DWF,
            ".dwfx"   => InputFileFormat.DWF,
            ".dgn"    => InputFileFormat.DGN,
            ".rvt"    => InputFileFormat.RVT,
            ".pln"    => InputFileFormat.PLN,
            ".skp"    => InputFileFormat.SKP,
            ".nwd"    => InputFileFormat.NWD,
            ".nwf"    => InputFileFormat.NWF,
            ".nwc"    => InputFileFormat.NWC,
            ".bcf"    => InputFileFormat.BCF,
            ".e57"    => InputFileFormat.E57,
            ".las"    => InputFileFormat.LAS,
            ".laz"    => InputFileFormat.LAZ,
            ".obj"    => InputFileFormat.OBJ,
            ".fbx"    => InputFileFormat.FBX,
            ".stl"    => InputFileFormat.STL,
            ".step"   => InputFileFormat.STEP,
            ".stp"    => InputFileFormat.STEP,
            ".pdf"    => InputFileFormat.PDF,
            ".xlsx"   => InputFileFormat.XLSX,
            ".json"   => InputFileFormat.JSON,
            ".gltf"   => InputFileFormat.GLTF,
            ".glb"    => InputFileFormat.GLB,
            ".usd"    => InputFileFormat.USD,
            ".usdz"   => InputFileFormat.USDZ,
            _         => InputFileFormat.IFC
        };
    }

    private static async Task<string> ExtractIfcZipAsync(string zipPath, CancellationToken ct)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "VERIFIQ_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(tempDir);
        System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, tempDir);
        var ifcFile = Directory.GetFiles(tempDir, "*.ifc", SearchOption.AllDirectories).FirstOrDefault()
            ?? throw new InvalidOperationException("No .ifc file found inside .ifczip archive.");
        return ifcFile;
    }

    private static async Task<string[]> ReadAllLinesAsync(string path, CancellationToken ct)
    {
        return await File.ReadAllLinesAsync(path, Encoding.UTF8, ct);
    }

    // ─── HEADER PARSING ──────────────────────────────────────────────────────

    private static void ParseHeader(string[] lines, IfcFile result)
    {
        var header = result.Header;
        bool inHeader = false;

        foreach (var line in lines)
        {
            var trimmed = line.Trim();

            if (trimmed.StartsWith("ISO-10303-21"))  { inHeader = true; continue; }
            if (trimmed.StartsWith("DATA;"))         { break; }

            if (!inHeader) continue;

            if (trimmed.StartsWith("FILE_DESCRIPTION"))
                header.Description = ExtractFirstString(trimmed);

            else if (trimmed.StartsWith("FILE_NAME"))
            {
                var parts = ExtractStrings(trimmed);
                if (parts.Count > 0) header.FileName = parts[0];
                if (parts.Count > 1 && DateTime.TryParse(parts[1], out var dt)) header.TimeStamp = dt;
                if (parts.Count > 2) header.Author = parts[2];
                if (parts.Count > 3) header.Organisation = parts[3];
                if (parts.Count > 5) header.Authorization = parts[5];
            }

            else if (trimmed.StartsWith("FILE_SCHEMA"))
            {
                var schema = ExtractFirstString(trimmed).ToUpperInvariant();
                header.SchemaIdentifier = schema;
                result.Schema = schema switch
                {
                    var s when s.Contains("IFC4X3") => IfcSchemaVersion.IFC4X3,
                    var s when s.Contains("IFC4")   => IfcSchemaVersion.IFC4,
                    var s when s.Contains("IFC2X3") => IfcSchemaVersion.IFC2X3,
                    _                               => IfcSchemaVersion.Unknown
                };
            }
        }
    }

    // ─── ENTITY INDEXING ─────────────────────────────────────────────────────

    private void IndexEntities(string[] lines)
    {
        bool inData = false;
        var entityPattern = new Regex(@"^#(\d+)\s*=\s*([A-Z][A-Z0-9_]*)\s*\((.*)\)\s*;?\s*$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Multi-line entity accumulation
        var buffer = new StringBuilder();
        int bufferedId = -1;

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();

            if (line.Equals("DATA;", StringComparison.OrdinalIgnoreCase)) { inData = true; continue; }
            if (line.StartsWith("ENDSEC") || line.StartsWith("END-ISO")) { inData = false; continue; }
            if (!inData || string.IsNullOrWhiteSpace(line)) continue;

            // Try to match a complete entity on one line
            var match = entityPattern.Match(line);
            if (match.Success)
            {
                int id = int.Parse(match.Groups[1].Value);
                string cls = match.Groups[2].Value.ToUpperInvariant();
                string args = match.Groups[3].Value;
                _entities[id] = (cls, args);
                continue;
            }

            // Partial line (multi-line entity) - accumulate
            if (line.StartsWith("#") && !line.Contains("="))
                continue; // reference only

            if (line.StartsWith("#") && line.Contains("="))
            {
                buffer.Clear();
                buffer.Append(line);
                // Extract id
                var idMatch = Regex.Match(line, @"^#(\d+)");
                if (idMatch.Success)
                    bufferedId = int.Parse(idMatch.Groups[1].Value);
            }
            else if (buffer.Length > 0)
            {
                buffer.Append(' ');
                buffer.Append(line);
                if (line.EndsWith(';'))
                {
                    // Try to parse the accumulated buffer
                    var bufMatch = entityPattern.Match(buffer.ToString());
                    if (bufMatch.Success)
                    {
                        int id = int.Parse(bufMatch.Groups[1].Value);
                        string cls = bufMatch.Groups[2].Value.ToUpperInvariant();
                        string args = bufMatch.Groups[3].Value;
                        _entities[id] = (cls, args);
                    }
                    buffer.Clear();
                    bufferedId = -1;
                }
            }
        }
    }

    // ─── RELATIONSHIP RESOLUTION ─────────────────────────────────────────────

    private void ResolveRelationships()
    {
        foreach (var (id, (cls, args)) in _entities)
        {
            switch (cls)
            {
                case "IFCRELDEFINESBYPROPERTIES":
                    ParseRelDefinesByProperties(id, args);
                    break;
                case "IFCRELASSOCIATESCLASSIFICATION":
                    ParseRelAssociatesClassification(id, args);
                    break;
                case "IFCRELCONTAINEDINSPATIALSTRUCTURE":
                    ParseRelContainedInSpatialStructure(id, args);
                    break;
                case "IFCRELAGGREGATES":
                    ParseRelAggregates(id, args);
                    break;
            }
        }
    }

    private void ParseRelDefinesByProperties(int relId, string args)
    {
        // IFCRELDEFINESBYPROPERTIES(GUID,OwnerHistory,Name,Description,(#elem1,#elem2,...),#psetId)
        var refs = ExtractAllEntityRefs(args);
        if (refs.Count < 2) return;

        // Last ref is the property set; all preceding refs (after OwnerHistory) are elements
        int psetId = refs[^1];
        var elementRefs = ExtractInlineList(args); // the (#elem,...) part

        foreach (var elemId in elementRefs)
        {
            if (!_relDefinesByProps.ContainsKey(elemId))
                _relDefinesByProps[elemId] = new();
            _relDefinesByProps[elemId].Add(psetId);
        }
    }

    private void ParseRelAssociatesClassification(int relId, string args)
    {
        var refs = ExtractAllEntityRefs(args);
        if (refs.Count < 2) return;
        int classifRef = refs[^1];
        var elementRefs = ExtractInlineList(args);
        foreach (var elemId in elementRefs)
        {
            if (!_relAssocClassif.ContainsKey(elemId))
                _relAssocClassif[elemId] = new();
            _relAssocClassif[elemId].Add(classifRef);
        }
    }

    private void ParseRelContainedInSpatialStructure(int relId, string args)
    {
        var refs = ExtractAllEntityRefs(args);
        if (refs.Count < 2) return;
        int containerId = refs[^1];
        var elementRefs = ExtractInlineList(args);
        foreach (var elemId in elementRefs)
        {
            if (!_relContainedInStorey.ContainsKey(elemId))
                _relContainedInStorey[elemId] = new();
            _relContainedInStorey[elemId].Add(containerId);
        }
    }

    private void ParseRelAggregates(int relId, string args)
    {
        var refs = ExtractAllEntityRefs(args);
        if (refs.Count < 2) return;
        int parentId = refs[0]; // After GUID, OwnerHistory
        var childRefs = ExtractInlineList(args);
        if (!_relAggregate.ContainsKey(parentId))
            _relAggregate[parentId] = new();
        _relAggregate[parentId].AddRange(childRefs);
    }

    // ─── SPATIAL HIERARCHY ────────────────────────────────────────────────────

    private void BuildSpatialHierarchy(IfcFile result)
    {
        foreach (var (id, (cls, args)) in _entities)
        {
            switch (cls)
            {
                case "IFCPROJECT":
                    result.Project = BuildIfcProject(id, args);
                    break;
                case "IFCSITE":
                    result.Sites.Add(BuildIfcSite(id, args));
                    break;
                case "IFCBUILDING":
                    result.Buildings.Add(BuildIfcBuilding(id, args));
                    break;
                case "IFCBUILDINGSTOREY":
                    result.Storeys.Add(BuildIfcStorey(id, args));
                    break;
                case "IFCSPACE":
                    result.Spaces.Add(BuildIfcSpace(id, args));
                    break;
            }
        }
    }

    private IfcProject BuildIfcProject(int id, string args)
    {
        var strs = ExtractStrings(args);
        return new IfcProject
        {
            StepId = id,
            GlobalId = strs.ElementAtOrDefault(0) ?? string.Empty,
            Name = strs.ElementAtOrDefault(2) ?? string.Empty,
            Description = strs.ElementAtOrDefault(3) ?? string.Empty,
            LongName = strs.ElementAtOrDefault(4) ?? string.Empty,
            Phase = strs.ElementAtOrDefault(5) ?? string.Empty,
            IfcClass = "IFCPROJECT"
        };
    }

    private IfcSite BuildIfcSite(int id, string args)
    {
        var strs = ExtractStrings(args);
        return new IfcSite
        {
            StepId = id,
            GlobalId = strs.ElementAtOrDefault(0) ?? string.Empty,
            Name = strs.ElementAtOrDefault(2) ?? string.Empty,
            LongName = strs.ElementAtOrDefault(4) ?? string.Empty,
            IfcClass = "IFCSITE"
        };
    }

    private IfcBuilding BuildIfcBuilding(int id, string args)
    {
        var strs = ExtractStrings(args);
        return new IfcBuilding
        {
            StepId = id,
            GlobalId = strs.ElementAtOrDefault(0) ?? string.Empty,
            Name = strs.ElementAtOrDefault(2) ?? string.Empty,
            LongName = strs.ElementAtOrDefault(4) ?? string.Empty,
            IfcClass = "IFCBUILDING"
        };
    }

    private IfcBuildingStorey BuildIfcStorey(int id, string args)
    {
        var strs = ExtractStrings(args);
        double? elevation = null;
        var allTokens = TokeniseArgs(args);
        // Elevation is typically the last real-number argument
        foreach (var token in ((IEnumerable<string>)allTokens).Reverse())
        {
            if (double.TryParse(token, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out double d))
            {
                elevation = d; break;
            }
        }
        return new IfcBuildingStorey
        {
            StepId = id,
            GlobalId = strs.ElementAtOrDefault(0) ?? string.Empty,
            Name = strs.ElementAtOrDefault(2) ?? string.Empty,
            LongName = strs.ElementAtOrDefault(4) ?? string.Empty,
            Elevation = elevation,
            IfcClass = "IFCBUILDINGSTOREY"
        };
    }

    private IfcSpace BuildIfcSpace(int id, string args)
    {
        var strs = ExtractStrings(args);
        return new IfcSpace
        {
            StepId = id,
            GlobalId = strs.ElementAtOrDefault(0) ?? string.Empty,
            Name = strs.ElementAtOrDefault(2) ?? string.Empty,
            LongName = strs.ElementAtOrDefault(4) ?? string.Empty,
            IfcClass = "IFCSPACE"
        };
    }

    // ─── ELEMENT EXTRACTION ───────────────────────────────────────────────────

    // IFC entity classes that represent physical building elements
    private static readonly HashSet<string> ElementClasses = new(StringComparer.OrdinalIgnoreCase)
    {
        "IFCWALL","IFCWALLSTANDARDCASE","IFCSLAB","IFCBEAM","IFCCOLUMN","IFCFOOTING",
        "IFCDOOR","IFCWINDOW","IFCROOF","IFCSTAIR","IFCSTAIRFLIGHT","IFCRAMP",
        "IFCRAMPFLIGHT","IFCRAILING","IFCPLATE","IFCMEMBER","IFCCURTAINWALL",
        "IFCCOVERING","IFCBUILDINGELEMENTPROXY","IFCPILE","IFCREINFORCINGBAR",
        "IFCREINFORCINGMESH","IFCTENDON","IFCSHADING","IFCFURNISHINGELEMENT",
        "IFCFURNITURE","IFCSYSTEMFURNITUREELEMENT","IFCTRANSPORTELEMENT",
        "IFCBUILDINGELEMENT","IFCPIPESEGMENT","IFCPIPEFITTING","IFCDUCTSEGMENT",
        "IFCDUCTFITTING","IFCLIGHTFIXTURE","IFCSENSOR","IFCFLOWSEGMENT",
        "IFCFLOWFITTING","IFCFLOWCONTROLLER","IFCFLOWTERMINAL","IFCFLOWINSTRUMENT",
        "IFCDISTRIBUTIONCONTROLELEMENT","IFCENERGYCONVERSIONDEVICE",
        "IFCELECTRICALDISTRIBUTIONBOARDTYPE","IFCJUNCTIONBOX","IFCCABLECARRIERFITTING",
        "IFCCABLECARRIERSEGMENT","IFCCABLESEGMENT","IFCCABLEFITTING",
        "IFCACTUATOR","IFCALARM","IFCCONTROLLER","IFCDAMPERFITTING",
        "IFCELECTRICMOTOR","IFCUNITARYCONTROLELEMENT",
        // Structural extras
        "IFCSTRUCTURALMEMBER","IFCSTRUCTURALCURVEMEMBER","IFCSTRUCTURALSURFACEMEMBER"
    };

    private void ExtractElements(IfcFile result)
    {
        // Build storey lookup: stepId -> storey
        var storeyById = result.Storeys.ToDictionary(s => s.StepId);

        foreach (var (id, (cls, args)) in _entities)
        {
            if (!ElementClasses.Contains(cls)) continue;

            var strs = ExtractStrings(args);
            var element = new IfcElement
            {
                StepId       = id,
                GlobalId     = strs.ElementAtOrDefault(0) ?? string.Empty,
                Name         = strs.ElementAtOrDefault(2) ?? string.Empty,
                Description  = strs.ElementAtOrDefault(3) ?? string.Empty,
                ObjectType   = strs.ElementAtOrDefault(4) ?? string.Empty,
                IfcClass     = cls,
                PredefinedType = ExtractPredefinedType(args)
            };

            // Assign storey
            if (_relContainedInStorey.TryGetValue(id, out var storeyIds))
            {
                var storeyId = storeyIds.FirstOrDefault();
                if (storeyById.TryGetValue(storeyId, out var storey))
                {
                    element.StoreyGuid = storey.GlobalId;
                    element.StoreyName = storey.Name;
                }
            }

            result.Elements.Add(element);
        }
    }

    // ─── PROPERTY SET ASSIGNMENT ──────────────────────────────────────────────

    private void AssignPropertySets(IfcFile result)
    {
        var psetById = new Dictionary<int, IfcPropertySet>();

        // First pass: build all property sets
        foreach (var (id, (cls, args)) in _entities)
        {
            if (cls != "IFCPROPERTYSET" && cls != "IFCQUANTITYSET") continue;
            var pset = ParsePropertySet(id, args, cls);
            psetById[id] = pset;
        }

        // Build unified lookup: stepId → IfcObject (elements + spaces)
        var elementByStepId = result.Elements.ToDictionary(e => e.StepId);
        var spaceByStepId   = result.Spaces.ToDictionary(s => s.StepId);
        var storeyById      = result.Storeys.ToDictionary(s => s.StepId);

        // Second pass: assign property sets to elements AND spaces
        foreach (var (objId, psetIds) in _relDefinesByProps)
        {
            IList<IfcPropertySet>? target = null;

            if (elementByStepId.TryGetValue(objId, out var element))
                target = element.PropertySets;
            else if (spaceByStepId.TryGetValue(objId, out var space))
                target = space.PropertySets;

            if (target == null) continue;

            foreach (var psetId in psetIds)
            {
                if (psetById.TryGetValue(psetId, out var pset))
                    target.Add(pset);
            }
        }

        // Assign StoreyGuid and StoreyName to spaces via spatial containment
        foreach (var space in result.Spaces)
        {
            if (_relContainedInStorey.TryGetValue(space.StepId, out var storeyIds))
            {
                var storeyId = storeyIds.FirstOrDefault();
                if (storeyById.TryGetValue(storeyId, out var storey))
                {
                    space.StoreyGuid = storey.GlobalId;
                    space.StoreyName = storey.Name;
                }
            }
        }

        result.PropertySets.AddRange(psetById.Values);
    }

    private IfcPropertySet ParsePropertySet(int id, string args, string cls)
    {
        var strs = ExtractStrings(args);
        var pset = new IfcPropertySet
        {
            StepId = id,
            GlobalId = strs.ElementAtOrDefault(0) ?? string.Empty,
            Name = strs.ElementAtOrDefault(2) ?? string.Empty,
            Description = strs.ElementAtOrDefault(3) ?? string.Empty
        };

        // Find property refs in the inline list
        var propRefs = ExtractInlineList(args);
        foreach (var propId in propRefs)
        {
            if (_entities.TryGetValue(propId, out var propEntity))
            {
                var prop = ParseProperty(propId, propEntity.ClassName, propEntity.RawArgs);
                if (prop != null) pset.Properties.Add(prop);
            }
        }

        return pset;
    }

    private IfcProperty? ParseProperty(int id, string cls, string args)
    {
        if (cls != "IFCPROPERTYSINGLEVALUE" &&
            cls != "IFCPROPERTYENUMERATEDVALUE" &&
            cls != "IFCPROPERTYLISTVALUE" &&
            cls != "IFCPROPERTYBOUNDEDVALUE" &&
            cls != "IFCQUANTITYLENGTH" &&
            cls != "IFCQUANTITYAREA" &&
            cls != "IFCQUANTITYVOLUME" &&
            cls != "IFCQUANTITYCOUNT" &&
            cls != "IFCQUANTITYWEIGHT")
            return null;

        var strs = ExtractStrings(args);
        var prop = new IfcProperty
        {
            Name = strs.ElementAtOrDefault(0) ?? string.Empty,
            PropertyType = cls
        };

        // For SingleValue: (Name, Description, NominalValue, Unit)
        // NominalValue is an IfcValue: IFCLABEL('...'), IFCBOOLEAN(.T.), IFCREAL(3.14), etc.
        if (cls == "IFCPROPERTYSINGLEVALUE")
        {
            prop.Value = ExtractNominalValue(args);
        }
        else if (cls == "IFCPROPERTYENUMERATEDVALUE")
        {
            // (Name, Description, ListOfValues(...), EnumReference)
            var listMatch = Regex.Match(args, @"IFCVALUE\s*\(\s*'([^']+)'\s*\)");
            var values = new List<string>();
            foreach (Match m in Regex.Matches(args, @"\.([A-Z_]+)\."))
                values.Add(m.Groups[1].Value);
            // Also try string values
            foreach (Match m in Regex.Matches(args, @"IFCLABEL\('([^']+)'\)"))
                values.Add(m.Groups[1].Value);
            prop.Value = values.Count == 1 ? (object)values[0] : values;
        }
        else if (cls is "IFCQUANTITYLENGTH" or "IFCQUANTITYAREA" or "IFCQUANTITYVOLUME")
        {
            // (Name, Description, Unit, Value, Formula)
            var tokens = TokeniseArgs(args);
            foreach (var token in tokens)
            {
                if (double.TryParse(token, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out double d))
                {
                    prop.Value = d; break;
                }
            }
        }

        return prop;
    }

    private static string? ExtractNominalValue(string args)
    {
        // IFCLABEL('value'), IFCTEXT('value')
        var labelMatch = Regex.Match(args, @"IFC(?:LABEL|TEXT|IDENTIFIER)\('([^']*)'\)");
        if (labelMatch.Success) return labelMatch.Groups[1].Value;

        // IFCBOOLEAN(.T.) or IFCBOOLEAN(.F.)
        var boolMatch = Regex.Match(args, @"IFCBOOLEAN\(\.(T|F|TRUE|FALSE)\.\)");
        if (boolMatch.Success) return boolMatch.Groups[1].Value switch
        {
            "T" or "TRUE"  => "TRUE",
            "F" or "FALSE" => "FALSE",
            _ => null
        };

        // IFCREAL(3.14), IFCINTEGER(42), IFCLENGTHMEASURE(...)
        var numMatch = Regex.Match(args, @"IFC(?:REAL|INTEGER|LENGTHMEASURE|AREAMEASURE|VOLUMEMEASURE)\(([0-9.\-E+]+)\)");
        if (numMatch.Success) return numMatch.Groups[1].Value;

        // .NOTDEFINED., .TRUE., .FALSE., etc.
        var dotMatch = Regex.Match(args, @"\.([A-Z_]+)\.");
        if (dotMatch.Success) return dotMatch.Groups[1].Value;

        return null;
    }

    // ─── BOUNDING BOX EXTRACTION ─────────────────────────────────────────────────

    /// <summary>
    /// Extracts bounding box dimensions from IfcBoundingBox entities and assigns them
    /// to elements. This enables the design code engine to check dimensions, widths,
    /// heights and thicknesses against regulatory minimums.
    /// IFC BoundingBox: IFCBOUNDINGBOX(Corner, XDim, YDim, ZDim)
    /// where Corner is IfcCartesianPoint and dims are in metres.
    /// When no IFCBOUNDINGBOX exists (common in Brep/SweptSolid models), falls back to
    /// extracting element origin from IFCLOCALPLACEMENT so the 3D viewer has positions.
    /// </summary>
    private void AssignBoundingBoxes(IfcFile result)
    {
        // Build IFCBOUNDINGBOX lookup: bboxStepId -> (xDim, yDim, zDim)
        var bboxById = new Dictionary<int, (double X, double Y, double Z)>();
        foreach (var (id, (cls, args)) in _entities)
        {
            if (cls != "IFCBOUNDINGBOX") continue;
            var tokens = TokeniseArgs(args);
            if (tokens.Count >= 4
                && double.TryParse(tokens[1].Trim(),
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out double x)
                && double.TryParse(tokens[2].Trim(),
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out double y)
                && double.TryParse(tokens[3].Trim(),
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out double z))
            {
                bboxById[id] = (x, y, z);
            }
        }

        // ── Build CartesianPoint lookup ──────────────────────────────────────
        // Used for local placement extraction when no bounding boxes are present.
        var cartPtById = new Dictionary<int, (double X, double Y, double Z)>();
        foreach (var (id, (cls, args)) in _entities)
        {
            if (cls != "IFCCARTESIANPOINT") continue;
            var tokens = TokeniseArgs(args);
            // IFCCARTESIANPOINT((x,y,z)) - args contains a nested tuple
            var raw = args.Trim().Trim('(', ')');
            var coords = raw.Split(',');
            if (coords.Length >= 3
                && double.TryParse(coords[0].Trim(),
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out double cx)
                && double.TryParse(coords[1].Trim(),
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out double cy)
                && double.TryParse(coords[2].Trim(),
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out double cz))
            {
                cartPtById[id] = (cx, cy, cz);
            }
        }

        // ── Build Axis2Placement3D → origin-point lookup ─────────────────────
        // IFCAXIS2PLACEMENT3D(#LocationPoint, ...) → CartesianPoint
        var axisOriginById = new Dictionary<int, (double X, double Y, double Z)>();
        foreach (var (id, (cls, args)) in _entities)
        {
            if (cls != "IFCAXIS2PLACEMENT3D") continue;
            var tokens = TokeniseArgs(args);
            if (tokens.Count >= 1 && tokens[0].Trim().StartsWith('#')
                && int.TryParse(tokens[0].Trim()[1..], out int ptId)
                && cartPtById.TryGetValue(ptId, out var pt))
            {
                axisOriginById[id] = pt;
            }
        }

        // ── Build LocalPlacement → origin lookup ─────────────────────────────
        // IFCLOCALPLACEMENT(#RelativeTo, #Placement3D) → origin
        var placementOriginById = new Dictionary<int, (double X, double Y, double Z)>();
        foreach (var (id, (cls, args)) in _entities)
        {
            if (cls != "IFCLOCALPLACEMENT") continue;
            var tokens = TokeniseArgs(args);
            // Token 1 (index 1) is the Axis2Placement3D reference
            if (tokens.Count >= 2 && tokens[1].Trim().StartsWith('#')
                && int.TryParse(tokens[1].Trim()[1..], out int axId)
                && axisOriginById.TryGetValue(axId, out var origin))
            {
                placementOriginById[id] = origin;
            }
        }

        var elementByStepId = result.Elements.ToDictionary(e => e.StepId);
        var spaceByStepId   = result.Spaces.ToDictionary(s => s.StepId);

        // ── Assign bounding boxes from IFCBOUNDINGBOX ──────────────────────────
        if (bboxById.Count > 0)
        {
            foreach (var (id, (cls, args)) in _entities)
            {
                if (!ElementClasses.Contains(cls) && cls != "IFCSPACE") continue;

                var tokens = TokeniseArgs(args);
                if (tokens.Count < 7) continue;

                var repToken = tokens[6].Trim();
                if (!repToken.StartsWith('#')) continue;
                if (!int.TryParse(repToken[1..], out int repShapeId)) continue;
                if (!_entities.TryGetValue(repShapeId, out var repShapeEnt)) continue;
                if (repShapeEnt.ClassName != "IFCPRODUCTDEFINITIONSHAPE") continue;

                (double X, double Y, double Z)? bbox = null;
                foreach (var innerRepId in ExtractAllEntityRefs(repShapeEnt.RawArgs))
                {
                    if (!_entities.TryGetValue(innerRepId, out var repEnt)) continue;
                    if (repEnt.ClassName != "IFCSHAPEREPRESENTATION") continue;
                    foreach (var itemId in ExtractAllEntityRefs(repEnt.RawArgs))
                    {
                        if (bboxById.TryGetValue(itemId, out var b)) { bbox = b; break; }
                    }
                    if (bbox.HasValue) break;
                }

                if (!bbox.HasValue) continue;

                var bb = new BoundingBox
                {
                    MinX = 0, MaxX = bbox.Value.X,
                    MinY = 0, MaxY = bbox.Value.Y,
                    MinZ = 0, MaxZ = bbox.Value.Z
                };
                if (elementByStepId.TryGetValue(id, out var elem)) elem.BoundingBox = bb;
            }
        }

        // ── Fallback: assign origin from LocalPlacement when no BoundingBox ───
        // This gives the 3D viewer a real position for every element even when
        // the model uses Brep or SweptSolid geometry (no IFCBOUNDINGBOX).
        // Produces a small representative box at the element's insertion point.
        if (placementOriginById.Count > 0)
        {
            foreach (var (id, (cls, args)) in _entities)
            {
                if (!ElementClasses.Contains(cls) && cls != "IFCSPACE") continue;

                var tokens = TokeniseArgs(args);
                // IFC element: token[5] (index 5) is usually the placement reference
                if (tokens.Count < 6) continue;
                var placToken = tokens[5].Trim();
                if (!placToken.StartsWith('#')) continue;
                if (!int.TryParse(placToken[1..], out int placId)) continue;
                if (!placementOriginById.TryGetValue(placId, out var origin)) continue;

                if (elementByStepId.TryGetValue(id, out var elem) && elem.BoundingBox == null)
                {
                    // Assign a representative size based on IFC class for 3D viewer display.
                    // These are approximate typical dimensions - not structural values.
                    var (w, h, d) = EstimateElementSize(cls);
                    elem.BoundingBox = new BoundingBox
                    {
                        MinX = origin.X,         MaxX = origin.X + w,
                        MinY = origin.Y,         MaxY = origin.Y + h,
                        MinZ = origin.Z,         MaxZ = origin.Z + d
                    };
                }
            }
        }
    }

    /// <summary>Builds a unit box mesh (-hx..hx, -hy..hy, 0..hz) as flat verts + triangle indices.</summary>
    private static (List<float> Verts, List<int> Idx) BuildBoxMesh(float hx, float hy, float hz)
    {
        var v = new List<float>();
        var i = new List<int>();
        // 6 faces, each 2 triangles = 12 triangles total
        // Each face: 4 unique verts (fan), normal embedded in order
        var faceVerts = new (float,float,float)[][]
        {
            new[]{(-hx,-hy,0f),( hx,-hy,0f),( hx, hy,0f),(-hx, hy,0f)},  // bottom
            new[]{(-hx,-hy,hz),( hx,-hy,hz),( hx, hy,hz),(-hx, hy,hz)},  // top
            new[]{(-hx,-hy, 0),(-hx, hy, 0),(-hx, hy,hz),(-hx,-hy,hz)},  // left
            new[]{( hx,-hy, 0),( hx,-hy,hz),( hx, hy,hz),( hx, hy, 0)},  // right
            new[]{(-hx,-hy, 0),(-hx,-hy,hz),( hx,-hy,hz),( hx,-hy, 0)},  // front
            new[]{(-hx, hy, 0),( hx, hy, 0),( hx, hy,hz),(-hx, hy,hz)},  // back
        };
        int vi = 0;
        foreach (var face in faceVerts)
        {
            int b = vi;
            foreach (var (fx,fy,fz) in face) { v.Add(fx); v.Add(fy); v.Add(fz); vi++; }
            i.Add(b); i.Add(b+1); i.Add(b+2);
            i.Add(b); i.Add(b+2); i.Add(b+3);
        }
        return (v, i);
    }

    /// <summary>Returns a representative (width, height, depth) in metres for common IFC classes.</summary>
    private static (double W, double H, double D) EstimateElementSize(string ifcClass) =>
        ifcClass.ToUpperInvariant() switch
        {
            "IFCWALL" or "IFCWALLSTANDARDCASE" => (0.25, 3.0, 5.0),
            "IFCSLAB"                           => (5.0,  0.2, 5.0),
            "IFCBEAM"                           => (0.3,  0.5, 5.0),
            "IFCCOLUMN"                         => (0.4,  3.0, 0.4),
            "IFCFOOTING" or "IFCPILE"           => (0.5,  1.0, 0.5),
            "IFCDOOR"                           => (1.0,  2.1, 0.1),
            "IFCWINDOW"                         => (1.2,  1.2, 0.1),
            "IFCSTAIR"                          => (1.2,  3.0, 3.0),
            "IFCSPACE"                          => (5.0,  3.0, 5.0),
            "IFCROOF"                           => (10.0, 0.3, 10.0),
            "IFCPLATE"                          => (2.0,  0.02, 1.0),
            "IFCMEMBER"                         => (0.15, 0.15, 3.0),
            "IFCRAILING"                        => (3.0,  1.1, 0.05),
            _                                   => (1.0,  1.0, 1.0),
        };

    // ─── CLASSIFICATION ASSIGNMENT ────────────────────────────────────────────

    private void AssignClassifications(IfcFile result)
    {
        var elementByStepId = result.Elements.ToDictionary(e => e.StepId);

        foreach (var (elemId, classifIds) in _relAssocClassif)
        {
            if (!elementByStepId.TryGetValue(elemId, out var element)) continue;
            foreach (var refId in classifIds)
            {
                if (_entities.TryGetValue(refId, out var refEntity) &&
                    refEntity.ClassName == "IFCCLASSIFICATIONREFERENCE")
                {
                    var classif = ParseClassificationReference(refId, refEntity.RawArgs);
                    element.Classifications.Add(classif);
                }
            }
        }

        // Also build the global classification system list
        foreach (var (id, (cls, args)) in _entities)
        {
            if (cls == "IFCCLASSIFICATION")
            {
                var strs = ExtractStrings(args);
                result.ClassificationSystems.Add(new IfcClassificationSystem
                {
                    Source = strs.ElementAtOrDefault(0) ?? string.Empty,
                    Edition = strs.ElementAtOrDefault(1) ?? string.Empty,
                    Name = strs.ElementAtOrDefault(3) ?? string.Empty,
                    Description = strs.ElementAtOrDefault(4) ?? string.Empty
                });
            }
        }
    }

    private static IfcClassificationReference ParseClassificationReference(int id, string args)
    {
        var strs = ExtractStrings(args);
        return new IfcClassificationReference
        {
            Location = strs.ElementAtOrDefault(0) ?? string.Empty,
            ItemReference = strs.ElementAtOrDefault(1) ?? string.Empty,
            Name = strs.ElementAtOrDefault(2) ?? string.Empty
        };
    }

    // ─── GEOREFERENCING ───────────────────────────────────────────────────────

    private void ExtractGeoreference(IfcFile result)
    {
        IfcGeoreference? geo = null;

        foreach (var (id, (cls, args)) in _entities)
        {
            if (cls == "IFCMAPCONVERSION")
            {
                geo ??= new IfcGeoreference();
                var tokens = TokeniseArgs(args);
                var nums = tokens.Select(t =>
                    double.TryParse(t, System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out double d) ? (double?)d : null)
                    .Where(n => n.HasValue).Select(n => n!.Value).ToList();
                if (nums.Count >= 3)
                {
                    geo.Eastings = nums[0];
                    geo.Northings = nums[1];
                    geo.OrthogonalHeight = nums[2];
                }
                if (nums.Count >= 5)
                {
                    geo.XAxisAbscissa = nums[3];
                    geo.XAxisOrdinate = nums[4];
                }
                if (nums.Count >= 6) geo.Scale = nums[5];
            }
            else if (cls == "IFCPROJECTEDCRS")
            {
                geo ??= new IfcGeoreference();
                var strs = ExtractStrings(args);
                geo.CrsName = strs.ElementAtOrDefault(0);
                geo.CrsDescription = strs.ElementAtOrDefault(1);
                geo.CrsGeodeticDatum = strs.ElementAtOrDefault(2);
                geo.CrsVerticalDatum = strs.ElementAtOrDefault(3);
                geo.MapProjection = strs.ElementAtOrDefault(4);
                geo.MapZone = strs.ElementAtOrDefault(5);
            }
        }

        result.Georeference = geo;
    }

    // ─── HELPER METHODS ───────────────────────────────────────────────────────

    private static string ExtractPredefinedType(string args)
    {
        // Predefined type is typically the last enumerated token: .STANDARD., .FLOOR., etc.
        var matches = Regex.Matches(args, @"\.([A-Z_]+)\.");
        if (matches.Count == 0) return string.Empty;
        return matches[^1].Groups[1].Value;
    }

    /// <summary>Extract quoted strings from IFC args.</summary>
    private static List<string> ExtractStrings(string args)
    {
        var result = new List<string>();
        foreach (Match m in Regex.Matches(args, @"'((?:[^'\\]|\\.|'')*)'"))
            result.Add(m.Groups[1].Value.Replace("''", "'"));
        return result;
    }

    private static string ExtractFirstString(string args)
    {
        var strs = ExtractStrings(args);
        return strs.Count > 0 ? strs[0] : string.Empty;
    }

    /// <summary>Extract all #nnnn entity references from args string.</summary>
    private static List<int> ExtractAllEntityRefs(string args)
    {
        var result = new List<int>();
        foreach (Match m in Regex.Matches(args, @"#(\d+)"))
            result.Add(int.Parse(m.Groups[1].Value));
        return result;
    }

    /// <summary>Extract entity references from the first inline list (...) found.</summary>
    private static List<int> ExtractInlineList(string args)
    {
        // Find the first (...) group that contains #refs
        var listMatch = Regex.Match(args, @"\(([^()]*#[^()]*)\)");
        if (!listMatch.Success) return new List<int>();
        return ExtractAllEntityRefs(listMatch.Groups[1].Value);
    }

    /// <summary>Simple tokeniser - splits args by comma outside quoted strings and parens.</summary>
    private static List<string> TokeniseArgs(string args)
    {
        var tokens = new List<string>();
        var buf = new StringBuilder();
        int depth = 0;
        bool inStr = false;

        foreach (char c in args)
        {
            if (c == '\'' && !inStr) { inStr = true; buf.Append(c); continue; }
            if (c == '\'' && inStr)  { inStr = false; buf.Append(c); continue; }
            if (inStr) { buf.Append(c); continue; }

            if (c == '(') { depth++; buf.Append(c); continue; }
            if (c == ')') { depth--; buf.Append(c); continue; }

            if (c == ',' && depth == 0)
            {
                tokens.Add(buf.ToString().Trim());
                buf.Clear();
            }
            else
            {
                buf.Append(c);
            }
        }
        if (buf.Length > 0) tokens.Add(buf.ToString().Trim());
        return tokens;
    }

    private void Report(int pct, string step) => _progress?.Invoke(pct, step);

    // ─── MESH EXTRACTION ─────────────────────────────────────────────────────
    // Extracts triangle meshes from IFCFACETEDBREP and IFCEXTRUDEDAREASOLID geometry.
    // These are the two most common geometry types in architectural IFC models.
    // The resulting IfcMesh is used by the 3D viewer for real geometry rendering.

    private void ExtractMeshes(IfcFile result)
    {
        // ── 1. Build CartesianPoint lookup (already computed, rebuild locally) ──
        var pts = new Dictionary<int, (float X, float Y, float Z)>();
        foreach (var (id, (cls, args)) in _entities)
        {
            if (cls != "IFCCARTESIANPOINT") continue;
            var raw    = args.Trim().Trim('(', ')');
            var coords = raw.Split(',');
            if (coords.Length >= 3 &&
                float.TryParse(coords[0].Trim(), System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out float cx) &&
                float.TryParse(coords[1].Trim(), System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out float cy) &&
                float.TryParse(coords[2].Trim(), System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out float cz))
            {
                pts[id] = (cx, cy, cz);
            }
        }

        // ── 2. Build PolyLoop → vertex list lookup ────────────────────────────
        var polyLoops = new Dictionary<int, List<int>>(); // loopId → [ptId, ptId, ...]
        foreach (var (id, (cls, args)) in _entities)
        {
            if (cls != "IFCPOLYLOOP") continue;
            var refs = ExtractAllEntityRefs(args);
            if (refs.Count >= 3) polyLoops[id] = refs;
        }

        // ── 3. Build Face → polyloop lookup ──────────────────────────────────
        var faces = new Dictionary<int, int>(); // faceId → outerLoopId
        foreach (var (id, (cls, args)) in _entities)
        {
            if (cls != "IFCFACE") continue;
            // IFCFACE has IFCFACEOUTERBOUND children
            foreach (var boundId in ExtractAllEntityRefs(args))
            {
                if (!_entities.TryGetValue(boundId, out var bound)) continue;
                if (bound.ClassName != "IFCFACEOUTERBOUND") continue;
                var loopRefs = ExtractAllEntityRefs(bound.RawArgs);
                if (loopRefs.Count > 0) { faces[id] = loopRefs[0]; break; }
            }
        }

        // ── 4. Build ClosedShell → face list lookup ───────────────────────────
        var shells = new Dictionary<int, List<int>>(); // shellId → [faceId, ...]
        foreach (var (id, (cls, args)) in _entities)
        {
            if (cls != "IFCCLOSEDSHELL") continue;
            shells[id] = ExtractAllEntityRefs(args);
        }

        // ── 5. Build FacetedBrep → mesh ───────────────────────────────────────
        var brepMeshes = new Dictionary<int, (List<float> Verts, List<int> Idx)>();
        foreach (var (id, (cls, args)) in _entities)
        {
            if (cls != "IFCFACETEDBREP") continue;
            var refs = ExtractAllEntityRefs(args);
            if (refs.Count == 0) continue;
            var shellId = refs[0];
            if (!shells.TryGetValue(shellId, out var faceList)) continue;

            var verts = new List<float>();
            var indices = new List<int>();

            foreach (var faceId in faceList)
            {
                if (!faces.TryGetValue(faceId, out int loopId)) continue;
                if (!polyLoops.TryGetValue(loopId, out var ptIds)) continue;
                if (ptIds.Count < 3) continue;

                // Fan triangulation - convert polygon to triangle fan from first vertex
                var loopVerts = new List<(float X, float Y, float Z)>();
                foreach (var ptId in ptIds)
                {
                    if (pts.TryGetValue(ptId, out var p)) loopVerts.Add(p);
                }
                if (loopVerts.Count < 3) continue;

                int baseIdx = verts.Count / 3;
                foreach (var v in loopVerts)
                {
                    verts.Add(v.X); verts.Add(v.Y); verts.Add(v.Z);
                }
                for (int k = 1; k < loopVerts.Count - 1; k++)
                {
                    indices.Add(baseIdx);
                    indices.Add(baseIdx + k);
                    indices.Add(baseIdx + k + 1);
                }
            }

            if (indices.Count > 0)
                brepMeshes[id] = (verts, indices);
        }

        // ── 5b. Build ExtrudedAreaSolid → box meshes ─────────────────────────
        // IFCEXTRUDEDAREASOLID(#SweptArea, #Position, #ExtrudedDirection, Depth)
        // For IFCRECTANGLEPROFILEDEF: generate a rectangular prism mesh.
        var extrudedMeshes = new Dictionary<int, (List<float> Verts, List<int> Idx)>();
        foreach (var (id, (cls, args)) in _entities)
        {
            if (cls != "IFCEXTRUDEDAREASOLID") continue;
            var tokens = TokeniseArgs(args);
            if (tokens.Count < 4) continue;

            // Token 0: SweptArea (profile), Token 3: Depth
            if (!int.TryParse(tokens[0].Trim().TrimStart('#'), out int profileId)) continue;
            if (!double.TryParse(tokens[3].Trim(),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out double depth)) continue;
            if (depth <= 0) continue;

            // Check if profile is a rectangle
            if (!_entities.TryGetValue(profileId, out var profEnt)) continue;
            if (profEnt.ClassName != "IFCRECTANGLEPROFILEDEF") continue;

            var profTokens = TokeniseArgs(profEnt.RawArgs);
            if (profTokens.Count < 5) continue;
            if (!double.TryParse(profTokens[3].Trim(),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out double xDim)) continue;
            if (!double.TryParse(profTokens[4].Trim(),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out double yDim)) continue;

            // Generate a box mesh: xDim × yDim × depth centred at origin
            float hx = (float)(xDim / 2), hy = (float)(yDim / 2), hz = (float)depth;
            var (verts, indices) = BuildBoxMesh(hx, hy, hz);
            extrudedMeshes[id] = (verts, indices);
        }

        // ── 5c. Resolve IfcMappedItem → RepresentationMap → reuse mesh ────────
        // IFCMAPPEDITEM(#MappingSource, #MappingTarget)
        // IFCREPRESENTATIONMAP(#Origin, #MappedRepresentation)
        // This allows hundreds of instances to share one mesh definition.
        var mappedItemMeshes = new Dictionary<int, (List<float> Verts, List<int> Idx)>();
        foreach (var (id, (cls, args)) in _entities)
        {
            if (cls != "IFCMAPPEDITEM") continue;
            var tokens = TokeniseArgs(args);
            if (tokens.Count < 1) continue;

            if (!int.TryParse(tokens[0].Trim().TrimStart('#'), out int mapSourceId)) continue;
            if (!_entities.TryGetValue(mapSourceId, out var mapEnt)) continue;
            if (mapEnt.ClassName != "IFCREPRESENTATIONMAP") continue;

            // RepresentationMap token 1 is the mapped representation
            var mapTokens = TokeniseArgs(mapEnt.RawArgs);
            if (mapTokens.Count < 2) continue;
            if (!int.TryParse(mapTokens[1].Trim().TrimStart('#'), out int repId)) continue;
            if (!_entities.TryGetValue(repId, out var repEnt)) continue;
            if (repEnt.ClassName != "IFCSHAPEREPRESENTATION") continue;

            // Look for FacetedBrep or ExtrudedAreaSolid inside the mapped representation
            foreach (var itemId in ExtractAllEntityRefs(repEnt.RawArgs))
            {
                if (brepMeshes.TryGetValue(itemId, out var bm))
                { mappedItemMeshes[id] = bm; break; }
                if (extrudedMeshes.TryGetValue(itemId, out var em))
                { mappedItemMeshes[id] = em; break; }
            }
        }

        // Merge all mesh sources for lookup during element assignment
        var allMeshes = new Dictionary<int, (List<float> Verts, List<int> Idx)>(brepMeshes);
        foreach (var (k, v) in extrudedMeshes) allMeshes.TryAdd(k, v);
        foreach (var (k, v) in mappedItemMeshes) allMeshes.TryAdd(k, v);

        if (allMeshes.Count == 0) return; // No geometry resolved

        // ── 6. Build LocalPlacement world-space matrix for each element ───────
        // We extract the origin and axes from IFCAXIS2PLACEMENT3D for a simplified
        // translation-only transform (ignores rotations for now, sufficient for
        // compliance colour-coded rendering where position matters more than orientation).
        var axis3d = new Dictionary<int, (float[] Origin, float[] AxisX, float[] AxisZ)>();
        foreach (var (id, (cls, args)) in _entities)
        {
            if (cls != "IFCAXIS2PLACEMENT3D") continue;
            var tokens = TokeniseArgs(args);
            float[] origin = {0,0,0}, axX = {1,0,0}, axZ = {0,0,1};
            if (tokens.Count >= 1 && tokens[0].Trim().StartsWith('#') &&
                int.TryParse(tokens[0].Trim()[1..], out int ptId) &&
                pts.TryGetValue(ptId, out var o))
                origin = new[]{ o.X, o.Y, o.Z };
            axis3d[id] = (origin, axX, axZ);
        }

        // Build placement origin from IFCLOCALPLACEMENT
        var placements = new Dictionary<int, float[]>(); // placementId → origin
        foreach (var (id, (cls, args)) in _entities)
        {
            if (cls != "IFCLOCALPLACEMENT") continue;
            var tokens = TokeniseArgs(args);
            if (tokens.Count >= 2 && tokens[1].Trim().StartsWith('#') &&
                int.TryParse(tokens[1].Trim()[1..], out int axId) &&
                axis3d.TryGetValue(axId, out var ax))
                placements[id] = ax.Origin;
        }

        // ── 7. Assign meshes to elements ──────────────────────────────────────
        var elementByStepId = result.Elements.ToDictionary(e => e.StepId);

        foreach (var (id, (cls, args)) in _entities)
        {
            if (!ElementClasses.Contains(cls)) continue;

            // Get element's placement origin (token index 5 = placement reference)
            var tokens = TokeniseArgs(args);
            float[] offset = {0, 0, 0};
            if (tokens.Count >= 6 && tokens[5].Trim().StartsWith('#') &&
                int.TryParse(tokens[5].Trim()[1..], out int plId) &&
                placements.TryGetValue(plId, out var placOrigin))
                offset = placOrigin;

            // Get shape representation (token index 6)
            if (tokens.Count < 7) continue;
            var repToken = tokens[6].Trim();
            if (!repToken.StartsWith('#')) continue;
            if (!int.TryParse(repToken[1..], out int repShapeId)) continue;
            if (!_entities.TryGetValue(repShapeId, out var repShapeEnt)) continue;
            if (repShapeEnt.ClassName != "IFCPRODUCTDEFINITIONSHAPE") continue;

            // Find FacetedBrep in shape representations
            (List<float> Verts, List<int> Idx)? meshData = null;
            foreach (var innerRepId in ExtractAllEntityRefs(repShapeEnt.RawArgs))
            {
                if (!_entities.TryGetValue(innerRepId, out var repEnt)) continue;
                if (repEnt.ClassName != "IFCSHAPEREPRESENTATION") continue;
                foreach (var itemId in ExtractAllEntityRefs(repEnt.RawArgs))
                {
                    if (allMeshes.TryGetValue(itemId, out var m)) { meshData = m; break; }
                }
                if (meshData.HasValue) break;
            }

            if (!meshData.HasValue) continue;
            if (!elementByStepId.TryGetValue(id, out var elem)) continue;

            // Apply placement offset to all vertices
            var verts  = meshData.Value.Verts;
            var vFinal = new float[verts.Count];
            for (int k = 0; k < verts.Count; k += 3)
            {
                vFinal[k]   = verts[k]   + offset[0];
                vFinal[k+1] = verts[k+1] + offset[1];
                vFinal[k+2] = verts[k+2] + offset[2];
            }

            elem.Mesh = new VERIFIQ.Core.Models.IfcMesh
            {
                Vertices = vFinal,
                Indices  = meshData.Value.Idx.ToArray()
            };

            // Also set bounding box from mesh extents
            if (elem.BoundingBox == null && vFinal.Length >= 3)
            {
                float mnX = float.MaxValue, mnY = float.MaxValue, mnZ = float.MaxValue;
                float mxX = float.MinValue, mxY = float.MinValue, mxZ = float.MinValue;
                for (int k = 0; k < vFinal.Length; k += 3)
                {
                    mnX = Math.Min(mnX, vFinal[k]);   mxX = Math.Max(mxX, vFinal[k]);
                    mnY = Math.Min(mnY, vFinal[k+1]); mxY = Math.Max(mxY, vFinal[k+1]);
                    mnZ = Math.Min(mnZ, vFinal[k+2]); mxZ = Math.Max(mxZ, vFinal[k+2]);
                }
                elem.BoundingBox = new VERIFIQ.Core.Models.BoundingBox
                    { MinX=mnX, MinY=mnY, MinZ=mnZ, MaxX=mxX, MaxY=mxY, MaxZ=mxZ };
            }
        }
    }
}
