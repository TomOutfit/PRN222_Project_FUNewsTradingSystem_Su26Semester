namespace FUNewsTradingSystem_DataAccessLayer.Models;

public class Tag
{
    public int TagID { get; set; }
    public string TagName { get; set; } = string.Empty;
    public string? Note { get; set; }

    public ICollection<NewsTag> NewsTags { get; set; } = new List<NewsTag>();
}
