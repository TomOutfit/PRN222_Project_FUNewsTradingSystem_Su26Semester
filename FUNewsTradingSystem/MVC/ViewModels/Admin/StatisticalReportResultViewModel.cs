using System.ComponentModel.DataAnnotations;

namespace FUNewsTradingSystem_MVC.ViewModels.Admin;

public class StatisticalReportResultViewModel
{
    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    public IEnumerable<NewsArticleStatDto> Results { get; set; } = Enumerable.Empty<NewsArticleStatDto>();

    public bool HasResults => Results.Any();
}

public class NewsArticleStatDto
{
    public int NewsArticleId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Headline { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string CreatedByName { get; set; } = string.Empty;
}
