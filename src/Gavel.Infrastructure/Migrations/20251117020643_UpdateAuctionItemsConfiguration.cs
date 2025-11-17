using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gavel.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAuctionItemsConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "AuctionItems");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "AuctionItems",
                type: "rowversion",
                rowVersion: true,
                nullable: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "AuctionItems");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "AuctionItems",
                type: "varbinary(max)",
                nullable: false);
        }
    }
}
