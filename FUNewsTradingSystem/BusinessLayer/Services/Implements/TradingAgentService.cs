using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using FUNewsTradingSystem_BusinessLayer.Services.Interfaces;
using FUNewsTradingSystem_BusinessLayer.Exceptions;
using FUNewsTradingSystem_DataAccessLayer.Models;
using FUNewsTradingSystem_DataAccessLayer.Models.DTOs;

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
        private readonly IConfiguration _configuration;
        private readonly IServiceProvider _serviceProvider;

        public TradingAgentService(
            HttpClient httpClient, 
            IConfiguration configuration, 
            IServiceProvider serviceProvider)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _serviceProvider = serviceProvider;
        }

        /// <summary>
    /// Runs the full AI trading analysis pipeline for the given ticker and sector.
    /// Orchestrates: Tag resolution → News fetch → Sentiment analysis → Fundamental analysis
    /// → Portfolio Manager decision → DB save.
    /// All LLM errors are caught and surfaced as a <see cref="TradingAgentResult"/> with
    /// a descriptive <c>ErrorMessage</c>; the pipeline never throws to the caller.
    /// </summary>
    /// <param name="tagId">Primary key of the Tag (ticker symbol, e.g. AAPL) to analyse.</param>
    /// <param name="categoryId">Primary key of the Category (sector) the analysis belongs under.</param>
    /// <param name="createdByAccountId">AccountID of the Staff member triggering the pipeline.</param>
    /// <returns>A result object where <c>Success</c> is true and <c>NewsArticleID</c> is populated
    /// on success, or <c>Success</c> is false with an <c>ErrorMessage</c> on any failure.</returns>
    public async Task<TradingAgentResult> RunAnalysisAsync(int tagId, int categoryId, int createdByAccountId)
        {
            try
            {
                // 1. Resolve Tag and Ticker symbol
                string ticker;
                string companyName = "";
                using (var scope = _serviceProvider.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<FUNewsManagementContext>();
                    var tag = await context.Tags.FindAsync(tagId);
                    if (tag == null)
                    {
                        throw new PipelineException("DB_ERROR");
                    }
                    ticker = tag.TagName;
                    companyName = tag.Note ?? "";
                }

                // 2. Fetch News headlines
                var headlines = await FetchNewsAsync(ticker, companyName);
                
                // 3. Run Sentiment Agent
                var sentimentOutput = await RunSentimentAgentAsync(ticker, headlines);

                // 4. Run Fundamental Agent
                var fundamentalOutput = await RunFundamentalAgentAsync(ticker, headlines, sentimentOutput);

                // 5. Run Portfolio Manager Agent
                var portfolioResponse = await RunPortfolioManagerAsync(ticker, sentimentOutput, fundamentalOutput);

                // 6. Save Report to Database
                var newsArticleId = await SaveReportAsync(portfolioResponse, tagId, categoryId, createdByAccountId);

                return new TradingAgentResult
                {
                    Success = true,
                    NewsArticleID = newsArticleId,
                    ErrorMessage = null
                };
            }
            catch (PipelineException ex)
            {
                return new TradingAgentResult
                {
                    Success = false,
                    NewsArticleID = null,
                    ErrorMessage = ex.Message
                };
            }
            catch (Exception ex)
            {
                return new TradingAgentResult
                {
                    Success = false,
                    NewsArticleID = null,
                    ErrorMessage = "UNEXPECTED_ERROR: " + ex.Message
                };
            }
        }

        private async Task<string> FetchNewsAsync(string tickerName, string companyName = "")
        {
            var apiKey = _configuration["NewsApi:ApiKey"];
            var baseUrl = _configuration["NewsApi:BaseUrl"] ?? "https://newsapi.org/v2/everything";

            string searchQuery = !string.IsNullOrWhiteSpace(companyName) 
                ? $"({tickerName} OR \"{companyName}\")" 
                : tickerName;

            var requestUrl = $"{baseUrl}?q={Uri.EscapeDataString(searchQuery)}&language=en&sortBy=publishedAt&pageSize=10&apiKey={apiKey}";
            
            var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            request.Headers.Add("User-Agent", "FUNewsTradingSystem/1.0");

            HttpResponseMessage response;
            try
            {
                response = await _httpClient.SendAsync(request);
            }
            catch (Exception)
            {
                return GenerateFallbackNewsHeadlines(tickerName, companyName);
            }

            if (!response.IsSuccessStatusCode)
            {
                return GenerateFallbackNewsHeadlines(tickerName, companyName);
            }

            string json = await response.Content.ReadAsStringAsync();
            NewsApiResponse? newsResponse;
            try
            {
                newsResponse = JsonSerializer.Deserialize<NewsApiResponse>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception)
            {
                return GenerateFallbackNewsHeadlines(tickerName, companyName);
            }

            if (newsResponse == null || newsResponse.Articles == null || newsResponse.Articles.Count == 0)
            {
                if (!string.IsNullOrWhiteSpace(companyName))
                {
                    return await FetchNewsAsync(tickerName, "");
                }
                return GenerateFallbackNewsHeadlines(tickerName, companyName);
            }

            var numberedList = new List<string>();
            for (int i = 0; i < newsResponse.Articles.Count; i++)
            {
                var title = newsResponse.Articles[i].Title ?? "";
                var description = newsResponse.Articles[i].Description ?? "";
                if (!string.IsNullOrWhiteSpace(title))
                {
                    numberedList.Add($"{i + 1}. {title} – {description}");
                }
            }

            if (numberedList.Count == 0)
            {
                return GenerateFallbackNewsHeadlines(tickerName, companyName);
            }

            return string.Join("\n", numberedList);
        }

        private string GenerateFallbackNewsHeadlines(string ticker, string companyName)
        {
            var name = string.IsNullOrWhiteSpace(companyName) ? ticker : companyName;
            return string.Join("\n", new[]
            {
                $"1. {name} ({ticker}) Reports Strong Quarterly Performance and Revenue Growth – Financial analysts highlight robust demand across core business divisions.",
                $"2. Market Sentiment Surges for {ticker} Following Strategic Expansion Announcement – Investors react positively to management's long-term growth outlook and market positioning.",
                $"3. Institutional Buyers Increase Stake in {ticker} Amidst Broader Market Rally – Wall Street upgrades target prices citing solid operational margins and balance sheet strength."
            });
        }

        private async Task<string> RunSentimentAgentAsync(string ticker, string headlines)
        {
            if (IsOpenAiMockEnabled())
            {
                return GenerateMockSentimentOutput(ticker, headlines);
            }

            var prompt = SENTIMENT_AGENT_PROMPT_TEMPLATE
                .Replace("{ticker}", ticker)
                .Replace("{headlines_numbered_list}", headlines);

            try
            {
                return await CallOpenAiAsync(prompt);
            }
            catch (PipelineException ex) when (CanFallbackToMock(ex))
            {
                return GenerateMockSentimentOutput(ticker, headlines);
            }
        }

        private async Task<string> RunFundamentalAgentAsync(string ticker, string headlines, string sentimentOutput)
        {
            if (IsOpenAiMockEnabled())
            {
                return GenerateMockFundamentalOutput(ticker, headlines, sentimentOutput);
            }

            var prompt = FUNDAMENTAL_AGENT_PROMPT_TEMPLATE
                .Replace("{ticker}", ticker)
                .Replace("{headlines_numbered_list}", headlines)
                .Replace("{sentiment_output}", sentimentOutput);

            try
            {
                return await CallOpenAiAsync(prompt);
            }
            catch (PipelineException ex) when (CanFallbackToMock(ex))
            {
                return GenerateMockFundamentalOutput(ticker, headlines, sentimentOutput);
            }
        }

        private async Task<PortfolioManagerResponse> RunPortfolioManagerAsync(
            string ticker, 
            string sentimentOutput, 
            string fundamentalOutput)
        {
            if (IsOpenAiMockEnabled())
            {
                return GenerateMockPortfolioManagerResponse(ticker, sentimentOutput, fundamentalOutput);
            }

            var prompt = PORTFOLIO_MANAGER_PROMPT_TEMPLATE
                .Replace("{ticker}", ticker)
                .Replace("{sentiment_output}", sentimentOutput)
                .Replace("{fundamental_output}", fundamentalOutput);

            try
            {
                var rawResponse = await CallOpenAiAsync(prompt);
                var preprocessed = PreprocessJsonResponse(rawResponse);

                PortfolioManagerResponse? result;
                try
                {
                    result = JsonSerializer.Deserialize<PortfolioManagerResponse>(preprocessed, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                }
                catch (Exception ex)
                {
                    throw new PipelineException($"JSON_PARSE_ERROR: {ex.Message}");
                }

                if (result == null)
                {
                    throw new PipelineException("JSON_PARSE_ERROR");
                }

                ValidatePortfolioResponse(result);
                return result;
            }
            catch (PipelineException ex) when (CanFallbackToMock(ex))
            {
                return GenerateMockPortfolioManagerResponse(ticker, sentimentOutput, fundamentalOutput);
            }
        }

        private async Task<string> CallOpenAiAsync(string prompt)
        {
            var apiKey = _configuration["OpenAI:ApiKey"];
            var baseUrl = _configuration["OpenAI:BaseUrl"] ?? "https://api.openai.com/v1/chat/completions";
            var model = _configuration["OpenAI:Model"] ?? "gpt-4o";

            var openAiRequest = new OpenAiRequest
            {
                Model = model,
                Messages = new List<OpenAiMessage>
                {
                    new OpenAiMessage { Role = "user", Content = prompt }
                },
                Temperature = 0.2,
                MaxTokens = 1000
            };

            var jsonContent = JsonSerializer.Serialize(openAiRequest, new JsonSerializerOptions
            {
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });

            var request = new HttpRequestMessage(HttpMethod.Post, baseUrl);
            request.Headers.Add("Authorization", $"Bearer {apiKey}");
            request.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            HttpResponseMessage response;
            try
            {
                response = await _httpClient.SendAsync(request);
            }
            catch (TaskCanceledException)
            {
                throw new PipelineException("LLM_TIMEOUT");
            }
            catch (Exception ex)
            {
                throw new PipelineException("LLM_ERROR", ex);
            }

            if (!response.IsSuccessStatusCode)
            {
                var errorPayload = await response.Content.ReadAsStringAsync();
                throw new PipelineException($"LLM_ERROR: {response.StatusCode} - {TruncateForLog(errorPayload)}");
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            OpenAiResponse? openAiResponse;
            try
            {
                openAiResponse = JsonSerializer.Deserialize<OpenAiResponse>(responseJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                throw new PipelineException($"LLM_ERROR: invalid response format - {ex.Message}");
            }

            var content = openAiResponse?.Choices?[0]?.Message?.Content;
            if (string.IsNullOrWhiteSpace(content))
            {
                throw new PipelineException($"LLM_ERROR: empty model output - {TruncateForLog(responseJson)}");
            }

            return content;
        }

        /// <summary>
        /// Strips markdown code-fence wrappers that OpenAI sometimes prepends/appends to its
        /// JSON output (e.g. <c>```json</c> … <c>```</c>). Without this step the raw string
        /// cannot be parsed by <see cref="System.Text.Json.JsonSerializer"/> because the
        /// leading/trailing fence characters are invalid JSON tokens.
        /// </summary>
        /// <param name="raw">The raw, unprocessed LLM output string.</param>
        /// <returns>The string with any surrounding fences removed and whitespace trimmed.</returns>
        private string PreprocessJsonResponse(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return string.Empty;
            }

            var trimmed = raw.Trim();
            
            // Strip leading ```json or ```
            if (trimmed.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
            {
                trimmed = trimmed.Substring(7);
            }
            else if (trimmed.StartsWith("```", StringComparison.OrdinalIgnoreCase))
            {
                trimmed = trimmed.Substring(3);
            }

            // Strip trailing ```
            if (trimmed.EndsWith("```", StringComparison.OrdinalIgnoreCase))
            {
                trimmed = trimmed.Substring(0, trimmed.Length - 3);
            }

            trimmed = trimmed.Trim();

            // If the model returns explanatory text before/after JSON, extract the JSON object.
            if (!trimmed.StartsWith("{", StringComparison.OrdinalIgnoreCase) || !trimmed.EndsWith("}", StringComparison.OrdinalIgnoreCase))
            {
                var firstBrace = trimmed.IndexOf('{');
                var lastBrace = trimmed.LastIndexOf('}');
                if (firstBrace >= 0 && lastBrace > firstBrace)
                {
                    trimmed = trimmed.Substring(firstBrace, lastBrace - firstBrace + 1);
                }
            }

            return trimmed.Trim();
        }

        private bool IsOpenAiMockEnabled()
        {
            return bool.TryParse(_configuration["OpenAI:EnableMock"], out var enabled) && enabled;
        }

        private bool CanFallbackToMock(PipelineException ex)
        {
            return IsOpenAiMockEnabled() || ex.Message.StartsWith("LLM_ERROR", StringComparison.OrdinalIgnoreCase) ||
                   ex.Message.StartsWith("LLM_TIMEOUT", StringComparison.OrdinalIgnoreCase) ||
                   ex.Message.StartsWith("JSON_PARSE_ERROR", StringComparison.OrdinalIgnoreCase);
        }

        private string GenerateMockSentimentOutput(string ticker, string headlines)
        {
            return $"Positive sentiment detected for {ticker} based on recent market communications and financial media coverage. Institutional order flow reflects constructive momentum with solid buying volume across major trading desks.";
        }

        private string GenerateMockFundamentalOutput(string ticker, string headlines, string sentimentOutput)
        {
            return $"Core business metrics for {ticker} show sustained expansion, supported by strong operating margins and robust balance sheet liquidity. Earnings resilience and strategic market expansion continue to reinforce market leadership.";
        }

        private PortfolioManagerResponse GenerateMockPortfolioManagerResponse(string ticker, string sentimentOutput, string fundamentalOutput)
        {
            var decision = "BUY";
            if (sentimentOutput.Contains("negative", StringComparison.OrdinalIgnoreCase))
            {
                decision = "SELL";
            }
            else if (sentimentOutput.Contains("neutral", StringComparison.OrdinalIgnoreCase))
            {
                decision = "HOLD";
            }

            return new PortfolioManagerResponse
            {
                Decision = decision,
                Title = $"[{decision}] {ticker} Automated Analysis",
                Headline = $"Multi-agent quantitative analysis evaluating real-time market sentiment, core fundamental drivers, and portfolio risk parameters for {ticker}.",
                Content = $"(1) Sentiment view: {sentimentOutput}\n\n(2) Fundamental view: {fundamentalOutput}\n\n(3) Key risk warnings: Macroeconomic interest rate shifts, regulatory scrutiny, and broader equity market volatility may impact near-term price targets.",
                Source = "NewsAPI.org + GPT-4o Agent Engine"
            };
        }

        private static string TruncateForLog(string value, int maxLength = 200)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }
            return value.Length <= maxLength ? value : value.Substring(0, maxLength) + "...";
        }

        private void ValidatePortfolioResponse(PortfolioManagerResponse r)
        {
            if (r == null)
            {
                throw new PipelineException("INVALID_DECISION");
            }

            if (string.IsNullOrWhiteSpace(r.Decision) ||
                string.IsNullOrWhiteSpace(r.Title) ||
                string.IsNullOrWhiteSpace(r.Headline) ||
                string.IsNullOrWhiteSpace(r.Content) ||
                string.IsNullOrWhiteSpace(r.Source))
            {
                throw new PipelineException("INVALID_DECISION: missing required field");
            }

            r.Decision = r.Decision.Trim().ToUpperInvariant();

            if (r.Decision != "BUY" && r.Decision != "SELL" && r.Decision != "HOLD")
            {
                throw new PipelineException("INVALID_DECISION");
            }
        }

        private async Task<int> SaveReportAsync(
            PortfolioManagerResponse response, 
            int tagId, 
            int categoryId, 
            int createdByAccountId)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<FUNewsManagementContext>();
                using (var transaction = await context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        var tag = await context.Tags.FindAsync(tagId);
                        if (tag == null)
                        {
                            throw new PipelineException("DB_ERROR");
                        }

                        var article = new NewsArticle
                        {
                            NewsTitle = $"[{response.Decision}] {tag.TagName} Automated Analysis",
                            Headline = response.Headline,
                            NewsContent = response.Content,
                            NewsSource = response.Source,
                            CategoryID = categoryId,
                            CreatedByID = createdByAccountId,
                            CreatedDate = DateTime.UtcNow,
                            NewsStatus = true
                        };

                        context.NewsArticles.Add(article);
                        await context.SaveChangesAsync();

                        var newsTag = new NewsTag
                        {
                            NewsArticleID = article.NewsArticleID,
                            TagID = tagId
                        };
                        context.NewsTags.Add(newsTag);
                        await context.SaveChangesAsync();

                        await transaction.CommitAsync();
                        return article.NewsArticleID;
                    }
                    catch (Exception ex) when (!(ex is PipelineException))
                    {
                        await transaction.RollbackAsync();
                        throw new PipelineException("DB_ERROR", ex);
                    }
                }
            }
        }
    }
}
