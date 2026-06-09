namespace FUNewsTradingSystem_MVC.ViewModels.Category;

public class CategoryListItemViewModel
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? CategoryDescription { get; set; }
    public int? ParentCategoryID { get; set; }
    public string? ParentCategoryName { get; set; }
    public bool IsActive { get; set; }
}
