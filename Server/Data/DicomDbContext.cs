using Microsoft.EntityFrameworkCore;
using MedView.Server.Models;

namespace MedView.Server.Data;

public class DicomDbContext : DbContext
{
    public DicomDbContext(DbContextOptions<DicomDbContext> options) : base(options)
    {
    }

    public DbSet<Study> Studies { get; set; }
    public DbSet<Series> Series { get; set; }
    public DbSet<Instance> Instances { get; set; }
    public DbSet<Annotation> Annotations { get; set; }
    public DbSet<Measurement> Measurements { get; set; }
    public DbSet<HangingProtocol> HangingProtocols { get; set; }
    public DbSet<UserSettings> UserSettings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Study configuration
        modelBuilder.Entity<Study>(entity =>
        {
            entity.HasIndex(e => e.StudyInstanceUid).IsUnique();
            entity.HasIndex(e => e.PatientId);
            entity.HasIndex(e => e.PatientName);
            entity.HasIndex(e => e.StudyDate);
            entity.HasIndex(e => e.AccessionNumber);
            
            // MySQL-specific string length configurations
            entity.Property(e => e.StudyInstanceUid).HasMaxLength(64);
            entity.Property(e => e.StudyId).HasMaxLength(64);
            entity.Property(e => e.StudyDescription).HasMaxLength(255);
            entity.Property(e => e.AccessionNumber).HasMaxLength(64);
            entity.Property(e => e.ReferringPhysicianName).HasMaxLength(255);
            entity.Property(e => e.PatientId).HasMaxLength(64);
            entity.Property(e => e.PatientName).HasMaxLength(255);
            entity.Property(e => e.PatientSex).HasMaxLength(16);
            entity.Property(e => e.PatientAge).HasMaxLength(16);
            entity.Property(e => e.InstitutionName).HasMaxLength(255);
            entity.Property(e => e.StudyTime).HasMaxLength(16);
        });

        // Series configuration
        modelBuilder.Entity<Series>(entity =>
        {
            entity.HasIndex(e => e.SeriesInstanceUid).IsUnique();
            entity.HasIndex(e => e.Modality);
            entity.HasIndex(e => e.StudyId);
            
            entity.HasOne(e => e.Study)
                  .WithMany(s => s.Series)
                  .HasForeignKey(e => e.StudyId)
                  .OnDelete(DeleteBehavior.Cascade);
            
            // MySQL-specific string length configurations
            entity.Property(e => e.SeriesInstanceUid).HasMaxLength(64);
            entity.Property(e => e.SeriesNumber).HasMaxLength(16);
            entity.Property(e => e.SeriesDescription).HasMaxLength(255);
            entity.Property(e => e.Modality).HasMaxLength(16);
            entity.Property(e => e.SeriesTime).HasMaxLength(16);
            entity.Property(e => e.BodyPartExamined).HasMaxLength(64);
            entity.Property(e => e.ProtocolName).HasMaxLength(255);
            entity.Property(e => e.ImageOrientationPatient).HasMaxLength(255);
        });

        // Instance configuration
        modelBuilder.Entity<Instance>(entity =>
        {
            entity.HasIndex(e => e.SopInstanceUid).IsUnique();
            entity.HasIndex(e => e.SeriesId);
            
            entity.HasOne(e => e.Series)
                  .WithMany(s => s.Instances)
                  .HasForeignKey(e => e.SeriesId)
                  .OnDelete(DeleteBehavior.Cascade);
            
            // MySQL-specific string length configurations
            entity.Property(e => e.SopInstanceUid).HasMaxLength(64);
            entity.Property(e => e.SopClassUid).HasMaxLength(64);
            entity.Property(e => e.FilePath).HasMaxLength(512);
            entity.Property(e => e.PhotometricInterpretation).HasMaxLength(64);
            entity.Property(e => e.PixelSpacing).HasMaxLength(64);
            entity.Property(e => e.ImagePositionPatient).HasMaxLength(255);
            entity.Property(e => e.ImageOrientationPatient).HasMaxLength(255);
            entity.Property(e => e.TransferSyntaxUid).HasMaxLength(64);
        });

        // Annotation configuration
        modelBuilder.Entity<Annotation>(entity =>
        {
            entity.HasIndex(e => e.InstanceId);
            
            entity.HasOne(e => e.Instance)
                  .WithMany(i => i.Annotations)
                  .HasForeignKey(e => e.InstanceId)
                  .OnDelete(DeleteBehavior.Cascade);
            
            // MySQL-specific string length configurations
            entity.Property(e => e.Type).HasMaxLength(64);
            entity.Property(e => e.Color).HasMaxLength(32);
            entity.Property(e => e.CreatedBy).HasMaxLength(255);
        });

        // Measurement configuration
        modelBuilder.Entity<Measurement>(entity =>
        {
            entity.HasIndex(e => e.InstanceId);
            
            entity.HasOne(e => e.Instance)
                  .WithMany(i => i.Measurements)
                  .HasForeignKey(e => e.InstanceId)
                  .OnDelete(DeleteBehavior.Cascade);
            
            // MySQL-specific string length configurations
            entity.Property(e => e.Type).HasMaxLength(64);
            entity.Property(e => e.Unit).HasMaxLength(32);
            entity.Property(e => e.Label).HasMaxLength(255);
            entity.Property(e => e.Color).HasMaxLength(32);
            entity.Property(e => e.CreatedBy).HasMaxLength(255);
        });

