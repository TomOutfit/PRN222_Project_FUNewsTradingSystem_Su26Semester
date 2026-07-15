namespace FUNewsTradingSystem_MVC.ViewModels.Report;

public class ReportDetailViewModel
{
    public int NewsArticleId { get; set; }
    public string NewsTitle { get; set; } = string.Empty;
    public string NewsContent { get; set; } = string.Empty;
    public string? NewsSource { get; set; }
    public string Headline { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int CreatedById { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public int? UpdatedById { get; set; }
    public string? UpdatedByName { get; set; }
    public bool NewsStatus { get; set; }
    public string Decision { get; set; } = string.Empty;
    public int ConfidenceScore { get; set; }
    public IEnumerable<string> TagNames { get; set; } = Enumerable.Empty<string>();
    public List<string> Tags { get; set; } = new();
}
