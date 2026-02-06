using FellowOakDicom;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MedView.Server.Data;
using MedView.Server.Models;
using MedView.Server.Models.DTOs;

namespace MedView.Server.Services;

public interface IStudyService
{
    Task<PagedResultDto<StudyDto>> SearchStudiesAsync(StudySearchDto search);
    Task<StudyDetailDto?> GetStudyByIdAsync(int id);
    Task<StudyDetailDto?> GetStudyByUidAsync(string studyInstanceUid);
    Task<UploadResultDto> ProcessUploadedFilesAsync(IEnumerable<string> filePaths);
    Task<bool> DeleteStudyAsync(int id);
    Task UpdateLastAccessedAsync(int studyId);
}

public class StudyService : IStudyService
{
    private readonly DicomDbContext _context;
    private readonly IDicomImageService _dicomImageService;
    private readonly ILogger<StudyService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IMemoryCache _cache;
    private readonly IEncryptionService _encryptionService;

    public StudyService(
        DicomDbContext context,
        IDicomImageService dicomImageService,
        ILogger<StudyService> logger,
        IConfiguration configuration,
        IMemoryCache cache,
        IEncryptionService encryptionService)
    {
        _context = context;
        _dicomImageService = dicomImageService;
        _logger = logger;
        _configuration = configuration;
        _cache = cache;
        _encryptionService = encryptionService;
    }

