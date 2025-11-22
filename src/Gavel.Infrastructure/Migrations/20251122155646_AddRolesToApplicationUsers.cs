using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gavel.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRolesToApplicationUsers : Migration
    {
        private readonly Guid _userRoleId = new Guid("8375e9a6-1991-4162-a585-579933160526");
        private readonly Guid _adminRoleId = new Guid("e9955e93-6673-464e-944d-fc7069030024");

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "Name", "NormalizedName", "ConcurrencyStamp" },
                values: new object[,]
                {
                    { _userRoleId, "User", "USER", Guid.NewGuid().ToString() },
                    { _adminRoleId, "Admin", "ADMIN", Guid.NewGuid().ToString() }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: _userRoleId);

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: _adminRoleId);
        }
    }
}