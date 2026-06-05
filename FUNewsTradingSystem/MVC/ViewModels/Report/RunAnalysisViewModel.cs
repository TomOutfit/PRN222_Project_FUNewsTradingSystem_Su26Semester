using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FUNewsTradingSystem_MVC.ViewModels.Report;

public class RunAnalysisViewModel
{
    [Required(ErrorMessage = "Please select a ticker symbol.")]
    public int SelectedTagId { get; set; }

    [Required(ErrorMessage = "Please select a market sector.")]
    public int SelectedCategoryId { get; set; }

    public SelectList? AvailableTags { get; set; }

    public SelectList? AvailableCategories { get; set; }
}
