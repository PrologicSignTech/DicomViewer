using Microsoft.AspNetCore.Mvc;
using MedView.Server.Services;

namespace MedView.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MeasurementsController : ControllerBase
{
    private readonly IAdvancedMeasurementService _measurementService;
    private readonly ILogger<MeasurementsController> _logger;

    public MeasurementsController(
        IAdvancedMeasurementService measurementService,
        ILogger<MeasurementsController> logger)
    {
        _measurementService = measurementService;
        _logger = logger;
    }

    /// <summary>
    /// Measure length between two points
    /// </summary>
    [HttpPost("length")]
    public async Task<IActionResult> MeasureLength([FromBody] LengthMeasureRequest request)
    {
        try
        {
            var result = await _measurementService.MeasureLengthAsync(
                request.FilePath,
                new Point2D(request.StartX, request.StartY),
                new Point2D(request.EndX, request.EndY),
                request.Frame
            );
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error measuring length");
            return StatusCode(500, new { message = "Error measuring length" });
        }
    }

    /// <summary>
    /// Measure angle between three points
    /// </summary>
    [HttpPost("angle")]
    public async Task<IActionResult> MeasureAngle([FromBody] AngleMeasureRequest request)
    {
        try
        {
            var result = await _measurementService.MeasureAngleAsync(
                request.FilePath,
                new Point2D(request.VertexX, request.VertexY),
                new Point2D(request.Point1X, request.Point1Y),
                new Point2D(request.Point2X, request.Point2Y),
                request.Frame
            );
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error measuring angle");
            return StatusCode(500, new { message = "Error measuring angle" });
        }
    }

    /// <summary>
    /// Measure area of polygon
    /// </summary>
    [HttpPost("area")]
    public async Task<IActionResult> MeasureArea([FromBody] AreaMeasureRequest request)
    {
        try
        {
            var polygon = request.Points.Select(p => new Point2D(p.X, p.Y)).ToList();
            var result = await _measurementService.MeasureAreaAsync(request.FilePath, polygon, request.Frame);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error measuring area");
            return StatusCode(500, new { message = "Error measuring area" });
        }
    }

    /// <summary>
    /// Calculate ellipse ROI statistics
    /// </summary>
    [HttpPost("roi/ellipse")]
    public async Task<IActionResult> CalculateEllipseRoi([FromBody] EllipseRoiRequest request)
    {
        try
        {
            var result = await _measurementService.CalculateEllipseRoiAsync(
                request.FilePath,
                new Point2D(request.CenterX, request.CenterY),
                request.RadiusX,
                request.RadiusY,
                request.Frame
            );
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating ellipse ROI");
            return StatusCode(500, new { message = "Error calculating ROI" });
        }
    }

    /// <summary>
    /// Calculate rectangle ROI statistics
    /// </summary>
    [HttpPost("roi/rectangle")]
    public async Task<IActionResult> CalculateRectangleRoi([FromBody] RectangleRoiRequest request)
    {
        try
        {
            var result = await _measurementService.CalculateRectangleRoiAsync(
                request.FilePath,
                new Point2D(request.TopLeftX, request.TopLeftY),
                new Point2D(request.BottomRightX, request.BottomRightY),
                request.Frame
            );
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating rectangle ROI");
            return StatusCode(500, new { message = "Error calculating ROI" });
        }
    }

    /// <summary>
    /// Calculate freehand ROI statistics
    /// </summary>
    [HttpPost("roi/freehand")]
    public async Task<IActionResult> CalculateFreehandRoi([FromBody] FreehandRoiRequest request)
    {
        try
        {
            var points = request.Points.Select(p => new Point2D(p.X, p.Y)).ToList();
            var result = await _measurementService.CalculateFreehandRoiAsync(request.FilePath, points, request.Frame);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating freehand ROI");
            return StatusCode(500, new { message = "Error calculating ROI" });
        }
    }

    /// <summary>
    /// Measure Hounsfield Unit at a point
    /// </summary>
    [HttpPost("hu")]
    public async Task<IActionResult> MeasureHU([FromBody] HuMeasureRequest request)
    {
        try
        {
            var result = await _measurementService.MeasureHounsfieldAsync(
                request.FilePath, request.X, request.Y, request.Frame);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error measuring HU");
            return StatusCode(500, new { message = "Error measuring HU" });
        }
    }

    /// <summary>
    /// Measure average HU in a region
    /// </summary>
    [HttpPost("hu/region")]
    public async Task<IActionResult> MeasureHURegion([FromBody] HuRegionMeasureRequest request)
    {
        try
        {
            var result = await _measurementService.MeasureHounsfieldRegionAsync(
                request.FilePath, new Point2D(request.CenterX, request.CenterY), request.Radius, request.Frame);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error measuring HU region");
            return StatusCode(500, new { message = "Error measuring HU region" });
        }
    }

    /// <summary>
    /// Calculate volume from contours
    /// </summary>
    [HttpPost("volume")]
    public async Task<IActionResult> CalculateVolume([FromBody] VolumeCalculateRequest request)
    {
        try
        {
            var contours = request.Contours.Select(c => 
                c.Select(p => new Point2D(p.X, p.Y)).ToList()
            ).ToList();
            
            var result = await _measurementService.CalculateVolumeAsync(request.FilePaths, contours);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating volume");
            return StatusCode(500, new { message = "Error calculating volume" });
        }
    }

