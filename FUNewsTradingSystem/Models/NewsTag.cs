namespace FUNewsTradingSystem_DataAccessLayer.Models;

public class NewsTag
{
    public int NewsArticleID { get; set; }
    public int TagID { get; set; }

    public NewsArticle NewsArticle { get; set; } = null!;
    public Tag Tag { get; set; } = null!;
}
