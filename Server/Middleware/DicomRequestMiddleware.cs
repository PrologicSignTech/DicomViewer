namespace MedView.Server.Middleware;

public class DicomRequestMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<DicomRequestMiddleware> _logger;

    public DicomRequestMiddleware(RequestDelegate next, ILogger<DicomRequestMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Log DICOM-related requests
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            _logger.LogInformation(
                "DICOM API Request: {Method} {Path}",
                context.Request.Method,
                context.Request.Path);
        }

        // Add CORS headers for DICOMweb compliance
        if (context.Request.Path.StartsWithSegments("/dicomweb"))
        {
            context.Response.Headers.Append("Access-Control-Allow-Origin", "*");
            context.Response.Headers.Append("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
            context.Response.Headers.Append("Access-Control-Allow-Headers", "Content-Type, Accept");
        }

        // Handle preflight requests
        if (context.Request.Method == "OPTIONS")
        {
            context.Response.StatusCode = 204;
            return;
        }

        await _next(context);
    }
}
