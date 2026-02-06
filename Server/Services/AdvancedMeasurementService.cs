using FellowOakDicom;
using FellowOakDicom.Imaging;
using System.Numerics;

namespace MedView.Server.Services;

public interface IAdvancedMeasurementService
{
    // Basic Measurements
    Task<LengthMeasurementResult> MeasureLengthAsync(string filePath, Point2D start, Point2D end, int frame = 0);
    Task<AngleMeasurementResult> MeasureAngleAsync(string filePath, Point2D vertex, Point2D point1, Point2D point2, int frame = 0);
    Task<AreaMeasurementResult> MeasureAreaAsync(string filePath, List<Point2D> polygon, int frame = 0);
    
    // ROI Statistics
    Task<RoiStatistics> CalculateEllipseRoiAsync(string filePath, Point2D center, double radiusX, double radiusY, int frame = 0);
    Task<RoiStatistics> CalculateRectangleRoiAsync(string filePath, Point2D topLeft, Point2D bottomRight, int frame = 0);
    Task<RoiStatistics> CalculateFreehandRoiAsync(string filePath, List<Point2D> points, int frame = 0);
    
    // HU Measurement
    Task<HuMeasurementResult> MeasureHounsfieldAsync(string filePath, int x, int y, int frame = 0);
    Task<HuMeasurementResult> MeasureHounsfieldRegionAsync(string filePath, Point2D center, int radius, int frame = 0);
    
    // Volume Calculation
    Task<VolumeCalculationResult> CalculateVolumeAsync(IEnumerable<string> filePaths, List<List<Point2D>> contours);
    
    // Cardiac Measurements
    Task<CardiacMeasurementResult> CalculateCardiacMeasurementsAsync(
        IEnumerable<string> edFrameFiles, IEnumerable<string> esFrameFiles,
        List<List<Point2D>> edContours, List<List<Point2D>> esContours);
    
    // Bone Density
    Task<BoneDensityResult> CalculateBoneDensityAsync(string filePath, List<Point2D> roiPoints, int frame = 0);
    
    // Distance Between Landmarks
    Task<List<DistanceMeasurement>> MeasureDistancesBetweenLandmarksAsync(
        string filePath, List<LandmarkPoint> landmarks, int frame = 0);
    
    // Profile Line
    Task<ProfileLineResult> GetProfileLineAsync(string filePath, Point2D start, Point2D end, int frame = 0);
    
    // Histogram
    Task<HistogramResult> CalculateHistogramAsync(string filePath, Point2D? roiTopLeft = null, Point2D? roiBottomRight = null, int frame = 0);
}

// Data structures
public record Point2D(double X, double Y);
public record Point3D(double X, double Y, double Z);

public record LengthMeasurementResult(
    double LengthPixels,
    double LengthMm,
    double LengthCm,
    Point2D Start,
    Point2D End,
    double PixelSpacingX,
    double PixelSpacingY
);

public record AngleMeasurementResult(
    double AngleDegrees,
    double AngleRadians,
    Point2D Vertex,
    Point2D Point1,
    Point2D Point2
);

public record AreaMeasurementResult(
    double AreaPixels,
    double AreaMm2,
    double AreaCm2,
    double PerimeterPixels,
    double PerimeterMm,
    List<Point2D> Polygon
);

public record RoiStatistics(
    double Mean,
    double StdDev,
    double Min,
    double Max,
    double Median,
    int PixelCount,
    double AreaMm2,
    double Sum,
    double Variance,
    string Unit // "HU" for CT, raw for others
);

public record HuMeasurementResult(
    double Value,
    int X,
    int Y,
    double RescaleSlope,
    double RescaleIntercept,
    string Interpretation // "Air", "Fat", "Water", "Soft Tissue", "Bone", etc.
);

public record VolumeCalculationResult(
    double VolumePixels,
    double VolumeMm3,
    double VolumeMl,
    double VolumeCm3,
    int SliceCount,
    double SliceThickness
);

