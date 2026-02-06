using FellowOakDicom;
using Microsoft.EntityFrameworkCore;
using MedView.Server.Data;
using MedView.Server.Models;
using System.Text;
using System.Text.Json;

namespace MedView.Server.Services;

public interface IReportingService
{
    // Structured Reporting (DICOM SR)
    Task<StructuredReport> CreateStructuredReportAsync(int studyId, StructuredReportRequest request);
    Task<StructuredReport?> GetStructuredReportAsync(int reportId);
    Task<List<StructuredReport>> GetStudyReportsAsync(int studyId);
    Task<StructuredReport> UpdateStructuredReportAsync(int reportId, StructuredReportRequest request);
    Task<byte[]> ExportReportToPdfAsync(int reportId);
    Task<byte[]> ExportReportToDicomSrAsync(int reportId);
    
    // Annotations
    Task<ReportAnnotation> AddAnnotationAsync(int instanceId, ReportAnnotationRequest request);
    Task<List<ReportAnnotation>> GetAnnotationsAsync(int instanceId);
    Task DeleteAnnotationAsync(int annotationId);
    
    // Templates
    Task<ReportTemplate> GetTemplateAsync(int templateId);
    Task<List<ReportTemplate>> GetTemplatesAsync(string? modality = null);
    Task<ReportTemplate> SaveTemplateAsync(ReportTemplate template);
    
    // Export
    Task<byte[]> ExportStudyWithAnnotationsAsync(int studyId, ExportFormat format);
    Task<string> GenerateHl7MessageAsync(int reportId);
}

public enum ExportFormat { Pdf, Html, Dicom, Hl7 }
public enum ReportStatus { Draft, Preliminary, Final, Amended, Cancelled }

public class StructuredReport
{
    public int Id { get; set; }
    public int StudyId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? TemplateId { get; set; }
    public string ReportContent { get; set; } = "{}"; // JSON structured content
    public string? Findings { get; set; }
    public string? Impression { get; set; }
    public string? Recommendations { get; set; }
    public ReportStatus Status { get; set; } = ReportStatus.Draft;
    public string? CreatedBy { get; set; }
    public string? SignedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SignedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? DicomSrUid { get; set; }
    public List<ReportMeasurement> Measurements { get; set; } = new();
    public List<ReportKeyImage> KeyImages { get; set; } = new();
}

