namespace FUNewsTradingSystem_BusinessLayer.Services.Interfaces
{
    public class TradingAgentResult
    {
        public bool Success { get; set; }
        public int? NewsArticleID { get; set; }
        public string? ErrorMessage { get; set; }
        public string? PipelineType { get; set; }
        public TradingAgentsRichData? RichData { get; set; }
    }

    public class TradingAgentsRichData
    {
        public string Decision { get; set; } = "";
        public string Signal { get; set; } = "";
        public string Ticker { get; set; } = "";
        public int ConfidenceScore { get; set; }
        public string? SentimentReport { get; set; }
        public string? FundamentalsReport { get; set; }
        public string? NewsReport { get; set; }
        public string? MarketReport { get; set; }
        public string? InvestmentPlan { get; set; }
        public string? TraderInvestmentPlan { get; set; }
        public string? FinalTradeDecision { get; set; }
    }
}
