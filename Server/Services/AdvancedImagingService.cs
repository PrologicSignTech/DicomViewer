using FellowOakDicom;
using FellowOakDicom.Imaging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Numerics;

namespace MedView.Server.Services;

public interface IAdvancedImagingService
{
    // MPR - Multi-planar Reconstruction
    Task<byte[]> RenderMprAsync(IEnumerable<string> filePaths, MprPlane plane, int sliceIndex, 
        double? windowCenter = null, double? windowWidth = null);
    Task<MprVolumeInfo> GetMprVolumeInfoAsync(IEnumerable<string> filePaths);
    
    // 3D Volume Rendering
    Task<byte[]> RenderVolumeAsync(IEnumerable<string> filePaths, VolumeRenderParams renderParams);
    
    // MIP / MinIP
    Task<byte[]> RenderMipAsync(IEnumerable<string> filePaths, MipType type, MprPlane plane,
        int startSlice, int endSlice, double? windowCenter = null, double? windowWidth = null);
    
    // Curved Planar Reformation
    Task<byte[]> RenderCprAsync(IEnumerable<string> filePaths, List<Vector3> centerlinePath,
        double? windowCenter = null, double? windowWidth = null);
    
    // Image Fusion (PET-CT, SPECT-CT)
    Task<byte[]> RenderFusionAsync(string baseFilePath, string overlayFilePath, 
        FusionParams fusionParams);
    
    // Image Enhancement
    Task<byte[]> ApplyEnhancementAsync(string filePath, ImageEnhancementParams enhancementParams);
    
    // LUT Application
    Task<byte[]> ApplyLutAsync(string filePath, string lutName, int frame = 0);
}

public enum MprPlane { Axial, Sagittal, Coronal, Oblique }
public enum MipType { Maximum, Minimum, Average }
public enum FusionColorMap { Hot, Cool, Rainbow, Grayscale }

public record MprVolumeInfo(
    int Width, int Height, int Depth,
    double PixelSpacingX, double PixelSpacingY, double SliceThickness,
    double[] VolumeCenter, double[] VoxelDimensions
);

public record VolumeRenderParams(
    double RotationX, double RotationY, double RotationZ,
    double WindowCenter, double WindowWidth,
    string TransferFunction, // "bone", "skin", "muscle", "vessels"
    double Opacity,
    bool EnableShading,
    int OutputWidth = 512, int OutputHeight = 512
);

public record FusionParams(
    double BaseWindowCenter, double BaseWindowWidth,
    double OverlayWindowCenter, double OverlayWindowWidth,
    FusionColorMap ColorMap,
    double OverlayOpacity,
    bool EnableThreshold,
    double ThresholdMin, double ThresholdMax
);

public record ImageEnhancementParams(
    bool Sharpen, double SharpenAmount,
    bool Smooth, double SmoothAmount,
    bool NoiseReduction, double NoiseReductionStrength,
    bool EdgeEnhancement, double EdgeEnhancementStrength,
    bool Invert,
    double Rotation,
    bool FlipHorizontal, bool FlipVertical,
    double Brightness, double Contrast, double Gamma
);

public class AdvancedImagingService : IAdvancedImagingService
{
    private readonly ILogger<AdvancedImagingService> _logger;
    private readonly IDicomImageService _dicomImageService;

    // Predefined LUTs
    private static readonly Dictionary<string, Func<double, (byte r, byte g, byte b)>> LookupTables = new()
    {
        ["hot"] = v => {
            var r = (byte)Math.Clamp(v * 3 * 255, 0, 255);
            var g = (byte)Math.Clamp((v - 0.33) * 3 * 255, 0, 255);
            var b = (byte)Math.Clamp((v - 0.67) * 3 * 255, 0, 255);
            return (r, g, b);
        },
        ["cool"] = v => {
            var r = (byte)(v * 255);
            var g = (byte)((1 - v) * 255);
            var b = (byte)255;
            return (r, g, b);
        },
        ["rainbow"] = v => {
            var h = v * 300; // Hue from 0 to 300
            return HslToRgb(h, 1, 0.5);
        },
        ["bone"] = v => {
            // Optimized for bone visualization
            var val = (byte)(v * 255);
            return (val, (byte)(val * 0.95), (byte)(val * 0.85));
        },
        ["cardiac"] = v => {
            // Red-yellow for cardiac
            return ((byte)(v * 255), (byte)(v * v * 255), 0);
        },
        ["pet"] = v => {
            // Standard PET colormap
            if (v < 0.25) return (0, (byte)(v * 4 * 255), (byte)(v * 4 * 255));
            if (v < 0.5) return ((byte)((v - 0.25) * 4 * 255), 255, (byte)((0.5 - v) * 4 * 255));
            if (v < 0.75) return (255, (byte)((0.75 - v) * 4 * 255), 0);
            return (255, 0, (byte)((v - 0.75) * 4 * 255));
        }
    };

