using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MedView.Server.Data;
using MedView.Server.Models.DTOs;
using MedView.Server.Services;

namespace MedView.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InstancesController : ControllerBase
{
    private readonly ISeriesService _seriesService;
    private readonly IDicomImageService _dicomImageService;
    private readonly IMeasurementService _measurementService;
    private readonly IAnnotationService _annotationService;
    private readonly DicomDbContext _context;
    private readonly ILogger<InstancesController> _logger;

    public InstancesController(
        ISeriesService seriesService,
        IDicomImageService dicomImageService,
        IMeasurementService measurementService,
        IAnnotationService annotationService,
        DicomDbContext context,
        ILogger<InstancesController> logger)
    {
        _seriesService = seriesService;
        _dicomImageService = dicomImageService;
        _measurementService = measurementService;
        _annotationService = annotationService;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get instance by ID
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<InstanceDetailDto>> GetInstanceById(int id)
    {
        var instance = await _seriesService.GetInstanceByIdAsync(id);
        if (instance == null)
            return NotFound(new { message = "Instance not found" });
        return Ok(instance);
    }

    /// <summary>
    /// Get instance by SOP Instance UID
    /// </summary>
    [HttpGet("uid/{sopInstanceUid}")]
    public async Task<ActionResult<InstanceDetailDto>> GetInstanceByUid(string sopInstanceUid)
    {
        var instance = await _seriesService.GetInstanceByUidAsync(sopInstanceUid);
        if (instance == null)
            return NotFound(new { message = "Instance not found" });
        return Ok(instance);
    }

    /// <summary>
    /// Get rendered image
    /// </summary>
    [HttpGet("{id:int}/image")]
    public async Task<IActionResult> GetImage(
        int id,
        [FromQuery] int frame = 0,
        [FromQuery] double? windowCenter = null,
        [FromQuery] double? windowWidth = null,
        [FromQuery] bool invert = false)
    {
        try
        {
            var instance = await _context.Instances.FindAsync(id);
            if (instance?.FilePath == null)
                return NotFound(new { message = "Instance not found" });

            var imageBytes = await _dicomImageService.RenderImageAsync(
                instance.FilePath, frame, windowCenter, windowWidth, invert);
            
            return File(imageBytes, "image/png");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rendering image");
            return StatusCode(500, new { message = "Error rendering image" });
        }
    }

    /// <summary>
    /// Get thumbnail
    /// </summary>
    [HttpGet("{id:int}/thumbnail")]
    public async Task<IActionResult> GetThumbnail(
        int id,
        [FromQuery] int width = 128,
        [FromQuery] int height = 128)
    {
        try
        {
            var instance = await _context.Instances.FindAsync(id);
            if (instance?.FilePath == null)
                return NotFound(new { message = "Instance not found" });

            var thumbnail = await _dicomImageService.RenderThumbnailAsync(
                instance.FilePath, width, height);
            
            return File(thumbnail, "image/png");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating thumbnail");
            return StatusCode(500, new { message = "Error generating thumbnail" });
        }
    }

    /// <summary>
    /// Get DICOM file
    /// </summary>
    [HttpGet("{id:int}/dicom")]
    public async Task<IActionResult> GetDicomFile(int id)
    {
        var instance = await _context.Instances.FindAsync(id);
        if (instance?.FilePath == null || !System.IO.File.Exists(instance.FilePath))
            return NotFound(new { message = "Instance not found" });

        var fileBytes = await System.IO.File.ReadAllBytesAsync(instance.FilePath);
        return File(fileBytes, "application/dicom", $"{instance.SopInstanceUid}.dcm");
    }

    /// <summary>
    /// Get all DICOM tags
    /// </summary>
    [HttpGet("{id:int}/tags")]
    public async Task<ActionResult<DicomTagsResponseDto>> GetDicomTags(int id)
    {
        try
        {
            var instance = await _context.Instances.FindAsync(id);
            if (instance?.FilePath == null)
                return NotFound(new { message = "Instance not found" });

            var tags = await _dicomImageService.GetAllTagsAsync(instance.FilePath);
            return Ok(new DicomTagsResponseDto(instance.SopInstanceUid, tags));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading DICOM tags");
            return StatusCode(500, new { message = "Error reading DICOM tags" });
        }
    }

    /// <summary>
    /// Get pixel value at coordinates
    /// </summary>
    [HttpGet("{id:int}/pixel-value")]
    public async Task<IActionResult> GetPixelValue(
        int id,
        [FromQuery] int x,
        [FromQuery] int y,
        [FromQuery] int frame = 0)
    {
        try
        {
            var instance = await _context.Instances.FindAsync(id);
            if (instance?.FilePath == null)
                return NotFound(new { message = "Instance not found" });

            var pixelValues = await _dicomImageService.GetPixelValuesAsync(instance.FilePath, frame);
            
            if (instance.Columns == null || instance.Rows == null)
                return BadRequest(new { message = "Image dimensions unknown" });

            if (x < 0 || x >= instance.Columns || y < 0 || y >= instance.Rows)
                return BadRequest(new { message = "Coordinates out of bounds" });

            var index = y * instance.Columns.Value + x;
            var value = pixelValues[index];

            return Ok(new
            {
                X = x,
                Y = y,
                Value = value,
                Unit = "HU" // Hounsfield Units for CT
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pixel value");
            return StatusCode(500, new { message = "Error getting pixel value" });
        }
    }

    #region Measurements

    /// <summary>
    /// Get measurements for an instance
    /// </summary>
    [HttpGet("{id:int}/measurements")]
    public async Task<ActionResult<IEnumerable<MeasurementDto>>> GetMeasurements(int id)
    {
        var measurements = await _measurementService.GetByInstanceIdAsync(id);
        return Ok(measurements);
    }

    /// <summary>
    /// Create a measurement
    /// </summary>
    [HttpPost("{id:int}/measurements")]
    public async Task<ActionResult<MeasurementDto>> CreateMeasurement(int id, [FromBody] CreateMeasurementDto dto)
    {
        var instance = await _context.Instances.FindAsync(id);
        if (instance == null)
            return NotFound(new { message = "Instance not found" });

        var measurement = await _measurementService.CreateAsync(id, dto);
        return CreatedAtAction(nameof(GetMeasurements), new { id }, measurement);
    }

    /// <summary>
    /// Update a measurement
    /// </summary>
    [HttpPut("measurements/{measurementId:int}")]
    public async Task<ActionResult<MeasurementDto>> UpdateMeasurement(int measurementId, [FromBody] CreateMeasurementDto dto)
    {
        var measurement = await _measurementService.UpdateAsync(measurementId, dto);
        if (measurement == null)
            return NotFound(new { message = "Measurement not found" });
        return Ok(measurement);
    }

    /// <summary>
    /// Delete a measurement
    /// </summary>
    [HttpDelete("measurements/{measurementId:int}")]
    public async Task<IActionResult> DeleteMeasurement(int measurementId)
    {
        var result = await _measurementService.DeleteAsync(measurementId);
        if (!result)
            return NotFound(new { message = "Measurement not found" });
        return NoContent();
    }

    #endregion

    #region Annotations

    /// <summary>
    /// Get annotations for an instance
    /// </summary>
    [HttpGet("{id:int}/annotations")]
    public async Task<ActionResult<IEnumerable<AnnotationDto>>> GetAnnotations(int id)
    {
        var annotations = await _annotationService.GetByInstanceIdAsync(id);
        return Ok(annotations);
    }

    /// <summary>
    /// Create an annotation
    /// </summary>
    [HttpPost("{id:int}/annotations")]
    public async Task<ActionResult<AnnotationDto>> CreateAnnotation(int id, [FromBody] CreateAnnotationDto dto)
    {
        var instance = await _context.Instances.FindAsync(id);
        if (instance == null)
            return NotFound(new { message = "Instance not found" });

        var annotation = await _annotationService.CreateAsync(id, dto);
        return CreatedAtAction(nameof(GetAnnotations), new { id }, annotation);
    }

    /// <summary>
    /// Update an annotation
    /// </summary>
    [HttpPut("annotations/{annotationId:int}")]
    public async Task<ActionResult<AnnotationDto>> UpdateAnnotation(int annotationId, [FromBody] CreateAnnotationDto dto)
    {
        var annotation = await _annotationService.UpdateAsync(annotationId, dto);
        if (annotation == null)
            return NotFound(new { message = "Annotation not found" });
        return Ok(annotation);
    }

    /// <summary>
    /// Delete an annotation
    /// </summary>
    [HttpDelete("annotations/{annotationId:int}")]
    public async Task<IActionResult> DeleteAnnotation(int annotationId)
    {
        var result = await _annotationService.DeleteAsync(annotationId);
        if (!result)
            return NotFound(new { message = "Annotation not found" });
        return NoContent();
    }

    #endregion

    /// <summary>
    /// Export image with annotations
    /// </summary>
    [HttpPost("{id:int}/export")]
    public async Task<IActionResult> ExportImage(int id, [FromBody] ExportRequestDto request)
    {
        try
        {
            var instance = await _context.Instances.FindAsync(id);
            if (instance?.FilePath == null)
                return NotFound(new { message = "Instance not found" });

            // For now, just return the rendered image
            // TODO: Overlay annotations and measurements
            var imageBytes = await _dicomImageService.RenderImageAsync(
                instance.FilePath, request.Frame ?? 0);

            var contentType = request.Format?.ToLower() switch
            {
                "jpeg" or "jpg" => "image/jpeg",
                "dicom" => "application/dicom",
                _ => "image/png"
            };

            var extension = request.Format?.ToLower() switch
            {
                "jpeg" or "jpg" => ".jpg",
                "dicom" => ".dcm",
                _ => ".png"
            };

            return File(imageBytes, contentType, $"export_{instance.SopInstanceUid}{extension}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting image");
            return StatusCode(500, new { message = "Error exporting image" });
        }
    }
}
