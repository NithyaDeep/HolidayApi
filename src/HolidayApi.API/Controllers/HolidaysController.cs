using HolidayApi.Application.DTOs;
using HolidayApi.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HolidayApi.API.Controllers;

/// <summary>
/// REST API surface for holiday operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class HolidaysController : ControllerBase
{
    private readonly IHolidayService _holidayService;

    public HolidaysController(IHolidayService holidayService) => _holidayService = holidayService;

    private const string InvalidYearMessage =
       "Year must be between 1900 and 2100.";

    private const string InvalidCountryMessage =
        "countryCode must be a valid 2-letter ISO 3166-1 alpha-2 code (e.g. NL, BE).";

    private const string InvalidTwoCountriesMessage =
        "Both country codes must be valid 2-letter ISO 3166-1 alpha-2 codes (e.g. NL, BE).";

    private const string SameCountryMessage =
        "countryCodeA and countryCodeB must be different.";
    private bool IsInvalidYear(int year)
        => year < 1900 || year > 2100;

    private string? NormalizeCountry(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return null;
        }

        var normalized = code.Trim().ToUpperInvariant();

        return normalized.Length == 2 
            ? normalized 
            : null;
    }

    // GET /api/holidays/fetch
    /// <summary>
    /// Fetches public holidays from the Nager.Date API and saves them to the database.
    /// </summary>
    [HttpGet("fetch")]
    [ProducesResponseType(typeof(FetchResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Fetch([FromQuery] int year, [FromQuery] string countryCode, CancellationToken ct)
    {
        if (IsInvalidYear(year))
        {
            return BadRequest(InvalidYearMessage);
        }
        var code = NormalizeCountry(countryCode);
        if (code is null)
        {
            return BadRequest(InvalidCountryMessage);
        }

        var result = await _holidayService.FetchAndSaveAsync(year, code, ct);

        if (result.RecordsSaved == 0 && !result.WasCached)
        {
            return BadRequest(new
            {
                message = $"No holiday data found for country code '{countryCode}'. " +
                          $"This country may not be supported by the Nager.Date API. " +
                          $"Check supported countries at: https://date.nager.at/api/v3/AvailableCountries"
            });
        }

        return Ok(result);
    }

    // GET /api/holidays/last-celebrated
    /// <summary>
    /// Returns the last 3 public holidays that have already occurred for the given country.
    /// Data must already be fetched via /fetch before calling this endpoint.
    /// </summary>
    [HttpGet("last-celebrated")]
    [ProducesResponseType(typeof(IReadOnlyList<LastCelebratedDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> LastCelebrated([FromQuery] string countryCode,
        CancellationToken ct)
    {
        var code = NormalizeCountry(countryCode);
        if (code is null)
        {
            return BadRequest(InvalidCountryMessage);
        }

        var result = await _holidayService.GetLastCelebratedAsync(code, ct);

        return Ok(result);
    }

    // GET /api/holidays/weekday-counts
    /// <summary>
    /// For each country, returns the count of public holidays NOT falling on weekends,
    /// for the given year. Results sorted descending by count.
    /// Pass multiple countryCodes as: ?year=2025&amp;countryCodes=NL&amp;countryCodes=BE&amp;countryCodes=DE
    /// </summary>
    [HttpGet("weekday-counts")]
    [ProducesResponseType(typeof(IReadOnlyList<WeekdayCountDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> WeekdayCounts([FromQuery] int year, [FromQuery] IEnumerable<string> countryCodes,
        CancellationToken ct)
    {
        if (IsInvalidYear(year))
        {
            return BadRequest(InvalidYearMessage);
        }

        var codes = countryCodes
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Select(c => c.Trim().ToUpperInvariant())
            .Distinct()
            .ToList();

        if (codes.Count == 0)
        {
            return BadRequest("At least one countryCode is required.");
        }

        if (codes.Any(c => c.Length != 2))
        {
            return BadRequest(InvalidCountryMessage);
        }

        var result = await _holidayService.GetWeekdayHolidayCountsAsync(year, codes, ct);

        return Ok(result);
    }

    // GET /api/holidays/shared
    /// <summary>
    /// Returns the deduplicated list of dates celebrated in BOTH countries for the given year.
    /// Each entry includes the local name from each country.
    /// </summary>
    [HttpGet("shared")]
    [ProducesResponseType(typeof(IReadOnlyList<SharedHolidayDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Shared(
        [FromQuery] int year,
        [FromQuery] string countryCodeA,
        [FromQuery] string countryCodeB,
        CancellationToken ct)
    {
        if (IsInvalidYear(year))
        {
            return BadRequest(InvalidYearMessage);
        }
        var codeA = NormalizeCountry(countryCodeA);
        var codeB = NormalizeCountry(countryCodeB);

        if (codeA is null || codeB is null)
        {
            return BadRequest(InvalidTwoCountriesMessage);
        }

        if (codeA == codeB)
        {
            return BadRequest(SameCountryMessage);
        }

        var result = await _holidayService.GetSharedHolidaysAsync(year, codeA, codeB, ct);

        return Ok(result);
    }
}