public class ReportMeasurement
{
    public int Id { get; set; }
    public int ReportId { get; set; }
    public string MeasurementType { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public double Value { get; set; }
    public string Unit { get; set; } = string.Empty;
    public int? InstanceId { get; set; }
    public int? Frame { get; set; }
    public string? PositionData { get; set; }
}

public class ReportKeyImage
{
    public int Id { get; set; }
    public int ReportId { get; set; }
    public int InstanceId { get; set; }
    public int? Frame { get; set; }
    public string? Caption { get; set; }
    public string? ThumbnailBase64 { get; set; }
}

public class ReportAnnotation
{
    public int Id { get; set; }
    public int InstanceId { get; set; }
    public string Type { get; set; } = string.Empty; // Text, Arrow, Marker, Rectangle, Ellipse, Freehand
    public string? Text { get; set; }
    public string? Color { get; set; } = "#FFFF00";
    public double? FontSize { get; set; } = 14;
    public double? LineWidth { get; set; } = 2;
    public string PositionData { get; set; } = "{}";
    public int? Frame { get; set; }
    public bool IsVisible { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
}

public class ReportTemplate
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Modality { get; set; }
    public string? BodyPart { get; set; }
    public string TemplateContent { get; set; } = "{}";
    public List<string> RequiredFields { get; set; } = new();
    public bool IsDefault { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public record StructuredReportRequest(
    string Title,
    string? TemplateId,
    string? Findings,
    string? Impression,
    string? Recommendations,
    ReportStatus Status,
    List<ReportMeasurementRequest>? Measurements,
    List<ReportKeyImageRequest>? KeyImages,
    Dictionary<string, object>? CustomFields
);

public record ReportMeasurementRequest(string MeasurementType, string Label, double Value, string Unit, int? InstanceId, int? Frame, string? PositionData);
public record ReportKeyImageRequest(int InstanceId, int? Frame, string? Caption);
public record ReportAnnotationRequest(string Type, string? Text, string? Color, double? FontSize, double? LineWidth, string PositionData, int? Frame);

public class ReportingService : IReportingService
{
    private readonly DicomDbContext _context;
    private readonly IDicomImageService _dicomImageService;
    private readonly ILogger<ReportingService> _logger;

    public ReportingService(DicomDbContext context, IDicomImageService dicomImageService, ILogger<ReportingService> logger)
    {
        _context = context;
        _dicomImageService = dicomImageService;
        _logger = logger;
    }

    public async Task<StructuredReport> CreateStructuredReportAsync(int studyId, StructuredReportRequest request)
    {
        var report = new StructuredReport
        {
            StudyId = studyId,
            Title = request.Title,
            TemplateId = request.TemplateId,
            Findings = request.Findings,
            Impression = request.Impression,
            Recommendations = request.Recommendations,
            Status = request.Status,
            CreatedAt = DateTime.UtcNow,
            ReportContent = JsonSerializer.Serialize(request.CustomFields ?? new Dictionary<string, object>())
        };

        if (request.Measurements != null)
        {
            report.Measurements = request.Measurements.Select(m => new ReportMeasurement
            {
                MeasurementType = m.MeasurementType,
                Label = m.Label,
                Value = m.Value,
                Unit = m.Unit,
                InstanceId = m.InstanceId,
                Frame = m.Frame,
                PositionData = m.PositionData
            }).ToList();
        }

        if (request.KeyImages != null)
        {
            foreach (var ki in request.KeyImages)
            {
                var instance = await _context.Instances.FindAsync(ki.InstanceId);
                string? thumbnail = null;
                
                if (instance?.FilePath != null)
                {
                    try
                    {
                        var thumbBytes = await _dicomImageService.RenderThumbnailAsync(instance.FilePath, 256, 256);
                        thumbnail = Convert.ToBase64String(thumbBytes);
                    }
                    catch { }
                }

                report.KeyImages.Add(new ReportKeyImage
                {
                    InstanceId = ki.InstanceId,
                    Frame = ki.Frame,
                    Caption = ki.Caption,
                    ThumbnailBase64 = thumbnail
                });
            }
        }

        _context.Set<StructuredReport>().Add(report);
        await _context.SaveChangesAsync();
        return report;
    }

    public async Task<StructuredReport?> GetStructuredReportAsync(int reportId)
    {
        return await _context.Set<StructuredReport>()
            .Include(r => r.Measurements)
            .Include(r => r.KeyImages)
            .FirstOrDefaultAsync(r => r.Id == reportId);
    }

    public async Task<List<StructuredReport>> GetStudyReportsAsync(int studyId)
    {
        return await _context.Set<StructuredReport>()
            .Where(r => r.StudyId == studyId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<StructuredReport> UpdateStructuredReportAsync(int reportId, StructuredReportRequest request)
    {
        var report = await _context.Set<StructuredReport>()
            .Include(r => r.Measurements)
            .Include(r => r.KeyImages)
            .FirstOrDefaultAsync(r => r.Id == reportId);

        if (report == null)
            throw new ArgumentException("Report not found");

        report.Title = request.Title;
        report.Findings = request.Findings;
        report.Impression = request.Impression;
        report.Recommendations = request.Recommendations;
        report.Status = request.Status;
        report.UpdatedAt = DateTime.UtcNow;
        report.ReportContent = JsonSerializer.Serialize(request.CustomFields ?? new Dictionary<string, object>());

        if (request.Status == ReportStatus.Final && report.SignedAt == null)
        {
            report.SignedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return report;
    }

    public async Task<byte[]> ExportReportToPdfAsync(int reportId)
    {
        var report = await GetStructuredReportAsync(reportId);
        if (report == null)
            throw new ArgumentException("Report not found");

        var study = await _context.Studies.FindAsync(report.StudyId);

        // Generate HTML for PDF conversion
        var html = GenerateReportHtml(report, study);
        
        // For actual PDF generation, you'd use a library like iTextSharp, PuppeteerSharp, or DinkToPdf
        // Here we return the HTML as bytes for demonstration
        return Encoding.UTF8.GetBytes(html);
    }

    public async Task<byte[]> ExportReportToDicomSrAsync(int reportId)
    {
        var report = await GetStructuredReportAsync(reportId);
        if (report == null)
            throw new ArgumentException("Report not found");

        var study = await _context.Studies.FindAsync(report.StudyId);

        // Create DICOM SR dataset
        var dataset = new DicomDataset();
        
        // Patient Module
        dataset.Add(DicomTag.PatientName, study?.PatientName ?? "");
        dataset.Add(DicomTag.PatientID, study?.PatientId ?? "");
        
        // General Study Module
        dataset.Add(DicomTag.StudyInstanceUID, study?.StudyInstanceUid ?? DicomUID.Generate().UID);
        dataset.Add(DicomTag.StudyDate, study?.StudyDate?.ToString("yyyyMMdd") ?? "");
        
        // SR Document Series Module
        dataset.Add(DicomTag.Modality, "SR");
        dataset.Add(DicomTag.SeriesInstanceUID, DicomUID.Generate().UID);
        
        // SR Document General Module
        dataset.Add(DicomTag.SOPClassUID, DicomUID.BasicTextSRStorage);
        dataset.Add(DicomTag.SOPInstanceUID, DicomUID.Generate().UID);
        dataset.Add(DicomTag.ContentDate, DateTime.Now.ToString("yyyyMMdd"));
        dataset.Add(DicomTag.ContentTime, DateTime.Now.ToString("HHmmss"));
        
        // Document Content
        dataset.Add(DicomTag.CompletionFlag, report.Status == ReportStatus.Final ? "COMPLETE" : "PARTIAL");
        dataset.Add(DicomTag.VerificationFlag, report.SignedAt != null ? "VERIFIED" : "UNVERIFIED");

        // Content Sequence (simplified)
        var contentSequence = new DicomSequence(DicomTag.ContentSequence);
        
        // Add findings as text content
        if (!string.IsNullOrEmpty(report.Findings))
        {
            var findingsItem = new DicomDataset();
            findingsItem.Add(DicomTag.RelationshipType, "CONTAINS");
            findingsItem.Add(DicomTag.ValueType, "TEXT");
            findingsItem.Add(DicomTag.TextValue, report.Findings);
            
            var conceptName = new DicomSequence(DicomTag.ConceptNameCodeSequence);
            var conceptItem = new DicomDataset();
            conceptItem.Add(DicomTag.CodeValue, "121070");
            conceptItem.Add(DicomTag.CodingSchemeDesignator, "DCM");
            conceptItem.Add(DicomTag.CodeMeaning, "Findings");
            conceptName.Items.Add(conceptItem);
            findingsItem.Add(conceptName);
            
            contentSequence.Items.Add(findingsItem);
        }

        // Add impression
        if (!string.IsNullOrEmpty(report.Impression))
        {
            var impressionItem = new DicomDataset();
            impressionItem.Add(DicomTag.RelationshipType, "CONTAINS");
            impressionItem.Add(DicomTag.ValueType, "TEXT");
            impressionItem.Add(DicomTag.TextValue, report.Impression);
            
            var conceptName = new DicomSequence(DicomTag.ConceptNameCodeSequence);
            var conceptItem = new DicomDataset();
            conceptItem.Add(DicomTag.CodeValue, "121073");
            conceptItem.Add(DicomTag.CodingSchemeDesignator, "DCM");
            conceptItem.Add(DicomTag.CodeMeaning, "Impression");
            conceptName.Items.Add(conceptItem);
            impressionItem.Add(conceptName);
            
            contentSequence.Items.Add(impressionItem);
        }

        // Add measurements
        foreach (var measurement in report.Measurements)
        {
            var measureItem = new DicomDataset();
            measureItem.Add(DicomTag.RelationshipType, "CONTAINS");
            measureItem.Add(DicomTag.ValueType, "NUM");
            
            var numSequence = new DicomSequence(DicomTag.MeasuredValueSequence);
            var numItem = new DicomDataset();
            numItem.Add(DicomTag.NumericValue, measurement.Value.ToString());
            
            var unitSequence = new DicomSequence(DicomTag.MeasurementUnitsCodeSequence);
            var unitItem = new DicomDataset();
            unitItem.Add(DicomTag.CodeValue, measurement.Unit);
            unitItem.Add(DicomTag.CodingSchemeDesignator, "UCUM");
            unitItem.Add(DicomTag.CodeMeaning, measurement.Unit);
            unitSequence.Items.Add(unitItem);
            numItem.Add(unitSequence);
            
            numSequence.Items.Add(numItem);
            measureItem.Add(numSequence);
            
            contentSequence.Items.Add(measureItem);
        }

        dataset.Add(contentSequence);

        var dicomFile = new DicomFile(dataset);
        using var ms = new MemoryStream();
        await dicomFile.SaveAsync(ms);
        return ms.ToArray();
    }

    public async Task<ReportAnnotation> AddAnnotationAsync(int instanceId, ReportAnnotationRequest request)
    {
        var annotation = new ReportAnnotation
        {
            InstanceId = instanceId,
            Type = request.Type,
            Text = request.Text,
            Color = request.Color ?? "#FFFF00",
            FontSize = request.FontSize ?? 14,
            LineWidth = request.LineWidth ?? 2,
            PositionData = request.PositionData,
            Frame = request.Frame,
            IsVisible = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Set<ReportAnnotation>().Add(annotation);
        await _context.SaveChangesAsync();
        return annotation;
    }

    public async Task<List<ReportAnnotation>> GetAnnotationsAsync(int instanceId)
    {
        return await _context.Set<ReportAnnotation>()
            .Where(a => a.InstanceId == instanceId)
            .OrderBy(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task DeleteAnnotationAsync(int annotationId)
    {
        var annotation = await _context.Set<ReportAnnotation>().FindAsync(annotationId);
        if (annotation != null)
        {
            _context.Set<ReportAnnotation>().Remove(annotation);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<ReportTemplate> GetTemplateAsync(int templateId)
    {
        return await _context.Set<ReportTemplate>().FindAsync(templateId)
            ?? throw new ArgumentException("Template not found");
    }

    public async Task<List<ReportTemplate>> GetTemplatesAsync(string? modality = null)
    {
        var query = _context.Set<ReportTemplate>().AsQueryable();
        if (!string.IsNullOrEmpty(modality))
            query = query.Where(t => t.Modality == null || t.Modality == modality);
        return await query.OrderBy(t => t.Name).ToListAsync();
    }

    public async Task<ReportTemplate> SaveTemplateAsync(ReportTemplate template)
    {
        if (template.Id == 0)
            _context.Set<ReportTemplate>().Add(template);
        else
            _context.Set<ReportTemplate>().Update(template);
        await _context.SaveChangesAsync();
        return template;
    }

    public async Task<byte[]> ExportStudyWithAnnotationsAsync(int studyId, ExportFormat format)
    {
        var study = await _context.Studies
            .Include(s => s.Series)
            .ThenInclude(ser => ser.Instances)
            .FirstOrDefaultAsync(s => s.Id == studyId);

        if (study == null)
            throw new ArgumentException("Study not found");

        var reports = await GetStudyReportsAsync(studyId);
        
        return format switch
        {
            ExportFormat.Html => Encoding.UTF8.GetBytes(GenerateStudyHtml(study, reports)),
            ExportFormat.Pdf => Encoding.UTF8.GetBytes(GenerateStudyHtml(study, reports)), // Would use PDF library
            _ => throw new NotSupportedException($"Export format {format} not supported")
        };
    }

    public async Task<string> GenerateHl7MessageAsync(int reportId)
    {
        var report = await GetStructuredReportAsync(reportId);
        if (report == null)
            throw new ArgumentException("Report not found");

        var study = await _context.Studies.FindAsync(report.StudyId);
        var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
        var messageId = Guid.NewGuid().ToString("N").Substring(0, 20);

        var sb = new StringBuilder();
        
        // MSH - Message Header
        sb.AppendLine($"MSH|^~\\&|MEDVIEW|HOSPITAL|RECEIVER|FACILITY|{timestamp}||ORU^R01|{messageId}|P|2.5");
        
        // PID - Patient Identification
        sb.AppendLine($"PID|1||{study?.PatientId ?? ""}^^^HOSPITAL||{study?.PatientName ?? ""}||{study?.PatientBirthDate?.ToString("yyyyMMdd") ?? ""}|{study?.PatientSex ?? ""}");
        
        // OBR - Observation Request
        sb.AppendLine($"OBR|1||{study?.AccessionNumber ?? ""}|RAD^Radiology^L|||{study?.StudyDate?.ToString("yyyyMMddHHmmss") ?? ""}|||||||||||||||||F");
        
        // OBX - Observation/Result segments
        int obxSeq = 1;
        
        if (!string.IsNullOrEmpty(report.Findings))
        {
            sb.AppendLine($"OBX|{obxSeq++}|TX|FINDINGS^Findings^L||{EscapeHl7(report.Findings)}||||||F");
        }
        
        if (!string.IsNullOrEmpty(report.Impression))
        {
            sb.AppendLine($"OBX|{obxSeq++}|TX|IMPRESSION^Impression^L||{EscapeHl7(report.Impression)}||||||F");
        }
        
        if (!string.IsNullOrEmpty(report.Recommendations))
        {
            sb.AppendLine($"OBX|{obxSeq++}|TX|RECOMMEND^Recommendations^L||{EscapeHl7(report.Recommendations)}||||||F");
        }

        // Add measurements
        foreach (var m in report.Measurements)
        {
            sb.AppendLine($"OBX|{obxSeq++}|NM|{m.MeasurementType}^{m.Label}^L||{m.Value}|{m.Unit}|||||F");
        }

        return sb.ToString();
    }

    #region Helper Methods

    private string GenerateReportHtml(StructuredReport report, Study? study)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html><html><head>");
        sb.AppendLine("<style>");
        sb.AppendLine("body { font-family: Arial, sans-serif; max-width: 800px; margin: 0 auto; padding: 20px; }");
        sb.AppendLine(".header { border-bottom: 2px solid #333; padding-bottom: 10px; margin-bottom: 20px; }");
        sb.AppendLine(".section { margin-bottom: 20px; }");
        sb.AppendLine(".section-title { font-weight: bold; color: #333; margin-bottom: 5px; }");
        sb.AppendLine(".measurement { display: flex; justify-content: space-between; padding: 5px 0; border-bottom: 1px solid #eee; }");
        sb.AppendLine(".key-image { display: inline-block; margin: 10px; text-align: center; }");
        sb.AppendLine(".key-image img { max-width: 200px; border: 1px solid #ccc; }");
        sb.AppendLine(".status { padding: 5px 10px; border-radius: 4px; display: inline-block; }");
        sb.AppendLine(".status-final { background: #4CAF50; color: white; }");
        sb.AppendLine(".status-draft { background: #FFC107; color: black; }");
        sb.AppendLine("</style></head><body>");

        sb.AppendLine("<div class='header'>");
        sb.AppendLine($"<h1>{report.Title}</h1>");
        sb.AppendLine($"<p><strong>Patient:</strong> {study?.PatientName ?? "N/A"} | <strong>ID:</strong> {study?.PatientId ?? "N/A"}</p>");
        sb.AppendLine($"<p><strong>Study Date:</strong> {study?.StudyDate?.ToString("MMMM dd, yyyy") ?? "N/A"}</p>");
        sb.AppendLine($"<p><strong>Study:</strong> {study?.StudyDescription ?? "N/A"}</p>");
        sb.AppendLine($"<span class='status status-{report.Status.ToString().ToLower()}'>{report.Status}</span>");
        sb.AppendLine("</div>");

        if (!string.IsNullOrEmpty(report.Findings))
        {
            sb.AppendLine("<div class='section'>");
            sb.AppendLine("<div class='section-title'>FINDINGS</div>");
            sb.AppendLine($"<p>{report.Findings}</p>");
            sb.AppendLine("</div>");
        }

        if (!string.IsNullOrEmpty(report.Impression))
        {
            sb.AppendLine("<div class='section'>");
            sb.AppendLine("<div class='section-title'>IMPRESSION</div>");
            sb.AppendLine($"<p>{report.Impression}</p>");
            sb.AppendLine("</div>");
        }

        if (!string.IsNullOrEmpty(report.Recommendations))
        {
            sb.AppendLine("<div class='section'>");
            sb.AppendLine("<div class='section-title'>RECOMMENDATIONS</div>");
            sb.AppendLine($"<p>{report.Recommendations}</p>");
            sb.AppendLine("</div>");
        }

        if (report.Measurements.Count > 0)
        {
            sb.AppendLine("<div class='section'>");
            sb.AppendLine("<div class='section-title'>MEASUREMENTS</div>");
            foreach (var m in report.Measurements)
            {
                sb.AppendLine($"<div class='measurement'><span>{m.Label}</span><span>{m.Value} {m.Unit}</span></div>");
            }
            sb.AppendLine("</div>");
        }

        if (report.KeyImages.Count > 0)
        {
            sb.AppendLine("<div class='section'>");
            sb.AppendLine("<div class='section-title'>KEY IMAGES</div>");
            foreach (var ki in report.KeyImages)
            {
                if (!string.IsNullOrEmpty(ki.ThumbnailBase64))
                {
                    sb.AppendLine("<div class='key-image'>");
                    sb.AppendLine($"<img src='data:image/png;base64,{ki.ThumbnailBase64}' />");
                    if (!string.IsNullOrEmpty(ki.Caption))
                        sb.AppendLine($"<p>{ki.Caption}</p>");
                    sb.AppendLine("</div>");
                }
            }
            sb.AppendLine("</div>");
        }

        sb.AppendLine("<div class='section'>");
        sb.AppendLine($"<p><small>Report generated: {report.CreatedAt:MMMM dd, yyyy HH:mm}</small></p>");
        if (report.SignedAt.HasValue)
            sb.AppendLine($"<p><small>Signed: {report.SignedAt:MMMM dd, yyyy HH:mm} by {report.SignedBy ?? "N/A"}</small></p>");
        sb.AppendLine("</div>");

        sb.AppendLine("</body></html>");
        return sb.ToString();
    }

    private string GenerateStudyHtml(Study study, List<StructuredReport> reports)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html><html><head><title>Study Export</title></head><body>");
        sb.AppendLine($"<h1>Study: {study.StudyDescription ?? "Unknown"}</h1>");
        sb.AppendLine($"<p>Patient: {study.PatientName} | Date: {study.StudyDate?.ToString("yyyy-MM-dd")}</p>");
        
        foreach (var report in reports)
        {
            sb.AppendLine(GenerateReportHtml(report, study));
            sb.AppendLine("<hr/>");
        }
        
        sb.AppendLine("</body></html>");
        return sb.ToString();
    }

    private static string EscapeHl7(string text)
    {
        return text
            .Replace("|", "\\F\\")
            .Replace("^", "\\S\\")
            .Replace("&", "\\T\\")
            .Replace("~", "\\R\\")
            .Replace("\\", "\\E\\")
            .Replace("\r\n", "~")
            .Replace("\n", "~");
    }

    #endregion
}
