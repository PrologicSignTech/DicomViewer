using FellowOakDicom;
using FellowOakDicom.Imaging;
using FellowOakDicom.IO.Buffer;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Jpeg;
using MedView.Server.Models;
using MedView.Server.Models.DTOs;

namespace MedView.Server.Services;

public interface IDicomImageService
{
    Task<DicomFile> OpenFileAsync(string filePath);
    Task<byte[]> RenderImageAsync(string filePath, int frame = 0, double? windowCenter = null, double? windowWidth = null, bool invert = false);
    Task<byte[]> RenderThumbnailAsync(string filePath, int width = 128, int height = 128);
    Task<Instance> ExtractMetadataAsync(DicomFile dicomFile, string filePath);
    Task<IEnumerable<DicomTagDto>> GetAllTagsAsync(string filePath);
    Task<byte[]> RenderMprSliceAsync(IEnumerable<string> filePaths, string plane, int sliceIndex, double? windowCenter = null, double? windowWidth = null);
    byte[] ApplyWindowLevel(ushort[] pixelData, int width, int height, double windowCenter, double windowWidth, double rescaleSlope, double rescaleIntercept, bool invert = false);
    Task<double[]> GetPixelValuesAsync(string filePath, int frame = 0);
}

public class DicomImageService : IDicomImageService
{
    private readonly ILogger<DicomImageService> _logger;
    private readonly IConfiguration _configuration;

    public DicomImageService(ILogger<DicomImageService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<DicomFile> OpenFileAsync(string filePath)
    {
        return await DicomFile.OpenAsync(filePath);
    }

    public async Task<byte[]> RenderImageAsync(string filePath, int frame = 0, double? windowCenter = null, double? windowWidth = null, bool invert = false)
    {
        try
        {
            var dicomFile = await DicomFile.OpenAsync(filePath);
            var dataset = dicomFile.Dataset;

            // Get image dimensions
            int width = dataset.GetSingleValue<int>(DicomTag.Columns);
            int height = dataset.GetSingleValue<int>(DicomTag.Rows);
            
            // Get pixel data
            var pixelData = DicomPixelData.Create(dataset);
            var frameData = pixelData.GetFrame(frame);
            
            // Get window/level values
            double wc = windowCenter ?? dataset.GetSingleValueOrDefault(DicomTag.WindowCenter, 128.0);
            double ww = windowWidth ?? dataset.GetSingleValueOrDefault(DicomTag.WindowWidth, 256.0);
            double rs = dataset.GetSingleValueOrDefault(DicomTag.RescaleSlope, 1.0);
            double ri = dataset.GetSingleValueOrDefault(DicomTag.RescaleIntercept, 0.0);

            // Determine bits allocated and create appropriate pixel array
            int bitsAllocated = dataset.GetSingleValueOrDefault(DicomTag.BitsAllocated, 16);
            string photometricInterpretation = dataset.GetSingleValueOrDefault(DicomTag.PhotometricInterpretation, "MONOCHROME2");

            byte[] imageBytes;

            if (photometricInterpretation.Contains("RGB") || 
                photometricInterpretation.Contains("YBR") ||
                dataset.GetSingleValueOrDefault(DicomTag.SamplesPerPixel, 1) == 3)
            {
                // Color image
                imageBytes = RenderColorImage(frameData, width, height);
            }
            else if (bitsAllocated == 8)
            {
                imageBytes = RenderGrayscale8(frameData.Data, width, height, wc, ww, invert);
            }
            else
            {
                // 16-bit grayscale
                ushort[] pixels = new ushort[width * height];
                Buffer.BlockCopy(frameData.Data, 0, pixels, 0, Math.Min(frameData.Data.Length, pixels.Length * 2));
                imageBytes = ApplyWindowLevel(pixels, width, height, wc, ww, rs, ri, invert);
            }

            return imageBytes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rendering DICOM image: {FilePath}", filePath);
            throw;
        }
    }

    private byte[] RenderColorImage(IByteBuffer frameData, int width, int height)
    {
        using var image = new Image<Rgb24>(width, height);
        var data = frameData.Data;
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = (y * width + x) * 3;
                if (index + 2 < data.Length)
                {
                    image[x, y] = new Rgb24(data[index], data[index + 1], data[index + 2]);
                }
            }
        }