    public AdvancedImagingService(ILogger<AdvancedImagingService> logger, IDicomImageService dicomImageService)
    {
        _logger = logger;
        _dicomImageService = dicomImageService;
    }

    public async Task<MprVolumeInfo> GetMprVolumeInfoAsync(IEnumerable<string> filePaths)
    {
        var files = filePaths.ToList();
        if (files.Count == 0)
            throw new ArgumentException("No files provided");

        var firstFile = await DicomFile.OpenAsync(files[0]);
        var dataset = firstFile.Dataset;

        int width = dataset.GetSingleValue<int>(DicomTag.Columns);
        int height = dataset.GetSingleValue<int>(DicomTag.Rows);
        int depth = files.Count;

        var pixelSpacing = dataset.GetValues<double>(DicomTag.PixelSpacing);
        double pixelSpacingX = pixelSpacing.Length > 0 ? pixelSpacing[0] : 1.0;
        double pixelSpacingY = pixelSpacing.Length > 1 ? pixelSpacing[1] : 1.0;
        double sliceThickness = dataset.GetSingleValueOrDefault(DicomTag.SliceThickness, 1.0);

        return new MprVolumeInfo(
            width, height, depth,
            pixelSpacingX, pixelSpacingY, sliceThickness,
            new[] { width / 2.0, height / 2.0, depth / 2.0 },
            new[] { pixelSpacingX, pixelSpacingY, sliceThickness }
        );
    }

    public async Task<byte[]> RenderMprAsync(IEnumerable<string> filePaths, MprPlane plane, int sliceIndex,
        double? windowCenter = null, double? windowWidth = null)
    {
        var files = filePaths.ToList();
        if (files.Count == 0)
            throw new ArgumentException("No files provided for MPR");

        // Load volume data
        var (volume, dims, wc, ww, rs, ri) = await LoadVolumeAsync(files);

        double finalWc = windowCenter ?? wc;
        double finalWw = windowWidth ?? ww;

        // Generate MPR slice based on plane
        Image<L8> image;
        int outputWidth, outputHeight;

        switch (plane)
        {
            case MprPlane.Sagittal:
                sliceIndex = Math.Clamp(sliceIndex, 0, dims.width - 1);
                outputWidth = dims.depth;
                outputHeight = dims.height;
                image = new Image<L8>(outputWidth, outputHeight);
                for (int y = 0; y < dims.height; y++)
                {
                    for (int z = 0; z < dims.depth; z++)
                    {
                        double value = volume[sliceIndex, y, z];
                        byte pixel = ApplyWindowLevelSingle(value, finalWc, finalWw);
                        image[z, y] = new L8(pixel);
                    }
                }
                break;

            case MprPlane.Coronal:
                sliceIndex = Math.Clamp(sliceIndex, 0, dims.height - 1);
                outputWidth = dims.width;
                outputHeight = dims.depth;
                image = new Image<L8>(outputWidth, outputHeight);
                for (int z = 0; z < dims.depth; z++)
                {
                    for (int x = 0; x < dims.width; x++)
                    {
                        double value = volume[x, sliceIndex, z];
                        byte pixel = ApplyWindowLevelSingle(value, finalWc, finalWw);
                        image[x, z] = new L8(pixel);
                    }
                }
                break;

            case MprPlane.Axial:
            default:
                sliceIndex = Math.Clamp(sliceIndex, 0, dims.depth - 1);
                outputWidth = dims.width;
                outputHeight = dims.height;
                image = new Image<L8>(outputWidth, outputHeight);
                for (int y = 0; y < dims.height; y++)
                {
                    for (int x = 0; x < dims.width; x++)
                    {
                        double value = volume[x, y, sliceIndex];
                        byte pixel = ApplyWindowLevelSingle(value, finalWc, finalWw);
                        image[x, y] = new L8(pixel);
                    }
                }
                break;
        }

        using var ms = new MemoryStream();
        image.SaveAsPng(ms);
        image.Dispose();
        return ms.ToArray();
    }