        // Hanging Protocol configuration
        modelBuilder.Entity<HangingProtocol>(entity =>
        {
            entity.HasIndex(e => new { e.Modality, e.BodyPart });
            
            // MySQL-specific string length configurations
            entity.Property(e => e.Name).HasMaxLength(255);
            entity.Property(e => e.Modality).HasMaxLength(16);
            entity.Property(e => e.BodyPart).HasMaxLength(64);
        });

        // User Settings configuration
        modelBuilder.Entity<UserSettings>(entity =>
        {
            entity.HasIndex(e => e.UserId).IsUnique();
            
            // MySQL-specific string length configurations
            entity.Property(e => e.UserId).HasMaxLength(255);
            entity.Property(e => e.DefaultLayout).HasMaxLength(32);
            entity.Property(e => e.DefaultTool).HasMaxLength(64);
            entity.Property(e => e.MeasurementColor).HasMaxLength(32);
            entity.Property(e => e.AnnotationColor).HasMaxLength(32);
        });

        // Seed default hanging protocols
        SeedHangingProtocols(modelBuilder);
        
        // Seed default window presets
        SeedDefaultData(modelBuilder);
    }

    private static void SeedHangingProtocols(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<HangingProtocol>().HasData(
            new HangingProtocol
            {
                Id = 1,
                Name = "CT Default",
                Description = "Default layout for CT studies",
                Modality = "CT",
                LayoutConfig = @"{
                    ""rows"": 1,
                    ""columns"": 1,
                    ""viewports"": [
                        {""position"": 0, ""seriesIndex"": 0}
                    ]
                }",
                Priority = 100,
                IsDefault = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new HangingProtocol
            {
                Id = 2,
                Name = "MR Brain",
                Description = "Multi-sequence brain MRI layout",
                Modality = "MR",
                BodyPart = "HEAD",
                LayoutConfig = @"{
                    ""rows"": 2,
                    ""columns"": 2,
                    ""viewports"": [
                        {""position"": 0, ""seriesDescription"": ""T1""},
                        {""position"": 1, ""seriesDescription"": ""T2""},
                        {""position"": 2, ""seriesDescription"": ""FLAIR""},
                        {""position"": 3, ""seriesDescription"": ""DWI""}
                    ]
                }",
                Priority = 90,
                IsDefault = false,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new HangingProtocol
            {
                Id = 3,
                Name = "Chest X-Ray Comparison",
                Description = "Side-by-side comparison for chest X-rays",
                Modality = "CR",
                BodyPart = "CHEST",
                LayoutConfig = @"{
                    ""rows"": 1,
                    ""columns"": 2,
                    ""viewports"": [
                        {""position"": 0, ""studyIndex"": 0, ""seriesIndex"": 0},
                        {""position"": 1, ""studyIndex"": 1, ""seriesIndex"": 0}
                    ],
                    ""enablePriorComparison"": true
                }",
                Priority = 80,
                IsDefault = false,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new HangingProtocol
            {
                Id = 4,
                Name = "Mammography",
                Description = "Standard mammography display",
                Modality = "MG",
                LayoutConfig = @"{
                    ""rows"": 2,
                    ""columns"": 2,
                    ""viewports"": [
                        {""position"": 0, ""viewPosition"": ""MLO"", ""laterality"": ""R""},
                        {""position"": 1, ""viewPosition"": ""MLO"", ""laterality"": ""L""},
                        {""position"": 2, ""viewPosition"": ""CC"", ""laterality"": ""R""},
                        {""position"": 3, ""viewPosition"": ""CC"", ""laterality"": ""L""}
                    ]
                }",
                Priority = 95,
                IsDefault = false,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            }
        );
    }

    private static void SeedDefaultData(ModelBuilder modelBuilder)
    {
        // Add default user settings template
        modelBuilder.Entity<UserSettings>().HasData(
            new UserSettings
            {
                Id = 1,
                UserId = "default",
                DefaultLayout = "1x1",
                DefaultWindowPresets = @"[
                    {""name"": ""CT Abdomen"", ""wc"": 40, ""ww"": 400},
                    {""name"": ""CT Bone"", ""wc"": 500, ""ww"": 2000},
                    {""name"": ""CT Brain"", ""wc"": 40, ""ww"": 80},
                    {""name"": ""CT Chest"", ""wc"": -600, ""ww"": 1500},
                    {""name"": ""CT Lung"", ""wc"": -400, ""ww"": 1500}
                ]",
                ShowAnnotations = true,
                ShowMeasurements = true,
                ShowOverlay = true,
                DefaultTool = "wwwc",
                MeasurementColor = "#FFFF00",
                AnnotationColor = "#00FF00",
                LastUpdated = DateTime.UtcNow
            }
        );
    }
}