    public async Task<PagedResultDto<StudyDto>> SearchStudiesAsync(StudySearchDto search)
    {
        // Use AsNoTracking for read-only queries to reduce memory and improve performance
        var query = _context.Studies.AsNoTracking().AsQueryable();

        if (!string.IsNullOrEmpty(search.PatientId))
            query = query.Where(s => s.PatientId != null && s.PatientId.Contains(search.PatientId));

        if (!string.IsNullOrEmpty(search.PatientName))
            query = query.Where(s => s.PatientName != null && s.PatientName.Contains(search.PatientName));

        if (!string.IsNullOrEmpty(search.StudyDescription))
            query = query.Where(s => s.StudyDescription != null && s.StudyDescription.Contains(search.StudyDescription));

        if (!string.IsNullOrEmpty(search.AccessionNumber))
            query = query.Where(s => s.AccessionNumber == search.AccessionNumber);

        if (search.StudyDateFrom.HasValue)
            query = query.Where(s => s.StudyDate >= search.StudyDateFrom.Value);

        if (search.StudyDateTo.HasValue)
            query = query.Where(s => s.StudyDate <= search.StudyDateTo.Value);

        if (!string.IsNullOrEmpty(search.Modality))
        {
            query = query.Where(s => s.Series.Any(ser => ser.Modality == search.Modality));
        }

        // Execute count query separately to optimize performance
        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)search.PageSize);

        // Use projection to select only needed fields - reduces data transfer from RDS
        var studies = await query
            .OrderByDescending(s => s.StudyDate)
            .ThenByDescending(s => s.CreatedAt)
            .Skip((search.Page - 1) * search.PageSize)
            .Take(search.PageSize)
            .ToListAsync();

        // Add encrypted UIDs to each study
        var studiesWithEncryption = studies.Select(s => new StudyDto(
            s.Id,
            s.StudyInstanceUid,
            s.StudyId,
            s.StudyDescription,
            s.StudyDate,
            s.AccessionNumber,
            s.PatientId,
            s.PatientName,
            s.PatientBirthDate,
            s.PatientSex,
            s.PatientAge,
            s.InstitutionName,
            s.NumberOfSeries,
            s.NumberOfInstances,
            s.CreatedAt,
            _encryptionService.EncryptStudyUid(s.StudyInstanceUid)
        ))
        .ToList();

        return new PagedResultDto<StudyDto>(
            studiesWithEncryption,
            totalCount,
            search.Page,
            search.PageSize,
            totalPages
        );
    }

    public async Task<StudyDetailDto?> GetStudyByIdAsync(int id)
    {
        // Use caching for frequently accessed studies to reduce RDS load
        var cacheKey = $"study_detail_{id}";
        
        if (_cache.TryGetValue<StudyDetailDto>(cacheKey, out var cachedStudy))
        {
            return cachedStudy;
        }

        // Use AsNoTracking with split query for better performance on large datasets
        var study = await _context.Studies
            .AsNoTracking()
            .AsSplitQuery() // Prevents cartesian explosion with 700+ images
            .Include(s => s.Series)
            .ThenInclude(ser => ser.Instances)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (study == null) return null;

        // Update last accessed asynchronously without blocking
        _ = Task.Run(() => UpdateLastAccessedAsync(id));

        var result = MapToDetailDto(study);
        
        // Cache for 5 minutes to reduce repeated RDS queries
        _cache.Set(cacheKey, result, TimeSpan.FromMinutes(5));
        
        return result;
    }

    public async Task<StudyDetailDto?> GetStudyByUidAsync(string studyInstanceUid)
    {
        // Use caching for frequently accessed studies
        var cacheKey = $"study_detail_uid_{studyInstanceUid}";
        
        if (_cache.TryGetValue<StudyDetailDto>(cacheKey, out var cachedStudy))
        {
            return cachedStudy;
        }

        // Use AsNoTracking with split query for better performance
        var study = await _context.Studies
            .AsNoTracking()
            .AsSplitQuery() // Critical for 700+ images to avoid cartesian explosion
            .Include(s => s.Series)
            .ThenInclude(ser => ser.Instances)
            .FirstOrDefaultAsync(s => s.StudyInstanceUid == studyInstanceUid);

        if (study == null) return null;

        // Update last accessed asynchronously without blocking
        _ = Task.Run(() => UpdateLastAccessedAsync(study.Id));

        var result = MapToDetailDto(study);
        
        // Cache for 5 minutes
        _cache.Set(cacheKey, result, TimeSpan.FromMinutes(5));
        
        return result;
    }

    private StudyDetailDto MapToDetailDto(Study study)
    {
        return new StudyDetailDto(
            study.Id,
            study.StudyInstanceUid,
            study.StudyId,
            study.StudyDescription,
            study.StudyDate,
            study.AccessionNumber,
            study.PatientId,
            study.PatientName,
            study.PatientBirthDate,
            study.PatientSex,
            study.PatientAge,
            study.InstitutionName,
            study.ReferringPhysicianName,
            study.NumberOfSeries,
            study.NumberOfInstances,
            study.Series.OrderBy(s => int.TryParse(s.SeriesNumber, out var n) ? n : 0).Select(ser => new SeriesDto(
                ser.Id,
                ser.SeriesInstanceUid,
                ser.SeriesNumber,
                ser.SeriesDescription,
                ser.Modality,
                ser.SeriesDate,
                ser.BodyPartExamined,
                ser.Rows,
                ser.Columns,
                ser.NumberOfInstances,
                $"/api/series/{ser.Id}/thumbnail"
            )),
            _encryptionService.EncryptStudyUid(study.StudyInstanceUid)
        );
    }

    public async Task<UploadResultDto> ProcessUploadedFilesAsync(IEnumerable<string> filePaths)
    {
        var errors = new List<string>();
        var processedStudies = new Dictionary<string, Study>();
        var processedSeries = new Dictionary<string, Series>();
        var instancesToAdd = new List<Instance>();
        int instancesProcessed = 0;

        var storagePath = _configuration["DicomSettings:StoragePath"] ?? "./DicomStorage";
        Directory.CreateDirectory(storagePath);

        // Process all files first without hitting database repeatedly
        foreach (var filePath in filePaths)
        {
            try
            {
                var dicomFile = await _dicomImageService.OpenFileAsync(filePath);
                var dataset = dicomFile.Dataset;

                // Extract Study UID
                var studyUid = dataset.GetSingleValueOrDefault(DicomTag.StudyInstanceUID, Guid.NewGuid().ToString());

                // Get or create study
                if (!processedStudies.TryGetValue(studyUid, out var study))
                {
                    // Check database only once per unique study
                    study = await _context.Studies
                        .AsNoTracking()
                        .FirstOrDefaultAsync(s => s.StudyInstanceUid == studyUid);
                    
                    if (study == null)
                    {
                        study = new Study
                        {
                            StudyInstanceUid = studyUid,
                            StudyId = dataset.GetSingleValueOrDefault<string>(DicomTag.StudyID, null),
                            StudyDescription = dataset.GetSingleValueOrDefault<string>(DicomTag.StudyDescription, null),
                            StudyDate = ParseDicomDate(dataset.GetSingleValueOrDefault<string>(DicomTag.StudyDate, null)),
                            StudyTime = dataset.GetSingleValueOrDefault<string>(DicomTag.StudyTime, null),
                            AccessionNumber = dataset.GetSingleValueOrDefault<string>(DicomTag.AccessionNumber, null),
                            ReferringPhysicianName = dataset.GetSingleValueOrDefault<string>(DicomTag.ReferringPhysicianName, null),
                            PatientId = dataset.GetSingleValueOrDefault<string>(DicomTag.PatientID, null),
                            PatientName = dataset.GetSingleValueOrDefault<string>(DicomTag.PatientName, null),
                            PatientBirthDate = ParseDicomDate(dataset.GetSingleValueOrDefault<string>(DicomTag.PatientBirthDate, null)),
                            PatientSex = dataset.GetSingleValueOrDefault<string>(DicomTag.PatientSex, null),
                            PatientAge = dataset.GetSingleValueOrDefault<string>(DicomTag.PatientAge, null),
                            InstitutionName = dataset.GetSingleValueOrDefault<string>(DicomTag.InstitutionName, null),
                            CreatedAt = DateTime.UtcNow
                        };
                        _context.Studies.Add(study);
                        // Save immediately to get ID for foreign key relationships
                        await _context.SaveChangesAsync();
                    }
                    processedStudies[studyUid] = study;
                }

                // Extract Series UID
                var seriesUid = dataset.GetSingleValueOrDefault(DicomTag.SeriesInstanceUID, Guid.NewGuid().ToString());

                // Get or create series
                var seriesKey = $"{studyUid}_{seriesUid}";
                if (!processedSeries.TryGetValue(seriesKey, out var series))
                {
                    series = await _context.Series
                        .AsNoTracking()
                        .FirstOrDefaultAsync(s => s.SeriesInstanceUid == seriesUid);
                    
                    if (series == null)
                    {
                        series = new Series
                        {
                            SeriesInstanceUid = seriesUid,
                            SeriesNumber = dataset.GetSingleValueOrDefault<string>(DicomTag.SeriesNumber, null),
                            SeriesDescription = dataset.GetSingleValueOrDefault<string>(DicomTag.SeriesDescription, null),
                            Modality = dataset.GetSingleValueOrDefault<string>(DicomTag.Modality, null),
                            SeriesDate = ParseDicomDate(dataset.GetSingleValueOrDefault<string>(DicomTag.SeriesDate, null)),
                            SeriesTime = dataset.GetSingleValueOrDefault<string>(DicomTag.SeriesTime, null),
                            BodyPartExamined = dataset.GetSingleValueOrDefault<string>(DicomTag.BodyPartExamined, null),
                            ProtocolName = dataset.GetSingleValueOrDefault<string>(DicomTag.ProtocolName, null),
                            Rows = dataset.GetSingleValueOrDefault<int?>(DicomTag.Rows, null),
                            Columns = dataset.GetSingleValueOrDefault<int?>(DicomTag.Columns, null),
                            SliceThickness = dataset.GetSingleValueOrDefault<double?>(DicomTag.SliceThickness, null),
                            SpacingBetweenSlices = dataset.GetSingleValueOrDefault<double?>(DicomTag.SpacingBetweenSlices, null),
                            StudyId = study.Id
                        };
                        _context.Series.Add(series);
                        // Save immediately to get ID for foreign key relationships
                        await _context.SaveChangesAsync();
                    }
                    processedSeries[seriesKey] = series;
                }

                // Check if instance already exists
                var sopInstanceUid = dataset.GetSingleValueOrDefault(DicomTag.SOPInstanceUID, Guid.NewGuid().ToString());
                var existingInstance = await _context.Instances
                    .AsNoTracking()
                    .AnyAsync(i => i.SopInstanceUid == sopInstanceUid);

                if (!existingInstance)
                {
                    // Copy file to permanent storage
                    var permanentPath = Path.Combine(storagePath, studyUid, seriesUid, $"{sopInstanceUid}.dcm");
                    Directory.CreateDirectory(Path.GetDirectoryName(permanentPath)!);
                    File.Copy(filePath, permanentPath, true);

                    // Extract instance metadata
                    var instance = await _dicomImageService.ExtractMetadataAsync(dicomFile, permanentPath);
                    instance.SeriesId = series.Id;
                    
                    instancesToAdd.Add(instance);
                    instancesProcessed++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing file: {FilePath}", filePath);
                errors.Add($"Error processing {Path.GetFileName(filePath)}: {ex.Message}");
            }
        }

        // Bulk insert all instances at once to minimize RDS round-trips
        if (instancesToAdd.Any())
        {
            _context.Instances.AddRange(instancesToAdd);
            await _context.SaveChangesAsync();
        }

        // Batch update counts for all processed studies and series
        foreach (var study in processedStudies.Values)
        {
            study.NumberOfSeries = await _context.Series.CountAsync(s => s.StudyId == study.Id);
            study.NumberOfInstances = await _context.Instances
                .Where(i => i.Series.StudyId == study.Id)
                .CountAsync();
        }

        foreach (var series in processedSeries.Values)
        {
            series.NumberOfInstances = await _context.Instances.CountAsync(i => i.SeriesId == series.Id);
        }

        // Single SaveChanges for all count updates
        await _context.SaveChangesAsync();
        
        // Invalidate cache for updated studies
        foreach (var study in processedStudies.Values)
        {
            _cache.Remove($"study_detail_{study.Id}");
            _cache.Remove($"study_detail_uid_{study.StudyInstanceUid}");
        }

        var resultStudies = processedStudies.Values.Select(s => new StudyDto(
            s.Id,
            s.StudyInstanceUid,
            s.StudyId,
            s.StudyDescription,
            s.StudyDate,
            s.AccessionNumber,
            s.PatientId,
            s.PatientName,
            s.PatientBirthDate,
            s.PatientSex,
            s.PatientAge,
            s.InstitutionName,
            s.NumberOfSeries,
            s.NumberOfInstances,
            s.CreatedAt,
            _encryptionService.EncryptStudyUid(s.StudyInstanceUid)
        )).ToList();

        return new UploadResultDto(
            errors.Count == 0,
            errors.Count == 0 ? "Upload successful" : "Upload completed with errors",
            processedStudies.Count,
            processedSeries.Count,
            instancesProcessed,
            errors.Count > 0 ? errors : null,
            resultStudies
        );
    }

    private DateTime? ParseDicomDate(string? dateString)
    {
        if (string.IsNullOrEmpty(dateString)) return null;
        
        if (DateTime.TryParseExact(dateString, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out var date))
            return date;
        
        if (DateTime.TryParse(dateString, out date))
            return date;
            
        return null;
    }

    public async Task<bool> DeleteStudyAsync(int id)
    {
        // First load the study to get StudyInstanceUid for file deletion and to check existence
        var studyUid = await _context.Studies
            .AsNoTracking()
            .Where(s => s.Id == id)
            .Select(s => s.StudyInstanceUid)
            .FirstOrDefaultAsync();

        if (studyUid == null) return false;

        // Delete files
        var storagePath = _configuration["DicomSettings:StoragePath"] ?? "./DicomStorage";
        var studyPath = Path.Combine(storagePath, studyUid);
        
        if (Directory.Exists(studyPath))
        {
            try
            {
                Directory.Delete(studyPath, true);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete study files: {Path}", studyPath);
            }
        }

        // Try to use ExecuteDeleteAsync for efficient bulk delete (MySQL/real database)
        // Fall back to Find+Remove for InMemoryDatabase (tests)
        try
        {
            var deleted = await _context.Studies
                .Where(s => s.Id == id)
                .ExecuteDeleteAsync();
            
            // Invalidate cache
            _cache.Remove($"study_detail_{id}");
            _cache.Remove($"study_detail_uid_{studyUid}");
            
            return deleted > 0;
        }
        catch (InvalidOperationException)
        {
            // Fallback for InMemoryDatabase which doesn't support ExecuteDelete
            var studyToDelete = await _context.Studies.FindAsync(id);
            if (studyToDelete != null)
            {
                _context.Studies.Remove(studyToDelete);
                await _context.SaveChangesAsync();
                
                // Invalidate cache
                _cache.Remove($"study_detail_{id}");
                _cache.Remove($"study_detail_uid_{studyUid}");
                
                return true;
            }
            return false;
        }
    }

    public async Task UpdateLastAccessedAsync(int studyId)
    {
        // Optimize: Use ExecuteUpdate for direct SQL update without loading entity
        await _context.Studies
            .Where(s => s.Id == studyId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(s => s.LastAccessedAt, DateTime.UtcNow));
    }
}
