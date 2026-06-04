using FUNewsTradingSystem_BusinessLayer.Services.Interfaces;
using System.Net.Http;

namespace FUNewsTradingSystem_BusinessLayer.Services.Implements
{
    public class TradingAgentService : ITradingAgentService
    {
        private readonly HttpClient _httpClient;
        private readonly Random _random = new Random();

        public TradingAgentService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<TradingAnalysisResult> AnalyzeArticleAsync(string rawContent)
        {
            // Placeholder logic: mock response with random sentiment/trading signals
            // In a real scenario, this would use _httpClient to call an external API (like OpenAI)
            
            await Task.Delay(100); // Simulate network call

            var sentiments = new[] { "Bullish", "Bearish", "Neutral" };
            var signals = new[] { "BUY", "SELL", "HOLD" };

            var index = _random.Next(0, 3);

            return new TradingAnalysisResult
            {
                Sentiment = sentiments[index],
                TradingSignal = signals[index],
                ConfidenceScore = Math.Round(_random.NextDouble() * 100, 2)
            };
        }
    }
}
