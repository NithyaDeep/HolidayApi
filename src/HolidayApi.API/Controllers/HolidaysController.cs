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

    // POST /api/holidays/fetch
    /// <summary>
    /// Fetches public holidays from the Nager.Date API and saves them to the database.
    /// </summary>
    [HttpPost("fetch")]
    [ProducesResponseType(typeof(FetchResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Fetch(
        [FromQuery] int year,
        [FromQuery] string countryCode,
        CancellationToken ct)
    {
        if (year < 1900 || year > 2100)
            return BadRequest("Year must be between 1900 and 2100.");

        if (string.IsNullOrWhiteSpace(countryCode) || countryCode.Length != 2)
            return BadRequest("countryCode must be a 2-character ISO 3166-1 alpha-2 code (e.g. NL, BE).");

        var result = await _holidayService.FetchAndSaveAsync(year, countryCode, ct);

        if (result.RecordsSaved == 0 && !result.WasCached)
            return BadRequest(new
            {
                message = $"No holiday data found for country code '{countryCode}'. " +
                          $"This country may not be supported by the Nager.Date API. " +
                          $"Check supported countries at: https://date.nager.at/api/v3/AvailableCountries"
            });

        return Ok(result);
    }

    // GET /api/holidays/last-celebrated
    /// <summary>
    /// Returns the last 3 public holidays that have already occurred for the given country.
    /// Data must already be fetched via /fetch before calling this endpoint.
    /// </summary>
    [HttpGet("last-celebrated")]
    [ProducesResponseType(typeof(IReadOnlyList<LastCelebratedDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> LastCelebrated(
        [FromQuery] string countryCode,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(countryCode) || countryCode.Length != 2)
            return BadRequest("countryCode must be a 2-character ISO code.");

        var result = await _holidayService.GetLastCelebratedAsync(countryCode, ct);
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
    public async Task<IActionResult> WeekdayCounts(
        [FromQuery] int year,
        [FromQuery] IEnumerable<string> countryCodes,
        CancellationToken ct)
    {
        var codes = countryCodes.ToList();
        if (codes.Count == 0)
            return BadRequest("At least one countryCode is required.");

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
        if (string.IsNullOrWhiteSpace(countryCodeA) || string.IsNullOrWhiteSpace(countryCodeB))
        {
            return BadRequest("Both countryCodeA and countryCodeB are required.");
        } 

        if (countryCodeA.ToUpperInvariant() == countryCodeB.ToUpperInvariant())
        {
            return BadRequest("countryCodeA and countryCodeB must be different.");
        }
            
        var result = await _holidayService.GetSharedHolidaysAsync(year, countryCodeA, countryCodeB, ct);

        return Ok(result);
    }
}
