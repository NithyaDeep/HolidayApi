using HolidayApi.Domain.Entities;
using HolidayApi.Domain.Interfaces;
using HolidayApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HolidayApi.Infrastructure.Repositories;

/// <summary>
/// Concrete EF Core implementation of IHolidayRepository.
/// </summary>
public sealed class HolidayRepository : IHolidayRepository
{
    private readonly AppDbContext _appDb;

    public HolidayRepository(AppDbContext db) => _appDb = db;


    public async Task UpsertBatchAsync(IEnumerable<PublicHoliday> holidays, CancellationToken ct = default)
    {
        var holidayList = holidays.ToList();
        if (holidayList.Count == 0) return;

        var countryCode = holidayList.First().CountryCode;

        // Deduplicate INCOMING data first — Nager API can return the same date
        // multiple times for one country (e.g. US state-level holidays on same date)
        // Keep only the first occurrence of each date — last-write-wins is fine here
        var deduplicated = holidayList
            .GroupBy(h => h.Date)
            .Select(g => g.First())
            .ToList();

        // Now check which dates already exist in the DB
        var dates = deduplicated.Select(h => h.Date).ToList();

        var existingDates = (await _appDb.PublicHolidays
            .Where(h => h.CountryCode == countryCode && dates.Contains(h.Date))
            .Select(h => h.Date)
            .ToListAsync(ct))
            .ToHashSet();

        // Only insert dates not already in the DB
        var toInsert = deduplicated
            .Where(h => !existingDates.Contains(h.Date))
            .ToList();

        if (toInsert.Count > 0)
        {
            await _appDb.PublicHolidays.AddRangeAsync(toInsert, ct);
            await _appDb.SaveChangesAsync(ct);
        }
    }

    public async Task<IReadOnlyList<PublicHoliday>> GetLastCelebratedAsync(
        string countryCode, int count, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        return await _appDb.PublicHolidays
            .AsNoTracking()
            .Where(h => h.CountryCode == countryCode && h.Date <= today)
            .OrderByDescending(h => h.Date)
            .Take(count)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<(string CountryCode, int Count)>> GetWeekdayHolidayCountsAsync(
        int year, IEnumerable<string> countryCodes, CancellationToken ct = default)
    {
        var codes = countryCodes.ToList();

        // Fetch the relevant records and filter weekdays in memory
        // Note: EF Core cannot translate DayOfWeek from DateOnly to SQL reliably across all providers,
        // so we pull the minimal dataset (year + countries) and evaluate IsWeekday() in C#.
        // This is intentional and documented — the set is bounded and small.
        var holidays = await _appDb.PublicHolidays
            .AsNoTracking()
            .Where(h => h.Year == year && codes.Contains(h.CountryCode))
            .Select(h => new { h.CountryCode, h.Date })
            .ToListAsync(ct);

        // IsWeekday logic lives on the Domain entity — tested independently
        return holidays
            .Where(h => h.Date.DayOfWeek != DayOfWeek.Saturday && h.Date.DayOfWeek != DayOfWeek.Sunday)
            .GroupBy(h => h.CountryCode)
            .Select(g => (CountryCode: g.Key, Count: g.Count()))
            .OrderByDescending(r => r.Count)
            .ToList();
    }

    public async Task<IReadOnlyList<SharedHolidayResult>> GetSharedHolidaysAsync(
        int year, string countryCodeA, string countryCodeB, CancellationToken ct = default)
    {
        // SQL JOIN approach: find dates present in both countries for the given year
        // This executes as a single JOIN query — no in-memory set operations on large datasets
        var results = await (
            from a in _appDb.PublicHolidays
            join b in _appDb.PublicHolidays
                on a.Date equals b.Date
            where a.Year == year
               && a.CountryCode == countryCodeA
               && b.CountryCode == countryCodeB
            orderby a.Date
            select new SharedHolidayResult(a.Date, a.LocalName, b.LocalName)
        ).AsNoTracking().ToListAsync(ct);

        return results;
    }

    public async Task<bool> ExistsAsync(int year, string countryCode, CancellationToken ct = default)
    {
        return await _appDb.PublicHolidays
            .AsNoTracking()
            .AnyAsync(h => h.Year == year && h.CountryCode == countryCode, ct);
    }
}
