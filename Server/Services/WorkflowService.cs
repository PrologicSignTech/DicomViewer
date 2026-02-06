using Microsoft.EntityFrameworkCore;
using MedView.Server.Data;
using MedView.Server.Models;
using System.Text.Json;

namespace MedView.Server.Services;

public interface IWorkflowService
{
    Task<StudyComparisonResult> CompareStudiesAsync(int currentStudyId, int? priorStudyId = null);
    Task<List<WorkflowStudyDto>> FindPriorStudiesAsync(int currentStudyId, int maxResults = 5);
    Task<HangingProtocolResult> ApplyHangingProtocolAsync(int studyId, int? protocolId = null);
    Task<List<HangingProtocol>> GetAvailableProtocolsAsync(string? modality = null, string? bodyPart = null);
    Task<HangingProtocol> SaveHangingProtocolAsync(HangingProtocol protocol);
    Task<SeriesSyncResult> SynchronizeSeriesAsync(List<int> seriesIds, SyncMode mode);
    Task<StudyBookmark> CreateBookmarkAsync(int studyId, BookmarkCreateRequest request);
    Task<List<StudyBookmark>> GetBookmarksAsync(int studyId);
    Task DeleteBookmarkAsync(int bookmarkId);
    Task<KeyImage> MarkAsKeyImageAsync(int instanceId, KeyImageRequest request);
    Task<List<KeyImage>> GetKeyImagesAsync(int studyId);
    Task DeleteKeyImageAsync(int keyImageId);
    Task<LayoutConfiguration> GetLayoutAsync(string layoutName);
    Task<LayoutConfiguration> SaveLayoutAsync(LayoutConfiguration layout);
    Task<List<LayoutConfiguration>> GetAvailableLayoutsAsync();
    Task<List<WorklistItem>> GetWorklistAsync(WorklistQuery query);
    Task<WorklistItem> UpdateWorklistItemStatusAsync(int itemId, string status);
}

public record WorkflowStudyDto(int Id, string StudyInstanceUid, string? PatientName, string? StudyDescription, DateTime? StudyDate, string? Modality);
public record StudyComparisonResult(WorkflowStudyDto CurrentStudy, WorkflowStudyDto? PriorStudy, TimeSpan? TimeDifference, List<SeriesComparisonPair> MatchedSeries, bool HasPrior);
public record SeriesComparisonPair(int CurrentSeriesId, int? PriorSeriesId, string? SeriesDescription, string? Modality, double MatchConfidence);
public record HangingProtocolResult(int ProtocolId, string ProtocolName, List<ViewportAssignment> ViewportAssignments, string LayoutType);
public record ViewportAssignment(int ViewportIndex, int Row, int Column, int? SeriesId, int? InstanceId, string? DisplayPreset);
public enum SyncMode { Position, Frame, WindowLevel, Zoom, All }
public record SeriesSyncResult(List<int> SyncedSeriesIds, SyncMode Mode, Dictionary<string, object> SyncState);

public class StudyBookmark
{
    public int Id { get; set; }
    public int StudyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string BookmarkData { get; set; } = "{}";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
}

public record BookmarkCreateRequest(string Name, string? Description, List<ViewportState> ViewportStates);
public record ViewportState(int ViewportIndex, int? SeriesId, int? InstanceId, int Frame, double WindowCenter, double WindowWidth, double Zoom, double PanX, double PanY, double Rotation, bool FlipH, bool FlipV, bool Invert);

public class KeyImage
{
    public int Id { get; set; }
    public int InstanceId { get; set; }
    public int StudyId { get; set; }
    public int? Frame { get; set; }
    public string? Description { get; set; }
    public string? Category { get; set; }
    public string? ThumbnailPath { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
}

public record KeyImageRequest(int? Frame, string? Description, string? Category, double? WindowCenter, double? WindowWidth);

public class LayoutConfiguration
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string LayoutType { get; set; } = "1x1";
    public int Rows { get; set; } = 1;
    public int Columns { get; set; } = 1;
    public string ViewportConfig { get; set; } = "{}";
    public bool IsDefault { get; set; }
    public string? ForModality { get; set; }
}

