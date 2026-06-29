using System.ComponentModel.DataAnnotations;

namespace FUNewsTradingSystem_MVC.ViewModels.Accounts
{
    public class EditAccountViewModel
    {
        [Required]
        public int AccountId { get; set; }

        [Required(ErrorMessage = "Account Name is required.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 100 characters.")]
        [Display(Name = "Account Name")]
        public string AccountName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid Email Address.")]
        [MaxLength(200)]
        [Display(Name = "Email Address")]
        public string AccountEmail { get; set; } = string.Empty;

        [MinLength(8, ErrorMessage = "Password must be at least 8 characters long.")]
        [MaxLength(500)]
        [Display(Name = "New Password (leave blank to keep current)")]
        public string? AccountPassword { get; set; }

        [Required]
        [Range(1, 2, ErrorMessage = "Select a valid role (Staff or Lecturer).")]
        [Display(Name = "Account Role")]
        public int AccountRole { get; set; }
    }
}
