using HolidayApi.API.Middleware;
using HolidayApi.Application.Interfaces;
using HolidayApi.Application.Services;
using HolidayApi.Domain.Interfaces;
using HolidayApi.Infrastructure.Data;
using HolidayApi.Infrastructure.ExternalServices;
using HolidayApi.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((ctx, lc) => lc
        .ReadFrom.Configuration(ctx.Configuration)
        .WriteTo.Console(outputTemplate:
            "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"));

    builder.Services.Configure<NagerApiSettings>(
    builder.Configuration.GetSection(NagerApiSettings.SectionName));

    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlServer(
            builder.Configuration.GetConnectionString("DefaultConnection"),
            sql => sql.EnableRetryOnFailure()   // handles transient SQL Azure failures
        ));

    builder.Services.AddHttpClient<INagerApiClient, NagerApiClient>(client =>
    {
        var settings = builder.Configuration
            .GetSection(NagerApiSettings.SectionName)
            .Get<NagerApiSettings>()!;

        client.BaseAddress = new Uri(settings.BaseUrl);
        client.DefaultRequestHeaders.Add("Accept", "application/json");
        client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);
    });

    builder.Services.AddScoped<IHolidayRepository, HolidayRepository>();
    builder.Services.AddScoped<IHolidayService, HolidayService>();

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new()
        {
            Title = "Holiday API",
            Version = "v1",
            Description = "Public holiday data powered by Nager.Date"
        });

    });
    builder.Services.AddSwaggerGen();

    var app = builder.Build();

    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
    }

    app.UseMiddleware<ExceptionMiddleware>();

    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Holiday API v1");
        c.RoutePrefix = string.Empty;
    });

    app.UseSerilogRequestLogging();
    app.UseHttpsRedirection();
    app.UseAuthorization();
    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application failed to start.");
}
finally
{
    Log.CloseAndFlush();
}
