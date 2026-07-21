using System.ComponentModel.DataAnnotations;

namespace FUNewsTradingSystem_MVC.ViewModels.Statistics
{
    public class StatisticsFilterViewModel
    {
        [Required(ErrorMessage = "Start Date is required.")]
        [DataType(DataType.Date)]
        [Display(Name = "Start Date")]
        public DateTime StartDate { get; set; } = DateTime.Today.AddDays(-30);

        [Required(ErrorMessage = "End Date is required.")]
        [DataType(DataType.Date)]
        [Display(Name = "End Date")]
        public DateTime EndDate { get; set; } = DateTime.Today;

        public int? TagId { get; set; }
    }
}
