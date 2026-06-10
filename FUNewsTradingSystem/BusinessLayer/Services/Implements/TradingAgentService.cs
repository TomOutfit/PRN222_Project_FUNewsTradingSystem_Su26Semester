using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
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
                using (var scope = _serviceProvider.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<FUNewsManagementContext>();
                    var tag = await context.Tags.FindAsync(tagId);
                    if (tag == null)
                    {
                        throw new PipelineException("DB_ERROR");
                    }
                    ticker = tag.TagName;
                }

                // 2. Fetch News headlines
                var headlines = await FetchNewsAsync(ticker);

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

        private async Task<string> FetchNewsAsync(string tickerName)
        {
            var apiKey = _configuration["NewsApi:ApiKey"];
            var baseUrl = _configuration["NewsApi:BaseUrl"] ?? "https://newsapi.org/v2/everything";

            var requestUrl = $"{baseUrl}?q={Uri.EscapeDataString(tickerName)}&sortBy=publishedAt&pageSize=10&apiKey={apiKey}";
            
            var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            request.Headers.Add("User-Agent", "FUNewsTradingSystem");

            HttpResponseMessage response;
            try
            {
                response = await _httpClient.SendAsync(request);
            }
            catch (TaskCanceledException)
            {
                throw new PipelineException("NEWS_TIMEOUT");
            }
            catch (Exception ex)
            {
                throw new PipelineException("NEWS_API_ERROR", ex);
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new PipelineException("NEWS_API_ERROR");
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
            catch (Exception ex)
            {
                throw new PipelineException("NEWS_API_ERROR", ex);
            }

            if (newsResponse == null || newsResponse.Articles == null || newsResponse.Articles.Count == 0)
            {
                throw new PipelineException("NO_NEWS");
            }

            var numberedList = new List<string>();
            for (int i = 0; i < newsResponse.Articles.Count; i++)
            {
                var title = newsResponse.Articles[i].Title ?? "";
                var description = newsResponse.Articles[i].Description ?? "";
                numberedList.Add($"{i + 1}. {title} – {description}");
            }

            return string.Join("\n", numberedList);
        }

        private async Task<string> RunSentimentAgentAsync(string ticker, string headlines)
        {
            var prompt = SENTIMENT_AGENT_PROMPT_TEMPLATE
                .Replace("{ticker}", ticker)
                .Replace("{headlines_numbered_list}", headlines);
            return await CallOpenAiAsync(prompt);
        }

        private async Task<string> RunFundamentalAgentAsync(string ticker, string headlines, string sentimentOutput)
        {
            var prompt = FUNDAMENTAL_AGENT_PROMPT_TEMPLATE
                .Replace("{ticker}", ticker)
                .Replace("{headlines_numbered_list}", headlines)
                .Replace("{sentiment_output}", sentimentOutput);
            return await CallOpenAiAsync(prompt);
        }

        private async Task<PortfolioManagerResponse> RunPortfolioManagerAsync(
            string ticker, 
            string sentimentOutput, 
            string fundamentalOutput)
        {
            var prompt = PORTFOLIO_MANAGER_PROMPT_TEMPLATE
                .Replace("{ticker}", ticker)
                .Replace("{sentiment_output}", sentimentOutput)
                .Replace("{fundamental_output}", fundamentalOutput);

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
                throw new PipelineException("JSON_PARSE_ERROR", ex);
            }

            if (result == null)
            {
                throw new PipelineException("JSON_PARSE_ERROR");
            }

            ValidatePortfolioResponse(result);
            return result;
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
                throw new PipelineException("LLM_ERROR");
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
                throw new PipelineException("LLM_ERROR", ex);
            }

            var content = openAiResponse?.Choices?[0]?.Message?.Content;
            if (string.IsNullOrWhiteSpace(content))
            {
                throw new PipelineException("LLM_ERROR");
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

            return trimmed.Trim();
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
                throw new PipelineException("INVALID_DECISION");
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
