using System.Collections.Generic;

namespace FUNewsTradingSystem_DataAccessLayer.Models.DTOs
{
    public class NewsApiResponse
    {
        public string Status { get; set; } = string.Empty;
        public int TotalResults { get; set; }
        public List<NewsApiArticle> Articles { get; set; } = new();
    }
}
