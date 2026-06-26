using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using X.PagedList;

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
        public StaticPagedList<NewsArticleStatDto> Results { get; set; } = new StaticPagedList<NewsArticleStatDto>(new List<NewsArticleStatDto>(), 1, 10, 0);
        public bool HasResults { get; set; }
    }
}