    public async Task<byte[]> RenderMipAsync(IEnumerable<string> filePaths, MipType type, MprPlane plane,
        int startSlice, int endSlice, double? windowCenter = null, double? windowWidth = null)
    {
        var files = filePaths.ToList();
        if (files.Count == 0)
            throw new ArgumentException("No files provided for MIP");

        var (volume, dims, wc, ww, rs, ri) = await LoadVolumeAsync(files);

        double finalWc = windowCenter ?? wc;
        double finalWw = windowWidth ?? ww;

        // Clamp slice range
        startSlice = Math.Max(0, startSlice);
        
        int outputWidth, outputHeight;
        double[,] projection;

        switch (plane)
        {
            case MprPlane.Sagittal:
                endSlice = Math.Min(dims.width - 1, endSlice);
                outputWidth = dims.depth;
                outputHeight = dims.height;
                projection = new double[outputWidth, outputHeight];
                
                for (int y = 0; y < dims.height; y++)
                {
                    for (int z = 0; z < dims.depth; z++)
                    {
                        double projValue = type == MipType.Maximum ? double.MinValue :
                                          type == MipType.Minimum ? double.MaxValue : 0;
                        int count = 0;
                        
                        for (int x = startSlice; x <= endSlice; x++)
                        {
                            double val = volume[x, y, z];
                            switch (type)
                            {
                                case MipType.Maximum:
                                    projValue = Math.Max(projValue, val);
                                    break;
                                case MipType.Minimum:
                                    projValue = Math.Min(projValue, val);
                                    break;
                                case MipType.Average:
                                    projValue += val;
                                    count++;
                                    break;
                            }
                        }
                        if (type == MipType.Average && count > 0) projValue /= count;
                        projection[z, y] = projValue;
                    }
                }
                break;

            case MprPlane.Coronal:
                endSlice = Math.Min(dims.height - 1, endSlice);
                outputWidth = dims.width;
                outputHeight = dims.depth;
                projection = new double[outputWidth, outputHeight];
                
                for (int x = 0; x < dims.width; x++)
                {
                    for (int z = 0; z < dims.depth; z++)
                    {
                        double projValue = type == MipType.Maximum ? double.MinValue :
                                          type == MipType.Minimum ? double.MaxValue : 0;
                        int count = 0;
                        
                        for (int y = startSlice; y <= endSlice; y++)
                        {
                            double val = volume[x, y, z];
                            switch (type)
                            {
                                case MipType.Maximum:
                                    projValue = Math.Max(projValue, val);
                                    break;
                                case MipType.Minimum:
                                    projValue = Math.Min(projValue, val);
                                    break;
                                case MipType.Average:
                                    projValue += val;
                                    count++;
                                    break;
                            }
                        }
                        if (type == MipType.Average && count > 0) projValue /= count;
                        projection[x, z] = projValue;
                    }
                }
                break;

            case MprPlane.Axial:
            default:
                endSlice = Math.Min(dims.depth - 1, endSlice);
                outputWidth = dims.width;
                outputHeight = dims.height;
                projection = new double[outputWidth, outputHeight];
                
                for (int x = 0; x < dims.width; x++)
                {
                    for (int y = 0; y < dims.height; y++)
                    {
                        double projValue = type == MipType.Maximum ? double.MinValue :
                                          type == MipType.Minimum ? double.MaxValue : 0;
                        int count = 0;
                        
                        for (int z = startSlice; z <= endSlice; z++)
                        {
                            double val = volume[x, y, z];
                            switch (type)
                            {
                                case MipType.Maximum:
                                    projValue = Math.Max(projValue, val);
                                    break;
                                case MipType.Minimum:
                                    projValue = Math.Min(projValue, val);
                                    break;
                                case MipType.Average:
                                    projValue += val;
                                    count++;
                                    break;
                            }
                        }
                        if (type == MipType.Average && count > 0) projValue /= count;
                        projection[x, y] = projValue;
                    }
                }
                break;
        }

        // Convert projection to image
        using var image = new Image<L8>(outputWidth, outputHeight);
        for (int y = 0; y < outputHeight; y++)
        {
            for (int x = 0; x < outputWidth; x++)
            {
                byte pixel = ApplyWindowLevelSingle(projection[x, y], finalWc, finalWw);
                image[x, y] = new L8(pixel);
            }
        }

        using var ms = new MemoryStream();
        image.SaveAsPng(ms);
        return ms.ToArray();
    }

