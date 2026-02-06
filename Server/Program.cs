using FellowOakDicom;
using FellowOakDicom.Imaging;
using MedView.Server.Data;
using MedView.Server.Services;
using MedView.Server.Middleware;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "MedView DICOM API", Version = "v1" });
});

// Configure CORS for React frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "https://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Database context
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<DicomDbContext>(options =>
{
    var serverVersion = new MySqlServerVersion(new Version(8, 0, 35));
    
    options.UseMySql(connectionString, serverVersion, mySqlOptions =>
    {
        // Enable retry on failure for transient errors (critical for RDS)
        mySqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null);
        
        // Reduce command timeout to release connections faster
        mySqlOptions.CommandTimeout(30);
        
        // Optimize for RDS - minimize connection overhead
        mySqlOptions.MigrationsAssembly("MedView.Server");
    })
    // Performance optimizations for high-load RDS scenarios
    .EnableSensitiveDataLogging(builder.Environment.IsDevelopment())
    .EnableDetailedErrors(builder.Environment.IsDevelopment())
    // Query splitting to avoid cartesian explosion on large datasets
    .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
}, 
// Connection pooling is critical for RDS performance
ServiceLifetime.Scoped,
ServiceLifetime.Singleton);

// Add memory cache for frequently accessed data
builder.Services.AddMemoryCache(options =>
{
    options.SizeLimit = 1024; // Limit cache size
    options.CompactionPercentage = 0.25;
});

// Register DICOM services - Core
builder.Services.AddSingleton<IDicomImageService, DicomImageService>();
builder.Services.AddScoped<IStudyService, StudyService>();
builder.Services.AddScoped<ISeriesService, SeriesService>();
builder.Services.AddScoped<IMeasurementService, MeasurementService>();
builder.Services.AddScoped<IAnnotationService, AnnotationService>();
builder.Services.AddScoped<IDicomWebService, DicomWebService>();
builder.Services.AddSingleton<IEncryptionService, EncryptionService>();

// Register DICOM services - Advanced Features
builder.Services.AddScoped<IAdvancedImagingService, AdvancedImagingService>();
builder.Services.AddScoped<IAdvancedMeasurementService, AdvancedMeasurementService>();
builder.Services.AddScoped<IWorkflowService, WorkflowService>();
builder.Services.AddScoped<IReportingService, ReportingService>();

// Configure fo-dicom
new DicomSetupBuilder()
    .RegisterServices(s => s.AddFellowOakDicom().AddImageManager<ImageSharpImageManager>())
    .Build();

// Configure file upload limits
builder.Services.Configure<IISServerOptions>(options =>
{
    options.MaxRequestBodySize = 500 * 1024 * 1024; // 500MB
});

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 500 * 1024 * 1024; // 500MB
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowReactApp");

// Custom middleware for DICOM request handling
app.UseMiddleware<DicomRequestMiddleware>();

app.UseAuthorization();

// Serve static files for React app in production
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();

// Fallback to index.html for SPA routing
app.MapFallbackToFile("index.html");

// Ensure database is created and migrated
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    
    try
    {
        var dbContext = services.GetRequiredService<DicomDbContext>();
        var autoMigrate = builder.Configuration.GetValue<bool>("Database:AutoMigrate", true);
        
        if (autoMigrate)
        {
            logger.LogInformation("Starting database migration...");
            // Use Migrate() instead of EnsureCreated() for production scenarios
            dbContext.Database.Migrate();
            logger.LogInformation("Database migration completed successfully");
        }
        else
        {
            // Just ensure database exists without migrations
            dbContext.Database.EnsureCreated();
            logger.LogInformation("Database verified");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while migrating the database");
        // In production, you might want to fail fast
        throw;
    }
}

app.Run();
