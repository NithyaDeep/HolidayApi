using HolidayApi.Domain.Entities;

namespace HolidayApi.Domain.Interfaces;

/// <summary>
/// Repository contract defined in the DOMAIN layer.
/// </summary>
public interface IHolidayRepository
{
    /// <summary>
    /// Upserts a batch of holidays — safe to call multiple times for the same year/country.
    /// </summary>
    Task UpsertBatchAsync(IEnumerable<PublicHoliday> holidays, CancellationToken ct = default);

    /// <summary>
    /// Returns the last N holidays (by date) that have already occurred for a given country.
    /// </summary>
    Task<IReadOnlyList<PublicHoliday>> GetLastCelebratedAsync(string countryCode, int count, CancellationToken ct = default);

    /// <summary>
    /// Returns the count of weekday-only public holidays per country for a given year,
    /// sorted descending by count.
    /// </summary>
    Task<IReadOnlyList<(string CountryCode, int Count)>> GetWeekdayHolidayCountsAsync(int year, IEnumerable<string> countryCodes, CancellationToken ct = default);

    /// <summary>
    /// Returns dates that are holidays in BOTH countries for a given year,
    /// with local names from each country.
    /// </summary>
    Task<IReadOnlyList<SharedHolidayResult>> GetSharedHolidaysAsync(int year, string countryCodeA, string countryCodeB, CancellationToken ct = default);

    /// <summary>
    /// Checks whether holiday data already exists for a given year and country.
    /// Used to decide whether to re-fetch from the external API.
    /// </summary>
    Task<bool> ExistsAsync(int year, string countryCode, CancellationToken ct = default);
}

/// <summary>
/// A value-object style result for shared-holiday queries.
/// </summary>
public record SharedHolidayResult(DateOnly Date, string LocalNameA, string LocalNameB);
