namespace FUNewsTradingSystem_MVC.ViewModels.Report;

public class SavedReportViewModel
{
    public int SavedReportID { get; set; }
    public DateTime SavedDate { get; set; }
    public ReportListItemViewModel Article { get; set; } = null!;
}
