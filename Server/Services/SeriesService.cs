using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MedView.Server.Data;
using MedView.Server.Models;
using MedView.Server.Models.DTOs;

namespace MedView.Server.Services;

public interface ISeriesService
{
    Task<SeriesDetailDto?> GetSeriesByIdAsync(int id);
    Task<SeriesDetailDto?> GetSeriesByUidAsync(string seriesInstanceUid);
    Task<IEnumerable<InstanceDto>> GetInstancesAsync(int seriesId);
    Task<InstanceDetailDto?> GetInstanceByIdAsync(int id);
    Task<InstanceDetailDto?> GetInstanceByUidAsync(string sopInstanceUid);
    Task<IEnumerable<string>> GetInstanceFilePathsAsync(int seriesId);
}

public class SeriesService : ISeriesService
{
    private readonly DicomDbContext _context;
    private readonly ILogger<SeriesService> _logger;
    private readonly IMemoryCache _cache;

    public SeriesService(
        DicomDbContext context, 
        ILogger<SeriesService> logger,
        IMemoryCache cache)
    {
        _context = context;
        _logger = logger;
        _cache = cache;
    }

    public async Task<SeriesDetailDto?> GetSeriesByIdAsync(int id)
    {
        // Cache series details to reduce RDS load (especially for 700+ image series)
        var cacheKey = $"series_detail_{id}";
        
        if (_cache.TryGetValue<SeriesDetailDto>(cacheKey, out var cachedSeries))
        {
            return cachedSeries;
        }

        // Use AsNoTracking and AsSplitQuery for optimal performance with large instance counts
        var series = await _context.Series
            .AsNoTracking()
            .AsSplitQuery() // Critical for series with 700+ instances
            .Include(s => s.Instances)
            .ThenInclude(i => i.Annotations)
            .Include(s => s.Instances)
            .ThenInclude(i => i.Measurements)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (series == null) return null;

        var result = MapToDetailDto(series);
        
        // Cache for 10 minutes
        _cache.Set(cacheKey, result, TimeSpan.FromMinutes(10));
        
        return result;
    }

    public async Task<SeriesDetailDto?> GetSeriesByUidAsync(string seriesInstanceUid)
    {
        // Cache series details
        var cacheKey = $"series_detail_uid_{seriesInstanceUid}";
        
        if (_cache.TryGetValue<SeriesDetailDto>(cacheKey, out var cachedSeries))
        {
            return cachedSeries;
        }

        // Use AsNoTracking and AsSplitQuery for optimal performance
        var series = await _context.Series
            .AsNoTracking()
            .AsSplitQuery() // Critical for series with 700+ instances
            .Include(s => s.Instances)
            .ThenInclude(i => i.Annotations)
            .Include(s => s.Instances)
            .ThenInclude(i => i.Measurements)
            .FirstOrDefaultAsync(s => s.SeriesInstanceUid == seriesInstanceUid);

        if (series == null) return null;

        var result = MapToDetailDto(series);
        
        // Cache for 10 minutes
        _cache.Set(cacheKey, result, TimeSpan.FromMinutes(10));
        
        return result;
    }

    private SeriesDetailDto MapToDetailDto(Series series)
    {
        return new SeriesDetailDto(
            series.Id,
            series.SeriesInstanceUid,
            series.SeriesNumber,
            series.SeriesDescription,
            series.Modality,
            series.SeriesDate,
            series.BodyPartExamined,
            series.ProtocolName,
            series.Rows,
            series.Columns,
            series.SliceThickness,
            series.NumberOfInstances,
            series.Instances
                .OrderBy(i => i.InstanceNumber ?? i.Id)
                .Select(i => new InstanceDto(
                    i.Id,
                    i.SopInstanceUid,
                    i.SopClassUid,
                    i.InstanceNumber,
                    i.Rows,
                    i.Columns,
                    i.WindowCenter,
                    i.WindowWidth,
                    i.RescaleIntercept,
                    i.RescaleSlope,
                    i.NumberOfFrames,
                    $"/api/instances/{i.Id}/thumbnail",
                    $"/api/instances/{i.Id}/image"
                ))
        );
    }

