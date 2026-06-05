using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FUNewsTradingSystem_DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAdminPasswordHash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "SystemAccount",
                keyColumn: "AccountID",
                keyValue: 1,
                column: "AccountPassword",
                value: "AQAAAAEAACcQAAAAELs+PZVdRHadHlMuaXWvzHD+7oMv2jsRUY/UxVGi1j32aL9NjpmmwVSDm9hKDppDjQ==");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "SystemAccount",
                keyColumn: "AccountID",
                keyValue: 1,
                column: "AccountPassword",
                value: "@@abc123@@_HASH_PLACEHOLDER");
        }
    }
}