    public async Task<byte[]> RenderVolumeAsync(IEnumerable<string> filePaths, VolumeRenderParams renderParams)
    {
        var files = filePaths.ToList();
        if (files.Count == 0)
            throw new ArgumentException("No files provided for volume rendering");

        var (volume, dims, wc, ww, rs, ri) = await LoadVolumeAsync(files);

        // Get transfer function
        var transferFunc = GetTransferFunction(renderParams.TransferFunction);

        // Create output image
        using var image = new Image<Rgba32>(renderParams.OutputWidth, renderParams.OutputHeight);

        // Ray casting for volume rendering
        double centerX = dims.width / 2.0;
        double centerY = dims.height / 2.0;
        double centerZ = dims.depth / 2.0;

        // Rotation matrices
        var rotX = CreateRotationMatrixX(renderParams.RotationX * Math.PI / 180);
        var rotY = CreateRotationMatrixY(renderParams.RotationY * Math.PI / 180);
        var rotZ = CreateRotationMatrixZ(renderParams.RotationZ * Math.PI / 180);
        var rotation = MultiplyMatrices(MultiplyMatrices(rotZ, rotY), rotX);

        double scale = Math.Min(renderParams.OutputWidth, renderParams.OutputHeight) / 
                       Math.Max(dims.width, Math.Max(dims.height, dims.depth));

        Parallel.For(0, renderParams.OutputHeight, py =>
        {
            for (int px = 0; px < renderParams.OutputWidth; px++)
            {
                // Ray direction (simple orthographic projection)
                double rayX = (px - renderParams.OutputWidth / 2.0) / scale;
                double rayY = (py - renderParams.OutputHeight / 2.0) / scale;

                // Accumulate color along ray
                double accumR = 0, accumG = 0, accumB = 0, accumA = 0;
                int maxSteps = (int)(Math.Sqrt(dims.width * dims.width + dims.height * dims.height + dims.depth * dims.depth));

                for (int step = 0; step < maxSteps && accumA < 0.99; step++)
                {
                    double t = step - maxSteps / 2.0;
                    
                    // Apply rotation to sample point
                    var rotated = ApplyRotation(rotation, rayX, rayY, t);
                    
                    int vx = (int)(rotated[0] + centerX);
                    int vy = (int)(rotated[1] + centerY);
                    int vz = (int)(rotated[2] + centerZ);

                    if (vx >= 0 && vx < dims.width && vy >= 0 && vy < dims.height && vz >= 0 && vz < dims.depth)
                    {
                        double value = volume[vx, vy, vz];
                        double normalized = (value - (renderParams.WindowCenter - renderParams.WindowWidth / 2)) / renderParams.WindowWidth;
                        normalized = Math.Clamp(normalized, 0, 1);

                        var (r, g, b, a) = transferFunc(normalized);
                        a *= renderParams.Opacity;

                        // Front-to-back compositing
                        accumR += (1 - accumA) * a * r;
                        accumG += (1 - accumA) * a * g;
                        accumB += (1 - accumA) * a * b;
                        accumA += (1 - accumA) * a;
                    }
                }

                image[px, py] = new Rgba32(
                    (byte)(accumR * 255),
                    (byte)(accumG * 255),
                    (byte)(accumB * 255),
                    (byte)(accumA * 255)
                );
            }
        });

        using var ms = new MemoryStream();
        image.SaveAsPng(ms);
        return ms.ToArray();
    }

    public async Task<byte[]> RenderCprAsync(IEnumerable<string> filePaths, List<Vector3> centerlinePath,
        double? windowCenter = null, double? windowWidth = null)
    {
        var files = filePaths.ToList();
        if (files.Count == 0 || centerlinePath.Count < 2)
            throw new ArgumentException("Invalid input for CPR");

        var (volume, dims, wc, ww, rs, ri) = await LoadVolumeAsync(files);

        double finalWc = windowCenter ?? wc;
        double finalWw = windowWidth ?? ww;

        // Calculate total path length
        double totalLength = 0;
        for (int i = 1; i < centerlinePath.Count; i++)
        {
            totalLength += Vector3.Distance(centerlinePath[i - 1], centerlinePath[i]);
        }

        int outputWidth = (int)totalLength;
        int outputHeight = Math.Max(dims.width, dims.height) / 2;

        using var image = new Image<L8>(outputWidth, outputHeight);

        // Sample along the centerline
        double currentLength = 0;
        int pathIndex = 0;

        for (int x = 0; x < outputWidth; x++)
        {
            // Find position along path
            while (pathIndex < centerlinePath.Count - 1)
            {
                double segmentLength = Vector3.Distance(centerlinePath[pathIndex], centerlinePath[pathIndex + 1]);
                if (currentLength + segmentLength >= x)
                    break;
                currentLength += segmentLength;
                pathIndex++;
            }

            if (pathIndex >= centerlinePath.Count - 1) break;

            // Interpolate position
            double t = (x - currentLength) / Vector3.Distance(centerlinePath[pathIndex], centerlinePath[pathIndex + 1]);
            Vector3 pos = Vector3.Lerp(centerlinePath[pathIndex], centerlinePath[pathIndex + 1], (float)t);

            // Calculate perpendicular direction
            Vector3 tangent = Vector3.Normalize(centerlinePath[pathIndex + 1] - centerlinePath[pathIndex]);
            Vector3 perpendicular = Vector3.Cross(tangent, Vector3.UnitZ);
            if (perpendicular.Length() < 0.1f)
                perpendicular = Vector3.Cross(tangent, Vector3.UnitY);
            perpendicular = Vector3.Normalize(perpendicular);

            // Sample perpendicular slice
            for (int y = 0; y < outputHeight; y++)
            {
                float offset = (y - outputHeight / 2.0f);
                Vector3 samplePos = pos + perpendicular * offset;

                int vx = (int)samplePos.X;
                int vy = (int)samplePos.Y;
                int vz = (int)samplePos.Z;

                if (vx >= 0 && vx < dims.width && vy >= 0 && vy < dims.height && vz >= 0 && vz < dims.depth)
                {
                    double value = volume[vx, vy, vz];
                    byte pixel = ApplyWindowLevelSingle(value, finalWc, finalWw);
                    image[x, y] = new L8(pixel);
                }
            }
        }

        using var ms = new MemoryStream();
        image.SaveAsPng(ms);
        return ms.ToArray();
    }

