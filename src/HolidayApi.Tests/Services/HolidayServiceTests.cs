using HolidayApi.Application.Interfaces;
using HolidayApi.Application.Services;
using HolidayApi.Domain.Entities;
using HolidayApi.Domain.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace HolidayApi.Tests.Services;

/// <summary>
/// Unit tests for HolidayService business logic.
/// </summary>
public class HolidayServiceTests
{
    private readonly IHolidayRepository _mockRepo;
    private readonly INagerApiClient    _mockApiClient;
    private readonly HolidayService     _sut;   // System Under Test

    public HolidayServiceTests()
    {
        _mockRepo      = Substitute.For<IHolidayRepository>();
        _mockApiClient = Substitute.For<INagerApiClient>();

        
        _sut = new HolidayService(
            _mockRepo,
            _mockApiClient,
            NullLogger<HolidayService>.Instance);
    }


    [Fact]
    public async Task FetchAndSave_WhenDataAlreadyExists_ReturnsWasCachedTrue_AndDoesNotCallApi()
    {
        // ARRANGE: Tell the mock repo to say "data already exists"
        _mockRepo.ExistsAsync(2025, "NL", Arg.Any<CancellationToken>())
                 .Returns(true);

        // ACT
        var result = await _sut.FetchAndSaveAsync(2025, "NL");

        // ASSERT: service returns cached flag, and never calls the API
        Assert.True(result.WasCached);
        Assert.Equal(0, result.RecordsSaved);

        // Verify the HTTP client was never called — a real test would have made an HTTP call
        await _mockApiClient.DidNotReceive()
              .GetPublicHolidaysAsync(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FetchAndSave_WhenNoExistingData_CallsApiAndSavesRecords()
    {
        // ARRANGE
        _mockRepo.ExistsAsync(2025, "NL", Arg.Any<CancellationToken>())
                 .Returns(false);

        _mockApiClient.GetPublicHolidaysAsync(2025, "NL", Arg.Any<CancellationToken>())
                      .Returns(new List<NagerHolidayDto>
                      {
                          new("2025-01-01", "Nieuwjaarsdag", "New Year's Day", "NL", true, new[] { "Public" }),
                          new("2025-04-18", "Goede Vrijdag",  "Good Friday",   "NL", true, new[] { "Public" })
                      });

        // ACT
        var result = await _sut.FetchAndSaveAsync(2025, "NL");

        // ASSERT
        Assert.False(result.WasCached);
        Assert.Equal(2, result.RecordsSaved);
        Assert.Equal("NL", result.CountryCode);

        // Verify the repo was called to save the data
        await _mockRepo.Received(1)
              .UpsertBatchAsync(Arg.Any<IEnumerable<PublicHoliday>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FetchAndSave_NormalisesCountryCodeToUpperCase()
    {
        _mockRepo.ExistsAsync(2025, "NL", Arg.Any<CancellationToken>()).Returns(false);
        _mockApiClient.GetPublicHolidaysAsync(2025, "NL", Arg.Any<CancellationToken>())
                      .Returns(new List<NagerHolidayDto>());

        // ACT: pass lowercase country code
        var result = await _sut.FetchAndSaveAsync(2025, "nl");

        // ASSERT: normalised to uppercase before hitting API or repo
        Assert.Equal("NL", result.CountryCode);
    }

  
    [Fact]
    public async Task GetLastCelebrated_ReturnsMappedDtosFromRepository()
    {
        // ARRANGE: fake repo returns two past holidays
        var fakeHolidays = new List<PublicHoliday>
        {
            PublicHoliday.Create(new DateOnly(2025, 4, 18), "Goede Vrijdag", "Good Friday", "NL", true, null),
            PublicHoliday.Create(new DateOnly(2025, 1, 1),  "Nieuwjaarsdag", "New Year's Day", "NL", true, null),
        };

        _mockRepo.GetLastCelebratedAsync("NL", 3, Arg.Any<CancellationToken>())
                 .Returns(fakeHolidays);

        // ACT
        var result = await _sut.GetLastCelebratedAsync("NL");

        // ASSERT: mapped correctly to DTOs
        Assert.Equal(2, result.Count);
        Assert.Equal("Good Friday",    result[0].Name);
        Assert.Equal("New Year's Day", result[1].Name);
        Assert.Equal(new DateOnly(2025, 4, 18), result[0].Date);
    }

    [Fact]
    public async Task GetLastCelebrated_ReturnsEmptyList_WhenNoDataInDb()
    {
        _mockRepo.GetLastCelebratedAsync("ZZ", 3, Arg.Any<CancellationToken>())
                 .Returns(new List<PublicHoliday>());

        var result = await _sut.GetLastCelebratedAsync("ZZ");

        Assert.Empty(result);
    }

    

    [Fact]
    public async Task GetWeekdayHolidayCounts_ReturnsCountsSortedDescending()
    {
        // ARRANGE: repo returns unsorted counts
        _mockRepo.GetWeekdayHolidayCountsAsync(2025, Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
                 .Returns(new List<(string, int)>
                 {
                     ("NL", 8),
                     ("DE", 12),
                     ("BE", 5)
                 });

        // ACT
        var result = await _sut.GetWeekdayHolidayCountsAsync(2025, new[] { "NL", "DE", "BE" });

        // ASSERT: sorted descending — DE (12) first
        Assert.Equal(3, result.Count);
        Assert.Equal("DE", result[0].CountryCode);
        Assert.Equal(12,   result[0].WeekdayHolidayCount);
        Assert.Equal("BE", result[2].CountryCode);
        Assert.Equal(5,    result[2].WeekdayHolidayCount);
    }

    [Fact]
    public async Task GetSharedHolidays_ThrowsArgumentException_WhenBothCodesAreSame()
    {
        // ASSERT
        await Assert.ThrowsAsync<ArgumentException>(
            () => _sut.GetSharedHolidaysAsync(2025, "NL", "NL"));
    }

    [Fact]
    public async Task GetSharedHolidays_ReturnsMappedSharedDates()
    {
        // ARRANGE
        _mockRepo.GetSharedHolidaysAsync(2025, "NL", "BE", Arg.Any<CancellationToken>())
                 .Returns(new List<SharedHolidayResult>
                 {
                     new(new DateOnly(2025, 1, 1), "Nieuwjaarsdag", "Nouvel An"),
                     new(new DateOnly(2025, 12, 25), "Kerstdag", "Noël")
                 });

        // ACT
        var result = await _sut.GetSharedHolidaysAsync(2025, "NL", "BE");

        // ASSERT
        Assert.Equal(2, result.Count);
        Assert.Equal("Nieuwjaarsdag", result[0].LocalNameCountryA);
        Assert.Equal("Nouvel An",     result[0].LocalNameCountryB);
        Assert.Equal(new DateOnly(2025, 12, 25), result[1].Date);
    }
}
