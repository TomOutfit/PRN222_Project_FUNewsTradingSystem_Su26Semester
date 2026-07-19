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
        public int ConfidenceScore { get; set; }
        public string Decision { get; set; } = string.Empty;
    }

    public class StatisticsResultViewModel
    {
        public StatisticsFilterViewModel Filter { get; set; } = new StatisticsFilterViewModel();
        public StaticPagedList<NewsArticleStatDto> Results { get; set; } = new StaticPagedList<NewsArticleStatDto>(new List<NewsArticleStatDto>(), 1, 10, 0);
        public bool HasResults { get; set; }

        // Chart stats
        public int BuyCount { get; set; }
        public int SellCount { get; set; }
        public int HoldCount { get; set; }
        public Dictionary<string, int> SectorCounts { get; set; } = new Dictionary<string, int>();
        public double AverageConfidence { get; set; }
        public double BuyAverageConfidence { get; set; }
        public double SellAverageConfidence { get; set; }
        public double HoldAverageConfidence { get; set; }

        // Daily trend for chart (all items, not paged)
        public Dictionary<string, int> DailyCounts { get; set; } = new Dictionary<string, int>();
    }
}
