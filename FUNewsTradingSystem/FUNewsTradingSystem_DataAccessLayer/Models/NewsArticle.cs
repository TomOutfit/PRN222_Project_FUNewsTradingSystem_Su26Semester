namespace FUNewsTradingSystem_DataAccessLayer.Models;

public class NewsArticle
{
    public string NewsArticleId { get; set; } = string.Empty;
    public string NewsTitle { get; set; } = string.Empty;
    public string NewsContent { get; set; } = string.Empty;
    public string? NewsSource { get; set; }
    public int? CategoryId { get; set; }
    public bool NewsStatus { get; set; }
    public DateTime? CreatedDate { get; set; }
    public int? CreatedBy { get; set; }
    public DateTime? UpdatedDate { get; set; }
    public int? UpdatedBy { get; set; }
    public string? NewsImage { get; set; }

    public Category? Category { get; set; }
    public SystemAccount? CreatedByAccount { get; set; }
    public SystemAccount? UpdatedByAccount { get; set; }
    public ICollection<NewsTag> NewsTags { get; set; } = new List<NewsTag>();
}
