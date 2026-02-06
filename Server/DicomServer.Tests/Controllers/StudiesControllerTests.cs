using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using MedView.Server.Controllers;
using MedView.Server.Models.DTOs;
using MedView.Server.Services;

namespace DicomServer.Tests.Controllers;

public class StudiesControllerTests
{
    [Fact]
    public async Task SearchStudies_ReturnsOkWithStudies()
    {
        // Arrange
        var mockStudyService = new Mock<IStudyService>();
        var mockDicomService = new Mock<IDicomImageService>();
        var mockLogger = new Mock<ILogger<StudiesController>>();
        var mockConfig = new Mock<IConfiguration>();

        var pagedResult = new PagedResultDto<StudyDto>(
            new List<StudyDto>
            {
                new StudyDto(1, "1.2.3.4", null, "Test Study", DateTime.UtcNow, null, "PAT001", "Test Patient", 
                    null, null, null, null, 1, 1, DateTime.UtcNow)
            },
            1, 1, 20, 1
        );

        mockStudyService
            .Setup(s => s.SearchStudiesAsync(It.IsAny<StudySearchDto>()))
            .ReturnsAsync(pagedResult);

        var controller = new StudiesController(mockStudyService.Object, mockDicomService.Object, 
            mockLogger.Object, mockConfig.Object);

        var search = new StudySearchDto(null, null, null, null, null, null, null, 1, 20);

        // Act
        var result = await controller.SearchStudies(search);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedResult = Assert.IsType<PagedResultDto<StudyDto>>(okResult.Value);
        Assert.Single(returnedResult.Items);
    }

    [Fact]
    public async Task GetStudyById_WithValidId_ReturnsOkWithStudy()
    {
        // Arrange
        var mockStudyService = new Mock<IStudyService>();
        var mockDicomService = new Mock<IDicomImageService>();
        var mockLogger = new Mock<ILogger<StudiesController>>();
        var mockConfig = new Mock<IConfiguration>();

        var studyDetail = new StudyDetailDto(
            1, "1.2.3.4", null, "Test Study", DateTime.UtcNow, null, "PAT001", "Test Patient",
            null, null, null, null, null, 1, 1, new List<SeriesDto>()
        );

        mockStudyService
            .Setup(s => s.GetStudyByIdAsync(1))
            .ReturnsAsync(studyDetail);

        var controller = new StudiesController(mockStudyService.Object, mockDicomService.Object,
            mockLogger.Object, mockConfig.Object);

        // Act
        var result = await controller.GetStudyById(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedStudy = Assert.IsType<StudyDetailDto>(okResult.Value);
        Assert.Equal(1, returnedStudy.Id);
    }

    [Fact]
    public async Task GetStudyById_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var mockStudyService = new Mock<IStudyService>();
        var mockDicomService = new Mock<IDicomImageService>();
        var mockLogger = new Mock<ILogger<StudiesController>>();
        var mockConfig = new Mock<IConfiguration>();

        mockStudyService
            .Setup(s => s.GetStudyByIdAsync(999))
            .ReturnsAsync((StudyDetailDto?)null);

        var controller = new StudiesController(mockStudyService.Object, mockDicomService.Object,
            mockLogger.Object, mockConfig.Object);

        // Act
        var result = await controller.GetStudyById(999);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetStudyByUid_WithValidUid_ReturnsOkWithStudy()
    {
        // Arrange
        var mockStudyService = new Mock<IStudyService>();
        var mockDicomService = new Mock<IDicomImageService>();
        var mockLogger = new Mock<ILogger<StudiesController>>();
        var mockConfig = new Mock<IConfiguration>();

        var studyDetail = new StudyDetailDto(
            1, "1.2.3.4.5", null, "Test Study", DateTime.UtcNow, null, "PAT001", "Test Patient",
            null, null, null, null, null, 1, 1, new List<SeriesDto>()
        );

        mockStudyService
            .Setup(s => s.GetStudyByUidAsync("1.2.3.4.5"))
            .ReturnsAsync(studyDetail);

        var controller = new StudiesController(mockStudyService.Object, mockDicomService.Object,
            mockLogger.Object, mockConfig.Object);

        // Act
        var result = await controller.GetStudyByUid("1.2.3.4.5");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedStudy = Assert.IsType<StudyDetailDto>(okResult.Value);
        Assert.Equal("1.2.3.4.5", returnedStudy.StudyInstanceUid);
    }

    [Fact]
    public async Task GetStudyByUid_WithInvalidUid_ReturnsNotFound()
    {
        // Arrange
        var mockStudyService = new Mock<IStudyService>();
        var mockDicomService = new Mock<IDicomImageService>();
        var mockLogger = new Mock<ILogger<StudiesController>>();
        var mockConfig = new Mock<IConfiguration>();

        mockStudyService
            .Setup(s => s.GetStudyByUidAsync("INVALID.UID"))
            .ReturnsAsync((StudyDetailDto?)null);

        var controller = new StudiesController(mockStudyService.Object, mockDicomService.Object,
            mockLogger.Object, mockConfig.Object);

        // Act
        var result = await controller.GetStudyByUid("INVALID.UID");

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task DeleteStudy_WithValidId_ReturnsNoContent()
    {
        // Arrange
        var mockStudyService = new Mock<IStudyService>();
        var mockDicomService = new Mock<IDicomImageService>();
        var mockLogger = new Mock<ILogger<StudiesController>>();
        var mockConfig = new Mock<IConfiguration>();

        mockStudyService
            .Setup(s => s.DeleteStudyAsync(1))
            .ReturnsAsync(true);

        var controller = new StudiesController(mockStudyService.Object, mockDicomService.Object,
            mockLogger.Object, mockConfig.Object);

        // Act
        var result = await controller.DeleteStudy(1);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task DeleteStudy_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var mockStudyService = new Mock<IStudyService>();
        var mockDicomService = new Mock<IDicomImageService>();
        var mockLogger = new Mock<ILogger<StudiesController>>();
        var mockConfig = new Mock<IConfiguration>();

        mockStudyService
            .Setup(s => s.DeleteStudyAsync(999))
            .ReturnsAsync(false);

        var controller = new StudiesController(mockStudyService.Object, mockDicomService.Object,
            mockLogger.Object, mockConfig.Object);

        // Act
        var result = await controller.DeleteStudy(999);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }
}
