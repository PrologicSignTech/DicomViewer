using Microsoft.AspNetCore.Mvc;
using MedView.Server.Services;
using MedView.Server.Models;

namespace MedView.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WorkflowController : ControllerBase
{
    private readonly IWorkflowService _workflowService;
    private readonly ILogger<WorkflowController> _logger;

    public WorkflowController(IWorkflowService workflowService, ILogger<WorkflowController> logger)
    {
        _workflowService = workflowService;
        _logger = logger;
    }

    [HttpGet("studies/{studyId}/compare")]
    public async Task<IActionResult> CompareStudies(int studyId, [FromQuery] int? priorStudyId = null)
    {
        var result = await _workflowService.CompareStudiesAsync(studyId, priorStudyId);
        return Ok(result);
    }

    [HttpGet("studies/{studyId}/priors")]
    public async Task<IActionResult> FindPriorStudies(int studyId, [FromQuery] int maxResults = 5)
    {
        var priors = await _workflowService.FindPriorStudiesAsync(studyId, maxResults);
        return Ok(priors);
    }

    [HttpPost("studies/{studyId}/hanging-protocol")]
    public async Task<IActionResult> ApplyHangingProtocol(int studyId, [FromQuery] int? protocolId = null)
    {
        var result = await _workflowService.ApplyHangingProtocolAsync(studyId, protocolId);
        return Ok(result);
    }

    [HttpGet("hanging-protocols")]
    public async Task<IActionResult> GetHangingProtocols([FromQuery] string? modality = null, [FromQuery] string? bodyPart = null)
    {
        var protocols = await _workflowService.GetAvailableProtocolsAsync(modality, bodyPart);
        return Ok(protocols);
    }

    [HttpPost("hanging-protocols")]
    public async Task<IActionResult> SaveHangingProtocol([FromBody] HangingProtocol protocol)
    {
        var saved = await _workflowService.SaveHangingProtocolAsync(protocol);
        return Ok(saved);
    }

    [HttpPost("series/sync")]
    public async Task<IActionResult> SynchronizeSeries([FromBody] SeriesSyncRequest request)
    {
        var result = await _workflowService.SynchronizeSeriesAsync(request.SeriesIds, request.Mode);
        return Ok(result);
    }

    [HttpPost("studies/{studyId}/bookmarks")]
    public async Task<IActionResult> CreateBookmark(int studyId, [FromBody] BookmarkCreateRequest request)
    {
        var bookmark = await _workflowService.CreateBookmarkAsync(studyId, request);
        return Ok(bookmark);
    }

    [HttpGet("studies/{studyId}/bookmarks")]
    public async Task<IActionResult> GetBookmarks(int studyId)
    {
        var bookmarks = await _workflowService.GetBookmarksAsync(studyId);
        return Ok(bookmarks);
    }

    [HttpDelete("bookmarks/{bookmarkId}")]
    public async Task<IActionResult> DeleteBookmark(int bookmarkId)
    {
        await _workflowService.DeleteBookmarkAsync(bookmarkId);
        return NoContent();
    }

    [HttpPost("instances/{instanceId}/key-image")]
    public async Task<IActionResult> MarkAsKeyImage(int instanceId, [FromBody] KeyImageRequest request)
    {
        var keyImage = await _workflowService.MarkAsKeyImageAsync(instanceId, request);
        return Ok(keyImage);
    }

    [HttpGet("studies/{studyId}/key-images")]
    public async Task<IActionResult> GetKeyImages(int studyId)
    {
        var keyImages = await _workflowService.GetKeyImagesAsync(studyId);
        return Ok(keyImages);
    }

    [HttpDelete("key-images/{keyImageId}")]
    public async Task<IActionResult> DeleteKeyImage(int keyImageId)
    {
        await _workflowService.DeleteKeyImageAsync(keyImageId);
        return NoContent();
    }

    [HttpGet("layouts")]
    public async Task<IActionResult> GetLayouts()
    {
        var layouts = await _workflowService.GetAvailableLayoutsAsync();
        return Ok(layouts);
    }

    [HttpPost("layouts")]
    public async Task<IActionResult> SaveLayout([FromBody] LayoutConfiguration layout)
    {
        var saved = await _workflowService.SaveLayoutAsync(layout);
        return Ok(saved);
    }