    public async Task<byte[]> RenderFusionAsync(string baseFilePath, string overlayFilePath, FusionParams fusionParams)
    {
        var baseFile = await DicomFile.OpenAsync(baseFilePath);
        var overlayFile = await DicomFile.OpenAsync(overlayFilePath);

        var baseDataset = baseFile.Dataset;
        var overlayDataset = overlayFile.Dataset;

        int width = baseDataset.GetSingleValue<int>(DicomTag.Columns);
        int height = baseDataset.GetSingleValue<int>(DicomTag.Rows);

        // Load base image
        var basePixelData = DicomPixelData.Create(baseDataset);
        var baseFrame = basePixelData.GetFrame(0);
        var basePixels = new ushort[width * height];
        Buffer.BlockCopy(baseFrame.Data, 0, basePixels, 0, Math.Min(baseFrame.Data.Length, basePixels.Length * 2));

        // Load overlay image
        int overlayWidth = overlayDataset.GetSingleValue<int>(DicomTag.Columns);
        int overlayHeight = overlayDataset.GetSingleValue<int>(DicomTag.Rows);
        var overlayPixelData = DicomPixelData.Create(overlayDataset);
        var overlayFrame = overlayPixelData.GetFrame(0);
        var overlayPixels = new ushort[overlayWidth * overlayHeight];
        Buffer.BlockCopy(overlayFrame.Data, 0, overlayPixels, 0, Math.Min(overlayFrame.Data.Length, overlayPixels.Length * 2));

        double baseRs = baseDataset.GetSingleValueOrDefault(DicomTag.RescaleSlope, 1.0);
        double baseRi = baseDataset.GetSingleValueOrDefault(DicomTag.RescaleIntercept, 0.0);
        double overlayRs = overlayDataset.GetSingleValueOrDefault(DicomTag.RescaleSlope, 1.0);
        double overlayRi = overlayDataset.GetSingleValueOrDefault(DicomTag.RescaleIntercept, 0.0);

        // Get color map function
        var colorMap = GetColorMapFunction(fusionParams.ColorMap);

        using var image = new Image<Rgba32>(width, height);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int idx = y * width + x;
                
                // Base image value
                double baseValue = basePixels[idx] * baseRs + baseRi;
                double baseNorm = (baseValue - (fusionParams.BaseWindowCenter - fusionParams.BaseWindowWidth / 2)) / 
                                  fusionParams.BaseWindowWidth;
                baseNorm = Math.Clamp(baseNorm, 0, 1);
                byte baseGray = (byte)(baseNorm * 255);

                // Overlay image value (with potential resampling)
                int ox = (int)(x * (double)overlayWidth / width);
                int oy = (int)(y * (double)overlayHeight / height);
                ox = Math.Clamp(ox, 0, overlayWidth - 1);
                oy = Math.Clamp(oy, 0, overlayHeight - 1);
                int overlayIdx = oy * overlayWidth + ox;

                double overlayValue = overlayPixels[overlayIdx] * overlayRs + overlayRi;
                
                // Apply threshold if enabled
                if (fusionParams.EnableThreshold && 
                    (overlayValue < fusionParams.ThresholdMin || overlayValue > fusionParams.ThresholdMax))
                {
                    image[x, y] = new Rgba32(baseGray, baseGray, baseGray, 255);
                    continue;
                }

                double overlayNorm = (overlayValue - (fusionParams.OverlayWindowCenter - fusionParams.OverlayWindowWidth / 2)) / 
                                     fusionParams.OverlayWindowWidth;
                overlayNorm = Math.Clamp(overlayNorm, 0, 1);

                // Apply color map to overlay
                var (r, g, b) = colorMap(overlayNorm);

                // Blend base and overlay
                double alpha = fusionParams.OverlayOpacity * overlayNorm;
                byte finalR = (byte)(baseGray * (1 - alpha) + r * alpha);
                byte finalG = (byte)(baseGray * (1 - alpha) + g * alpha);
                byte finalB = (byte)(baseGray * (1 - alpha) + b * alpha);

                image[x, y] = new Rgba32(finalR, finalG, finalB, 255);
            }
        }

        using var ms = new MemoryStream();
        image.SaveAsPng(ms);
        return ms.ToArray();
    }

    public async Task<byte[]> ApplyEnhancementAsync(string filePath, ImageEnhancementParams enhancementParams)
    {
        var dicomFile = await DicomFile.OpenAsync(filePath);
        var dataset = dicomFile.Dataset;

        int width = dataset.GetSingleValue<int>(DicomTag.Columns);
        int height = dataset.GetSingleValue<int>(DicomTag.Rows);

        var pixelData = DicomPixelData.Create(dataset);
        var frameData = pixelData.GetFrame(0);
        var pixels = new ushort[width * height];
        Buffer.BlockCopy(frameData.Data, 0, pixels, 0, Math.Min(frameData.Data.Length, pixels.Length * 2));

        double rs = dataset.GetSingleValueOrDefault(DicomTag.RescaleSlope, 1.0);
        double ri = dataset.GetSingleValueOrDefault(DicomTag.RescaleIntercept, 0.0);
        double wc = dataset.GetSingleValueOrDefault(DicomTag.WindowCenter, 128.0);
        double ww = dataset.GetSingleValueOrDefault(DicomTag.WindowWidth, 256.0);

        // Convert to floating point for processing
        var floatImage = new double[width, height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                floatImage[x, y] = pixels[y * width + x] * rs + ri;
            }
        }

        // Apply enhancements
        if (enhancementParams.NoiseReduction)
        {
            floatImage = ApplyGaussianBlur(floatImage, width, height, enhancementParams.NoiseReductionStrength);
        }

        if (enhancementParams.Sharpen)
        {
            floatImage = ApplyUnsharpMask(floatImage, width, height, enhancementParams.SharpenAmount);
        }

        if (enhancementParams.EdgeEnhancement)
        {
            floatImage = ApplyEdgeEnhancement(floatImage, width, height, enhancementParams.EdgeEnhancementStrength);
        }

        if (enhancementParams.Smooth)
        {
            floatImage = ApplyGaussianBlur(floatImage, width, height, enhancementParams.SmoothAmount);
        }

        // Create output image
        using var image = new Image<L8>(width, height);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                double value = floatImage[x, y];
                
                // Apply brightness and contrast
                value = (value - wc) * (1 + enhancementParams.Contrast) + wc + enhancementParams.Brightness;
                
                // Apply gamma
                double normalized = (value - (wc - ww / 2)) / ww;
                normalized = Math.Clamp(normalized, 0, 1);
                if (enhancementParams.Gamma != 1.0)
                {
                    normalized = Math.Pow(normalized, 1.0 / enhancementParams.Gamma);
                }

                byte pixel = (byte)(normalized * 255);
                
                if (enhancementParams.Invert)
                    pixel = (byte)(255 - pixel);

                image[x, y] = new L8(pixel);
            }
        }

        // Apply geometric transformations
        if (enhancementParams.Rotation != 0 || enhancementParams.FlipHorizontal || enhancementParams.FlipVertical)
        {
            image.Mutate(ctx =>
            {
                if (enhancementParams.FlipHorizontal)
                    ctx.Flip(FlipMode.Horizontal);
                if (enhancementParams.FlipVertical)
                    ctx.Flip(FlipMode.Vertical);
                if (enhancementParams.Rotation != 0)
                    ctx.Rotate((float)enhancementParams.Rotation);
            });
        }

        using var ms = new MemoryStream();
        image.SaveAsPng(ms);
        return ms.ToArray();
    }

    public async Task<byte[]> ApplyLutAsync(string filePath, string lutName, int frame = 0)
    {
        var dicomFile = await DicomFile.OpenAsync(filePath);
        var dataset = dicomFile.Dataset;

        int width = dataset.GetSingleValue<int>(DicomTag.Columns);
        int height = dataset.GetSingleValue<int>(DicomTag.Rows);

        var pixelData = DicomPixelData.Create(dataset);
        var frameData = pixelData.GetFrame(frame);
        var pixels = new ushort[width * height];
        Buffer.BlockCopy(frameData.Data, 0, pixels, 0, Math.Min(frameData.Data.Length, pixels.Length * 2));

        double rs = dataset.GetSingleValueOrDefault(DicomTag.RescaleSlope, 1.0);
        double ri = dataset.GetSingleValueOrDefault(DicomTag.RescaleIntercept, 0.0);
        double wc = dataset.GetSingleValueOrDefault(DicomTag.WindowCenter, 128.0);
        double ww = dataset.GetSingleValueOrDefault(DicomTag.WindowWidth, 256.0);

        if (!LookupTables.TryGetValue(lutName.ToLower(), out var lutFunc))
        {
            lutFunc = LookupTables["hot"]; // Default
        }

        using var image = new Image<Rgb24>(width, height);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                double value = pixels[y * width + x] * rs + ri;
                double normalized = (value - (wc - ww / 2)) / ww;
                normalized = Math.Clamp(normalized, 0, 1);

                var (r, g, b) = lutFunc(normalized);
                image[x, y] = new Rgb24(r, g, b);
            }
        }

        using var ms = new MemoryStream();
        image.SaveAsPng(ms);
        return ms.ToArray();
    }

    #region Helper Methods

    private async Task<(double[,,] volume, (int width, int height, int depth) dims, double wc, double ww, double rs, double ri)> 
        LoadVolumeAsync(List<string> files)
    {
        // Sort by slice location
        var sortedFiles = new List<(string path, double location)>();
        foreach (var path in files)
        {
            var file = await DicomFile.OpenAsync(path);
            var loc = file.Dataset.GetSingleValueOrDefault(DicomTag.SliceLocation, 0.0);
            sortedFiles.Add((path, loc));
        }
        sortedFiles = sortedFiles.OrderBy(f => f.location).ToList();

        var firstFile = await DicomFile.OpenAsync(sortedFiles[0].path);
        var dataset = firstFile.Dataset;

        int width = dataset.GetSingleValue<int>(DicomTag.Columns);
        int height = dataset.GetSingleValue<int>(DicomTag.Rows);
        int depth = sortedFiles.Count;

        double wc = dataset.GetSingleValueOrDefault(DicomTag.WindowCenter, 40.0);
        double ww = dataset.GetSingleValueOrDefault(DicomTag.WindowWidth, 400.0);
        double rs = dataset.GetSingleValueOrDefault(DicomTag.RescaleSlope, 1.0);
        double ri = dataset.GetSingleValueOrDefault(DicomTag.RescaleIntercept, 0.0);

        var volume = new double[width, height, depth];

        for (int z = 0; z < depth; z++)
        {
            var file = await DicomFile.OpenAsync(sortedFiles[z].path);
            var pixelData = DicomPixelData.Create(file.Dataset);
            var frameData = pixelData.GetFrame(0);

            var pixels = new ushort[width * height];
            Buffer.BlockCopy(frameData.Data, 0, pixels, 0, Math.Min(frameData.Data.Length, pixels.Length * 2));

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    volume[x, y, z] = pixels[y * width + x] * rs + ri;
                }
            }
        }

        return (volume, (width, height, depth), wc, ww, rs, ri);
    }

    private static byte ApplyWindowLevelSingle(double value, double wc, double ww)
    {
        double minValue = wc - ww / 2;
        double maxValue = wc + ww / 2;
        
        if (value <= minValue) return 0;
        if (value >= maxValue) return 255;
        return (byte)(((value - minValue) / ww) * 255);
    }

    private static Func<double, (double r, double g, double b, double a)> GetTransferFunction(string name)
    {
        return name.ToLower() switch
        {
            "bone" => v => (v > 0.6 ? 1.0 : v * 1.5, v > 0.6 ? 0.9 : v * 1.3, v > 0.6 ? 0.7 : v, v > 0.3 ? v : 0),
            "skin" => v => (1.0, 0.8, 0.6, v > 0.15 && v < 0.25 ? 0.3 : 0),
            "muscle" => v => (0.8, 0.3, 0.3, v > 0.2 && v < 0.4 ? 0.4 : 0),
            "vessels" => v => (1.0, 0.2, 0.2, v > 0.5 ? v : 0),
            _ => v => (v, v, v, v * 0.5)
        };
    }

    private static Func<double, (byte r, byte g, byte b)> GetColorMapFunction(FusionColorMap colorMap)
    {
        return colorMap switch
        {
            FusionColorMap.Hot => LookupTables["hot"],
            FusionColorMap.Cool => LookupTables["cool"],
            FusionColorMap.Rainbow => LookupTables["rainbow"],
            _ => v => ((byte)(v * 255), (byte)(v * 255), (byte)(v * 255))
        };
    }

    private static double[,] CreateRotationMatrixX(double angle)
    {
        var cos = Math.Cos(angle);
        var sin = Math.Sin(angle);
        return new double[,] {
            { 1, 0, 0 },
            { 0, cos, -sin },
            { 0, sin, cos }
        };
    }

    private static double[,] CreateRotationMatrixY(double angle)
    {
        var cos = Math.Cos(angle);
        var sin = Math.Sin(angle);
        return new double[,] {
            { cos, 0, sin },
            { 0, 1, 0 },
            { -sin, 0, cos }
        };
    }

    private static double[,] CreateRotationMatrixZ(double angle)
    {
        var cos = Math.Cos(angle);
        var sin = Math.Sin(angle);
        return new double[,] {
            { cos, -sin, 0 },
            { sin, cos, 0 },
            { 0, 0, 1 }
        };
    }

    private static double[,] MultiplyMatrices(double[,] a, double[,] b)
    {
        var result = new double[3, 3];
        for (int i = 0; i < 3; i++)
            for (int j = 0; j < 3; j++)
                for (int k = 0; k < 3; k++)
                    result[i, j] += a[i, k] * b[k, j];
        return result;
    }

    private static double[] ApplyRotation(double[,] matrix, double x, double y, double z)
    {
        return new[]
        {
            matrix[0, 0] * x + matrix[0, 1] * y + matrix[0, 2] * z,
            matrix[1, 0] * x + matrix[1, 1] * y + matrix[1, 2] * z,
            matrix[2, 0] * x + matrix[2, 1] * y + matrix[2, 2] * z
        };
    }

    private static (byte r, byte g, byte b) HslToRgb(double h, double s, double l)
    {
        double c = (1 - Math.Abs(2 * l - 1)) * s;
        double x = c * (1 - Math.Abs((h / 60) % 2 - 1));
        double m = l - c / 2;

        double r, g, b;
        if (h < 60) { r = c; g = x; b = 0; }
        else if (h < 120) { r = x; g = c; b = 0; }
        else if (h < 180) { r = 0; g = c; b = x; }
        else if (h < 240) { r = 0; g = x; b = c; }
        else if (h < 300) { r = x; g = 0; b = c; }
        else { r = c; g = 0; b = x; }

        return ((byte)((r + m) * 255), (byte)((g + m) * 255), (byte)((b + m) * 255));
    }

    private static double[,] ApplyGaussianBlur(double[,] image, int width, int height, double sigma)
    {
        var result = new double[width, height];
        int kernelSize = (int)(sigma * 6) | 1; // Make odd
        var kernel = CreateGaussianKernel(kernelSize, sigma);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                double sum = 0;
                double weightSum = 0;
                int half = kernelSize / 2;

                for (int ky = -half; ky <= half; ky++)
                {
                    for (int kx = -half; kx <= half; kx++)
                    {
                        int px = Math.Clamp(x + kx, 0, width - 1);
                        int py = Math.Clamp(y + ky, 0, height - 1);
                        double weight = kernel[kx + half, ky + half];
                        sum += image[px, py] * weight;
                        weightSum += weight;
                    }
                }
                result[x, y] = sum / weightSum;
            }
        }
        return result;
    }

    private static double[,] CreateGaussianKernel(int size, double sigma)
    {
        var kernel = new double[size, size];
        int half = size / 2;
        double sum = 0;

        for (int y = -half; y <= half; y++)
        {
            for (int x = -half; x <= half; x++)
            {
                double value = Math.Exp(-(x * x + y * y) / (2 * sigma * sigma));
                kernel[x + half, y + half] = value;
                sum += value;
            }
        }

        // Normalize
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                kernel[x, y] /= sum;

        return kernel;
    }

    private static double[,] ApplyUnsharpMask(double[,] image, int width, int height, double amount)
    {
        var blurred = ApplyGaussianBlur(image, width, height, 2.0);
        var result = new double[width, height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                result[x, y] = image[x, y] + amount * (image[x, y] - blurred[x, y]);
            }
        }
        return result;
    }

    private static double[,] ApplyEdgeEnhancement(double[,] image, int width, int height, double strength)
    {
        var result = new double[width, height];
        
        // Sobel kernels
        int[,] sobelX = { { -1, 0, 1 }, { -2, 0, 2 }, { -1, 0, 1 } };
        int[,] sobelY = { { -1, -2, -1 }, { 0, 0, 0 }, { 1, 2, 1 } };

        for (int y = 1; y < height - 1; y++)
        {
            for (int x = 1; x < width - 1; x++)
            {
                double gx = 0, gy = 0;
                
                for (int ky = -1; ky <= 1; ky++)
                {
                    for (int kx = -1; kx <= 1; kx++)
                    {
                        gx += image[x + kx, y + ky] * sobelX[ky + 1, kx + 1];
                        gy += image[x + kx, y + ky] * sobelY[ky + 1, kx + 1];
                    }
                }

                double edge = Math.Sqrt(gx * gx + gy * gy);
                result[x, y] = image[x, y] + strength * edge;
            }
        }
        return result;
    }

    #endregion
}
