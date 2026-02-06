using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using MedView.Server.Data;
using MedView.Server.Models;
using MedView.Server.Models.DTOs;
using MedView.Server.Services;

namespace DicomServer.Tests.Services;

public class StudyServiceTests
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
    public async Task SearchStudiesAsync_WithPatientId_ReturnsMatchingStudies()
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
            NumberOfSeries = 0,
            NumberOfInstances = 0
        };
        context.Studies.Add(study);
        await context.SaveChangesAsync();

        var mockDicomService = new Mock<IDicomImageService>();
        var mockLogger = new Mock<ILogger<StudyService>>();
        var mockConfig = new Mock<IConfiguration>();
        var cache = CreateMemoryCache();
        
        var service = new StudyService(context, mockDicomService.Object, mockLogger.Object, mockConfig.Object, cache);
        var search = new StudySearchDto(PatientId: "PAT001", null, null, null, null, null, null, 1, 20);

        // Act
        var result = await service.SearchStudiesAsync(search);

        // Assert
        Assert.Single(result.Items);
        Assert.Equal("PAT001", result.Items.First().PatientId);
    }

    [Fact]
    public async Task SearchStudiesAsync_WithNoMatches_ReturnsEmptyList()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var mockDicomService = new Mock<IDicomImageService>();
        var mockLogger = new Mock<ILogger<StudyService>>();
        var mockConfig = new Mock<IConfiguration>();
        var cache = CreateMemoryCache();
        
        var service = new StudyService(context, mockDicomService.Object, mockLogger.Object, mockConfig.Object, cache);
        var search = new StudySearchDto(PatientId: "NONEXISTENT", null, null, null, null, null, null, 1, 20);

        // Act
        var result = await service.SearchStudiesAsync(search);

        // Assert
        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task GetStudyByIdAsync_WithValidId_ReturnsStudy()
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
            NumberOfSeries = 0,
            NumberOfInstances = 0
        };
        context.Studies.Add(study);
        await context.SaveChangesAsync();

        var mockDicomService = new Mock<IDicomImageService>();
        var mockLogger = new Mock<ILogger<StudyService>>();
        var mockConfig = new Mock<IConfiguration>();
        var cache = CreateMemoryCache();
        
        var service = new StudyService(context, mockDicomService.Object, mockLogger.Object, mockConfig.Object, cache);

        // Act
        var result = await service.GetStudyByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("1.2.3.4", result.StudyInstanceUid);
        Assert.Equal("PAT001", result.PatientId);
    }

    [Fact]
    public async Task GetStudyByIdAsync_WithInvalidId_ReturnsNull()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var mockDicomService = new Mock<IDicomImageService>();
        var mockLogger = new Mock<ILogger<StudyService>>();
        var mockConfig = new Mock<IConfiguration>();
        var cache = CreateMemoryCache();
        
        var service = new StudyService(context, mockDicomService.Object, mockLogger.Object, mockConfig.Object, cache);

        // Act
        var result = await service.GetStudyByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetStudyByUidAsync_WithValidUid_ReturnsStudy()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var study = new Study
        {
            Id = 1,
            StudyInstanceUid = "1.2.3.4.5",
            PatientId = "PAT001",
            PatientName = "Test Patient",
            StudyDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            NumberOfSeries = 0,
            NumberOfInstances = 0
        };
        context.Studies.Add(study);
        await context.SaveChangesAsync();

        var mockDicomService = new Mock<IDicomImageService>();
        var mockLogger = new Mock<ILogger<StudyService>>();
        var mockConfig = new Mock<IConfiguration>();
        var cache = CreateMemoryCache();
        
        var service = new StudyService(context, mockDicomService.Object, mockLogger.Object, mockConfig.Object, cache);

        // Act
        var result = await service.GetStudyByUidAsync("1.2.3.4.5");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("1.2.3.4.5", result.StudyInstanceUid);
    }

    [Fact]
    public async Task GetStudyByUidAsync_WithInvalidUid_ReturnsNull()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var mockDicomService = new Mock<IDicomImageService>();
        var mockLogger = new Mock<ILogger<StudyService>>();
        var mockConfig = new Mock<IConfiguration>();
        var cache = CreateMemoryCache();
        
        var service = new StudyService(context, mockDicomService.Object, mockLogger.Object, mockConfig.Object, cache);

        // Act
        var result = await service.GetStudyByUidAsync("INVALID.UID");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteStudyAsync_WithValidId_ReturnsTrue()
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
            NumberOfSeries = 0,
            NumberOfInstances = 0
        };
        context.Studies.Add(study);
        await context.SaveChangesAsync();
        
        // Clear the change tracker to avoid tracking conflicts with ExecuteDeleteAsync
        context.ChangeTracker.Clear();

        var mockDicomService = new Mock<IDicomImageService>();
        var mockLogger = new Mock<ILogger<StudyService>>();
        var mockConfig = new Mock<IConfiguration>();
        mockConfig.Setup(c => c["DicomSettings:StoragePath"]).Returns("./TestStorage");
        var cache = CreateMemoryCache();
        
        var service = new StudyService(context, mockDicomService.Object, mockLogger.Object, mockConfig.Object, cache);

        // Act
        var result = await service.DeleteStudyAsync(1);

        // Assert
        Assert.True(result);
        var deleted = await context.Studies.FindAsync(1);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task DeleteStudyAsync_WithInvalidId_ReturnsFalse()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var mockDicomService = new Mock<IDicomImageService>();
        var mockLogger = new Mock<ILogger<StudyService>>();
        var mockConfig = new Mock<IConfiguration>();
        var cache = CreateMemoryCache();
        
        var service = new StudyService(context, mockDicomService.Object, mockLogger.Object, mockConfig.Object, cache);

        // Act
        var result = await service.DeleteStudyAsync(999);

        // Assert
        Assert.False(result);
    }
}
