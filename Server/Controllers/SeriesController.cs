using Microsoft.AspNetCore.Mvc;
using MedView.Server.Models.DTOs;
using MedView.Server.Services;

namespace MedView.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SeriesController : ControllerBase
{
    private readonly ISeriesService _seriesService;
    private readonly IDicomImageService _dicomImageService;
    private readonly ILogger<SeriesController> _logger;

    public SeriesController(
        ISeriesService seriesService,
        IDicomImageService dicomImageService,
        ILogger<SeriesController> logger)
    {
        _seriesService = seriesService;
        _dicomImageService = dicomImageService;
        _logger = logger;
    }

    /// <summary>
    /// Get series by ID
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<SeriesDetailDto>> GetSeriesById(int id)
    {
        var series = await _seriesService.GetSeriesByIdAsync(id);
        if (series == null)
            return NotFound(new { message = "Series not found" });
        return Ok(series);
    }

    /// <summary>
    /// Get series by Series Instance UID
    /// </summary>
    [HttpGet("uid/{seriesInstanceUid}")]
    public async Task<ActionResult<SeriesDetailDto>> GetSeriesByUid(string seriesInstanceUid)
    {
        var series = await _seriesService.GetSeriesByUidAsync(seriesInstanceUid);
        if (series == null)
            return NotFound(new { message = "Series not found" });
        return Ok(series);
    }

    /// <summary>
    /// Get instances in a series
    /// </summary>
    [HttpGet("{id:int}/instances")]
    public async Task<ActionResult<IEnumerable<InstanceDto>>> GetInstances(int id)
    {
        var instances = await _seriesService.GetInstancesAsync(id);
        return Ok(instances);
    }

    /// <summary>
    /// Get series thumbnail (first instance thumbnail)
    /// </summary>
    [HttpGet("{id:int}/thumbnail")]
    public async Task<IActionResult> GetThumbnail(int id, [FromQuery] int width = 128, [FromQuery] int height = 128)
    {
        try
        {
            var filePaths = await _seriesService.GetInstanceFilePathsAsync(id);
            var firstFile = filePaths.FirstOrDefault();
            
            if (firstFile == null)
                return NotFound(new { message = "No instances in series" });

            var thumbnail = await _dicomImageService.RenderThumbnailAsync(firstFile, width, height);
            return File(thumbnail, "image/png");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating series thumbnail");
            return StatusCode(500, new { message = "Error generating thumbnail" });
        }
    }

    /// <summary>
    /// Get MPR (Multi-Planar Reconstruction) slice
    /// </summary>
    [HttpGet("{id:int}/mpr")]
    public async Task<IActionResult> GetMprSlice(
        int id,
        [FromQuery] string plane = "axial",
        [FromQuery] int sliceIndex = 0,
        [FromQuery] double? windowCenter = null,
        [FromQuery] double? windowWidth = null)
    {
        try
        {
            var filePaths = await _seriesService.GetInstanceFilePathsAsync(id);
            if (!filePaths.Any())
                return NotFound(new { message = "No instances in series" });

            var imageBytes = await _dicomImageService.RenderMprSliceAsync(
                filePaths, plane, sliceIndex, windowCenter, windowWidth);
            
            return File(imageBytes, "image/png");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating MPR slice");
            return StatusCode(500, new { message = "Error generating MPR slice" });
        }
    }

    /// <summary>
    /// Get series metadata for MPR (slice count, dimensions)
    /// </summary>
    [HttpGet("{id:int}/mpr-info")]
    public async Task<IActionResult> GetMprInfo(int id)
    {
        var series = await _seriesService.GetSeriesByIdAsync(id);
        if (series == null)
            return NotFound(new { message = "Series not found" });

        return Ok(new
        {
            series.Rows,
            series.Columns,
            SliceCount = series.NumberOfInstances,
            series.SliceThickness
        });
    }
}
