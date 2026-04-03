using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gavel.Api.Migrations
{
    /// <inheritdoc />
    public partial class CreateProxyBidTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "ProxyBids",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.CreateIndex(
                name: "IX_ProxyBids_LotId_MaxAmount_CreatedAt",
                table: "ProxyBids",
                columns: new[] { "LotId", "MaxAmount", "CreatedAt" },
                descending: new[] { false, true, false });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProxyBids_LotId_MaxAmount_CreatedAt",
                table: "ProxyBids");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "ProxyBids");
        }
    }
}
