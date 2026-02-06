using System.Net.Http.Headers;
using System.Text.Json;
using MedView.Server.Models.DTOs;

namespace MedView.Server.Services;

public interface IDicomWebService
{
    Task<IEnumerable<StudyDto>> QueryStudiesAsync(string baseUrl, StudySearchDto search);
    Task<byte[]> RetrieveInstanceAsync(string baseUrl, string studyUid, string seriesUid, string instanceUid);
    Task<bool> StoreInstancesAsync(string baseUrl, IEnumerable<string> filePaths);
}

public class DicomWebService : IDicomWebService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<DicomWebService> _logger;
    private readonly IConfiguration _configuration;

    public DicomWebService(ILogger<DicomWebService> logger, IConfiguration configuration)
    {
        _httpClient = new HttpClient();
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// QIDO-RS: Query for studies
    /// </summary>
    public async Task<IEnumerable<StudyDto>> QueryStudiesAsync(string baseUrl, StudySearchDto search)
    {
        try
        {
            var queryParams = new List<string>();

            if (!string.IsNullOrEmpty(search.PatientId))
                queryParams.Add($"PatientID={Uri.EscapeDataString(search.PatientId)}");

            if (!string.IsNullOrEmpty(search.PatientName))
                queryParams.Add($"PatientName={Uri.EscapeDataString(search.PatientName)}");

            if (!string.IsNullOrEmpty(search.StudyDescription))
                queryParams.Add($"StudyDescription={Uri.EscapeDataString(search.StudyDescription)}");

            if (!string.IsNullOrEmpty(search.AccessionNumber))
                queryParams.Add($"AccessionNumber={Uri.EscapeDataString(search.AccessionNumber)}");

            if (search.StudyDateFrom.HasValue || search.StudyDateTo.HasValue)
            {
                var from = search.StudyDateFrom?.ToString("yyyyMMdd") ?? "";
                var to = search.StudyDateTo?.ToString("yyyyMMdd") ?? "";
                queryParams.Add($"StudyDate={from}-{to}");
            }

            if (!string.IsNullOrEmpty(search.Modality))
                queryParams.Add($"ModalitiesInStudy={Uri.EscapeDataString(search.Modality)}");

            queryParams.Add($"limit={search.PageSize}");
            queryParams.Add($"offset={(search.Page - 1) * search.PageSize}");

            var queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
            var url = $"{baseUrl}/studies{queryString}";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/dicom+json"));

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var results = JsonSerializer.Deserialize<List<Dictionary<string, JsonElement>>>(content);

            if (results == null) return Enumerable.Empty<StudyDto>();

            return results.Select(r => new StudyDto(
                0, // ID not available from QIDO
                GetDicomValue(r, "0020000D") ?? "", // Study Instance UID
                GetDicomValue(r, "00200010"), // Study ID
                GetDicomValue(r, "00081030"), // Study Description
                ParseDicomDate(GetDicomValue(r, "00080020")), // Study Date
                GetDicomValue(r, "00080050"), // Accession Number
                GetDicomValue(r, "00100020"), // Patient ID
                GetDicomValue(r, "00100010"), // Patient Name
                ParseDicomDate(GetDicomValue(r, "00100030")), // Patient Birth Date
                GetDicomValue(r, "00100040"), // Patient Sex
                GetDicomValue(r, "00101010"), // Patient Age
                GetDicomValue(r, "00080080"), // Institution Name
                int.TryParse(GetDicomValue(r, "00201206"), out var numSeries) ? numSeries : 0,
                int.TryParse(GetDicomValue(r, "00201208"), out var numInstances) ? numInstances : 0,
                DateTime.UtcNow
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying QIDO-RS: {Url}", baseUrl);
            throw;
        }
    }

    /// <summary>
    /// WADO-RS: Retrieve instance
    /// </summary>
    public async Task<byte[]> RetrieveInstanceAsync(string baseUrl, string studyUid, string seriesUid, string instanceUid)
    {
        try
        {
            var url = $"{baseUrl}/studies/{studyUid}/series/{seriesUid}/instances/{instanceUid}";
            
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/dicom"));

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsByteArrayAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving instance via WADO-RS");
            throw;
        }
    }

    /// <summary>
    /// STOW-RS: Store instances
    /// </summary>
    public async Task<bool> StoreInstancesAsync(string baseUrl, IEnumerable<string> filePaths)
    {
        try
        {
            var url = $"{baseUrl}/studies";

            using var content = new MultipartContent("related", $"----boundary{Guid.NewGuid()}");
            content.Headers.ContentType!.Parameters.Add(
                new NameValueHeaderValue("type", "\"application/dicom\""));

            foreach (var filePath in filePaths)
            {
                var fileContent = new ByteArrayContent(await File.ReadAllBytesAsync(filePath));
                fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/dicom");
                content.Add(fileContent);
            }

            var response = await _httpClient.PostAsync(url, content);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing instances via STOW-RS");
            return false;
        }
    }

    private static string? GetDicomValue(Dictionary<string, JsonElement> item, string tag)
    {
        if (item.TryGetValue(tag, out var element))
        {
            if (element.TryGetProperty("Value", out var valueArray) && 
                valueArray.ValueKind == JsonValueKind.Array && 
                valueArray.GetArrayLength() > 0)
            {
                var firstValue = valueArray[0];
                if (firstValue.ValueKind == JsonValueKind.String)
                    return firstValue.GetString();
                if (firstValue.ValueKind == JsonValueKind.Object && 
                    firstValue.TryGetProperty("Alphabetic", out var alphabetic))
                    return alphabetic.GetString();
                return firstValue.ToString();
            }
        }
        return null;
    }

    private static DateTime? ParseDicomDate(string? dateString)
    {
        if (string.IsNullOrEmpty(dateString)) return null;
        if (DateTime.TryParseExact(dateString, "yyyyMMdd", null, 
            System.Globalization.DateTimeStyles.None, out var date))
            return date;
        return null;
    }
}
