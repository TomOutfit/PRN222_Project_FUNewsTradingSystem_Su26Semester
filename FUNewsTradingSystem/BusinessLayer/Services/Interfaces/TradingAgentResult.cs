namespace FUNewsTradingSystem_BusinessLayer.Services.Interfaces
{
    public class TradingAgentResult
    {
        public bool Success { get; set; }
        public int? NewsArticleID { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