public record CardiacMeasurementResult(
    double EdvMl, // End-Diastolic Volume
    double EsvMl, // End-Systolic Volume
    double StrokeVolumeMl,
    double EjectionFractionPercent,
    double CardiacOutputLpm, // if heart rate available
    double MyocardialMassG,
    List<double> WallThicknessMm,
    List<double> WallMotionMm
);

public record BoneDensityResult(
    double MeanHU,
    double BMDEstimateMgCm3, // Estimated BMD
    string TScoreCategory, // "Normal", "Osteopenia", "Osteoporosis"
    double AreaMm2,
    RoiStatistics RoiStats
);

public record LandmarkPoint(string Name, Point2D Position);

public record DistanceMeasurement(
    string FromLandmark,
    string ToLandmark,
    double DistanceMm,
    double DistanceCm
);

public record ProfileLineResult(
    List<double> Values,
    List<Point2D> Points,
    double Mean,
    double StdDev,
    double Min,
    double Max,
    double LengthMm
);

public record HistogramResult(
    List<int> Bins,
    double BinWidth,
    double MinValue,
    double MaxValue,
    double Mean,
    double StdDev,
    double Median,
    List<double> Percentiles // 5th, 25th, 50th, 75th, 95th
);

public class AdvancedMeasurementService : IAdvancedMeasurementService
{
    private readonly ILogger<AdvancedMeasurementService> _logger;

    public AdvancedMeasurementService(ILogger<AdvancedMeasurementService> logger)
    {
        _logger = logger;
    }

    public async Task<LengthMeasurementResult> MeasureLengthAsync(string filePath, Point2D start, Point2D end, int frame = 0)
    {
        var dicomFile = await DicomFile.OpenAsync(filePath);
        var dataset = dicomFile.Dataset;

        var pixelSpacing = GetPixelSpacing(dataset);
        
        double dx = (end.X - start.X) * pixelSpacing.x;
        double dy = (end.Y - start.Y) * pixelSpacing.y;
        double lengthMm = Math.Sqrt(dx * dx + dy * dy);

        double lengthPixels = Math.Sqrt(Math.Pow(end.X - start.X, 2) + Math.Pow(end.Y - start.Y, 2));

        return new LengthMeasurementResult(
            lengthPixels,
            lengthMm,
            lengthMm / 10.0,
            start,
            end,
            pixelSpacing.x,
            pixelSpacing.y
        );
    }

    public async Task<AngleMeasurementResult> MeasureAngleAsync(string filePath, Point2D vertex, Point2D point1, Point2D point2, int frame = 0)
    {
        // Calculate vectors
        var v1 = new Vector2((float)(point1.X - vertex.X), (float)(point1.Y - vertex.Y));
        var v2 = new Vector2((float)(point2.X - vertex.X), (float)(point2.Y - vertex.Y));

        // Calculate angle using dot product
        float dot = Vector2.Dot(v1, v2);
        float mag1 = v1.Length();
        float mag2 = v2.Length();

        double angleRadians = Math.Acos(dot / (mag1 * mag2));
        double angleDegrees = angleRadians * 180.0 / Math.PI;

        return await Task.FromResult(new AngleMeasurementResult(
            angleDegrees,
            angleRadians,
            vertex,
            point1,
            point2
        ));
    }

    public async Task<AreaMeasurementResult> MeasureAreaAsync(string filePath, List<Point2D> polygon, int frame = 0)
    {
        var dicomFile = await DicomFile.OpenAsync(filePath);
        var dataset = dicomFile.Dataset;

        var pixelSpacing = GetPixelSpacing(dataset);

        // Calculate area using Shoelace formula
        double areaPixels = 0;
        int n = polygon.Count;
        for (int i = 0; i < n; i++)
        {
            int j = (i + 1) % n;
            areaPixels += polygon[i].X * polygon[j].Y;
            areaPixels -= polygon[j].X * polygon[i].Y;
        }
        areaPixels = Math.Abs(areaPixels) / 2.0;

        // Convert to mm²
        double areaMm2 = areaPixels * pixelSpacing.x * pixelSpacing.y;

        // Calculate perimeter
        double perimeterPixels = 0;
        for (int i = 0; i < n; i++)
        {
            int j = (i + 1) % n;
            double dx = polygon[j].X - polygon[i].X;
            double dy = polygon[j].Y - polygon[i].Y;
            perimeterPixels += Math.Sqrt(dx * dx + dy * dy);
        }
        double perimeterMm = perimeterPixels * Math.Sqrt(pixelSpacing.x * pixelSpacing.y);

        return new AreaMeasurementResult(
            areaPixels,
            areaMm2,
            areaMm2 / 100.0,
            perimeterPixels,
            perimeterMm,
            polygon
        );
    }

