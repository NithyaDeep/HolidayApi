using HolidayApi.Application.DTOs;

namespace HolidayApi.Application.Interfaces;

/// <summary>
/// Application service contract.
/// </summary>
public interface IHolidayService
{
    /// <summary>Fetches from external API and saves to DB. Idempotent.</summary>
    Task<FetchResultDto> FetchAndSaveAsync(int year, string countryCode, CancellationToken ct = default);

    /// <summary>Returns the last 3 holidays that have already occurred for a country.</summary>
    Task<IReadOnlyList<LastCelebratedDto>> GetLastCelebratedAsync(string countryCode, CancellationToken ct = default);

    /// <summary>
    /// For each country in the list, returns the count of public holidays not on weekends,
    /// for the given year. Result is sorted descending by count.
    /// </summary>
    Task<IReadOnlyList<WeekdayCountDto>> GetWeekdayHolidayCountsAsync(int year, IEnumerable<string> countryCodes, CancellationToken ct = default);

    /// <summary>
    /// Returns the deduplicated list of dates celebrated in both countries,
    /// with local names from each country.
    /// </summary>
    Task<IReadOnlyList<SharedHolidayDto>> GetSharedHolidaysAsync(int year, string countryCodeA, string countryCodeB, CancellationToken ct = default);
}
