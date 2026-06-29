using System.ComponentModel.DataAnnotations;

namespace FUNewsTradingSystem_MVC.ViewModels.Category 
{
    public class EditCategoryViewModel
    {
        [Required(ErrorMessage = "Category ID is required.")]
        [Display(Name = "Category ID")]
        public int CategoryID { get; set; } // Khóa chính dùng để xác định bản ghi cần sửa dưới DB

        [Required(ErrorMessage = "Category Name is required.")]
        [StringLength(200, ErrorMessage = "Category Name cannot exceed 200 characters.")]
        [Display(Name = "Category Name")]
        public string CategoryName { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Category Description cannot exceed 500 characters.")]
        [Display(Name = "Description")]
        public string? CategoryDescription { get; set; }

        [Display(Name = "Parent Category")]
        public int? ParentCategoryID { get; set; }

        [Required]
        [Display(Name = "Is Active")]
        public bool IsActive { get; set; }
    }
}
