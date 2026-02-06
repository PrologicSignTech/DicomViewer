using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using MedView.Server.Data;
using MedView.Server.Models;
using MedView.Server.Models.DTOs;
using MedView.Server.Services;

namespace DicomServer.Tests.Services;

public class SeriesServiceTests
{
    private DicomDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<DicomDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new DicomDbContext(options);
    }

    private IMemoryCache CreateMemoryCache()
    {
        return new MemoryCache(new MemoryCacheOptions());
    }

    [Fact]
    public async Task GetSeriesByIdAsync_WithValidId_ReturnsSeries()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var study = new Study
        {
            Id = 1,
            StudyInstanceUid = "1.2.3",
            PatientId = "PAT001",
            CreatedAt = DateTime.UtcNow,
            NumberOfSeries = 1,
            NumberOfInstances = 0
        };
        var series = new Series
        {
            Id = 1,
            SeriesInstanceUid = "1.2.3.4",
            SeriesNumber = "1",
            Modality = "CT",
            StudyId = 1,
            Study = study,
            NumberOfInstances = 0
        };
        context.Studies.Add(study);
        context.Series.Add(series);
        await context.SaveChangesAsync();

        var mockLogger = new Mock<ILogger<SeriesService>>();
        var cache = CreateMemoryCache();
        var service = new SeriesService(context, mockLogger.Object, cache);

        // Act
        var result = await service.GetSeriesByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("1.2.3.4", result.SeriesInstanceUid);
        Assert.Equal("CT", result.Modality);
    }

    [Fact]
    public async Task GetSeriesByIdAsync_WithInvalidId_ReturnsNull()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var mockLogger = new Mock<ILogger<SeriesService>>();
        var cache = CreateMemoryCache();
        var service = new SeriesService(context, mockLogger.Object, cache);

        // Act
        var result = await service.GetSeriesByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetSeriesByUidAsync_WithValidUid_ReturnsSeries()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var study = new Study
        {
            Id = 1,
            StudyInstanceUid = "1.2.3",
            PatientId = "PAT001",
            CreatedAt = DateTime.UtcNow,
            NumberOfSeries = 1,
            NumberOfInstances = 0
        };
        var series = new Series
        {
            Id = 1,
            SeriesInstanceUid = "1.2.3.4.5",
            SeriesNumber = "1",
            Modality = "MR",
            StudyId = 1,
            Study = study,
            NumberOfInstances = 0
        };
        context.Studies.Add(study);
        context.Series.Add(series);
        await context.SaveChangesAsync();

        var mockLogger = new Mock<ILogger<SeriesService>>();
        var cache = CreateMemoryCache();
        var service = new SeriesService(context, mockLogger.Object, cache);

        // Act
        var result = await service.GetSeriesByUidAsync("1.2.3.4.5");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("1.2.3.4.5", result.SeriesInstanceUid);
        Assert.Equal("MR", result.Modality);
    }

    [Fact]
    public async Task GetSeriesByUidAsync_WithInvalidUid_ReturnsNull()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var mockLogger = new Mock<ILogger<SeriesService>>();
        var cache = CreateMemoryCache();
        var service = new SeriesService(context, mockLogger.Object, cache);

        // Act
        var result = await service.GetSeriesByUidAsync("INVALID.UID");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetInstancesAsync_WithValidSeriesId_ReturnsInstances()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var study = new Study
        {
            Id = 1,
            StudyInstanceUid = "1.2.3",
            PatientId = "PAT001",
            CreatedAt = DateTime.UtcNow,
            NumberOfSeries = 1,
            NumberOfInstances = 2
        };
        var series = new Series
        {
            Id = 1,
            SeriesInstanceUid = "1.2.3.4",
            SeriesNumber = "1",
            Modality = "CT",
            StudyId = 1,
            Study = study,
            NumberOfInstances = 2
        };
        var instance1 = new Instance
        {
            Id = 1,
            SopInstanceUid = "1.2.3.4.5.1",
            InstanceNumber = 1,
            SeriesId = 1,
            Series = series,
            NumberOfFrames = 1
        };
        var instance2 = new Instance
        {
            Id = 2,
            SopInstanceUid = "1.2.3.4.5.2",
            InstanceNumber = 2,
            SeriesId = 1,
            Series = series,
            NumberOfFrames = 1
        };
        context.Studies.Add(study);
        context.Series.Add(series);
        context.Instances.AddRange(instance1, instance2);
        await context.SaveChangesAsync();

        var mockLogger = new Mock<ILogger<SeriesService>>();
        var cache = CreateMemoryCache();
        var service = new SeriesService(context, mockLogger.Object, cache);

        // Act
        var result = await service.GetInstancesAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task GetInstancesAsync_WithNoInstances_ReturnsEmptyList()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var study = new Study
        {
            Id = 1,
            StudyInstanceUid = "1.2.3",
            PatientId = "PAT001",
            CreatedAt = DateTime.UtcNow,
            NumberOfSeries = 1,
            NumberOfInstances = 0
        };
        var series = new Series
        {
            Id = 1,
            SeriesInstanceUid = "1.2.3.4",
            SeriesNumber = "1",
            Modality = "CT",
            StudyId = 1,
            Study = study,
            NumberOfInstances = 0
        };
        context.Studies.Add(study);
        context.Series.Add(series);
        await context.SaveChangesAsync();

        var mockLogger = new Mock<ILogger<SeriesService>>();
        var cache = CreateMemoryCache();
        var service = new SeriesService(context, mockLogger.Object, cache);

        // Act
        var result = await service.GetInstancesAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }
}
