using System.ComponentModel.DataAnnotations;

namespace FUNewsTradingSystem_MVC.ViewModels.Statistics
{
    public class NewsArticleStatDto
    {
        public int NewsArticleID { get; set; }
        public string NewsTitle { get; set; } = string.Empty;
        public string Headline { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string CreatedByName { get; set; } = string.Empty;
    }

    public class StatisticsResultViewModel
    {
        public StatisticsFilterViewModel Filter { get; set; } = new StatisticsFilterViewModel();
        public List<NewsArticleStatDto> Results { get; set; } = new List<NewsArticleStatDto>();
        public bool HasResults { get; set; }
    }
}
