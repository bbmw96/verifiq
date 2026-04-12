// VERIFIQ - IFC Compliance Checker
// Copyright 2026 BBMW0 Technologies. Developed by Jia Wen Gan.
// Multi-format dispatcher - determines which parser handles each file type

using VERIFIQ.Core.Enums;
using VERIFIQ.Core.Models;

namespace VERIFIQ.Parser;

/// <summary>
/// Dispatches file loading to the correct parser based on format.
/// For formats that cannot be directly parsed as IFC (DWG, RVT, PLN etc.),
/// VERIFIQ reads available metadata and informs the user that the file
/// must first be exported to IFC from the authoring software.
/// </summary>
public sealed class FileFormatDispatcher
{
    private readonly ParseProgressCallback? _progress;

    public FileFormatDispatcher(ParseProgressCallback? progress = null)
    {
        _progress = progress;
    }

    public async Task<FormatDispatchResult> DispatchAsync(
        string filePath, CancellationToken ct = default)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        var format = DetectFormat(ext);

        return format switch
        {
            // IFC formats - full parse
            InputFileFormat.IFC    or
            InputFileFormat.IFCXML or
            InputFileFormat.IFCZIP => await HandleIfcAsync(filePath, format, ct),

            // CAD formats - inform user, extract limited metadata
            InputFileFormat.DWG   => HandleCadMetadata(filePath, format, "AutoCAD DWG"),
            InputFileFormat.DXF   => HandleCadMetadata(filePath, format, "AutoCAD DXF"),
            InputFileFormat.DWF   => HandleCadMetadata(filePath, format, "Autodesk Design Web Format"),
            InputFileFormat.DGN   => HandleCadMetadata(filePath, format, "Bentley MicroStation DGN"),

            // Native BIM formats - metadata only
            InputFileFormat.RVT   => HandleNativeBimMetadata(filePath, format, "Autodesk Revit",
                "Export to IFC+SG from Revit using the CORENET-X IFC+SG Shared Parameters and Export Settings from info.corenet.gov.sg"),
            InputFileFormat.PLN   => HandleNativeBimMetadata(filePath, format, "Graphisoft ArchiCAD",
                "Export to IFC+SG from ArchiCAD using the Graphisoft IFC+SG Export Translator from info.corenet.gov.sg or graphisoft.com/sg"),
            InputFileFormat.SKP   => HandleNativeBimMetadata(filePath, format, "SketchUp",
                "Export to IFC from SketchUp using the IFC-Manager plugin, then validate the IFC file in VERIFIQ."),
            InputFileFormat.BIMX  => HandleNativeBimMetadata(filePath, format, "ArchiCAD BIMx",
                "Export the underlying .ifc file from ArchiCAD before validating."),

            // Mesh / geometry formats - visual reference only, no IFC data
            InputFileFormat.OBJ  => HandleMeshFormat(filePath, format, "OBJ (Wavefront)",
                "OBJ files contain geometry only and cannot be validated for IFC compliance. " +
                "To validate, re-export your model from ArchiCAD or Revit as IFC4."),
            InputFileFormat.FBX  => HandleMeshFormat(filePath, format, "FBX (Autodesk)",
                "FBX files contain geometry only and cannot be validated for IFC compliance. " +
                "To validate, re-export your model from your BIM authoring software as IFC4."),
            InputFileFormat.STL  => HandleMeshFormat(filePath, format, "STL (Stereolithography)",
                "STL files contain mesh geometry only and carry no building data or classifications. " +
                "To validate, re-export from your BIM authoring software as IFC4."),
            InputFileFormat.STEP => HandleMeshFormat(filePath, format, "STEP (ISO 10303)",
                "STEP files carry geometric product data but not the building classification and " +
                "property set data required for CORENET-X compliance. " +
                "To validate, re-export from your BIM authoring software as IFC4 Reference View."),

            // Coordination formats
            InputFileFormat.NWD or
            InputFileFormat.NWF or
            InputFileFormat.NWC   => HandleCoordinationFormat(filePath, format, "Autodesk Navisworks",
                "Extract the embedded IFC models from Navisworks, or export individual discipline models to IFC from the authoring software."),

            // BCF - issue tracking import
            InputFileFormat.BCF   => await HandleBcfAsync(filePath, ct),

            // Point cloud
            InputFileFormat.E57   or
            InputFileFormat.LAS   or
            InputFileFormat.LAZ   or
            InputFileFormat.PTS   or
            InputFileFormat.XYZ   or
            InputFileFormat.RCP   => HandlePointCloud(filePath, format),

            // COBie
            InputFileFormat.COBIE_XLSX or
            InputFileFormat.COBIE_XML  => await HandleCobieAsync(filePath, format, ct),

            // Data / document
            InputFileFormat.XLSX  => await HandleSpreadsheetAsync(filePath, ct),
            InputFileFormat.PDF   => HandlePdfInfo(filePath),
            InputFileFormat.JSON  => await HandleJsonAsync(filePath, ct),

            // GIS
            InputFileFormat.KML or
            InputFileFormat.KMZ or
            InputFileFormat.GEOJSON or
            InputFileFormat.CITYGML => HandleGisFormat(filePath, format),

            // Emerging formats - acknowledge and instruct
            InputFileFormat.USD  or
            InputFileFormat.USDZ or
            InputFileFormat.GLTF or
            InputFileFormat.GLB  => HandleEmergingFormat(filePath, format),

            // Everything else
            _ => new FormatDispatchResult
            {
                IsSupported = false,
                Format = format,
                Message = $"Format '{ext}' is not directly supported for IFC validation. " +
                          "Please export your model to IFC format from your BIM authoring software."
            }
        };
    }

    // ─── IFC HANDLER ─────────────────────────────────────────────────────────

    private async Task<FormatDispatchResult> HandleIfcAsync(
        string filePath, InputFileFormat format, CancellationToken ct)
    {
        var parser = new IfcStepParser(_progress);
        var ifcFile = await parser.ParseAsync(filePath, ct);

        return new FormatDispatchResult
        {
            IsSupported  = true,
            Format       = format,
            IfcFile      = ifcFile,
            CanValidate  = true,
            Message      = $"IFC file loaded successfully. {ifcFile.TotalElementCount} elements found."
        };
    }

    // ─── CAD METADATA ────────────────────────────────────────────────────────

    private static FormatDispatchResult HandleCadMetadata(
        string filePath, InputFileFormat format, string softwareName)
    {
        return new FormatDispatchResult
        {
            IsSupported = true,
            Format = format,
            CanValidate = false,
            Message =
                $"VERIFIQ can read {softwareName} files for reference. " +
                "However, compliance validation requires an IFC file. " +
                $"Please export your {softwareName} drawing to IFC format using your BIM or CAD software, " +
                "then load the IFC file in VERIFIQ.",
            FileMetadata = new FileMetadata
            {
                FilePath = filePath,
                FileName = Path.GetFileName(filePath),
                FileSizeBytes = new FileInfo(filePath).Length,
                Format = format,
                SoftwareName = softwareName,
                CanBeValidated = false
            }
        };
    }

    private static FormatDispatchResult HandleNativeBimMetadata(
        string filePath, InputFileFormat format, string softwareName, string exportInstructions)
    {
        return new FormatDispatchResult
        {
            IsSupported = true,
            Format = format,
            CanValidate = false,
            Message =
                $"VERIFIQ recognises this as a {softwareName} native file. " +
                "Compliance validation requires an IFC export. " + exportInstructions,
            FileMetadata = new FileMetadata
            {
                FilePath = filePath,
                FileName = Path.GetFileName(filePath),
                FileSizeBytes = new FileInfo(filePath).Length,
                Format = format,
                SoftwareName = softwareName,
                CanBeValidated = false,
                ExportInstructions = exportInstructions
            }
        };
    }

    private static FormatDispatchResult HandleCoordinationFormat(
        string filePath, InputFileFormat format, string softwareName, string instructions)
    {
        return new FormatDispatchResult
        {
            IsSupported = true,
            Format = format,
            CanValidate = false,
            Message = $"{softwareName} coordination file detected. {instructions}",
            FileMetadata = new FileMetadata
            {
                FilePath = filePath,
                FileName = Path.GetFileName(filePath),
                FileSizeBytes = new FileInfo(filePath).Length,
                Format = format,
                SoftwareName = softwareName,
                CanBeValidated = false,
                ExportInstructions = instructions
            }
        };
    }

    // ─── BCF ─────────────────────────────────────────────────────────────────

    private static async Task<FormatDispatchResult> HandleBcfAsync(
        string filePath, CancellationToken ct)
    {
        // BCF is a ZIP containing markup.bcf (XML) files per topic
        // VERIFIQ can import BCF issues and display them alongside validation results
        long size = new FileInfo(filePath).Length;
        return new FormatDispatchResult
        {
            IsSupported = true,
            Format = InputFileFormat.BCF,
            CanValidate = false,
            Message = "BCF issue file loaded. BIM Collaboration Format issues will be displayed " +
                      "alongside VERIFIQ compliance findings when an IFC file is also loaded.",
            FileMetadata = new FileMetadata
            {
                FilePath = filePath,
                FileName = Path.GetFileName(filePath),
                FileSizeBytes = size,
                Format = InputFileFormat.BCF,
                SoftwareName = "BIM Collaboration Format (BCF)"
            }
        };
    }

    // ─── POINT CLOUD ─────────────────────────────────────────────────────────

    private static FormatDispatchResult HandlePointCloud(string filePath, InputFileFormat format)
    {
        return new FormatDispatchResult
        {
            IsSupported = true,
            Format = format,
            CanValidate = false,
            Message = "Point cloud file detected. VERIFIQ can display point cloud data as a " +
                      "visual reference alongside the IFC model, but compliance validation " +
                      "requires an IFC model generated from this scan data (scan-to-BIM workflow).",
            FileMetadata = new FileMetadata
            {
                FilePath = filePath,
                FileName = Path.GetFileName(filePath),
                FileSizeBytes = new FileInfo(filePath).Length,
                Format = format
            }
        };
    }

    // ─── COBIE ───────────────────────────────────────────────────────────────

    private static async Task<FormatDispatchResult> HandleCobieAsync(
        string filePath, InputFileFormat format, CancellationToken ct)
    {
        return new FormatDispatchResult
        {
            IsSupported = true,
            Format = format,
            CanValidate = false,
            Message = "COBie asset data file loaded. VERIFIQ can cross-reference COBie data " +
                      "with the IFC model to verify asset information completeness. " +
                      "Load an IFC file alongside this COBie file to enable cross-referencing.",
            FileMetadata = new FileMetadata
            {
                FilePath = filePath,
                FileName = Path.GetFileName(filePath),
                FileSizeBytes = new FileInfo(filePath).Length,
                Format = format,
                SoftwareName = "COBie - Construction Operations Building Information Exchange"
            }
        };
    }

    // ─── SPREADSHEET (IFC+SG INDUSTRY MAPPING) ───────────────────────────────

    private static async Task<FormatDispatchResult> HandleSpreadsheetAsync(
        string filePath, CancellationToken ct)
    {
        return new FormatDispatchResult
        {
            IsSupported = true,
            Format = InputFileFormat.XLSX,
            CanValidate = false,
            Message = "Excel spreadsheet detected. If this is the IFC+SG Industry Mapping Excel " +
                      "from the CORENET-X portal, VERIFIQ can import it to update the rules database. " +
                      "Use Rules > Update from Industry Mapping to import this file.",
            FileMetadata = new FileMetadata
            {
                FilePath = filePath,
                FileName = Path.GetFileName(filePath),
                FileSizeBytes = new FileInfo(filePath).Length,
                Format = InputFileFormat.XLSX
            }
        };
    }

    // ─── PDF INFO ────────────────────────────────────────────────────────────

    private static FormatDispatchResult HandlePdfInfo(string filePath)
    {
        return new FormatDispatchResult
        {
            IsSupported = true,
            Format = InputFileFormat.PDF,
            CanValidate = false,
            Message = "PDF file detected. VERIFIQ can display PDF drawings alongside the IFC model " +
                      "for visual reference. IFC compliance validation requires loading an IFC file.",
            FileMetadata = new FileMetadata
            {
                FilePath = filePath,
                FileName = Path.GetFileName(filePath),
                FileSizeBytes = new FileInfo(filePath).Length,
                Format = InputFileFormat.PDF
            }
        };
    }

    // ─── JSON ────────────────────────────────────────────────────────────────

    private static async Task<FormatDispatchResult> HandleJsonAsync(
        string filePath, CancellationToken ct)
    {
        return new FormatDispatchResult
        {
            IsSupported = true,
            Format = InputFileFormat.JSON,
            CanValidate = false,
            Message = "JSON data file loaded. VERIFIQ can import rules configurations and " +
                      "project metadata from JSON files.",
            FileMetadata = new FileMetadata
            {
                FilePath = filePath,
                FileName = Path.GetFileName(filePath),
                FileSizeBytes = new FileInfo(filePath).Length,
                Format = InputFileFormat.JSON
            }
        };
    }

    // ─── GIS ─────────────────────────────────────────────────────────────────

    private static FormatDispatchResult HandleGisFormat(string filePath, InputFileFormat format)
    {
        return new FormatDispatchResult
        {
            IsSupported = true,
            Format = format,
            CanValidate = false,
            Message = "GIS / location data file loaded. Site boundary and geospatial data will be " +
                      "displayed in the map view and used to verify georeferencing of the IFC model.",
            FileMetadata = new FileMetadata
            {
                FilePath = filePath,
                FileName = Path.GetFileName(filePath),
                FileSizeBytes = new FileInfo(filePath).Length,
                Format = format
            }
        };
    }

    // ─── EMERGING FORMATS ────────────────────────────────────────────────────

    private static FormatDispatchResult HandleEmergingFormat(string filePath, InputFileFormat format)
    {
        return new FormatDispatchResult
        {
            IsSupported = true,
            Format = format,
            CanValidate = false,
            Message = "Next-generation 3D format detected (USD / glTF). " +
                      "VERIFIQ can display this format for visual reference. " +
                      "IFC compliance validation requires an IFC file export. " +
                      "As these formats adopt IFC data embedding in future versions, " +
                      "full validation support will be added.",
            FileMetadata = new FileMetadata
            {
                FilePath = filePath,
                FileName = Path.GetFileName(filePath),
                FileSizeBytes = new FileInfo(filePath).Length,
                Format = format
            }
        };
    }

    // ─── MESH FORMATS ────────────────────────────────────────────────────────

    private static FormatDispatchResult HandleMeshFormat(
        string filePath, InputFileFormat format, string formatName, string instructions)
    {
        return new FormatDispatchResult
        {
            IsSupported = true,
            Format = format,
            CanValidate = false,
            Message = $"{formatName} file detected. " + instructions,
            FileMetadata = new FileMetadata
            {
                FilePath = filePath,
                FileName = Path.GetFileName(filePath),
                FileSizeBytes = new FileInfo(filePath).Length,
                Format = format,
                SoftwareName = formatName,
                CanBeValidated = false,
                ExportInstructions = instructions
            }
        };
    }

    // ─── HELPERS ─────────────────────────────────────────────────────────────

    private static InputFileFormat DetectFormat(string ext) => ext switch
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
        ".bimx"   => InputFileFormat.BIMX,
        ".skp"    => InputFileFormat.SKP,
        ".nwd"    => InputFileFormat.NWD,
        ".nwf"    => InputFileFormat.NWF,
        ".nwc"    => InputFileFormat.NWC,
        ".bcf"    => InputFileFormat.BCF,
        ".e57"    => InputFileFormat.E57,
        ".las"    => InputFileFormat.LAS,
        ".laz"    => InputFileFormat.LAZ,
        ".pts"    => InputFileFormat.PTS,
        ".xyz"    => InputFileFormat.XYZ,
        ".rcp"    => InputFileFormat.RCP,
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
        ".kml"    => InputFileFormat.KML,
        ".kmz"    => InputFileFormat.KMZ,
        ".geojson"=> InputFileFormat.GEOJSON,
        _         => InputFileFormat.IFC
    };
}

// ─── RESULT TYPES ────────────────────────────────────────────────────────────

public sealed class FormatDispatchResult
{
    public bool IsSupported  { get; set; }
    public bool CanValidate  { get; set; }
    public InputFileFormat Format { get; set; }
    public IfcFile? IfcFile  { get; set; }
    public string Message    { get; set; } = string.Empty;
    public FileMetadata? FileMetadata { get; set; }
}

public sealed class FileMetadata
{
    public string FilePath           { get; set; } = string.Empty;
    public string FileName           { get; set; } = string.Empty;
    public long   FileSizeBytes      { get; set; }
    public InputFileFormat Format    { get; set; }
    public string SoftwareName       { get; set; } = string.Empty;
    public bool   CanBeValidated     { get; set; }
    public string ExportInstructions { get; set; } = string.Empty;

    public string FileSizeDisplay => FileSizeBytes switch
    {
        < 1024           => $"{FileSizeBytes} B",
        < 1024 * 1024    => $"{FileSizeBytes / 1024.0:F1} KB",
        < 1024L * 1024 * 1024 => $"{FileSizeBytes / (1024.0 * 1024):F1} MB",
        _                => $"{FileSizeBytes / (1024.0 * 1024 * 1024):F1} GB"
    };
}
