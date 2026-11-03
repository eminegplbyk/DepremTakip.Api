using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DepremTakip.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddEarthquakeSource : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "Earthquakes",
                type: "nvarchar(8)",
                maxLength: 8,
                nullable: false,
                defaultValue: "AFAD");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Source",
                table: "Earthquakes");
        }
    }
}
