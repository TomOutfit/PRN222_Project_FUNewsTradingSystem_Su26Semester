namespace FUNewsTradingSystem_DataAccessLayer.Models;

public class TagCategoryMap
{
    public int TagCategoryMapID { get; set; }
    public int TagID { get; set; }
    public int CategoryID { get; set; }

    public Tag Tag { get; set; } = null!;
    public Category Category { get; set; } = null!;
}