    [HttpGet("worklist")]
    public async Task<IActionResult> GetWorklist([FromQuery] WorklistQuery query)
    {
        var items = await _workflowService.GetWorklistAsync(query);
        return Ok(items);
    }

    [HttpPut("worklist/{itemId}/status")]
    public async Task<IActionResult> UpdateWorklistStatus(int itemId, [FromBody] StatusUpdateRequest request)
    {
        var item = await _workflowService.UpdateWorklistItemStatusAsync(itemId, request.Status);
        return Ok(item);
    }
}

public record SeriesSyncRequest(List<int> SeriesIds, SyncMode Mode);
public record StatusUpdateRequest(string Status);

[ApiController]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly IReportingService _reportingService;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(IReportingService reportingService, ILogger<ReportsController> logger)
    {
        _reportingService = reportingService;
        _logger = logger;
    }

    [HttpPost("studies/{studyId}/reports")]
    public async Task<IActionResult> CreateReport(int studyId, [FromBody] StructuredReportRequest request)
    {
        var report = await _reportingService.CreateStructuredReportAsync(studyId, request);
        return Ok(report);
    }

    [HttpGet("{reportId}")]
    public async Task<IActionResult> GetReport(int reportId)
    {
        var report = await _reportingService.GetStructuredReportAsync(reportId);
        return report == null ? NotFound() : Ok(report);
    }

    [HttpGet("studies/{studyId}/reports")]
    public async Task<IActionResult> GetStudyReports(int studyId)
    {
        var reports = await _reportingService.GetStudyReportsAsync(studyId);
        return Ok(reports);
    }

    [HttpPut("{reportId}")]
    public async Task<IActionResult> UpdateReport(int reportId, [FromBody] StructuredReportRequest request)
    {
        var report = await _reportingService.UpdateStructuredReportAsync(reportId, request);
        return Ok(report);
    }

    [HttpGet("{reportId}/export/pdf")]
    public async Task<IActionResult> ExportToPdf(int reportId)
    {
        var pdfBytes = await _reportingService.ExportReportToPdfAsync(reportId);
        return File(pdfBytes, "text/html", $"report_{reportId}.html");
    }

    [HttpGet("{reportId}/export/dicom-sr")]
    public async Task<IActionResult> ExportToDicomSr(int reportId)
    {
        var dicomBytes = await _reportingService.ExportReportToDicomSrAsync(reportId);
        return File(dicomBytes, "application/dicom", $"sr_{reportId}.dcm");
    }

    [HttpGet("{reportId}/export/hl7")]
    public async Task<IActionResult> ExportToHl7(int reportId)
    {
        var hl7Message = await _reportingService.GenerateHl7MessageAsync(reportId);
        return Content(hl7Message, "text/plain");
    }

    [HttpPost("instances/{instanceId}/annotations")]
    public async Task<IActionResult> AddAnnotation(int instanceId, [FromBody] ReportAnnotationRequest request)
    {
        var annotation = await _reportingService.AddAnnotationAsync(instanceId, request);
        return Ok(annotation);
    }

    [HttpGet("instances/{instanceId}/annotations")]
    public async Task<IActionResult> GetAnnotations(int instanceId)
    {
        var annotations = await _reportingService.GetAnnotationsAsync(instanceId);
        return Ok(annotations);
    }

    [HttpDelete("annotations/{annotationId}")]
    public async Task<IActionResult> DeleteAnnotation(int annotationId)
    {
        await _reportingService.DeleteAnnotationAsync(annotationId);
        return NoContent();
    }

    [HttpGet("templates")]
    public async Task<IActionResult> GetTemplates([FromQuery] string? modality = null)
    {
        var templates = await _reportingService.GetTemplatesAsync(modality);
        return Ok(templates);
    }

    [HttpPost("templates")]
    public async Task<IActionResult> SaveTemplate([FromBody] ReportTemplate template)
    {
        var saved = await _reportingService.SaveTemplateAsync(template);
        return Ok(saved);
    }

    [HttpGet("studies/{studyId}/export")]
    public async Task<IActionResult> ExportStudy(int studyId, [FromQuery] string format = "html")
    {
        var exportFormat = format.ToLower() == "html" ? ExportFormat.Html : ExportFormat.Pdf;
        var exportBytes = await _reportingService.ExportStudyWithAnnotationsAsync(studyId, exportFormat);
        return File(exportBytes, "text/html", $"study_{studyId}.html");
    }
}