    public async Task<RoiStatistics> CalculateEllipseRoiAsync(string filePath, Point2D center, double radiusX, double radiusY, int frame = 0)
    {
        var (pixels, width, height, pixelSpacing, rescale) = await LoadImageDataAsync(filePath, frame);
        var values = new List<double>();

        int minX = Math.Max(0, (int)(center.X - radiusX));
        int maxX = Math.Min(width - 1, (int)(center.X + radiusX));
        int minY = Math.Max(0, (int)(center.Y - radiusY));
        int maxY = Math.Min(height - 1, (int)(center.Y + radiusY));

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                // Check if point is inside ellipse
                double dx = (x - center.X) / radiusX;
                double dy = (y - center.Y) / radiusY;
                if (dx * dx + dy * dy <= 1)
                {
                    double value = pixels[y * width + x] * rescale.slope + rescale.intercept;
                    values.Add(value);
                }
            }
        }

        return CalculateStatistics(values, pixelSpacing, radiusX, radiusY);
    }

    public async Task<RoiStatistics> CalculateRectangleRoiAsync(string filePath, Point2D topLeft, Point2D bottomRight, int frame = 0)
    {
        var (pixels, width, height, pixelSpacing, rescale) = await LoadImageDataAsync(filePath, frame);
        var values = new List<double>();

        int minX = Math.Max(0, (int)topLeft.X);
        int maxX = Math.Min(width - 1, (int)bottomRight.X);
        int minY = Math.Max(0, (int)topLeft.Y);
        int maxY = Math.Min(height - 1, (int)bottomRight.Y);

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                double value = pixels[y * width + x] * rescale.slope + rescale.intercept;
                values.Add(value);
            }
        }

        double roiWidth = (maxX - minX) * pixelSpacing.x;
        double roiHeight = (maxY - minY) * pixelSpacing.y;
        return CalculateStatistics(values, pixelSpacing, roiWidth / 2, roiHeight / 2);
    }

    public async Task<RoiStatistics> CalculateFreehandRoiAsync(string filePath, List<Point2D> points, int frame = 0)
    {
        var (pixels, width, height, pixelSpacing, rescale) = await LoadImageDataAsync(filePath, frame);
        var values = new List<double>();

        // Get bounding box
        double minX = points.Min(p => p.X);
        double maxX = points.Max(p => p.X);
        double minY = points.Min(p => p.Y);
        double maxY = points.Max(p => p.Y);

        for (int y = (int)minY; y <= (int)maxY && y < height; y++)
        {
            for (int x = (int)minX; x <= (int)maxX && x < width; x++)
            {
                if (IsPointInPolygon(new Point2D(x, y), points))
                {
                    double value = pixels[y * width + x] * rescale.slope + rescale.intercept;
                    values.Add(value);
                }
            }
        }

        return CalculateStatistics(values, pixelSpacing, (maxX - minX) / 2, (maxY - minY) / 2);
    }

    public async Task<HuMeasurementResult> MeasureHounsfieldAsync(string filePath, int x, int y, int frame = 0)
    {
        var (pixels, width, height, pixelSpacing, rescale) = await LoadImageDataAsync(filePath, frame);

        if (x < 0 || x >= width || y < 0 || y >= height)
            throw new ArgumentException("Coordinates out of bounds");

        double rawValue = pixels[y * width + x];
        double huValue = rawValue * rescale.slope + rescale.intercept;

        return new HuMeasurementResult(
            huValue,
            x,
            y,
            rescale.slope,
            rescale.intercept,
            InterpretHU(huValue)
        );
    }

    public async Task<HuMeasurementResult> MeasureHounsfieldRegionAsync(string filePath, Point2D center, int radius, int frame = 0)
    {
        var roi = await CalculateEllipseRoiAsync(filePath, center, radius, radius, frame);
        
        return new HuMeasurementResult(
            roi.Mean,
            (int)center.X,
            (int)center.Y,
            1.0, // Already rescaled
            0.0,
            InterpretHU(roi.Mean)
        );
    }

    public async Task<VolumeCalculationResult> CalculateVolumeAsync(IEnumerable<string> filePaths, List<List<Point2D>> contours)
    {
        var files = filePaths.ToList();
        if (files.Count != contours.Count)
            throw new ArgumentException("Number of files must match number of contours");

        double totalVolumePixels = 0;
        double sliceThickness = 1.0;
        (double x, double y) pixelSpacing = (1.0, 1.0);

        for (int i = 0; i < files.Count; i++)
        {
            var dicomFile = await DicomFile.OpenAsync(files[i]);
            var dataset = dicomFile.Dataset;

            if (i == 0)
            {
                sliceThickness = dataset.GetSingleValueOrDefault(DicomTag.SliceThickness, 1.0);
                pixelSpacing = GetPixelSpacing(dataset);
            }

            // Calculate area of contour
            var contour = contours[i];
            if (contour.Count < 3) continue;

            double area = 0;
            for (int j = 0; j < contour.Count; j++)
            {
                int k = (j + 1) % contour.Count;
                area += contour[j].X * contour[k].Y;
                area -= contour[k].X * contour[j].Y;
            }
            area = Math.Abs(area) / 2.0;
            totalVolumePixels += area;
        }

        // Convert to mm³
        double volumeMm3 = totalVolumePixels * pixelSpacing.x * pixelSpacing.y * sliceThickness;

        return new VolumeCalculationResult(
            totalVolumePixels,
            volumeMm3,
            volumeMm3 / 1000.0, // mL
            volumeMm3 / 1000.0, // cm³ = mL
            files.Count,
            sliceThickness
        );
    }

    public async Task<CardiacMeasurementResult> CalculateCardiacMeasurementsAsync(
        IEnumerable<string> edFrameFiles, IEnumerable<string> esFrameFiles,
        List<List<Point2D>> edContours, List<List<Point2D>> esContours)
    {
        // Calculate EDV
        var edVolume = await CalculateVolumeAsync(edFrameFiles, edContours);
        
        // Calculate ESV
        var esVolume = await CalculateVolumeAsync(esFrameFiles, esContours);

        double edvMl = edVolume.VolumeMl;
        double esvMl = esVolume.VolumeMl;
        double strokeVolume = edvMl - esvMl;
        double ejectionFraction = (strokeVolume / edvMl) * 100;

        // Estimate myocardial mass (assuming epicardial contours provided)
        // This is a simplified calculation
        double myocardialMass = 0; // Would need epicardial and endocardial contours

        return new CardiacMeasurementResult(
            edvMl,
            esvMl,
            strokeVolume,
            ejectionFraction,
            0, // Would need heart rate for cardiac output
            myocardialMass,
            new List<double>(), // Wall thickness measurements
            new List<double>()  // Wall motion measurements
        );
    }

    public async Task<BoneDensityResult> CalculateBoneDensityAsync(string filePath, List<Point2D> roiPoints, int frame = 0)
    {
        var roiStats = await CalculateFreehandRoiAsync(filePath, roiPoints, frame);
        
        // Estimate BMD from HU (simplified model)
        // Actual BMD calculation requires calibration phantom
        double bmdEstimate = (roiStats.Mean + 1000) * 0.8; // Simplified conversion

        string tScoreCategory;
        if (roiStats.Mean > 100)
            tScoreCategory = "Normal";
        else if (roiStats.Mean > -100)
            tScoreCategory = "Osteopenia";
        else
            tScoreCategory = "Osteoporosis";

        return new BoneDensityResult(
            roiStats.Mean,
            bmdEstimate,
            tScoreCategory,
            roiStats.AreaMm2,
            roiStats
        );
    }

    public async Task<List<DistanceMeasurement>> MeasureDistancesBetweenLandmarksAsync(
        string filePath, List<LandmarkPoint> landmarks, int frame = 0)
    {
        var dicomFile = await DicomFile.OpenAsync(filePath);
        var pixelSpacing = GetPixelSpacing(dicomFile.Dataset);

        var results = new List<DistanceMeasurement>();

        for (int i = 0; i < landmarks.Count; i++)
        {
            for (int j = i + 1; j < landmarks.Count; j++)
            {
                var p1 = landmarks[i].Position;
                var p2 = landmarks[j].Position;

                double dx = (p2.X - p1.X) * pixelSpacing.x;
                double dy = (p2.Y - p1.Y) * pixelSpacing.y;
                double distanceMm = Math.Sqrt(dx * dx + dy * dy);

                results.Add(new DistanceMeasurement(
                    landmarks[i].Name,
                    landmarks[j].Name,
                    distanceMm,
                    distanceMm / 10.0
                ));
            }
        }

        return results;
    }

    public async Task<ProfileLineResult> GetProfileLineAsync(string filePath, Point2D start, Point2D end, int frame = 0)
    {
        var (pixels, width, height, pixelSpacing, rescale) = await LoadImageDataAsync(filePath, frame);

        var values = new List<double>();
        var points = new List<Point2D>();

        // Use Bresenham's line algorithm
        int x0 = (int)start.X, y0 = (int)start.Y;
        int x1 = (int)end.X, y1 = (int)end.Y;

        int dx = Math.Abs(x1 - x0);
        int dy = Math.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;

        while (true)
        {
            if (x0 >= 0 && x0 < width && y0 >= 0 && y0 < height)
            {
                double value = pixels[y0 * width + x0] * rescale.slope + rescale.intercept;
                values.Add(value);
                points.Add(new Point2D(x0, y0));
            }

            if (x0 == x1 && y0 == y1) break;

            int e2 = 2 * err;
            if (e2 > -dy) { err -= dy; x0 += sx; }
            if (e2 < dx) { err += dx; y0 += sy; }
        }

        // Calculate statistics
        double mean = values.Count > 0 ? values.Average() : 0;
        double stdDev = values.Count > 1 ? Math.Sqrt(values.Average(v => Math.Pow(v - mean, 2))) : 0;

        // Calculate length
        double lengthPixels = Math.Sqrt(Math.Pow(end.X - start.X, 2) + Math.Pow(end.Y - start.Y, 2));
        double lengthMm = lengthPixels * Math.Sqrt(pixelSpacing.x * pixelSpacing.y);

        return new ProfileLineResult(
            values,
            points,
            mean,
            stdDev,
            values.Count > 0 ? values.Min() : 0,
            values.Count > 0 ? values.Max() : 0,
            lengthMm
        );
    }

    public async Task<HistogramResult> CalculateHistogramAsync(string filePath, Point2D? roiTopLeft = null, Point2D? roiBottomRight = null, int frame = 0)
    {
        var (pixels, width, height, pixelSpacing, rescale) = await LoadImageDataAsync(filePath, frame);

        int startX = roiTopLeft != null ? (int)roiTopLeft.X : 0;
        int endX = roiBottomRight != null ? (int)roiBottomRight.X : width - 1;
        int startY = roiTopLeft != null ? (int)roiTopLeft.Y : 0;
        int endY = roiBottomRight != null ? (int)roiBottomRight.Y : height - 1;

        var values = new List<double>();

        for (int y = startY; y <= endY && y < height; y++)
        {
            for (int x = startX; x <= endX && x < width; x++)
            {
                double value = pixels[y * width + x] * rescale.slope + rescale.intercept;
                values.Add(value);
            }
        }

        if (values.Count == 0)
            throw new InvalidOperationException("No pixels in ROI");

        double minValue = values.Min();
        double maxValue = values.Max();
        double range = maxValue - minValue;
        int binCount = 256;
        double binWidth = range / binCount;

        var bins = new int[binCount];
        foreach (var value in values)
        {
            int bin = Math.Min((int)((value - minValue) / binWidth), binCount - 1);
            bins[bin]++;
        }

        values.Sort();
        double median = values[values.Count / 2];
        double mean = values.Average();
        double stdDev = Math.Sqrt(values.Average(v => Math.Pow(v - mean, 2)));

        var percentiles = new List<double>
        {
            values[(int)(values.Count * 0.05)],
            values[(int)(values.Count * 0.25)],
            median,
            values[(int)(values.Count * 0.75)],
            values[(int)(values.Count * 0.95)]
        };

        return new HistogramResult(
            bins.ToList(),
            binWidth,
            minValue,
            maxValue,
            mean,
            stdDev,
            median,
            percentiles
        );
    }

    #region Helper Methods

    private async Task<(ushort[] pixels, int width, int height, (double x, double y) pixelSpacing, (double slope, double intercept) rescale)> 
        LoadImageDataAsync(string filePath, int frame)
    {
        var dicomFile = await DicomFile.OpenAsync(filePath);
        var dataset = dicomFile.Dataset;

        int width = dataset.GetSingleValue<int>(DicomTag.Columns);
        int height = dataset.GetSingleValue<int>(DicomTag.Rows);

        var pixelData = DicomPixelData.Create(dataset);
        var frameData = pixelData.GetFrame(frame);
        var pixels = new ushort[width * height];
        Buffer.BlockCopy(frameData.Data, 0, pixels, 0, Math.Min(frameData.Data.Length, pixels.Length * 2));

        var pixelSpacing = GetPixelSpacing(dataset);
        double slope = dataset.GetSingleValueOrDefault(DicomTag.RescaleSlope, 1.0);
        double intercept = dataset.GetSingleValueOrDefault(DicomTag.RescaleIntercept, 0.0);

        return (pixels, width, height, pixelSpacing, (slope, intercept));
    }

    private static (double x, double y) GetPixelSpacing(DicomDataset dataset)
    {
        try
        {
            var pixelSpacing = dataset.GetValues<double>(DicomTag.PixelSpacing);
            return (pixelSpacing.Length > 0 ? pixelSpacing[0] : 1.0, 
                    pixelSpacing.Length > 1 ? pixelSpacing[1] : 1.0);
        }
        catch
        {
            return (1.0, 1.0);
        }
    }

    private static RoiStatistics CalculateStatistics(List<double> values, (double x, double y) pixelSpacing, double radiusX, double radiusY)
    {
        if (values.Count == 0)
            return new RoiStatistics(0, 0, 0, 0, 0, 0, 0, 0, 0, "HU");

        values.Sort();
        
        double sum = values.Sum();
        double mean = sum / values.Count;
        double variance = values.Average(v => Math.Pow(v - mean, 2));
        double stdDev = Math.Sqrt(variance);
        double median = values[values.Count / 2];
        double areaMm2 = Math.PI * radiusX * pixelSpacing.x * radiusY * pixelSpacing.y;

        return new RoiStatistics(
            mean,
            stdDev,
            values.Min(),
            values.Max(),
            median,
            values.Count,
            areaMm2,
            sum,
            variance,
            "HU"
        );
    }

    private static bool IsPointInPolygon(Point2D point, List<Point2D> polygon)
    {
        bool inside = false;
        int j = polygon.Count - 1;

        for (int i = 0; i < polygon.Count; i++)
        {
            if (((polygon[i].Y > point.Y) != (polygon[j].Y > point.Y)) &&
                (point.X < (polygon[j].X - polygon[i].X) * (point.Y - polygon[i].Y) / 
                 (polygon[j].Y - polygon[i].Y) + polygon[i].X))
            {
                inside = !inside;
            }
            j = i;
        }

        return inside;
    }

    private static string InterpretHU(double hu)
    {
        return hu switch
        {
            < -950 => "Air",
            < -50 => "Lung/Fat",
            < 20 => "Water/Fluid",
            < 70 => "Soft Tissue",
            < 200 => "Blood/Muscle",
            < 400 => "Calcification",
            _ => "Bone"
        };
    }

    #endregion
}
