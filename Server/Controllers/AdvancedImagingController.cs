using Microsoft.AspNetCore.Mvc;
using MedView.Server.Services;
using System.Numerics;

namespace MedView.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdvancedImagingController : ControllerBase
{
    private readonly IAdvancedImagingService _advancedImagingService;
    private readonly ISeriesService _seriesService;
    private readonly ILogger<AdvancedImagingController> _logger;

    public AdvancedImagingController(
        IAdvancedImagingService advancedImagingService,
        ISeriesService seriesService,
        ILogger<AdvancedImagingController> logger)
    {
        _advancedImagingService = advancedImagingService;
        _seriesService = seriesService;
        _logger = logger;
    }

    /// <summary>
    /// Get MPR volume information
    /// </summary>
    [HttpGet("series/{seriesId}/mpr-info")]
    public async Task<IActionResult> GetMprVolumeInfo(int seriesId)
    {
        try
        {
            var filePaths = await _seriesService.GetInstanceFilePathsAsync(seriesId);
            if (!filePaths.Any())
                return NotFound(new { message = "No instances in series" });

            var info = await _advancedImagingService.GetMprVolumeInfoAsync(filePaths);
            return Ok(info);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting MPR volume info");
            return StatusCode(500, new { message = "Error getting volume info" });
        }
    }

    /// <summary>
    /// Render MPR slice
    /// </summary>
    [HttpGet("series/{seriesId}/mpr")]
    public async Task<IActionResult> RenderMpr(
        int seriesId,
        [FromQuery] string plane = "axial",
        [FromQuery] int sliceIndex = 0,
        [FromQuery] double? windowCenter = null,
        [FromQuery] double? windowWidth = null)
    {
        try
        {
            var filePaths = await _seriesService.GetInstanceFilePathsAsync(seriesId);
            if (!filePaths.Any())
                return NotFound(new { message = "No instances in series" });

            var mprPlane = plane.ToLower() switch
            {
                "sagittal" => MprPlane.Sagittal,
                "coronal" => MprPlane.Coronal,
                _ => MprPlane.Axial
            };

            var imageBytes = await _advancedImagingService.RenderMprAsync(
                filePaths, mprPlane, sliceIndex, windowCenter, windowWidth);
            
            return File(imageBytes, "image/png");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rendering MPR");
            return StatusCode(500, new { message = "Error rendering MPR" });
        }
    }

    /// <summary>
    /// Render MIP/MinIP/Average projection
    /// </summary>
    [HttpGet("series/{seriesId}/projection")]
    public async Task<IActionResult> RenderProjection(
        int seriesId,
        [FromQuery] string type = "mip",
        [FromQuery] string plane = "axial",
        [FromQuery] int startSlice = 0,
        [FromQuery] int endSlice = -1,
        [FromQuery] double? windowCenter = null,
        [FromQuery] double? windowWidth = null)
    {
        try
        {
            var filePaths = await _seriesService.GetInstanceFilePathsAsync(seriesId);
            if (!filePaths.Any())
                return NotFound(new { message = "No instances in series" });

            if (endSlice < 0) endSlice = filePaths.Count() - 1;

            var mipType = type.ToLower() switch
            {
                "minip" => MipType.Minimum,
                "average" => MipType.Average,
                _ => MipType.Maximum
            };

            var mprPlane = plane.ToLower() switch
            {
                "sagittal" => MprPlane.Sagittal,
                "coronal" => MprPlane.Coronal,
                _ => MprPlane.Axial
            };

            var imageBytes = await _advancedImagingService.RenderMipAsync(
                filePaths, mipType, mprPlane, startSlice, endSlice, windowCenter, windowWidth);
            
            return File(imageBytes, "image/png");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rendering projection");
            return StatusCode(500, new { message = "Error rendering projection" });
        }
    }

