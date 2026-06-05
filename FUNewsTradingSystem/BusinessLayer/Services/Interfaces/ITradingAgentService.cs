namespace FUNewsTradingSystem_BusinessLayer.Services.Interfaces
{
    public interface ITradingAgentService
    {
        Task<TradingAgentResult> RunAnalysisAsync(int tagId, int categoryId, int createdByAccountId);
    }

    public class TradingAgentResult
    {
        public bool Success { get; set; }
        public int? NewsArticleID { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
