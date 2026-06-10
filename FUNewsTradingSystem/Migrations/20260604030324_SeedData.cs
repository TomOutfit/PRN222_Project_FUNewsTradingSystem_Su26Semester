using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FUNewsTradingSystem_DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class SeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Category",
                columns: new[] { "CategoryID", "CategoryDescription", "CategoryName", "IsActive", "ParentCategoryID" },
                values: new object[,]
                {
                    { 1, "Technology sector", "Technology", true, null },
                    { 2, "Healthcare sector", "Healthcare", true, null },
                    { 3, "Financial sector", "Finance", true, null },
                    { 4, "Energy sector", "Energy", true, null },
                    { 5, "Digital assets", "Cryptocurrencies", true, null },
                    { 6, "Goods bought and used by consumers", "Consumer Goods", true, null }
                });

            migrationBuilder.InsertData(
                table: "SystemAccount",
                columns: new[] { "AccountID", "AccountEmail", "AccountName", "AccountPassword", "AccountRole" },
                values: new object[] { 1, "admin@FUNewsTradingSystem.org", "System Admin", "@@abc123@@_HASH_PLACEHOLDER", 3 });

            migrationBuilder.InsertData(
                table: "Tag",
                columns: new[] { "TagID", "Note", "TagName" },
                values: new object[,]
                {
                    { 1, "Apple Inc.", "AAPL" },
                    { 2, "NVIDIA Corporation", "NVDA" },
                    { 3, "Microsoft Corporation", "MSFT" },
                    { 4, "Alphabet Inc. (Google)", "GOOGL" },
                    { 5, "Tesla, Inc.", "TSLA" },
                    { 6, "Bitcoin", "BTC" },
                    { 7, "Ethereum", "ETH" },
                    { 8, "Amazon.com, Inc.", "AMZN" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Category",
                keyColumn: "CategoryID",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Category",
                keyColumn: "CategoryID",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Category",
                keyColumn: "CategoryID",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Category",
                keyColumn: "CategoryID",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Category",
                keyColumn: "CategoryID",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Category",
                keyColumn: "CategoryID",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "SystemAccount",
                keyColumn: "AccountID",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Tag",
                keyColumn: "TagID",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Tag",
                keyColumn: "TagID",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Tag",
                keyColumn: "TagID",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Tag",
                keyColumn: "TagID",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Tag",
                keyColumn: "TagID",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Tag",
                keyColumn: "TagID",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Tag",
                keyColumn: "TagID",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "Tag",
                keyColumn: "TagID",
                keyValue: 8);
        }
    }
}
