using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Gavel.Api.Migrations
{
    /// <inheritdoc />
    public partial class CreateBidTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_PublicNotice",
                table: "PublicNotice");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Photo",
                table: "Photo");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "PublicNotice",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "Photo",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddColumn<Guid>(
                name: "CurrentBidderId",
                table: "Lots",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MinimumIncrement",
                table: "Lots",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Lots",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddPrimaryKey(
                name: "PK_PublicNotice",
                table: "PublicNotice",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Photo",
                table: "Photo",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "Bids",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LotId = table.Column<Guid>(type: "uuid", nullable: false),
                    BidderId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SourceIP = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bids", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProxyBids",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LotId = table.Column<Guid>(type: "uuid", nullable: false),
                    BidderId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaxAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProxyBids", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PublicNotice_LotId",
                table: "PublicNotice",
                column: "LotId");

            migrationBuilder.CreateIndex(
                name: "IX_Photo_LotId",
                table: "Photo",
                column: "LotId");

            migrationBuilder.CreateIndex(
                name: "IX_Bids_LotId",
                table: "Bids",
                column: "LotId");

            migrationBuilder.CreateIndex(
                name: "IX_Bids_LotId_Amount_DESC",
                table: "Bids",
                columns: new[] { "LotId", "Amount" },
                descending: new[] { false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Bids");

            migrationBuilder.DropTable(
                name: "ProxyBids");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PublicNotice",
                table: "PublicNotice");

            migrationBuilder.DropIndex(
                name: "IX_PublicNotice_LotId",
                table: "PublicNotice");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Photo",
                table: "Photo");

            migrationBuilder.DropIndex(
                name: "IX_Photo_LotId",
                table: "Photo");

            migrationBuilder.DropColumn(
                name: "CurrentBidderId",
                table: "Lots");

            migrationBuilder.DropColumn(
                name: "MinimumIncrement",
                table: "Lots");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Lots");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "PublicNotice",
                type: "integer",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Photo",
                type: "integer",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddPrimaryKey(
                name: "PK_PublicNotice",
                table: "PublicNotice",
                columns: new[] { "LotId", "Id" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_Photo",
                table: "Photo",
                columns: new[] { "LotId", "Id" });
        }
    }
}
