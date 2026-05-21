namespace HolidayApi.Domain.Entities;

/// <summary>
/// Core domain entity representing a public holiday.
/// </summary>
public class PublicHoliday
{
    public int Id { get; private set; }
    public DateOnly Date { get; private set; }
    public string LocalName { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string CountryCode { get; private set; } = string.Empty;
    public bool IsGlobal { get; private set; }
    public string? Types { get; private set; }   // stored as comma-separated e.g. "Public,Bank"
    public int Year { get; private set; }

    
    private PublicHoliday() { }

    /// <summary>
    /// Factory method enforces invariants at construction time.
    /// If business rules change (e.g. CountryCode must be exactly 2 chars), this is the one place to update.
    /// </summary>
    public static PublicHoliday Create(
        DateOnly date,
        string localName,
        string name,
        string countryCode,
        bool isGlobal,
        IEnumerable<string>? types)
    {
        return string.IsNullOrWhiteSpace(countryCode) || countryCode.Length != 2
            ? throw new ArgumentException("CountryCode must be a 2-character ISO code.", nameof(countryCode))
            : new PublicHoliday
        {
            Date        = date,
            LocalName   = localName ?? string.Empty,
            Name        = name ?? string.Empty,
            CountryCode = countryCode.ToUpperInvariant(),
            IsGlobal    = isGlobal,
            Types       = types is null ? null : string.Join(",", types),
            Year        = date.Year
        };
    }

    /// <summary>
    /// Domain behaviour: is this holiday on a weekday?
    /// </summary>
    public bool IsWeekday()
    {
        var dayOfWeek = Date.DayOfWeek;

        return dayOfWeek != DayOfWeek.Saturday && dayOfWeek != DayOfWeek.Sunday;
    }
}
