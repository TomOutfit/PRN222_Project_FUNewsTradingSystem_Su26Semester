using FUNewsTradingSystem_BusinessLayer.Services.Interfaces;
using System.Net.Http;

namespace FUNewsTradingSystem_BusinessLayer.Services.Implements
{
    public class TradingAgentService : ITradingAgentService
    {
        public static readonly string SENTIMENT_AGENT_PROMPT_TEMPLATE = 
@"You are a financial Sentiment Analyst. Given the following recent news headlines about {ticker}:

{headlines_numbered_list}

Analyze the prevailing market sentiment. Classify it as Positive, Negative, or Neutral.
Provide a concise reasoning paragraph of 2–3 sentences.
Respond with only the analysis text — no JSON, no headers, no labels.";

        public static readonly string FUNDAMENTAL_AGENT_PROMPT_TEMPLATE = 
@"You are a Financial Fundamental Analyst. Given the following recent news headlines about {ticker}:

{headlines_numbered_list}

And the following Sentiment Analysis:
{sentiment_output}

Evaluate the core business and fundamental impact of this news on {ticker}.
Consider revenue implications, competitive positioning, and long-term outlook.
Provide a concise analysis of 2–3 sentences.
Respond with only the analysis text — no JSON, no headers, no labels.";

        public static readonly string PORTFOLIO_MANAGER_PROMPT_TEMPLATE = 
@"You are a Portfolio Manager. Based on the analyses below for {ticker}, produce a final trading decision.

Sentiment Analysis:
{sentiment_output}

Fundamental Analysis:
{fundamental_output}

Respond ONLY with a single valid JSON object. Do not include any text before or after the JSON.
Do not use markdown code fences. The JSON must conform exactly to this schema:
{
  ""decision"": ""BUY"" | ""SELL"" | ""HOLD"",
  ""title"": ""A concise title that includes the decision and ticker symbol"",
  ""headline"": ""One sentence summarizing the core reasoning for the decision"",
  ""content"": ""A structured paragraph covering: (1) Sentiment view, (2) Fundamental view, (3) Key risk warnings"",
  ""source"": ""Description of the data sources and AI model used""
}";

        private readonly HttpClient _httpClient;

        public TradingAgentService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<TradingAgentResult> RunAnalysisAsync(int tagId, int categoryId, int createdByAccountId)
        {
            // TODO: Implement full pipeline with actual API calls
            await Task.Delay(100); // Simulate network call

            // Mock implementation - replace with actual pipeline
            return new TradingAgentResult
            {
                Success = true,
                NewsArticleID = 1,
                ErrorMessage = null
            };
        }
    }
}
