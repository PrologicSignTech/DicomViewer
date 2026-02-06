using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using MedView.Server.Controllers;
using MedView.Server.Models.DTOs;
using MedView.Server.Services;

namespace DicomServer.Tests.Controllers;

public class SeriesControllerTests
{
    [Fact]
    public async Task GetSeriesById_WithValidId_ReturnsOkWithSeries()
    {
        // Arrange
        var mockSeriesService = new Mock<ISeriesService>();
        var mockDicomService = new Mock<IDicomImageService>();
        var mockLogger = new Mock<ILogger<SeriesController>>();

        var seriesDetail = new SeriesDetailDto(
            1, "1.2.3.4", "1", "Test Series", "CT", DateTime.UtcNow, "CHEST", "Protocol1",
            512, 512, 1.5, 1, new List<InstanceDto>()
        );

        mockSeriesService
            .Setup(s => s.GetSeriesByIdAsync(1))
            .ReturnsAsync(seriesDetail);

        var controller = new SeriesController(mockSeriesService.Object, mockDicomService.Object, mockLogger.Object);

        // Act
        var result = await controller.GetSeriesById(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedSeries = Assert.IsType<SeriesDetailDto>(okResult.Value);
        Assert.Equal(1, returnedSeries.Id);
        Assert.Equal("CT", returnedSeries.Modality);
    }

    [Fact]
    public async Task GetSeriesById_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var mockSeriesService = new Mock<ISeriesService>();
        var mockDicomService = new Mock<IDicomImageService>();
        var mockLogger = new Mock<ILogger<SeriesController>>();

        mockSeriesService
            .Setup(s => s.GetSeriesByIdAsync(999))
            .ReturnsAsync((SeriesDetailDto?)null);

        var controller = new SeriesController(mockSeriesService.Object, mockDicomService.Object, mockLogger.Object);

        // Act
        var result = await controller.GetSeriesById(999);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetSeriesByUid_WithValidUid_ReturnsOkWithSeries()
    {
        // Arrange
        var mockSeriesService = new Mock<ISeriesService>();
        var mockDicomService = new Mock<IDicomImageService>();
        var mockLogger = new Mock<ILogger<SeriesController>>();

        var seriesDetail = new SeriesDetailDto(
            1, "1.2.3.4.5", "1", "Test Series", "MR", DateTime.UtcNow, "HEAD", "Protocol2",
            256, 256, 2.0, 1, new List<InstanceDto>()
        );

        mockSeriesService
            .Setup(s => s.GetSeriesByUidAsync("1.2.3.4.5"))
            .ReturnsAsync(seriesDetail);

        var controller = new SeriesController(mockSeriesService.Object, mockDicomService.Object, mockLogger.Object);

        // Act
        var result = await controller.GetSeriesByUid("1.2.3.4.5");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedSeries = Assert.IsType<SeriesDetailDto>(okResult.Value);
        Assert.Equal("1.2.3.4.5", returnedSeries.SeriesInstanceUid);
        Assert.Equal("MR", returnedSeries.Modality);
    }

    [Fact]
    public async Task GetSeriesByUid_WithInvalidUid_ReturnsNotFound()
    {
        // Arrange
        var mockSeriesService = new Mock<ISeriesService>();
        var mockDicomService = new Mock<IDicomImageService>();
        var mockLogger = new Mock<ILogger<SeriesController>>();

        mockSeriesService
            .Setup(s => s.GetSeriesByUidAsync("INVALID.UID"))
            .ReturnsAsync((SeriesDetailDto?)null);

        var controller = new SeriesController(mockSeriesService.Object, mockDicomService.Object, mockLogger.Object);

        // Act
        var result = await controller.GetSeriesByUid("INVALID.UID");

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetInstances_WithValidSeriesId_ReturnsOkWithInstances()
    {
        // Arrange
        var mockSeriesService = new Mock<ISeriesService>();
        var mockDicomService = new Mock<IDicomImageService>();
        var mockLogger = new Mock<ILogger<SeriesController>>();

        var instances = new List<InstanceDto>
        {
            new InstanceDto(1, "1.2.3.4.5.1", null, 1, 512, 512, 40, 400, 0, 1, 1, null, null),
            new InstanceDto(2, "1.2.3.4.5.2", null, 2, 512, 512, 40, 400, 0, 1, 1, null, null)
        };

        mockSeriesService
            .Setup(s => s.GetInstancesAsync(1))
            .ReturnsAsync(instances);

        var controller = new SeriesController(mockSeriesService.Object, mockDicomService.Object, mockLogger.Object);

        // Act
        var result = await controller.GetInstances(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedInstances = Assert.IsType<List<InstanceDto>>(okResult.Value);
        Assert.Equal(2, returnedInstances.Count);
    }

    [Fact]
    public async Task GetInstances_WithNoInstances_ReturnsOkWithEmptyList()
    {
        // Arrange
        var mockSeriesService = new Mock<ISeriesService>();
        var mockDicomService = new Mock<IDicomImageService>();
        var mockLogger = new Mock<ILogger<SeriesController>>();

        mockSeriesService
            .Setup(s => s.GetInstancesAsync(1))
            .ReturnsAsync(new List<InstanceDto>());

        var controller = new SeriesController(mockSeriesService.Object, mockDicomService.Object, mockLogger.Object);

        // Act
        var result = await controller.GetInstances(1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedInstances = Assert.IsType<List<InstanceDto>>(okResult.Value);
        Assert.Empty(returnedInstances);
    }
}
