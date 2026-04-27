namespace HolidayApi.Application.DTOs;

public record LastCelebratedDto(DateOnly Date, string Name);

public record WeekdayCountDto(string CountryCode, int WeekdayHolidayCount);

public record SharedHolidayDto(DateOnly Date, string LocalNameCountryA, string LocalNameCountryB);

public record FetchResultDto(string CountryCode, int Year, int RecordsSaved, bool WasCached);
