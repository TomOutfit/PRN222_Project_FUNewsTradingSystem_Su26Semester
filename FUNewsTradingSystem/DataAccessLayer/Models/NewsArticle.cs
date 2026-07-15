namespace FUNewsTradingSystem_DataAccessLayer.Models;

public class NewsArticle
{
    public int NewsArticleID { get; set; }
    public string NewsTitle { get; set; } = string.Empty;
    public string Headline { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public string NewsContent { get; set; } = string.Empty;
    public string? NewsSource { get; set; }
    public int CategoryID { get; set; }
    public bool NewsStatus { get; set; } = true;
    public int? CreatedByID { get; set; }
    public int? UpdatedByID { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public int? ConfidenceScore { get; set; }

    public Category Category { get; set; } = null!;
    public SystemAccount? CreatedByAccount { get; set; }
    public SystemAccount? UpdatedByAccount { get; set; }
    public ICollection<NewsTag> NewsTagList { get; set; } = new List<NewsTag>();
}
