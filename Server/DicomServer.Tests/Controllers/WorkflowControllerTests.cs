using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using MedView.Server.Controllers;
using MedView.Server.Models;
using MedView.Server.Services;

namespace DicomServer.Tests.Controllers;

public class WorkflowControllerTests
{
    [Fact]
    public async Task CompareStudies_WithValidStudyId_ReturnsOkWithComparison()
    {
        // Arrange
        var mockWorkflowService = new Mock<IWorkflowService>();
        var mockLogger = new Mock<ILogger<WorkflowController>>();

        var currentStudy = new WorkflowStudyDto(1, "1.2.3.4", "Test Patient", "Test Study", DateTime.UtcNow, "CT");
        var comparison = new StudyComparisonResult(
            currentStudy,
            null,
            null,
            new List<SeriesComparisonPair>(),
            false
        );

        mockWorkflowService
            .Setup(s => s.CompareStudiesAsync(1, null))
            .ReturnsAsync(comparison);

        var controller = new WorkflowController(mockWorkflowService.Object, mockLogger.Object);

        // Act
        var result = await controller.CompareStudies(1, null);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedComparison = Assert.IsType<StudyComparisonResult>(okResult.Value);
        Assert.Equal(1, returnedComparison.CurrentStudy.Id);
        Assert.False(returnedComparison.HasPrior);
    }

    [Fact]
    public async Task CompareStudies_WithPriorStudyId_ReturnsOkWithComparisonIncludingPrior()
    {
        // Arrange
        var mockWorkflowService = new Mock<IWorkflowService>();
        var mockLogger = new Mock<ILogger<WorkflowController>>();

        var currentStudy = new WorkflowStudyDto(2, "1.2.3.4.current", "Test Patient", "Current Study", DateTime.UtcNow, "CT");
        var priorStudy = new WorkflowStudyDto(1, "1.2.3.4.prior", "Test Patient", "Prior Study", DateTime.UtcNow.AddDays(-30), "CT");
        var comparison = new StudyComparisonResult(
            currentStudy,
            priorStudy,
            TimeSpan.FromDays(30),
            new List<SeriesComparisonPair>(),
            true
        );

        mockWorkflowService
            .Setup(s => s.CompareStudiesAsync(2, 1))
            .ReturnsAsync(comparison);

        var controller = new WorkflowController(mockWorkflowService.Object, mockLogger.Object);

        // Act
        var result = await controller.CompareStudies(2, 1);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedComparison = Assert.IsType<StudyComparisonResult>(okResult.Value);
        Assert.Equal(2, returnedComparison.CurrentStudy.Id);
        Assert.True(returnedComparison.HasPrior);
        Assert.NotNull(returnedComparison.PriorStudy);
        Assert.Equal(1, returnedComparison.PriorStudy.Id);
    }

    [Fact]
    public async Task FindPriorStudies_WithValidStudyId_ReturnsOkWithPriors()
    {
        // Arrange
        var mockWorkflowService = new Mock<IWorkflowService>();
        var mockLogger = new Mock<ILogger<WorkflowController>>();

        var priors = new List<WorkflowStudyDto>
        {
            new WorkflowStudyDto(1, "1.2.3.4.1", "Test Patient", "Prior Study 1", DateTime.UtcNow.AddDays(-30), "CT"),
            new WorkflowStudyDto(2, "1.2.3.4.2", "Test Patient", "Prior Study 2", DateTime.UtcNow.AddDays(-60), "CT")
        };

        mockWorkflowService
            .Setup(s => s.FindPriorStudiesAsync(3, 5))
            .ReturnsAsync(priors);

        var controller = new WorkflowController(mockWorkflowService.Object, mockLogger.Object);

        // Act
        var result = await controller.FindPriorStudies(3, 5);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedPriors = Assert.IsType<List<WorkflowStudyDto>>(okResult.Value);
        Assert.Equal(2, returnedPriors.Count);
    }

    [Fact]
    public async Task FindPriorStudies_WithNoPriors_ReturnsOkWithEmptyList()
    {
        // Arrange
        var mockWorkflowService = new Mock<IWorkflowService>();
        var mockLogger = new Mock<ILogger<WorkflowController>>();

        mockWorkflowService
            .Setup(s => s.FindPriorStudiesAsync(1, 5))
            .ReturnsAsync(new List<WorkflowStudyDto>());

        var controller = new WorkflowController(mockWorkflowService.Object, mockLogger.Object);

        // Act
        var result = await controller.FindPriorStudies(1, 5);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedPriors = Assert.IsType<List<WorkflowStudyDto>>(okResult.Value);
        Assert.Empty(returnedPriors);
    }

    [Fact]
    public async Task ApplyHangingProtocol_WithValidStudyId_ReturnsOkWithProtocolResult()
    {
        // Arrange
        var mockWorkflowService = new Mock<IWorkflowService>();
        var mockLogger = new Mock<ILogger<WorkflowController>>();

        var protocolResult = new HangingProtocolResult(
            1,
            "Default CT Protocol",
            new List<ViewportAssignment>
            {
                new ViewportAssignment(0, 0, 0, 1, 1, null)
            },
            "1x1"
        );

        mockWorkflowService
            .Setup(s => s.ApplyHangingProtocolAsync(1, null))
            .ReturnsAsync(protocolResult);

        var controller = new WorkflowController(mockWorkflowService.Object, mockLogger.Object);

        // Act
        var result = await controller.ApplyHangingProtocol(1, null);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedResult = Assert.IsType<HangingProtocolResult>(okResult.Value);
        Assert.Equal("Default CT Protocol", returnedResult.ProtocolName);
        Assert.Single(returnedResult.ViewportAssignments);
    }

    [Fact]
    public async Task ApplyHangingProtocol_WithSpecificProtocolId_ReturnsOkWithProtocolResult()
    {
        // Arrange
        var mockWorkflowService = new Mock<IWorkflowService>();
        var mockLogger = new Mock<ILogger<WorkflowController>>();

        var protocolResult = new HangingProtocolResult(
            5,
            "Custom Protocol",
            new List<ViewportAssignment>
            {
                new ViewportAssignment(0, 0, 0, 1, 1, "Lung"),
                new ViewportAssignment(1, 0, 1, 2, 2, "Mediastinum")
            },
            "1x2"
        );

        mockWorkflowService
            .Setup(s => s.ApplyHangingProtocolAsync(1, 5))
            .ReturnsAsync(protocolResult);

        var controller = new WorkflowController(mockWorkflowService.Object, mockLogger.Object);

        // Act
        var result = await controller.ApplyHangingProtocol(1, 5);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedResult = Assert.IsType<HangingProtocolResult>(okResult.Value);
        Assert.Equal("Custom Protocol", returnedResult.ProtocolName);
        Assert.Equal(2, returnedResult.ViewportAssignments.Count);
    }
}
