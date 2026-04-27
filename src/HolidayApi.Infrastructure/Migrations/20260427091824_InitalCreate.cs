using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HolidayApi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitalCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PublicHolidays",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    LocalName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CountryCode = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    IsGlobal = table.Column<bool>(type: "bit", nullable: false),
                    Types = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Year = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PublicHolidays", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PublicHolidays_CountryCode",
                table: "PublicHolidays",
                column: "CountryCode");

            migrationBuilder.CreateIndex(
                name: "IX_PublicHolidays_Date_CountryCode",
                table: "PublicHolidays",
                columns: new[] { "Date", "CountryCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PublicHolidays_Year",
                table: "PublicHolidays",
                column: "Year");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PublicHolidays");
        }
    }
}
