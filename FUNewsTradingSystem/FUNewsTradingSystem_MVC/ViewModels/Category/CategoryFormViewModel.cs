using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FUNewsTradingSystem_MVC.ViewModels.Category;

public class CategoryFormViewModel
{
    public int? CategoryId { get; set; }

    [Required]
    [StringLength(200)]
    public string CategoryName { get; set; } = string.Empty;

    [StringLength(500)]
    public string? CategoryDescription { get; set; }

    public int? ParentCategoryId { get; set; }

    public bool IsActive { get; set; } = true;

    public IEnumerable<SelectListItem>? ParentCategories { get; set; }
}
