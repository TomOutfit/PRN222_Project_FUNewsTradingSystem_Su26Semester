namespace FUNewsTradingSystem_BusinessLayer.Services.Interfaces
{
    public interface ITradingAgentService
    {
        Task<TradingAnalysisResult> AnalyzeArticleAsync(string rawContent);
    }

    public class TradingAnalysisResult
    {
        public string Sentiment { get; set; } = string.Empty;
        public string TradingSignal { get; set; } = string.Empty;
        public double ConfidenceScore { get; set; }
    }
}