    /// <summary>
    /// Render 3D volume
    /// </summary>
    [HttpPost("series/{seriesId}/volume")]
    public async Task<IActionResult> RenderVolume(int seriesId, [FromBody] VolumeRenderRequest request)
    {
        try
        {
            var filePaths = await _seriesService.GetInstanceFilePathsAsync(seriesId);
            if (!filePaths.Any())
                return NotFound(new { message = "No instances in series" });

            var renderParams = new VolumeRenderParams(
                request.RotationX,
                request.RotationY,
                request.RotationZ,
                request.WindowCenter,
                request.WindowWidth,
                request.TransferFunction,
                request.Opacity,
                request.EnableShading,
                request.OutputWidth,
                request.OutputHeight
            );

            var imageBytes = await _advancedImagingService.RenderVolumeAsync(filePaths, renderParams);
            return File(imageBytes, "image/png");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rendering volume");
            return StatusCode(500, new { message = "Error rendering volume" });
        }
    }

    /// <summary>
    /// Render Curved Planar Reformation
    /// </summary>
    [HttpPost("series/{seriesId}/cpr")]
    public async Task<IActionResult> RenderCpr(int seriesId, [FromBody] CprRequest request)
    {
        try
        {
            var filePaths = await _seriesService.GetInstanceFilePathsAsync(seriesId);
            if (!filePaths.Any())
                return NotFound(new { message = "No instances in series" });

            var centerline = request.CenterlinePath.Select(p => new Vector3((float)p.X, (float)p.Y, (float)p.Z)).ToList();

            var imageBytes = await _advancedImagingService.RenderCprAsync(
                filePaths, centerline, request.WindowCenter, request.WindowWidth);
            
            return File(imageBytes, "image/png");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rendering CPR");
            return StatusCode(500, new { message = "Error rendering CPR" });
        }
    }

    /// <summary>
    /// Render image fusion (PET-CT, etc.)
    /// </summary>
    [HttpPost("fusion")]
    public async Task<IActionResult> RenderFusion([FromBody] FusionRequest request)
    {
        try
        {
            var fusionParams = new FusionParams(
                request.BaseWindowCenter,
                request.BaseWindowWidth,
                request.OverlayWindowCenter,
                request.OverlayWindowWidth,
                Enum.Parse<FusionColorMap>(request.ColorMap, true),
                request.OverlayOpacity,
                request.EnableThreshold,
                request.ThresholdMin,
                request.ThresholdMax
            );

            var imageBytes = await _advancedImagingService.RenderFusionAsync(
                request.BaseFilePath, request.OverlayFilePath, fusionParams);
            
            return File(imageBytes, "image/png");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rendering fusion");
            return StatusCode(500, new { message = "Error rendering fusion" });
        }
    }

