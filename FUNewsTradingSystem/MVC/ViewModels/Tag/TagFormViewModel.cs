using System.ComponentModel.DataAnnotations;

namespace FUNewsTradingSystem_MVC.ViewModels.Tag;

public class TagFormViewModel
{
    public int? TagId { get; set; }

    [Required]
    [StringLength(50)]
    public string TagName { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Note { get; set; }
}
