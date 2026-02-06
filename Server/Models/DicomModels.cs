using System.ComponentModel.DataAnnotations;

namespace MedView.Server.Models;

/// <summary>
/// Represents a DICOM Study
/// </summary>
public class Study
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public string StudyInstanceUid { get; set; } = string.Empty;
    
    public string? StudyId { get; set; }
    public string? StudyDescription { get; set; }
    public DateTime? StudyDate { get; set; }
    public string? StudyTime { get; set; }
    public string? AccessionNumber { get; set; }
    public string? ReferringPhysicianName { get; set; }
    
    // Patient Information
    public string? PatientId { get; set; }
    public string? PatientName { get; set; }
    public DateTime? PatientBirthDate { get; set; }
    public string? PatientSex { get; set; }
    public string? PatientAge { get; set; }
    
    // Institution
    public string? InstitutionName { get; set; }
    
    // Metadata
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastAccessedAt { get; set; }
    public int NumberOfSeries { get; set; }
    public int NumberOfInstances { get; set; }
    
    // Navigation
    public ICollection<Series> Series { get; set; } = new List<Series>();
}

/// <summary>
/// Represents a DICOM Series within a Study
/// </summary>
public class Series
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public string SeriesInstanceUid { get; set; } = string.Empty;
    
    public string? SeriesNumber { get; set; }
    public string? SeriesDescription { get; set; }
    public string? Modality { get; set; }
    public DateTime? SeriesDate { get; set; }
    public string? SeriesTime { get; set; }
    public string? BodyPartExamined { get; set; }
    public string? ProtocolName { get; set; }
    
    // Image Properties
    public int? Rows { get; set; }
    public int? Columns { get; set; }
    public double? SliceThickness { get; set; }
    public double? SpacingBetweenSlices { get; set; }
    public string? ImageOrientationPatient { get; set; }
    
    public int NumberOfInstances { get; set; }
    
    // Foreign Key
    public int StudyId { get; set; }
    public Study Study { get; set; } = null!;
    
    // Navigation
    public ICollection<Instance> Instances { get; set; } = new List<Instance>();
}

/// <summary>
/// Represents a DICOM Instance (Image)
/// </summary>
public class Instance
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public string SopInstanceUid { get; set; } = string.Empty;
    
    public string? SopClassUid { get; set; }
    public int? InstanceNumber { get; set; }
    public string? FilePath { get; set; }
    public long? FileSize { get; set; }
    
    // Image Properties
    public int? Rows { get; set; }
    public int? Columns { get; set; }
    public int? BitsAllocated { get; set; }
    public int? BitsStored { get; set; }
    public int? HighBit { get; set; }
    public string? PhotometricInterpretation { get; set; }
    public int? SamplesPerPixel { get; set; }
    public string? PixelRepresentation { get; set; }
    
    // Window/Level defaults
    public double? WindowCenter { get; set; }
    public double? WindowWidth { get; set; }
    public double? RescaleIntercept { get; set; }
    public double? RescaleSlope { get; set; }
    
    // Position
    public string? ImagePositionPatient { get; set; }
    public string? ImageOrientationPatient { get; set; }
    public string? PixelSpacing { get; set; }
    public double? SliceLocation { get; set; }
    
    // Multi-frame support
    public int NumberOfFrames { get; set; } = 1;
    public double? FrameTime { get; set; }
    
    public string? TransferSyntaxUid { get; set; }
    
    // Foreign Key
    public int SeriesId { get; set; }
    public Series Series { get; set; } = null!;
    
    // Navigation
    public ICollection<Annotation> Annotations { get; set; } = new List<Annotation>();
    public ICollection<Measurement> Measurements { get; set; } = new List<Measurement>();
}

/// <summary>
/// Represents an annotation on a DICOM image
/// </summary>
public class Annotation
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public string Type { get; set; } = string.Empty; // Text, Arrow, Freehand, Rectangle, Ellipse
    
    public string? Text { get; set; }
    public string? Color { get; set; }
    public double? FontSize { get; set; }
    public bool IsVisible { get; set; } = true;
    
    // Position data stored as JSON
    public string PositionData { get; set; } = "{}";
    
    public int? FrameNumber { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
    
    // Foreign Key
    public int InstanceId { get; set; }
    public Instance Instance { get; set; } = null!;
}

/// <summary>
/// Represents a measurement on a DICOM image
/// </summary>
public class Measurement
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public string Type { get; set; } = string.Empty; // Length, Angle, Area, EllipseRoi, RectangleRoi, Probe
    
    public double? Value { get; set; }
    public string? Unit { get; set; } // mm, cm, degrees, mm², cm², HU
    public string? Label { get; set; }
    public string? Color { get; set; }
    public bool IsVisible { get; set; } = true;
    
    // For ROI measurements
    public double? Mean { get; set; }
    public double? StdDev { get; set; }
    public double? Min { get; set; }
    public double? Max { get; set; }
    public double? Area { get; set; }
    
    // Position data stored as JSON
    public string PositionData { get; set; } = "{}";
    
    public int? FrameNumber { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
    
    // Foreign Key
    public int InstanceId { get; set; }
    public Instance Instance { get; set; } = null!;
}

/// <summary>
/// Hanging Protocol for automated display layouts
/// </summary>
public class HangingProtocol
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public string Name { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    public string? Modality { get; set; }
    public string? BodyPart { get; set; }
    
    // Layout configuration as JSON
    public string LayoutConfig { get; set; } = "{}";
    
    public int Priority { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
}

/// <summary>
/// User preferences and settings
/// </summary>
public class UserSettings
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public string UserId { get; set; } = string.Empty;
    
    // Display preferences
    public string? DefaultLayout { get; set; }
    public string? DefaultWindowPresets { get; set; }
    public bool ShowAnnotations { get; set; } = true;
    public bool ShowMeasurements { get; set; } = true;
    public bool ShowOverlay { get; set; } = true;
    
    // Tool preferences
    public string? DefaultTool { get; set; }
    public string? MeasurementColor { get; set; }
    public string? AnnotationColor { get; set; }
    
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}
