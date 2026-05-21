using HolidayApi.Application.DTOs;
using HolidayApi.Application.Interfaces;
using HolidayApi.Domain.Entities;
using HolidayApi.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace HolidayApi.Application.Services;

/// <summary>
/// Orchestrates all holiday use cases.
/// </summary>
public sealed class HolidayService : IHolidayService
{
    private readonly IHolidayRepository _repository;
    private readonly INagerApiClient _apiClient;
    private readonly ILogger<HolidayService> _logger;

    public HolidayService(IHolidayRepository repository, INagerApiClient apiClient, ILogger<HolidayService> logger)
    {
        _repository = repository;
        _apiClient  = apiClient;
        _logger     = logger;
    }

    /// <summary>
    /// Fetches holiday data from the Nager API for a given year and country, then saves it to the database.
    /// </summary>
    /// <param name="year">The year for which to fetch holidays.</param>
    /// <param name="countryCode">The ISO 2-character country code.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A DTO containing the fetch result.</returns>
    public async Task<FetchResultDto> FetchAndSaveAsync(int year, string countryCode, CancellationToken ct = default)
    {
        countryCode = countryCode.Trim().ToUpperInvariant();

        // Check cache first — avoid hammering external API on repeat calls
        if (await _repository.ExistsAsync(year, countryCode, ct))
        {
            _logger.LogInformation("Holiday data for {Country}/{Year} already in DB — skipping fetch.", countryCode, year);

            return new FetchResultDto(countryCode, year, 0, WasCached: true);
        }

        _logger.LogInformation("Fetching holidays for {Country}/{Year} from Nager API.", countryCode, year);
        var apiData = await _apiClient.GetPublicHolidaysAsync(year, countryCode, ct);
        // Map API DTOs → Domain Entities using the factory method
        // The factory enforces invariants (e.g. CountryCode must be 2 chars)
        var entities = apiData
            .Select(dto => PublicHoliday.Create(
                DateOnly.Parse(dto.Date),
                dto.LocalName,
                dto.Name,
                dto.CountryCode,
                dto.Global,
                dto.Types))
            .ToList();

        await _repository.UpsertBatchAsync(entities, ct);

        _logger.LogInformation("Saved {Count} holidays for {Country}/{Year}.", entities.Count, countryCode, year);

        return new FetchResultDto(countryCode, year, entities.Count, WasCached: false);
    }

    /// <summary>
    /// Retrieves the last 3 holidays that have been celebrated for a given country.
    /// </summary>
    /// <param name="countryCode">The ISO 2-character country code.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A list of DTOs representing the last celebrated holidays.</returns>
    public async Task<IReadOnlyList<LastCelebratedDto>> GetLastCelebratedAsync(string countryCode, CancellationToken ct = default)
    {
        var holidays = await _repository.GetLastCelebratedAsync(countryCode, count: 3, ct);

        return holidays
            .Select(h => new LastCelebratedDto(h.Date, h.Name))
            .ToList();
    }

    /// <summary>
    /// Returns the count of holidays that fall on a weekday (Mon-Fri) for each specified country in a given year.
    /// </summary>
    /// <param name="year">The year for which to count weekday holidays.</param>
    /// <param name="countryCodes">A collection of ISO 2-character country codes.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A list of DTOs representing the weekday holiday counts per country.</returns>
    public async Task<IReadOnlyList<WeekdayCountDto>> GetWeekdayHolidayCountsAsync(
        int year, IEnumerable<string> countryCodes, CancellationToken ct = default)
    {
        // The repository performs the grouping + counting in SQL (not in memory)
        var results = await _repository.GetWeekdayHolidayCountsAsync(year, countryCodes, ct);

        return results
            .Select(r => new WeekdayCountDto(r.CountryCode, r.Count))
            .OrderByDescending(r => r.WeekdayHolidayCount)
            .ToList();
    }

    /// <summary>
    /// Retrieves holidays that are celebrated on the same date in two different countries for a given year.
    /// </summary>
    /// <param name="year">The year for which to retrieve shared holidays.</param>
    /// <param name="countryCodeA">The ISO 2-character country code for the first country.</param>      
    /// <param name="countryCodeB">The ISO 2-character country code for the second country.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A list of DTOs representing the shared holidays.</returns>
    /// <exception cref="ArgumentException"></exception>
    public async Task<IReadOnlyList<SharedHolidayDto>> GetSharedHolidaysAsync(
        int year, string countryCodeA, string countryCodeB, CancellationToken ct = default)
    {
        
        var shared = await _repository.GetSharedHolidaysAsync(year, countryCodeA, countryCodeB, ct);

        return shared
            .Select(s => new SharedHolidayDto(s.Date, s.LocalNameA, s.LocalNameB))
            .ToList();
    }
}
