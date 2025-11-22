using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gavel.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNameToApplicationUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Bids_AuctionItemId",
                table: "Bids");

            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Bids_AuctionItemId_Amount",
                table: "Bids",
                columns: new[] { "AuctionItemId", "Amount" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Bids_AuctionItemId_Amount",
                table: "Bids");

            migrationBuilder.DropColumn(
                name: "FirstName",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LastName",
                table: "Users");

            migrationBuilder.CreateIndex(
                name: "IX_Bids_AuctionItemId",
                table: "Bids",
                column: "AuctionItemId");
        }
    }
}
