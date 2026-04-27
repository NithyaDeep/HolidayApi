using HolidayApi.Domain.Entities;
using Xunit;

namespace HolidayApi.Tests.Services;

/// <summary>
/// Tests for domain entity behaviour.
/// </summary>
public class PublicHolidayTests
{
    [Theory]
    [InlineData(2025, 1, 1,  false)]  // Wednesday — weekday
    [InlineData(2025, 1, 4,  true)]   // Saturday  — weekend
    [InlineData(2025, 1, 5,  true)]   // Sunday    — weekend
    [InlineData(2025, 4, 18, false)]  // Friday    — weekday
    [InlineData(2025, 12, 25, false)] // Thursday  — weekday
    public void IsWeekday_ReturnsCorrectResult(int year, int month, int day, bool expectedIsWeekend)
    {
        var holiday = PublicHoliday.Create(
            new DateOnly(year, month, day), "Test", "Test", "NL", true, null);

        // IsWeekday() should be the inverse of expectedIsWeekend
        Assert.Equal(!expectedIsWeekend, holiday.IsWeekday());
    }

    [Fact]
    public void Create_SetsYearFromDate()
    {
        var holiday = PublicHoliday.Create(
            new DateOnly(2025, 6, 15), "Test", "Test", "NL", true, null);

        Assert.Equal(2025, holiday.Year);
    }

    [Fact]
    public void Create_NormalisesCountryCodeToUpperCase()
    {
        var holiday = PublicHoliday.Create(
            new DateOnly(2025, 1, 1), "Test", "Test", "nl", true, null);

        Assert.Equal("NL", holiday.CountryCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("N")]
    [InlineData("NLD")]
    [InlineData("  ")]
    public void Create_ThrowsArgumentException_ForInvalidCountryCode(string invalidCode)
    {
        Assert.Throws<ArgumentException>(() =>
            PublicHoliday.Create(new DateOnly(2025, 1, 1), "Test", "Test", invalidCode, true, null));
    }

    [Fact]
    public void Create_JoinsTypesAsCommaSeparated()
    {
        var holiday = PublicHoliday.Create(
            new DateOnly(2025, 1, 1), "Test", "Test", "NL", true,
            new[] { "Public", "Bank" });

        Assert.Equal("Public,Bank", holiday.Types);
    }
}
