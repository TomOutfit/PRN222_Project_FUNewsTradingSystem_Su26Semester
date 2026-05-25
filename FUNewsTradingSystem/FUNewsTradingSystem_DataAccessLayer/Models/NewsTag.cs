namespace FUNewsTradingSystem_DataAccessLayer.Models;

public class NewsTag
{
    public string NewsArticleId { get; set; } = string.Empty;
    public int TagId { get; set; }

    public NewsArticle NewsArticle { get; set; } = null!;
    public Tag Tag { get; set; } = null!;
}
