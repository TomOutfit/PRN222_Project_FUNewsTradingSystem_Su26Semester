namespace FUNewsTradingSystem_DataAccessLayer.Models;

public class SavedReport
{
    public int SavedReportID { get; set; }
    public int AccountID { get; set; }
    public int NewsArticleID { get; set; }
    public DateTime SavedDate { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }

    public SystemAccount Account { get; set; } = null!;
    public NewsArticle NewsArticle { get; set; } = null!;
}
