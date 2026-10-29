using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DepremTakip.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Earthquakes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventId = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Magnitude = table.Column<double>(type: "float", nullable: false),
                    MagnitudeType = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    DepthKm = table.Column<double>(type: "float", nullable: false),
                    Latitude = table.Column<double>(type: "float", nullable: false),
                    Longitude = table.Column<double>(type: "float", nullable: false),
                    Location = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Province = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    District = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Earthquakes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Earthquakes_EventId",
                table: "Earthquakes",
                column: "EventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Earthquakes_Magnitude",
                table: "Earthquakes",
                column: "Magnitude");

            migrationBuilder.CreateIndex(
                name: "IX_Earthquakes_OccurredAtUtc",
                table: "Earthquakes",
                column: "OccurredAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Earthquakes_Province",
                table: "Earthquakes",
                column: "Province");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Earthquakes");
        }
    }
}
