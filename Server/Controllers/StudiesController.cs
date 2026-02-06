using Microsoft.AspNetCore.Mvc;
using MedView.Server.Models.DTOs;
using MedView.Server.Services;

namespace MedView.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StudiesController : ControllerBase
{
    private readonly IStudyService _studyService;
    private readonly IDicomImageService _dicomImageService;
    private readonly ILogger<StudiesController> _logger;
    private readonly IConfiguration _configuration;
    private readonly IEncryptionService _encryptionService;

    public StudiesController(
        IStudyService studyService,
        IDicomImageService dicomImageService,
        ILogger<StudiesController> logger,
        IConfiguration configuration,
        IEncryptionService encryptionService)
    {
        _studyService = studyService;
        _dicomImageService = dicomImageService;
        _logger = logger;
        _configuration = configuration;
        _encryptionService = encryptionService;
    }

    /// <summary>
    /// Search studies with optional filters
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PagedResultDto<StudyDto>>> SearchStudies([FromQuery] StudySearchDto search)
    {
        var result = await _studyService.SearchStudiesAsync(search);
        return Ok(result);
    }

    /// <summary>
    /// Get study by ID
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<StudyDetailDto>> GetStudyById(int id)
    {
        var study = await _studyService.GetStudyByIdAsync(id);
        if (study == null)
            return NotFound(new { message = "Study not found" });
        return Ok(study);
    }

    /// <summary>
    /// Get study by Study Instance UID
    /// </summary>
    [HttpGet("uid/{studyInstanceUid}")]
    public async Task<ActionResult<StudyDetailDto>> GetStudyByUid(string studyInstanceUid)
    {
        var study = await _studyService.GetStudyByUidAsync(studyInstanceUid);
        if (study == null)
            return NotFound(new { message = "Study not found" });
        return Ok(study);
    }

    /// <summary>
    /// Get study by encrypted Study Instance UID
    /// </summary>
    [HttpGet("encrypted/{encryptedStudyUid}")]
    public async Task<ActionResult<StudyDetailDto>> GetStudyByEncryptedUid(string encryptedStudyUid)
    {
        try
        {
            var studyInstanceUid = _encryptionService.DecryptStudyUid(encryptedStudyUid);
            var study = await _studyService.GetStudyByUidAsync(studyInstanceUid);
            if (study == null)
                return NotFound(new { message = "Study not found" });
            return Ok(study);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error decrypting study UID");
            return BadRequest(new { message = "Invalid encrypted study UID" });
        }
    }

    /// <summary>
    /// Get encrypted Study Instance UID for sharing
    /// </summary>
    [HttpGet("{id:int}/encrypt-uid")]
    public async Task<ActionResult<object>> GetEncryptedStudyUid(int id)
    {
        var study = await _studyService.GetStudyByIdAsync(id);
        if (study == null)
            return NotFound(new { message = "Study not found" });
        
        var encryptedUid = _encryptionService.EncryptStudyUid(study.StudyInstanceUid);
        return Ok(new { encryptedUid, studyInstanceUid = study.StudyInstanceUid });
    }

    /// <summary>
    /// Upload DICOM files
    /// </summary>
    [HttpPost("upload")]
    [RequestSizeLimit(500 * 1024 * 1024)] // 500MB
    public async Task<ActionResult<UploadResultDto>> UploadFiles([FromForm] List<IFormFile> files)
    {
        if (files == null || files.Count == 0)
            return BadRequest(new { message = "No files uploaded" });

        var tempPath = _configuration["DicomSettings:TempPath"] ?? "./TempFiles";
        Directory.CreateDirectory(tempPath);

        var filePaths = new List<string>();

        try
        {
            foreach (var file in files)
            {
                if (file.Length == 0) continue;

                var tempFilePath = Path.Combine(tempPath, $"{Guid.NewGuid()}.dcm");
                using (var stream = new FileStream(tempFilePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
                filePaths.Add(tempFilePath);
            }

            var result = await _studyService.ProcessUploadedFilesAsync(filePaths);
            return Ok(result);
        }
        finally
        {
            // Clean up temp files
            foreach (var path in filePaths)
            {
                try { System.IO.File.Delete(path); } catch { }
            }
        }
    }

    /// <summary>
    /// Upload a folder of DICOM files (via form data)
    /// </summary>
    [HttpPost("upload-folder")]
    [RequestSizeLimit(1024 * 1024 * 1024)] // 1GB
    public async Task<ActionResult<UploadResultDto>> UploadFolder([FromForm] List<IFormFile> files)
    {
        return await UploadFiles(files);
    }

    /// <summary>
    /// Delete a study
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteStudy(int id)
    {
        var result = await _studyService.DeleteStudyAsync(id);
        if (!result)
            return NotFound(new { message = "Study not found" });
        return NoContent();
    }
}
