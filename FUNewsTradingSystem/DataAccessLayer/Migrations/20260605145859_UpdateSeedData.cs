using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FUNewsTradingSystem_DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "SystemAccount",
                keyColumn: "AccountID",
                keyValue: 1,
                columns: new[] { "AccountEmail", "AccountName", "AccountRole" },
                values: new object[] { "staff@funews.org", "Staff Member", 1 });

            migrationBuilder.InsertData(
                table: "SystemAccount",
                columns: new[] { "AccountID", "AccountEmail", "AccountName", "AccountPassword", "AccountRole" },
                values: new object[] { 2, "lecturer@funews.org", "Lecturer Member", "AQAAAAEAACcQAAAAELs+PZVdRHadHlMuaXWvzHD+7oMv2jsRUY/UxVGi1j32aL9NjpmmwVSDm9hKDppDjQ==", 2 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "SystemAccount",
                keyColumn: "AccountID",
                keyValue: 2);

            migrationBuilder.UpdateData(
                table: "SystemAccount",
                keyColumn: "AccountID",
                keyValue: 1,
                columns: new[] { "AccountEmail", "AccountName", "AccountRole" },
                values: new object[] { "admin@FUNewsTradingSystem.org", "System Admin", 3 });
        }
    }
}