    public async Task<IEnumerable<InstanceDto>> GetInstancesAsync(int seriesId)
    {
        // Use AsNoTracking for read-only query with projection to minimize data transfer
        return await _context.Instances
            .AsNoTracking()
            .Where(i => i.SeriesId == seriesId)
            .OrderBy(i => i.InstanceNumber ?? i.Id)
            .Select(i => new InstanceDto(
                i.Id,
                i.SopInstanceUid,
                i.SopClassUid,
                i.InstanceNumber,
                i.Rows,
                i.Columns,
                i.WindowCenter,
                i.WindowWidth,
                i.RescaleIntercept,
                i.RescaleSlope,
                i.NumberOfFrames,
                $"/api/instances/{i.Id}/thumbnail",
                $"/api/instances/{i.Id}/image"
            ))
            .ToListAsync();
    }

    public async Task<InstanceDetailDto?> GetInstanceByIdAsync(int id)
    {
        // Cache instance details to reduce repeated queries
        var cacheKey = $"instance_detail_{id}";
        
        if (_cache.TryGetValue<InstanceDetailDto>(cacheKey, out var cachedInstance))
        {
            return cachedInstance;
        }

        // Use AsNoTracking for read-only query
        var instance = await _context.Instances
            .AsNoTracking()
            .Include(i => i.Annotations)
            .Include(i => i.Measurements)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (instance == null) return null;

        var result = MapInstanceToDetailDto(instance);
        
        // Cache for 15 minutes
        _cache.Set(cacheKey, result, TimeSpan.FromMinutes(15));
        
        return result;
    }

    public async Task<InstanceDetailDto?> GetInstanceByUidAsync(string sopInstanceUid)
    {
        // Cache instance details
        var cacheKey = $"instance_detail_uid_{sopInstanceUid}";
        
        if (_cache.TryGetValue<InstanceDetailDto>(cacheKey, out var cachedInstance))
        {
            return cachedInstance;
        }

        // Use AsNoTracking for read-only query
        var instance = await _context.Instances
            .AsNoTracking()
            .Include(i => i.Annotations)
            .Include(i => i.Measurements)
            .FirstOrDefaultAsync(i => i.SopInstanceUid == sopInstanceUid);

        if (instance == null) return null;

        var result = MapInstanceToDetailDto(instance);
        
        // Cache for 15 minutes
        _cache.Set(cacheKey, result, TimeSpan.FromMinutes(15));
        
        return result;
    }

    private InstanceDetailDto MapInstanceToDetailDto(Instance instance)
    {
        return new InstanceDetailDto(
            instance.Id,
            instance.SopInstanceUid,
            instance.SopClassUid,
            instance.InstanceNumber,
            instance.Rows,
            instance.Columns,
            instance.BitsAllocated,
            instance.BitsStored,
            instance.PhotometricInterpretation,
            instance.WindowCenter,
            instance.WindowWidth,
            instance.RescaleIntercept,
            instance.RescaleSlope,
            instance.PixelSpacing,
            instance.ImagePositionPatient,
            instance.ImageOrientationPatient,
            instance.SliceLocation,
            instance.NumberOfFrames,
            instance.FrameTime,
            instance.TransferSyntaxUid,
            instance.Annotations.Select(a => new AnnotationDto(
                a.Id,
                a.Type,
                a.Text,
                a.Color,
                a.FontSize,
                a.IsVisible,
                a.PositionData,
                a.FrameNumber,
                a.CreatedAt,
                a.CreatedBy
            )),
            instance.Measurements.Select(m => new MeasurementDto(
                m.Id,
                m.Type,
                m.Value,
                m.Unit,
                m.Label,
                m.Color,
                m.IsVisible,
                m.Mean,
                m.StdDev,
                m.Min,
                m.Max,
                m.Area,
                m.PositionData,
                m.FrameNumber,
                m.CreatedAt,
                m.CreatedBy
            ))
        );
    }

    public async Task<IEnumerable<string>> GetInstanceFilePathsAsync(int seriesId)
    {
        // Use AsNoTracking and projection to only fetch file paths - minimal data transfer from RDS
        return await _context.Instances
            .AsNoTracking()
            .Where(i => i.SeriesId == seriesId && i.FilePath != null)
            .OrderBy(i => i.SliceLocation ?? i.InstanceNumber ?? i.Id)
            .Select(i => i.FilePath!)
            .ToListAsync();
    }
}
