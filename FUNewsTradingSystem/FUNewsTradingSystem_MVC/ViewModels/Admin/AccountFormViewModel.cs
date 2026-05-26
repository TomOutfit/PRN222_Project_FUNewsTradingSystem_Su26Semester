using System.ComponentModel.DataAnnotations;

namespace FUNewsTradingSystem_MVC.ViewModels.Admin;

public class AccountFormViewModel
{
    public int? AccountId { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string AccountName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string AccountEmail { get; set; } = string.Empty;

    [Required]
    [Range(1, 3)]
    public int AccountRole { get; set; }

    [StringLength(100, MinimumLength = 8)]
    public string? AccountPassword { get; set; }

    public string? AccountRoleLabel { get; set; }
}
