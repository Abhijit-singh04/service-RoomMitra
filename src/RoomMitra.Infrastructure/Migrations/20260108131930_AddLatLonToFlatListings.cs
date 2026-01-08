using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoomMitra.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLatLonToFlatListings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "FlatListings",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "FlatListings",
                type: "double precision",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "nearby_essentials",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    flat_listing_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    distance_meters = table.Column<int>(type: "integer", nullable: false),
                    fetched_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_nearby_essentials", x => x.id);
                    table.ForeignKey(
                        name: "FK_nearby_essentials_FlatListings_flat_listing_id",
                        column: x => x.flat_listing_id,
                        principalTable: "FlatListings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_nearby_essentials_flat_listing_id",
                table: "nearby_essentials",
                column: "flat_listing_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "nearby_essentials");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "FlatListings");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "FlatListings");
        }
    }
}
