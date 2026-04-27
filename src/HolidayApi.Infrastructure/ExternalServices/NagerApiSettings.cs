namespace HolidayApi.Infrastructure.ExternalServices
{
    public class NagerApiSettings
    {
        public const string SectionName = "NagerApi";
        public string BaseUrl { get; set; } = string.Empty;
        public int TimeoutSeconds { get; set; } = 30;
    } 
}
