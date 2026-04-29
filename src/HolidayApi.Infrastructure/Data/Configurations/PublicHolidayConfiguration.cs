using HolidayApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HolidayApi.Infrastructure.Data.Configurations;

/// <summary>
/// Fluent API configuration for PublicHoliday.
/// The composite unique index on (Date, CountryCode) enables safe upsert.
/// </summary>
public class PublicHolidayConfiguration : IEntityTypeConfiguration<PublicHoliday>
{
    public void Configure(EntityTypeBuilder<PublicHoliday> builder)
    {
        builder.ToTable("PublicHolidays");

        builder.HasKey(h => h.Id);

        builder.Property(h => h.CountryCode)
            .HasMaxLength(2)
            .IsRequired();

        builder.Property(h => h.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(h => h.LocalName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(h => h.Types)
            .HasMaxLength(200);

        // Composite unique index — prevents duplicate imports
        // This is what makes the fetch endpoint idempotent
        builder.HasIndex(h => new { h.Date, h.CountryCode })
            .IsUnique()
            .HasDatabaseName("IX_PublicHolidays_Date_CountryCode");

        // Individual indexes for query performance
        builder.HasIndex(h => h.CountryCode)
            .HasDatabaseName("IX_PublicHolidays_CountryCode");

        builder.HasIndex(h => h.Year)
            .HasDatabaseName("IX_PublicHolidays_Year");
    }
}