    /// <summary>
    /// Calculate cardiac measurements (EF, volumes)
    /// </summary>
    [HttpPost("cardiac")]
    public async Task<IActionResult> CalculateCardiacMeasurements([FromBody] CardiacMeasureRequest request)
    {
        try
        {
            var edContours = request.EdContours.Select(c => 
                c.Select(p => new Point2D(p.X, p.Y)).ToList()
            ).ToList();
            
            var esContours = request.EsContours.Select(c => 
                c.Select(p => new Point2D(p.X, p.Y)).ToList()
            ).ToList();

            var result = await _measurementService.CalculateCardiacMeasurementsAsync(
                request.EdFrameFiles, request.EsFrameFiles, edContours, esContours);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating cardiac measurements");
            return StatusCode(500, new { message = "Error calculating cardiac measurements" });
        }
    }

    /// <summary>
    /// Calculate bone density
    /// </summary>
    [HttpPost("bone-density")]
    public async Task<IActionResult> CalculateBoneDensity([FromBody] BoneDensityRequest request)
    {
        try
        {
            var roiPoints = request.RoiPoints.Select(p => new Point2D(p.X, p.Y)).ToList();
            var result = await _measurementService.CalculateBoneDensityAsync(
                request.FilePath, roiPoints, request.Frame);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating bone density");
            return StatusCode(500, new { message = "Error calculating bone density" });
        }
    }

    /// <summary>
    /// Measure distances between landmarks
    /// </summary>
    [HttpPost("landmarks")]
    public async Task<IActionResult> MeasureLandmarkDistances([FromBody] LandmarkDistanceRequest request)
    {
        try
        {
            var landmarks = request.Landmarks.Select(l => 
                new LandmarkPoint(l.Name, new Point2D(l.X, l.Y))
            ).ToList();

            var result = await _measurementService.MeasureDistancesBetweenLandmarksAsync(
                request.FilePath, landmarks, request.Frame);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error measuring landmark distances");
            return StatusCode(500, new { message = "Error measuring landmarks" });
        }
    }

    /// <summary>
    /// Get profile line values
    /// </summary>
    [HttpPost("profile")]
    public async Task<IActionResult> GetProfileLine([FromBody] ProfileLineRequest request)
    {
        try
        {
            var result = await _measurementService.GetProfileLineAsync(
                request.FilePath,
                new Point2D(request.StartX, request.StartY),
                new Point2D(request.EndX, request.EndY),
                request.Frame
            );
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting profile line");
            return StatusCode(500, new { message = "Error getting profile" });
        }
    }

    /// <summary>
    /// Calculate histogram
    /// </summary>
    [HttpPost("histogram")]
    public async Task<IActionResult> CalculateHistogram([FromBody] HistogramRequest request)
    {
        try
        {
            Point2D? topLeft = request.RoiTopLeftX.HasValue && request.RoiTopLeftY.HasValue
                ? new Point2D(request.RoiTopLeftX.Value, request.RoiTopLeftY.Value) : null;
            Point2D? bottomRight = request.RoiBottomRightX.HasValue && request.RoiBottomRightY.HasValue
                ? new Point2D(request.RoiBottomRightX.Value, request.RoiBottomRightY.Value) : null;

            var result = await _measurementService.CalculateHistogramAsync(
                request.FilePath, topLeft, bottomRight, request.Frame);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating histogram");
            return StatusCode(500, new { message = "Error calculating histogram" });
        }
    }
}

// Request DTOs
public record PointDto(double X, double Y);

public record LengthMeasureRequest(string FilePath, double StartX, double StartY, double EndX, double EndY, int Frame = 0);
public record AngleMeasureRequest(string FilePath, double VertexX, double VertexY, double Point1X, double Point1Y, double Point2X, double Point2Y, int Frame = 0);
public record AreaMeasureRequest(string FilePath, List<PointDto> Points, int Frame = 0);
public record EllipseRoiRequest(string FilePath, double CenterX, double CenterY, double RadiusX, double RadiusY, int Frame = 0);
public record RectangleRoiRequest(string FilePath, double TopLeftX, double TopLeftY, double BottomRightX, double BottomRightY, int Frame = 0);
public record FreehandRoiRequest(string FilePath, List<PointDto> Points, int Frame = 0);
public record HuMeasureRequest(string FilePath, int X, int Y, int Frame = 0);
public record HuRegionMeasureRequest(string FilePath, double CenterX, double CenterY, int Radius, int Frame = 0);
public record VolumeCalculateRequest(List<string> FilePaths, List<List<PointDto>> Contours);
public record CardiacMeasureRequest(List<string> EdFrameFiles, List<string> EsFrameFiles, List<List<PointDto>> EdContours, List<List<PointDto>> EsContours);
public record BoneDensityRequest(string FilePath, List<PointDto> RoiPoints, int Frame = 0);
public record LandmarkDto(string Name, double X, double Y);
public record LandmarkDistanceRequest(string FilePath, List<LandmarkDto> Landmarks, int Frame = 0);
public record ProfileLineRequest(string FilePath, double StartX, double StartY, double EndX, double EndY, int Frame = 0);
public record HistogramRequest(string FilePath, double? RoiTopLeftX, double? RoiTopLeftY, double? RoiBottomRightX, double? RoiBottomRightY, int Frame = 0);