    /// <summary>
    /// Apply image enhancement
    /// </summary>
    [HttpPost("instances/{instanceId}/enhance")]
    public async Task<IActionResult> ApplyEnhancement(int instanceId, [FromBody] EnhancementRequest request)
    {
        try
        {
            // Get instance file path
            // For now, return error as we need to get file path from database
            var enhancementParams = new ImageEnhancementParams(
                request.Sharpen, request.SharpenAmount,
                request.Smooth, request.SmoothAmount,
                request.NoiseReduction, request.NoiseReductionStrength,
                request.EdgeEnhancement, request.EdgeEnhancementStrength,
                request.Invert,
                request.Rotation,
                request.FlipHorizontal, request.FlipVertical,
                request.Brightness, request.Contrast, request.Gamma
            );

            // Would need to get file path and call service
            return StatusCode(501, new { message = "Enhancement endpoint needs instance file path resolution" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying enhancement");
            return StatusCode(500, new { message = "Error applying enhancement" });
        }
    }

    /// <summary>
    /// Apply LUT (Lookup Table)
    /// </summary>
    [HttpGet("instances/{instanceId}/lut/{lutName}")]
    public async Task<IActionResult> ApplyLut(int instanceId, string lutName, [FromQuery] int frame = 0)
    {
        try
        {
            // Would need to get file path and call service
            return StatusCode(501, new { message = "LUT endpoint needs instance file path resolution" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying LUT");
            return StatusCode(500, new { message = "Error applying LUT" });
        }
    }

    /// <summary>
    /// Get available LUTs
    /// </summary>
    [HttpGet("luts")]
    public IActionResult GetAvailableLuts()
    {
        var luts = new[]
        {
            new { Name = "hot", Description = "Hot colormap (red-yellow)" },
            new { Name = "cool", Description = "Cool colormap (cyan-magenta)" },
            new { Name = "rainbow", Description = "Rainbow colormap" },
            new { Name = "bone", Description = "Bone visualization" },
            new { Name = "cardiac", Description = "Cardiac imaging" },
            new { Name = "pet", Description = "PET standard colormap" }
        };
        return Ok(luts);
    }

    /// <summary>
    /// Get available window presets
    /// </summary>
    [HttpGet("window-presets")]
    public IActionResult GetWindowPresets()
    {
        var presets = new[]
        {
            new { Name = "CT Abdomen", WindowCenter = 40, WindowWidth = 400 },
            new { Name = "CT Bone", WindowCenter = 500, WindowWidth = 2000 },
            new { Name = "CT Brain", WindowCenter = 40, WindowWidth = 80 },
            new { Name = "CT Chest", WindowCenter = -600, WindowWidth = 1500 },
            new { Name = "CT Lung", WindowCenter = -400, WindowWidth = 1500 },
            new { Name = "CT Liver", WindowCenter = 60, WindowWidth = 150 },
            new { Name = "CT Soft Tissue", WindowCenter = 40, WindowWidth = 350 },
            new { Name = "CT Stroke", WindowCenter = 40, WindowWidth = 40 },
            new { Name = "CT Spine", WindowCenter = 50, WindowWidth = 350 },
            new { Name = "CT Angio", WindowCenter = 300, WindowWidth = 600 },
            new { Name = "MR T1", WindowCenter = 500, WindowWidth = 1000 },
            new { Name = "MR T2", WindowCenter = 300, WindowWidth = 600 },
            new { Name = "MR FLAIR", WindowCenter = 400, WindowWidth = 800 },
            new { Name = "PET", WindowCenter = 10000, WindowWidth = 20000 }
        };
        return Ok(presets);
    }
}

// Request DTOs
public record VolumeRenderRequest(
    double RotationX = 0,
    double RotationY = 0,
    double RotationZ = 0,
    double WindowCenter = 40,
    double WindowWidth = 400,
    string TransferFunction = "default",
    double Opacity = 0.5,
    bool EnableShading = true,
    int OutputWidth = 512,
    int OutputHeight = 512
);

public record CprRequest(
    List<Point3DDto> CenterlinePath,
    double? WindowCenter = null,
    double? WindowWidth = null
);

public record Point3DDto(double X, double Y, double Z);

public record FusionRequest(
    string BaseFilePath,
    string OverlayFilePath,
    double BaseWindowCenter = 40,
    double BaseWindowWidth = 400,
    double OverlayWindowCenter = 5000,
    double OverlayWindowWidth = 10000,
    string ColorMap = "hot",
    double OverlayOpacity = 0.5,
    bool EnableThreshold = false,
    double ThresholdMin = 0,
    double ThresholdMax = double.MaxValue
);

public record EnhancementRequest(
    bool Sharpen = false,
    double SharpenAmount = 1.0,
    bool Smooth = false,
    double SmoothAmount = 1.0,
    bool NoiseReduction = false,
    double NoiseReductionStrength = 1.0,
    bool EdgeEnhancement = false,
    double EdgeEnhancementStrength = 1.0,
    bool Invert = false,
    double Rotation = 0,
    bool FlipHorizontal = false,
    bool FlipVertical = false,
    double Brightness = 0,
    double Contrast = 0,
    double Gamma = 1.0
);
