using System.ComponentModel.DataAnnotations;

namespace FUNewsTradingSystem_MVC.ViewModels.Tag;

public class EditTagViewModel
{
    [Required]
    public int TagID { get; set; }

    [Required]
    [MaxLength(50)]
    public string TagName { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Note { get; set; }
}