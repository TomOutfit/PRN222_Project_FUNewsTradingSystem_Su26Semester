using System;

namespace FUNewsTradingSystem_DataAccessLayer.Models.DTOs
{
    public class NewsApiArticle
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string PublishedAt { get; set; } = string.Empty;
        public NewsApiSource Source { get; set; } = null!;
    }

    public class NewsApiSource
    {
        public string? Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
