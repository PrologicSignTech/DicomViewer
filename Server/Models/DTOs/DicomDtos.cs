namespace MedView.Server.Models.DTOs;

// Study DTOs
public record StudyDto(
    int Id,
    string StudyInstanceUid,
    string? StudyId,
    string? StudyDescription,
    DateTime? StudyDate,
    string? AccessionNumber,
    string? PatientId,
    string? PatientName,
    DateTime? PatientBirthDate,
    string? PatientSex,
    string? PatientAge,
    string? InstitutionName,
    int NumberOfSeries,
    int NumberOfInstances,
    DateTime CreatedAt,
    string? EncryptedStudyUid = null
);

public record StudyDetailDto(
    int Id,
    string StudyInstanceUid,
    string? StudyId,
    string? StudyDescription,
    DateTime? StudyDate,
    string? AccessionNumber,
    string? PatientId,
    string? PatientName,
    DateTime? PatientBirthDate,
    string? PatientSex,
    string? PatientAge,
    string? InstitutionName,
    string? ReferringPhysicianName,
    int NumberOfSeries,
    int NumberOfInstances,
    IEnumerable<SeriesDto> Series,
    string? EncryptedStudyUid = null
);

// Series DTOs
public record SeriesDto(
    int Id,
    string SeriesInstanceUid,
    string? SeriesNumber,
    string? SeriesDescription,
    string? Modality,
    DateTime? SeriesDate,
    string? BodyPartExamined,
    int? Rows,
    int? Columns,
    int NumberOfInstances,
    string? ThumbnailUrl
);

public record SeriesDetailDto(
    int Id,
    string SeriesInstanceUid,
    string? SeriesNumber,
    string? SeriesDescription,
    string? Modality,
    DateTime? SeriesDate,
    string? BodyPartExamined,
    string? ProtocolName,
    int? Rows,
    int? Columns,
    double? SliceThickness,
    int NumberOfInstances,
    IEnumerable<InstanceDto> Instances
);

// Instance DTOs
public record InstanceDto(
    int Id,
    string SopInstanceUid,
    string? SopClassUid,
    int? InstanceNumber,
    int? Rows,
    int? Columns,
    double? WindowCenter,
    double? WindowWidth,
    double? RescaleIntercept,
    double? RescaleSlope,
    int NumberOfFrames,
    string? ThumbnailUrl,
    string? ImageUrl
);

public record InstanceDetailDto(
    int Id,
    string SopInstanceUid,
    string? SopClassUid,
    int? InstanceNumber,
    int? Rows,
    int? Columns,
    int? BitsAllocated,
    int? BitsStored,
    string? PhotometricInterpretation,
    double? WindowCenter,
    double? WindowWidth,
    double? RescaleIntercept,
    double? RescaleSlope,
    string? PixelSpacing,
    string? ImagePositionPatient,
    string? ImageOrientationPatient,
    double? SliceLocation,
    int NumberOfFrames,
    double? FrameTime,
    string? TransferSyntaxUid,
    IEnumerable<AnnotationDto> Annotations,
    IEnumerable<MeasurementDto> Measurements
);

// Annotation DTOs
public record AnnotationDto(
    int Id,
    string Type,
    string? Text,
    string? Color,
    double? FontSize,
    bool IsVisible,
    string PositionData,
    int? FrameNumber,
    DateTime CreatedAt,
    string? CreatedBy
);

public record CreateAnnotationDto(
    string Type,
    string? Text,
    string? Color,
    double? FontSize,
    string PositionData,
    int? FrameNumber
);

// Measurement DTOs
public record MeasurementDto(
    int Id,
    string Type,
    double? Value,
    string? Unit,
    string? Label,
    string? Color,
    bool IsVisible,
    double? Mean,
    double? StdDev,
    double? Min,
    double? Max,
    double? Area,
    string PositionData,
    int? FrameNumber,
    DateTime CreatedAt,
    string? CreatedBy
);

public record CreateMeasurementDto(
    string Type,
    double? Value,
    string? Unit,
    string? Label,
    string? Color,
    double? Mean,
    double? StdDev,
    double? Min,
    double? Max,
    double? Area,
    string PositionData,
    int? FrameNumber
);

// Upload Response
public record UploadResultDto(
    bool Success,
    string? Message,
    int StudiesProcessed,
    int SeriesProcessed,
    int InstancesProcessed,
    IEnumerable<string>? Errors,
    IEnumerable<StudyDto>? Studies
);

// DICOM Tags
public record DicomTagDto(
    string Tag,
    string Name,
    string VR,
    string Value,
    int? Length
);

public record DicomTagsResponseDto(
    string SopInstanceUid,
    IEnumerable<DicomTagDto> Tags
);

// Window Presets
public record WindowPresetDto(
    string Name,
    double WindowCenter,
    double WindowWidth
);

// Image Request/Response
public record ImageRequestDto(
    int? Frame,
    double? WindowCenter,
    double? WindowWidth,
    bool? Invert,
    int? Quality,
    string? Format // png, jpeg, webp
);

// Hanging Protocol DTOs
public record HangingProtocolDto(
    int Id,
    string Name,
    string? Description,
    string? Modality,
    string? BodyPart,
    string LayoutConfig,
    int Priority,
    bool IsDefault
);

// Search/Query DTOs
public record StudySearchDto(
    string? PatientId,
    string? PatientName,
    string? StudyDescription,
    string? AccessionNumber,
    DateTime? StudyDateFrom,
    DateTime? StudyDateTo,
    string? Modality,
    int Page = 1,
    int PageSize = 20
);

public record PagedResultDto<T>(
    IEnumerable<T> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages
);

// Export DTOs
public record ExportRequestDto(
    int InstanceId,
    int? Frame,
    string Format, // png, jpeg, dicom
    bool IncludeAnnotations,
    bool IncludeMeasurements,
    int? Quality
);

// MPR Request
public record MprRequestDto(
    string SeriesInstanceUid,
    string Plane, // axial, sagittal, coronal
    int SliceIndex,
    double? WindowCenter,
    double? WindowWidth
);

// Volume Rendering
public record VolumeRenderRequestDto(
    string SeriesInstanceUid,
    string RenderType, // mip, minip, average, volume
    double? WindowCenter,
    double? WindowWidth,
    double? RotationX,
    double? RotationY,
    double? RotationZ
);
