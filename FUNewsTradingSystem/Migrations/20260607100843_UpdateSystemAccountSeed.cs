using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FUNewsTradingSystem_DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSystemAccountSeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "SystemAccount",
                keyColumn: "AccountID",
                keyValue: 1,
                columns: new[] { "AccountEmail", "AccountName", "AccountPassword" },
                values: new object[] { "staff@FUNewsTradingSystem.org", "Test Staff", "@@abc123@@_HASH_PLACEHOLDER" });

            migrationBuilder.UpdateData(
                table: "SystemAccount",
                keyColumn: "AccountID",
                keyValue: 2,
                columns: new[] { "AccountEmail", "AccountName", "AccountPassword" },
                values: new object[] { "lecturer@FUNewsTradingSystem.org", "Test Lecturer", "@@abc123@@_HASH_PLACEHOLDER" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "SystemAccount",
                keyColumn: "AccountID",
                keyValue: 1,
                columns: new[] { "AccountEmail", "AccountName", "AccountPassword" },
                values: new object[] { "staff@funews.org", "Staff Member", "AQAAAAEAACcQAAAAELs+PZVdRHadHlMuaXWvzHD+7oMv2jsRUY/UxVGi1j32aL9NjpmmwVSDm9hKDppDjQ==" });

            migrationBuilder.UpdateData(
                table: "SystemAccount",
                keyColumn: "AccountID",
                keyValue: 2,
                columns: new[] { "AccountEmail", "AccountName", "AccountPassword" },
                values: new object[] { "lecturer@funews.org", "Lecturer Member", "AQAAAAEAACcQAAAAELs+PZVdRHadHlMuaXWvzHD+7oMv2jsRUY/UxVGi1j32aL9NjpmmwVSDm9hKDppDjQ==" });
        }
    }
}
