namespace FUNewsTradingSystem_MVC.ViewModels.Tag;

public class TagListItemViewModel
{
    public int TagId { get; set; }
    public string TagName { get; set; } = string.Empty;
    public string? Note { get; set; }
}