        using var ms = new MemoryStream();
        image.SaveAsPng(ms);
        return ms.ToArray();
    }

    private byte[] RenderGrayscale8(byte[] data, int width, int height, double wc, double ww, bool invert)
    {
        using var image = new Image<L8>(width, height);
        
        double minValue = wc - ww / 2;
        double maxValue = wc + ww / 2;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;
                if (index < data.Length)
                {
                    double value = data[index];
                    value = Math.Max(minValue, Math.Min(maxValue, value));
                    byte pixelValue = (byte)((value - minValue) / ww * 255);
                    
                    if (invert)
                        pixelValue = (byte)(255 - pixelValue);
                    
                    image[x, y] = new L8(pixelValue);
                }
            }
        }

        using var ms = new MemoryStream();
        image.SaveAsPng(ms);
        return ms.ToArray();
    }

    public byte[] ApplyWindowLevel(ushort[] pixelData, int width, int height, double windowCenter, double windowWidth, double rescaleSlope, double rescaleIntercept, bool invert = false)
    {
        using var image = new Image<L8>(width, height);

        double minValue = windowCenter - windowWidth / 2;
        double maxValue = windowCenter + windowWidth / 2;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;
                if (index < pixelData.Length)
                {
                    // Apply rescale
                    double value = pixelData[index] * rescaleSlope + rescaleIntercept;
                    
                    // Apply window/level
                    if (value <= minValue)
                        value = 0;
                    else if (value >= maxValue)
                        value = 255;
                    else
                        value = ((value - minValue) / windowWidth) * 255;

                    byte pixelValue = (byte)Math.Clamp(value, 0, 255);
                    
                    if (invert)
                        pixelValue = (byte)(255 - pixelValue);

                    image[x, y] = new L8(pixelValue);
                }
            }
        }

        using var ms = new MemoryStream();
        image.SaveAsPng(ms);
        return ms.ToArray();
    }

    public async Task<byte[]> RenderThumbnailAsync(string filePath, int width = 128, int height = 128)
    {
        try
        {
            var imageBytes = await RenderImageAsync(filePath, 0);
            
            using var image = Image.Load(imageBytes);
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(width, height),
                Mode = ResizeMode.Max
            }));

            using var ms = new MemoryStream();
            image.SaveAsPng(ms, new PngEncoder { CompressionLevel = PngCompressionLevel.BestSpeed });
            return ms.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating thumbnail: {FilePath}", filePath);
            throw;
        }
    }

    public async Task<Instance> ExtractMetadataAsync(DicomFile dicomFile, string filePath)
    {
        var dataset = dicomFile.Dataset;
        
        var instance = new Instance
        {
            SopInstanceUid = dataset.GetSingleValueOrDefault(DicomTag.SOPInstanceUID, Guid.NewGuid().ToString()),
            SopClassUid = dataset.GetSingleValueOrDefault<string>(DicomTag.SOPClassUID, null),
            InstanceNumber = dataset.GetSingleValueOrDefault<int?>(DicomTag.InstanceNumber, null),
            FilePath = filePath,
            FileSize = new FileInfo(filePath).Length,
            
            Rows = dataset.GetSingleValueOrDefault<int?>(DicomTag.Rows, null),
            Columns = dataset.GetSingleValueOrDefault<int?>(DicomTag.Columns, null),
            BitsAllocated = dataset.GetSingleValueOrDefault<int?>(DicomTag.BitsAllocated, null),
            BitsStored = dataset.GetSingleValueOrDefault<int?>(DicomTag.BitsStored, null),
            HighBit = dataset.GetSingleValueOrDefault<int?>(DicomTag.HighBit, null),
            PhotometricInterpretation = dataset.GetSingleValueOrDefault<string>(DicomTag.PhotometricInterpretation, null),
            SamplesPerPixel = dataset.GetSingleValueOrDefault<int?>(DicomTag.SamplesPerPixel, null),
            PixelRepresentation = dataset.GetSingleValueOrDefault<string>(DicomTag.PixelRepresentation, null),
            
            WindowCenter = dataset.GetSingleValueOrDefault<double?>(DicomTag.WindowCenter, null),
            WindowWidth = dataset.GetSingleValueOrDefault<double?>(DicomTag.WindowWidth, null),
            RescaleIntercept = dataset.GetSingleValueOrDefault<double?>(DicomTag.RescaleIntercept, null),
            RescaleSlope = dataset.GetSingleValueOrDefault<double?>(DicomTag.RescaleSlope, null),
            
            ImagePositionPatient = GetMultiValueString(dataset, DicomTag.ImagePositionPatient),
            ImageOrientationPatient = GetMultiValueString(dataset, DicomTag.ImageOrientationPatient),
            PixelSpacing = GetMultiValueString(dataset, DicomTag.PixelSpacing),
            SliceLocation = dataset.GetSingleValueOrDefault<double?>(DicomTag.SliceLocation, null),
            
            NumberOfFrames = dataset.GetSingleValueOrDefault(DicomTag.NumberOfFrames, 1),
            FrameTime = dataset.GetSingleValueOrDefault<double?>(DicomTag.FrameTime, null),
            
            TransferSyntaxUid = dicomFile.FileMetaInfo.TransferSyntax?.UID?.UID
        };

        return await Task.FromResult(instance);
    }

    private string? GetMultiValueString(DicomDataset dataset, DicomTag tag)
    {
        try
        {
            if (dataset.TryGetValues<double>(tag, out var values))
            {
                return string.Join("\\", values);
            }
        }
        catch { }
        return null;
    }

    public async Task<IEnumerable<DicomTagDto>> GetAllTagsAsync(string filePath)
    {
        var dicomFile = await DicomFile.OpenAsync(filePath);
        var tags = new List<DicomTagDto>();

        void ProcessDataset(DicomDataset dataset, string prefix = "")
        {
            foreach (var item in dataset)
            {
                var tagString = $"{prefix}({item.Tag.Group:X4},{item.Tag.Element:X4})";
                var dictEntry = DicomDictionary.Default[item.Tag];
                var name = dictEntry?.Name ?? "Unknown";
                var vr = item.ValueRepresentation.Code;
                
                string value;
                try
                {
                    if (item is DicomSequence seq)
                    {
                        value = $"[Sequence with {seq.Items.Count} item(s)]";
                        tags.Add(new DicomTagDto(tagString, name, vr, value, seq.Items.Count));
                        
                        for (int i = 0; i < seq.Items.Count; i++)
                        {
                            ProcessDataset(seq.Items[i], $"{prefix}[{i}].");
                        }
                        continue;
                    }
                    else if (item is DicomElement element)
                    {
                        if (element.Tag == DicomTag.PixelData)
                        {
                            value = "[Pixel Data]";
                        }
                        else if (element.Count > 100)
                        {
                            value = $"[{element.Count} values]";
                        }
                        else
                        {
                            value = dataset.GetValueOrDefault(item.Tag, 0, string.Empty);
                        }
                    }
                    else
                    {
                        value = "[Unknown type]";
                    }
                }
                catch
                {
                    value = "[Error reading value]";
                }

                tags.Add(new DicomTagDto(tagString, name, vr, value, null));
            }
        }

        ProcessDataset(dicomFile.FileMetaInfo, "File Meta: ");
        ProcessDataset(dicomFile.Dataset);

        return tags;
    }

    public async Task<byte[]> RenderMprSliceAsync(IEnumerable<string> filePaths, string plane, int sliceIndex, double? windowCenter = null, double? windowWidth = null)
    {
        var files = filePaths.ToList();
        if (files.Count == 0)
            throw new ArgumentException("No files provided for MPR");

        // Load first file to get dimensions
        var firstFile = await DicomFile.OpenAsync(files[0]);
        var dataset = firstFile.Dataset;
        
        int columns = dataset.GetSingleValue<int>(DicomTag.Columns);
        int rows = dataset.GetSingleValue<int>(DicomTag.Rows);
        int slices = files.Count;

        // Load all slices into a 3D volume
        var volume = new double[columns, rows, slices];
        
        double wc = windowCenter ?? dataset.GetSingleValueOrDefault(DicomTag.WindowCenter, 40.0);
        double ww = windowWidth ?? dataset.GetSingleValueOrDefault(DicomTag.WindowWidth, 400.0);
        double rs = dataset.GetSingleValueOrDefault(DicomTag.RescaleSlope, 1.0);
        double ri = dataset.GetSingleValueOrDefault(DicomTag.RescaleIntercept, 0.0);

        // Sort files by slice location
        var sortedFiles = new List<(string path, double location)>();
        foreach (var path in files)
        {
            var file = await DicomFile.OpenAsync(path);
            var loc = file.Dataset.GetSingleValueOrDefault(DicomTag.SliceLocation, 0.0);
            sortedFiles.Add((path, loc));
        }
        sortedFiles = sortedFiles.OrderBy(f => f.location).ToList();

        // Load volume data
        for (int z = 0; z < slices; z++)
        {
            var file = await DicomFile.OpenAsync(sortedFiles[z].path);
            var pixelData = DicomPixelData.Create(file.Dataset);
            var frameData = pixelData.GetFrame(0);
            
            int bitsAllocated = file.Dataset.GetSingleValueOrDefault(DicomTag.BitsAllocated, 16);
            
            if (bitsAllocated == 16)
            {
                var pixels = new ushort[columns * rows];
                Buffer.BlockCopy(frameData.Data, 0, pixels, 0, Math.Min(frameData.Data.Length, pixels.Length * 2));
                
                for (int y = 0; y < rows; y++)
                {
                    for (int x = 0; x < columns; x++)
                    {
                        volume[x, y, z] = pixels[y * columns + x] * rs + ri;
                    }
                }
            }
        }

        // Generate MPR slice based on plane
        Image<L8> image;
        
        switch (plane.ToLower())
        {
            case "sagittal":
                sliceIndex = Math.Clamp(sliceIndex, 0, columns - 1);
                image = new Image<L8>(slices, rows);
                for (int y = 0; y < rows; y++)
                {
                    for (int z = 0; z < slices; z++)
                    {
                        double value = volume[sliceIndex, y, z];
                        byte pixel = ApplyWindowLevelSingle(value, wc, ww);
                        image[z, y] = new L8(pixel);
                    }
                }
                break;
                
            case "coronal":
                sliceIndex = Math.Clamp(sliceIndex, 0, rows - 1);
                image = new Image<L8>(columns, slices);
                for (int z = 0; z < slices; z++)
                {
                    for (int x = 0; x < columns; x++)
                    {
                        double value = volume[x, sliceIndex, z];
                        byte pixel = ApplyWindowLevelSingle(value, wc, ww);
                        image[x, z] = new L8(pixel);
                    }
                }
                break;
                
            case "axial":
            default:
                sliceIndex = Math.Clamp(sliceIndex, 0, slices - 1);
                image = new Image<L8>(columns, rows);
                for (int y = 0; y < rows; y++)
                {
                    for (int x = 0; x < columns; x++)
                    {
                        double value = volume[x, y, sliceIndex];
                        byte pixel = ApplyWindowLevelSingle(value, wc, ww);
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

    private byte ApplyWindowLevelSingle(double value, double wc, double ww)
    {
        double minValue = wc - ww / 2;
        double maxValue = wc + ww / 2;
        
        if (value <= minValue) return 0;
        if (value >= maxValue) return 255;
        return (byte)(((value - minValue) / ww) * 255);
    }

    public async Task<double[]> GetPixelValuesAsync(string filePath, int frame = 0)
    {
        var dicomFile = await DicomFile.OpenAsync(filePath);
        var dataset = dicomFile.Dataset;
        
        int width = dataset.GetSingleValue<int>(DicomTag.Columns);
        int height = dataset.GetSingleValue<int>(DicomTag.Rows);
        double rs = dataset.GetSingleValueOrDefault(DicomTag.RescaleSlope, 1.0);
        double ri = dataset.GetSingleValueOrDefault(DicomTag.RescaleIntercept, 0.0);
        
        var pixelData = DicomPixelData.Create(dataset);
        var frameData = pixelData.GetFrame(frame);
        
        var values = new double[width * height];
        var pixels = new ushort[width * height];
        Buffer.BlockCopy(frameData.Data, 0, pixels, 0, Math.Min(frameData.Data.Length, pixels.Length * 2));
        
        for (int i = 0; i < values.Length; i++)
        {
            values[i] = pixels[i] * rs + ri;
        }
        
        return values;
    }
}
