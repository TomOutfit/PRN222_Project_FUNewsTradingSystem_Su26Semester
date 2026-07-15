namespace FUNewsTradingSystem_MVC.ViewModels.Report;

public class ReportListItemViewModel
{
    public int NewsArticleId { get; set; }
    public string NewsTitle { get; set; } = string.Empty;
    public string Headline { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public IEnumerable<string> TagNames { get; set; } = Enumerable.Empty<string>();
    public bool NewsStatus { get; set; }
    public string Decision { get; set; } = string.Empty;
    public int ConfidenceScore { get; set; }
}
