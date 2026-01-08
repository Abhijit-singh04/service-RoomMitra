using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoomMitra.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixChatForeignKeyToFlatListings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Conversations_Properties_PropertyId",
                table: "Conversations");

            migrationBuilder.RenameColumn(
                name: "PropertyOwnerId",
                table: "Conversations",
                newName: "FlatListingOwnerId");

            migrationBuilder.RenameColumn(
                name: "PropertyId",
                table: "Conversations",
                newName: "FlatListingId");

            migrationBuilder.RenameIndex(
                name: "IX_Conversations_Unique_Participants_Property",
                table: "Conversations",
                newName: "IX_Conversations_Unique_Participants_FlatListing");

            migrationBuilder.RenameIndex(
                name: "IX_Conversations_PropertyOwnerId",
                table: "Conversations",
                newName: "IX_Conversations_FlatListingOwnerId");

            migrationBuilder.RenameIndex(
                name: "IX_Conversations_PropertyId",
                table: "Conversations",
                newName: "IX_Conversations_FlatListingId");

            migrationBuilder.AddForeignKey(
                name: "FK_Conversations_FlatListings_FlatListingId",
                table: "Conversations",
                column: "FlatListingId",
                principalTable: "FlatListings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Conversations_FlatListings_FlatListingId",
                table: "Conversations");

            migrationBuilder.RenameColumn(
                name: "FlatListingOwnerId",
                table: "Conversations",
                newName: "PropertyOwnerId");

            migrationBuilder.RenameColumn(
                name: "FlatListingId",
                table: "Conversations",
                newName: "PropertyId");

            migrationBuilder.RenameIndex(
                name: "IX_Conversations_Unique_Participants_FlatListing",
                table: "Conversations",
                newName: "IX_Conversations_Unique_Participants_Property");

            migrationBuilder.RenameIndex(
                name: "IX_Conversations_FlatListingOwnerId",
                table: "Conversations",
                newName: "IX_Conversations_PropertyOwnerId");

            migrationBuilder.RenameIndex(
                name: "IX_Conversations_FlatListingId",
                table: "Conversations",
                newName: "IX_Conversations_PropertyId");

            migrationBuilder.AddForeignKey(
                name: "FK_Conversations_Properties_PropertyId",
                table: "Conversations",
                column: "PropertyId",
                principalTable: "Properties",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
