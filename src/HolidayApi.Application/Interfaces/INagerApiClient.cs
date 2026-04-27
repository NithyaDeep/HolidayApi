namespace HolidayApi.Application.Interfaces;

/// <summary>
/// Contract for fetching holiday data from an external source.
/// </summary>
public interface INagerApiClient
{
    Task<IReadOnlyList<NagerHolidayDto>> GetPublicHolidaysAsync(int year, string countryCode, CancellationToken ct = default);
}

/// <summary>
/// Maps 1:1 to the Nager.Date API JSON response.
/// Kept in Application because the service needs to understand what the API returns.
/// </summary>
public record NagerHolidayDto(
    string Date,
    string LocalName,
    string Name,
    string CountryCode,
    bool Global,
    IEnumerable<string>? Types
);