public class WorklistItem
{
    public int Id { get; set; }
    public int StudyId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string? PatientId { get; set; }
    public string? AccessionNumber { get; set; }
    public string? Modality { get; set; }
    public string? StudyDescription { get; set; }
    public DateTime? StudyDate { get; set; }
    public string Status { get; set; } = "New";
    public string? Priority { get; set; }
    public string? AssignedTo { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReadAt { get; set; }
}

public record WorklistQuery(string? Status, string? Priority, string? Modality, string? AssignedTo, DateTime? DateFrom, DateTime? DateTo, int Page = 1, int PageSize = 50);

public class WorkflowService : IWorkflowService
{
    private readonly DicomDbContext _context;
    private readonly ILogger<WorkflowService> _logger;

    public WorkflowService(DicomDbContext context, ILogger<WorkflowService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<StudyComparisonResult> CompareStudiesAsync(int currentStudyId, int? priorStudyId = null)
    {
        var currentStudy = await _context.Studies.Include(s => s.Series).FirstOrDefaultAsync(s => s.Id == currentStudyId);
        if (currentStudy == null) throw new ArgumentException("Study not found");

        Study? priorStudy = priorStudyId.HasValue 
            ? await _context.Studies.Include(s => s.Series).FirstOrDefaultAsync(s => s.Id == priorStudyId.Value)
            : await _context.Studies.Include(s => s.Series)
                .Where(s => s.PatientId == currentStudy.PatientId && s.Id != currentStudyId && s.StudyDate < currentStudy.StudyDate)
                .OrderByDescending(s => s.StudyDate).FirstOrDefaultAsync();

        var matchedSeries = currentStudy.Series.Select(currentSeries =>
        {
            var matched = priorStudy?.Series.Where(s => s.Modality == currentSeries.Modality)
                .OrderByDescending(s => CalculateSeriesMatchScore(currentSeries, s)).FirstOrDefault();
            return new SeriesComparisonPair(currentSeries.Id, matched?.Id, currentSeries.SeriesDescription, currentSeries.Modality,
                matched != null ? CalculateSeriesMatchScore(currentSeries, matched) : 0);
        }).ToList();

        return new StudyComparisonResult(MapToStudyDto(currentStudy), priorStudy != null ? MapToStudyDto(priorStudy) : null,
            priorStudy?.StudyDate != null && currentStudy.StudyDate != null ? currentStudy.StudyDate.Value - priorStudy.StudyDate.Value : null,
            matchedSeries, priorStudy != null);
    }

    public async Task<List<WorkflowStudyDto>> FindPriorStudiesAsync(int currentStudyId, int maxResults = 5)
    {
        var currentStudy = await _context.Studies.FindAsync(currentStudyId);
        if (currentStudy == null) return new List<WorkflowStudyDto>();
        return await _context.Studies.Where(s => s.PatientId == currentStudy.PatientId && s.Id != currentStudyId)
            .OrderByDescending(s => s.StudyDate).Take(maxResults).Select(s => MapToStudyDto(s)).ToListAsync();
    }

    public async Task<HangingProtocolResult> ApplyHangingProtocolAsync(int studyId, int? protocolId = null)
    {
        var study = await _context.Studies.Include(s => s.Series).ThenInclude(ser => ser.Instances).FirstOrDefaultAsync(s => s.Id == studyId);
        if (study == null) throw new ArgumentException("Study not found");

        var modality = study.Series.FirstOrDefault()?.Modality;
        var protocol = protocolId.HasValue 
            ? await _context.HangingProtocols.FindAsync(protocolId.Value)
            : await _context.HangingProtocols.Where(p => p.IsActive)
                .OrderByDescending(p => (p.Modality == modality ? 10 : 0) + p.Priority).FirstOrDefaultAsync();

        if (protocol == null)
            return new HangingProtocolResult(0, "Default", new List<ViewportAssignment> {
                new(0, 0, 0, study.Series.FirstOrDefault()?.Id, study.Series.FirstOrDefault()?.Instances.FirstOrDefault()?.Id, null)
            }, "1x1");

        var layoutConfig = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(protocol.LayoutConfig);
        var viewports = new List<ViewportAssignment>();
        
        if (layoutConfig?.TryGetValue("viewports", out var viewportsJson) == true)
        {
            foreach (var (vp, i) in viewportsJson.EnumerateArray().Select((v, i) => (v, i)))
            {
                var pos = vp.GetProperty("position").GetInt32();
                int? seriesId = null, instanceId = null;
                
                if (vp.TryGetProperty("seriesIndex", out var idx) && idx.GetInt32() < study.Series.Count)
                {
                    var series = study.Series.ElementAt(idx.GetInt32());
                    seriesId = series.Id;
                    instanceId = series.Instances.FirstOrDefault()?.Id;
                }
                viewports.Add(new ViewportAssignment(pos, pos / 2, pos % 2, seriesId, instanceId, null));
            }
        }

        var rows = layoutConfig?.TryGetValue("rows", out var r) == true ? r.GetInt32() : 1;
        var cols = layoutConfig?.TryGetValue("columns", out var c) == true ? c.GetInt32() : 1;
        return new HangingProtocolResult(protocol.Id, protocol.Name, viewports, $"{rows}x{cols}");
    }

    public async Task<List<HangingProtocol>> GetAvailableProtocolsAsync(string? modality = null, string? bodyPart = null)
    {
        var query = _context.HangingProtocols.Where(p => p.IsActive);
        if (!string.IsNullOrEmpty(modality)) query = query.Where(p => p.Modality == null || p.Modality == modality);
        if (!string.IsNullOrEmpty(bodyPart)) query = query.Where(p => p.BodyPart == null || p.BodyPart == bodyPart);
        return await query.OrderByDescending(p => p.Priority).ToListAsync();
    }

    public async Task<HangingProtocol> SaveHangingProtocolAsync(HangingProtocol protocol)
    {
        if (protocol.Id == 0) _context.HangingProtocols.Add(protocol);
        else _context.HangingProtocols.Update(protocol);
        await _context.SaveChangesAsync();
        return protocol;
    }

    public async Task<SeriesSyncResult> SynchronizeSeriesAsync(List<int> seriesIds, SyncMode mode)
    {
        var syncState = new Dictionary<string, object>();
        if (seriesIds.Count == 0) return new SeriesSyncResult(seriesIds, mode, syncState);
        
        var refSeries = await _context.Series.Include(s => s.Instances).FirstOrDefaultAsync(s => s.Id == seriesIds[0]);
        var refInstance = refSeries?.Instances.FirstOrDefault();
        if (refInstance != null)
        {
            syncState["referenceSeriesId"] = refSeries!.Id;
            syncState["sliceLocation"] = refInstance.SliceLocation ?? 0;
            syncState["windowCenter"] = refInstance.WindowCenter ?? 40;
            syncState["windowWidth"] = refInstance.WindowWidth ?? 400;
        }
        return new SeriesSyncResult(seriesIds, mode, syncState);
    }

    public async Task<StudyBookmark> CreateBookmarkAsync(int studyId, BookmarkCreateRequest request)
    {
        var bookmark = new StudyBookmark { StudyId = studyId, Name = request.Name, Description = request.Description,
            BookmarkData = JsonSerializer.Serialize(request.ViewportStates), CreatedAt = DateTime.UtcNow };
        _context.Set<StudyBookmark>().Add(bookmark);
        await _context.SaveChangesAsync();
        return bookmark;
    }

    public async Task<List<StudyBookmark>> GetBookmarksAsync(int studyId) =>
        await _context.Set<StudyBookmark>().Where(b => b.StudyId == studyId).OrderByDescending(b => b.CreatedAt).ToListAsync();

    public async Task DeleteBookmarkAsync(int bookmarkId)
    {
        var bookmark = await _context.Set<StudyBookmark>().FindAsync(bookmarkId);
        if (bookmark != null) { _context.Set<StudyBookmark>().Remove(bookmark); await _context.SaveChangesAsync(); }
    }

    public async Task<KeyImage> MarkAsKeyImageAsync(int instanceId, KeyImageRequest request)
    {
        var instance = await _context.Instances.Include(i => i.Series).FirstOrDefaultAsync(i => i.Id == instanceId);
        if (instance == null) throw new ArgumentException("Instance not found");
        
        var keyImage = new KeyImage { InstanceId = instanceId, StudyId = instance.Series.StudyId, Frame = request.Frame,
            Description = request.Description, Category = request.Category, CreatedAt = DateTime.UtcNow };
        _context.Set<KeyImage>().Add(keyImage);
        await _context.SaveChangesAsync();
        return keyImage;
    }

    public async Task<List<KeyImage>> GetKeyImagesAsync(int studyId) =>
        await _context.Set<KeyImage>().Where(k => k.StudyId == studyId).OrderByDescending(k => k.CreatedAt).ToListAsync();

    public async Task DeleteKeyImageAsync(int keyImageId)
    {
        var keyImage = await _context.Set<KeyImage>().FindAsync(keyImageId);
        if (keyImage != null) { _context.Set<KeyImage>().Remove(keyImage); await _context.SaveChangesAsync(); }
    }

    public async Task<LayoutConfiguration> GetLayoutAsync(string layoutName) =>
        await _context.Set<LayoutConfiguration>().FirstOrDefaultAsync(l => l.Name == layoutName)
        ?? new LayoutConfiguration { Name = "Default", LayoutType = "1x1", Rows = 1, Columns = 1 };

    public async Task<LayoutConfiguration> SaveLayoutAsync(LayoutConfiguration layout)
    {
        if (layout.Id == 0) _context.Set<LayoutConfiguration>().Add(layout);
        else _context.Set<LayoutConfiguration>().Update(layout);
        await _context.SaveChangesAsync();
        return layout;
    }

    public async Task<List<LayoutConfiguration>> GetAvailableLayoutsAsync() =>
        await _context.Set<LayoutConfiguration>().ToListAsync();

    public async Task<List<WorklistItem>> GetWorklistAsync(WorklistQuery query)
    {
        var q = _context.Set<WorklistItem>().AsQueryable();
        if (!string.IsNullOrEmpty(query.Status)) q = q.Where(w => w.Status == query.Status);
        if (!string.IsNullOrEmpty(query.Priority)) q = q.Where(w => w.Priority == query.Priority);
        if (!string.IsNullOrEmpty(query.Modality)) q = q.Where(w => w.Modality == query.Modality);
        if (!string.IsNullOrEmpty(query.AssignedTo)) q = q.Where(w => w.AssignedTo == query.AssignedTo);
        if (query.DateFrom.HasValue) q = q.Where(w => w.StudyDate >= query.DateFrom.Value);
        if (query.DateTo.HasValue) q = q.Where(w => w.StudyDate <= query.DateTo.Value);
        return await q.OrderByDescending(w => w.Priority == "Stat" ? 3 : w.Priority == "Urgent" ? 2 : 1)
            .ThenByDescending(w => w.CreatedAt).Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync();
    }

    public async Task<WorklistItem> UpdateWorklistItemStatusAsync(int itemId, string status)
    {
        var item = await _context.Set<WorklistItem>().FindAsync(itemId) ?? throw new ArgumentException("Worklist item not found");
        item.Status = status;
        if (status == "Read") item.ReadAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return item;
    }

    private static double CalculateSeriesMatchScore(Series current, Series prior)
    {
        double score = current.Modality == prior.Modality ? 0.4 : 0;
        if (!string.IsNullOrEmpty(current.SeriesDescription) && !string.IsNullOrEmpty(prior.SeriesDescription))
            score += current.SeriesDescription.Equals(prior.SeriesDescription, StringComparison.OrdinalIgnoreCase) ? 0.4 : 0.2;
        if (current.BodyPartExamined == prior.BodyPartExamined) score += 0.2;
        return score;
    }

    private static WorkflowStudyDto MapToStudyDto(Study study) =>
        new(study.Id, study.StudyInstanceUid, study.PatientName, study.StudyDescription, study.StudyDate, study.Series.FirstOrDefault()?.Modality);
}
