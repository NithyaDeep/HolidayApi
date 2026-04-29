using HolidayApi.Application.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace HolidayApi.Infrastructure.ExternalServices;

/// <summary>
/// HTTP client for the Nager.Date public holiday API.
/// </summary>
public sealed class NagerApiClient : INagerApiClient
{
    private readonly HttpClient _http;
    private readonly ILogger<NagerApiClient> _logger;

    public NagerApiClient(HttpClient http, ILogger<NagerApiClient> logger)
    {
        _http   = http;
        _logger = logger;
    }

    public async Task<IReadOnlyList<NagerHolidayDto>> GetPublicHolidaysAsync(
        int year, string countryCode, CancellationToken ct = default)
    {
        var url = $"api/v3/PublicHolidays/{year}/{countryCode}";
        _logger.LogInformation("GET {Url}", url);

        var response = await _http.GetAsync(url, ct);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("No holidays found for {Country}/{Year} — country may not be supported.", countryCode, year);
            return Array.Empty<NagerHolidayDto>();
        }

        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(ct);

        if (string.IsNullOrWhiteSpace(content))
        {
            _logger.LogWarning("Empty response body for {CountryCode}/{Year} — country not supported by Nager API.",
                countryCode, year);
            return Array.Empty<NagerHolidayDto>();
        }

        var result = JsonSerializer.Deserialize<List<NagerApiResponse>>(content,
        new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
        ?? new List<NagerApiResponse>();

        return result
            .Select(r => new NagerHolidayDto(
                r.Date,
                r.LocalName,
                r.Name,
                r.CountryCode,
                r.Global,
                r.Types))
            .ToList();
    }

    private record NagerApiResponse(
        string Date,
        string LocalName,
        string Name,
        string CountryCode,
        bool Global,
        IEnumerable<string>? Types
    );
}
