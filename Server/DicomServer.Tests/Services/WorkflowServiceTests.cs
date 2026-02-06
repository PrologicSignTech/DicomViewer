using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using MedView.Server.Data;
using MedView.Server.Models;
using MedView.Server.Services;

namespace DicomServer.Tests.Services;

public class WorkflowServiceTests
{
    private DicomDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<DicomDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new DicomDbContext(options);
    }

    [Fact]
    public async Task CompareStudiesAsync_WithValidStudyId_ReturnsComparison()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var currentStudy = new Study
        {
            Id = 1,
            StudyInstanceUid = "1.2.3.4",
            PatientId = "PAT001",
            PatientName = "Test Patient",
            StudyDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            NumberOfSeries = 1,
            NumberOfInstances = 0
        };
        var currentSeries = new Series
        {
            Id = 1,
            SeriesInstanceUid = "1.2.3.4.1",
            Modality = "CT",
            StudyId = 1,
            Study = currentStudy,
            NumberOfInstances = 0
        };
        currentStudy.Series.Add(currentSeries);
        context.Studies.Add(currentStudy);
        await context.SaveChangesAsync();

        var mockLogger = new Mock<ILogger<WorkflowService>>();
        var service = new WorkflowService(context, mockLogger.Object);

        // Act
        var result = await service.CompareStudiesAsync(1, null);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.CurrentStudy.Id);
        Assert.False(result.HasPrior);
        Assert.Null(result.PriorStudy);
    }

    [Fact]
    public async Task CompareStudiesAsync_WithPriorStudy_ReturnsComparisonWithPrior()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var priorStudy = new Study
        {
            Id = 1,
            StudyInstanceUid = "1.2.3.4.prior",
            PatientId = "PAT001",
            PatientName = "Test Patient",
            StudyDate = DateTime.UtcNow.AddDays(-30),
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            NumberOfSeries = 1,
            NumberOfInstances = 0
        };
        var priorSeries = new Series
        {
            Id = 1,
            SeriesInstanceUid = "1.2.3.4.1.prior",
            Modality = "CT",
            StudyId = 1,
            Study = priorStudy,
            NumberOfInstances = 0
        };
        priorStudy.Series.Add(priorSeries);

        var currentStudy = new Study
        {
            Id = 2,
            StudyInstanceUid = "1.2.3.4.current",
            PatientId = "PAT001",
            PatientName = "Test Patient",
            StudyDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            NumberOfSeries = 1,
            NumberOfInstances = 0
        };
        var currentSeries = new Series
        {
            Id = 2,
            SeriesInstanceUid = "1.2.3.4.1.current",
            Modality = "CT",
            StudyId = 2,
            Study = currentStudy,
            NumberOfInstances = 0
        };
        currentStudy.Series.Add(currentSeries);

        context.Studies.AddRange(priorStudy, currentStudy);
        await context.SaveChangesAsync();

        var mockLogger = new Mock<ILogger<WorkflowService>>();
        var service = new WorkflowService(context, mockLogger.Object);

        // Act
        var result = await service.CompareStudiesAsync(2, 1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.CurrentStudy.Id);
        Assert.True(result.HasPrior);
        Assert.NotNull(result.PriorStudy);
        Assert.Equal(1, result.PriorStudy.Id);
    }

    [Fact]
    public async Task FindPriorStudiesAsync_WithPriorStudies_ReturnsPriors()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var prior1 = new Study
        {
            Id = 1,
            StudyInstanceUid = "1.2.3.4.1",
            PatientId = "PAT001",
            PatientName = "Test Patient",
            StudyDate = DateTime.UtcNow.AddDays(-60),
            CreatedAt = DateTime.UtcNow.AddDays(-60),
            NumberOfSeries = 0,
            NumberOfInstances = 0
        };
        var prior2 = new Study
        {
            Id = 2,
            StudyInstanceUid = "1.2.3.4.2",
            PatientId = "PAT001",
            PatientName = "Test Patient",
            StudyDate = DateTime.UtcNow.AddDays(-30),
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            NumberOfSeries = 0,
            NumberOfInstances = 0
        };
        var current = new Study
        {
            Id = 3,
            StudyInstanceUid = "1.2.3.4.3",
            PatientId = "PAT001",
            PatientName = "Test Patient",
            StudyDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            NumberOfSeries = 0,
            NumberOfInstances = 0
        };
        context.Studies.AddRange(prior1, prior2, current);
        await context.SaveChangesAsync();

        var mockLogger = new Mock<ILogger<WorkflowService>>();
        var service = new WorkflowService(context, mockLogger.Object);

        // Act
        var result = await service.FindPriorStudiesAsync(3, 5);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal(2, result[0].Id); // Most recent prior first
        Assert.Equal(1, result[1].Id);
    }

    [Fact]
    public async Task FindPriorStudiesAsync_WithNoPriors_ReturnsEmptyList()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var current = new Study
        {
            Id = 1,
            StudyInstanceUid = "1.2.3.4",
            PatientId = "PAT001",
            PatientName = "Test Patient",
            StudyDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            NumberOfSeries = 0,
            NumberOfInstances = 0
        };
        context.Studies.Add(current);
        await context.SaveChangesAsync();

        var mockLogger = new Mock<ILogger<WorkflowService>>();
        var service = new WorkflowService(context, mockLogger.Object);

        // Act
        var result = await service.FindPriorStudiesAsync(1, 5);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task ApplyHangingProtocolAsync_WithValidStudy_ReturnsProtocolResult()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var study = new Study
        {
            Id = 1,
            StudyInstanceUid = "1.2.3.4",
            PatientId = "PAT001",
            PatientName = "Test Patient",
            StudyDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            NumberOfSeries = 1,
            NumberOfInstances = 1
        };
        var series = new Series
        {
            Id = 1,
            SeriesInstanceUid = "1.2.3.4.1",
            Modality = "CT",
            StudyId = 1,
            Study = study,
            NumberOfInstances = 1
        };
        var instance = new Instance
        {
            Id = 1,
            SopInstanceUid = "1.2.3.4.1.1",
            SeriesId = 1,
            Series = series,
            NumberOfFrames = 1
        };
        study.Series.Add(series);
        series.Instances.Add(instance);
        context.Studies.Add(study);
        await context.SaveChangesAsync();

        var mockLogger = new Mock<ILogger<WorkflowService>>();
        var service = new WorkflowService(context, mockLogger.Object);

        // Act
        var result = await service.ApplyHangingProtocolAsync(1, null);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Default", result.ProtocolName);
        Assert.Single(result.ViewportAssignments);
    }

    [Fact]
    public async Task SynchronizeSeriesAsync_WithValidSeriesIds_ReturnsSyncResult()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var study = new Study
        {
            Id = 1,
            StudyInstanceUid = "1.2.3.4",
            PatientId = "PAT001",
            CreatedAt = DateTime.UtcNow,
            NumberOfSeries = 2,
            NumberOfInstances = 2
        };
        var series1 = new Series
        {
            Id = 1,
            SeriesInstanceUid = "1.2.3.4.1",
            Modality = "CT",
            StudyId = 1,
            Study = study,
            NumberOfInstances = 1
        };
        var instance1 = new Instance
        {
            Id = 1,
            SopInstanceUid = "1.2.3.4.1.1",
            SeriesId = 1,
            Series = series1,
            NumberOfFrames = 1,
            WindowCenter = 40,
            WindowWidth = 400
        };
        series1.Instances.Add(instance1);
        study.Series.Add(series1);
        context.Studies.Add(study);
        await context.SaveChangesAsync();

        var mockLogger = new Mock<ILogger<WorkflowService>>();
        var service = new WorkflowService(context, mockLogger.Object);

        // Act
        var result = await service.SynchronizeSeriesAsync(new List<int> { 1 }, SyncMode.Position);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.SyncedSeriesIds);
        Assert.Equal(SyncMode.Position, result.Mode);
    }
}
