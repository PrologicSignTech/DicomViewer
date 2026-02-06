using Microsoft.EntityFrameworkCore;
using MedView.Server.Data;
using MedView.Server.Models;
using MedView.Server.Models.DTOs;

namespace MedView.Server.Services;

public interface IMeasurementService
{
    Task<MeasurementDto?> GetByIdAsync(int id);
    Task<IEnumerable<MeasurementDto>> GetByInstanceIdAsync(int instanceId);
    Task<MeasurementDto> CreateAsync(int instanceId, CreateMeasurementDto dto);
    Task<MeasurementDto?> UpdateAsync(int id, CreateMeasurementDto dto);
    Task<bool> DeleteAsync(int id);
    Task<bool> ToggleVisibilityAsync(int id);
}

public class MeasurementService : IMeasurementService
{
    private readonly DicomDbContext _context;
    private readonly ILogger<MeasurementService> _logger;

    public MeasurementService(DicomDbContext context, ILogger<MeasurementService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<MeasurementDto?> GetByIdAsync(int id)
    {
        var measurement = await _context.Measurements.FindAsync(id);
        return measurement == null ? null : MapToDto(measurement);
    }

    public async Task<IEnumerable<MeasurementDto>> GetByInstanceIdAsync(int instanceId)
    {
        return await _context.Measurements
            .Where(m => m.InstanceId == instanceId)
            .Select(m => MapToDto(m))
            .ToListAsync();
    }

    public async Task<MeasurementDto> CreateAsync(int instanceId, CreateMeasurementDto dto)
    {
        var measurement = new Measurement
        {
            InstanceId = instanceId,
            Type = dto.Type,
            Value = dto.Value,
            Unit = dto.Unit,
            Label = dto.Label,
            Color = dto.Color,
            Mean = dto.Mean,
            StdDev = dto.StdDev,
            Min = dto.Min,
            Max = dto.Max,
            Area = dto.Area,
            PositionData = dto.PositionData,
            FrameNumber = dto.FrameNumber,
            IsVisible = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Measurements.Add(measurement);
        await _context.SaveChangesAsync();

        return MapToDto(measurement);
    }

    public async Task<MeasurementDto?> UpdateAsync(int id, CreateMeasurementDto dto)
    {
        var measurement = await _context.Measurements.FindAsync(id);
        if (measurement == null) return null;

        measurement.Type = dto.Type;
        measurement.Value = dto.Value;
        measurement.Unit = dto.Unit;
        measurement.Label = dto.Label;
        measurement.Color = dto.Color;
        measurement.Mean = dto.Mean;
        measurement.StdDev = dto.StdDev;
        measurement.Min = dto.Min;
        measurement.Max = dto.Max;
        measurement.Area = dto.Area;
        measurement.PositionData = dto.PositionData;
        measurement.FrameNumber = dto.FrameNumber;

        await _context.SaveChangesAsync();
        return MapToDto(measurement);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var measurement = await _context.Measurements.FindAsync(id);
        if (measurement == null) return false;

        _context.Measurements.Remove(measurement);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ToggleVisibilityAsync(int id)
    {
        var measurement = await _context.Measurements.FindAsync(id);
        if (measurement == null) return false;

        measurement.IsVisible = !measurement.IsVisible;
        await _context.SaveChangesAsync();
        return true;
    }

    private static MeasurementDto MapToDto(Measurement m) => new(
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
    );
}

public interface IAnnotationService
{
    Task<AnnotationDto?> GetByIdAsync(int id);
    Task<IEnumerable<AnnotationDto>> GetByInstanceIdAsync(int instanceId);
    Task<AnnotationDto> CreateAsync(int instanceId, CreateAnnotationDto dto);
    Task<AnnotationDto?> UpdateAsync(int id, CreateAnnotationDto dto);
    Task<bool> DeleteAsync(int id);
    Task<bool> ToggleVisibilityAsync(int id);
}

public class AnnotationService : IAnnotationService
{
    private readonly DicomDbContext _context;
    private readonly ILogger<AnnotationService> _logger;

    public AnnotationService(DicomDbContext context, ILogger<AnnotationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<AnnotationDto?> GetByIdAsync(int id)
    {
        var annotation = await _context.Annotations.FindAsync(id);
        return annotation == null ? null : MapToDto(annotation);
    }

    public async Task<IEnumerable<AnnotationDto>> GetByInstanceIdAsync(int instanceId)
    {
        return await _context.Annotations
            .Where(a => a.InstanceId == instanceId)
            .Select(a => MapToDto(a))
            .ToListAsync();
    }

    public async Task<AnnotationDto> CreateAsync(int instanceId, CreateAnnotationDto dto)
    {
        var annotation = new Annotation
        {
            InstanceId = instanceId,
            Type = dto.Type,
            Text = dto.Text,
            Color = dto.Color,
            FontSize = dto.FontSize,
            PositionData = dto.PositionData,
            FrameNumber = dto.FrameNumber,
            IsVisible = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Annotations.Add(annotation);
        await _context.SaveChangesAsync();

        return MapToDto(annotation);
    }

    public async Task<AnnotationDto?> UpdateAsync(int id, CreateAnnotationDto dto)
    {
        var annotation = await _context.Annotations.FindAsync(id);
        if (annotation == null) return null;

        annotation.Type = dto.Type;
        annotation.Text = dto.Text;
        annotation.Color = dto.Color;
        annotation.FontSize = dto.FontSize;
        annotation.PositionData = dto.PositionData;
        annotation.FrameNumber = dto.FrameNumber;

        await _context.SaveChangesAsync();
        return MapToDto(annotation);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var annotation = await _context.Annotations.FindAsync(id);
        if (annotation == null) return false;

        _context.Annotations.Remove(annotation);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ToggleVisibilityAsync(int id)
    {
        var annotation = await _context.Annotations.FindAsync(id);
        if (annotation == null) return false;

        annotation.IsVisible = !annotation.IsVisible;
        await _context.SaveChangesAsync();
        return true;
    }

    private static AnnotationDto MapToDto(Annotation a) => new(
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
    );
}
