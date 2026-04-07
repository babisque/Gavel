using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gavel.Api.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSettlementPaidAndStatusConversion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Settlements",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PaidAt",
                table: "Settlements",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaidAt",
                table: "Settlements");

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "Settlements",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);
        }
    }
}
