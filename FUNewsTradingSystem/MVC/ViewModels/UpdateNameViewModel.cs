using System.ComponentModel.DataAnnotations;

namespace FUNewsTradingSystem_MVC.ViewModels;

public class UpdateNameViewModel
{
    [Required(ErrorMessage = "Name is required.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 100 characters.")]
    public string AccountName { get; set; } = string.Empty;
}
