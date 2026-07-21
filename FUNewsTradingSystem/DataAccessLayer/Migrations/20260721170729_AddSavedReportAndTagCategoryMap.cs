using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddSavedReportAndTagCategoryMap : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ConfidenceScore",
                table: "NewsArticle",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SavedReport",
                columns: table => new
                {
                    SavedReportID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccountID = table.Column<int>(type: "int", nullable: false),
                    NewsArticleID = table.Column<int>(type: "int", nullable: false),
                    SavedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedReport", x => x.SavedReportID);
                    table.ForeignKey(
                        name: "FK_SavedReport_NewsArticle_NewsArticleID",
                        column: x => x.NewsArticleID,
                        principalTable: "NewsArticle",
                        principalColumn: "NewsArticleID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SavedReport_SystemAccount_AccountID",
                        column: x => x.AccountID,
                        principalTable: "SystemAccount",
                        principalColumn: "AccountID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TagCategoryMap",
                columns: table => new
                {
                    TagCategoryMapID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TagID = table.Column<int>(type: "int", nullable: false),
                    CategoryID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TagCategoryMap", x => x.TagCategoryMapID);
                    table.ForeignKey(
                        name: "FK_TagCategoryMap_Category_CategoryID",
                        column: x => x.CategoryID,
                        principalTable: "Category",
                        principalColumn: "CategoryID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TagCategoryMap_Tag_TagID",
                        column: x => x.TagID,
                        principalTable: "Tag",
                        principalColumn: "TagID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "TagCategoryMap",
                columns: new[] { "TagCategoryMapID", "CategoryID", "TagID" },
                values: new object[,]
                {
                    { 1, 1, 1 },
                    { 2, 1, 2 },
                    { 3, 1, 3 },
                    { 4, 1, 4 },
                    { 5, 1, 5 },
                    { 6, 1, 8 },
                    { 7, 5, 6 },
                    { 8, 5, 7 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_SavedReport_AccountID_NewsArticleID",
                table: "SavedReport",
                columns: new[] { "AccountID", "NewsArticleID" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SavedReport_NewsArticleID",
                table: "SavedReport",
                column: "NewsArticleID");

            migrationBuilder.CreateIndex(
                name: "IX_TagCategoryMap_CategoryID",
                table: "TagCategoryMap",
                column: "CategoryID");

            migrationBuilder.CreateIndex(
                name: "IX_TagCategoryMap_TagID_CategoryID",
                table: "TagCategoryMap",
                columns: new[] { "TagID", "CategoryID" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SavedReport");

            migrationBuilder.DropTable(
                name: "TagCategoryMap");

            migrationBuilder.DropColumn(
                name: "ConfidenceScore",
                table: "NewsArticle");
        }
    }
}
