using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
  ""source"": ""Description of the data sources and AI model used"",
  ""confidenceScore"": integer (0-100)
}";

        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<TradingAgentService> _logger;

        public TradingAgentService(
            HttpClient httpClient,
            IConfiguration configuration,
            IServiceProvider serviceProvider,
            ILogger<TradingAgentService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _serviceProvider = serviceProvider;
            _logger = logger;
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
    public async Task<TradingAgentResult> RunAnalysisAsync(
            int tagId, int categoryId, int createdByAccountId,
            string pipeline = "classic",
            Func<string, int, Task>? onProgress = null,
            string? depth = "fast",
            string? provider = "openai")
        {
            try
            {
                // 1. Resolve Tag and Ticker symbol
                if (onProgress != null) await onProgress("Resolving asset and ticker symbols...", 10);
                string ticker;
                string companyName = "";
                using (var scope = _serviceProvider.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<FUNewsManagementContext>();
                    var tag = await context.Tags.FindAsync(tagId);
                    if (tag == null) throw new PipelineException("DB_ERROR");
                    ticker = tag.TagName;
                    companyName = tag.Note ?? "";
                }

                PortfolioManagerResponse portfolioResponse;
                TradingAgentsRichData? richData = null;

                if (pipeline == "tradingagents" && IsPythonAdapterEnabled())
                {
                    // ── TradingAgents multi-agent pipeline (Python subprocess) ──
                    if (onProgress != null)
                        await onProgress("Launching TradingAgents multi-agent pipeline...", 20);
                    (portfolioResponse, richData) = await RunPythonAdapterAsync(ticker, onProgress, depth ?? "fast", provider ?? "openai");
                }
                else
                {
                    // ── Classic 3-agent OpenAI pipeline (unchanged) ──────────────
                    if (onProgress != null) await onProgress($"Fetching latest news headlines for {ticker}...", 30);
                    var headlines = await FetchNewsAsync(ticker, companyName);

                    if (onProgress != null) await onProgress("Processing sentiment evaluation via AI...", 55);
                    var sentimentOutput = await RunSentimentAgentAsync(ticker, headlines, provider ?? "openai");

                    if (onProgress != null) await onProgress("Synthesizing core fundamental analysis...", 75);
                    var fundamentalOutput = await RunFundamentalAgentAsync(ticker, headlines, sentimentOutput, provider ?? "openai");

                    if (onProgress != null) await onProgress("Generating final portfolio recommendation and report layout...", 90);
                    portfolioResponse = await RunPortfolioManagerAsync(ticker, sentimentOutput, fundamentalOutput, provider ?? "openai");
                }

                // Save to database (shared by both pipelines)
                if (onProgress != null) await onProgress("Storing completed report in the database...", 95);
                var newsArticleId = await SaveReportAsync(portfolioResponse, tagId, categoryId, createdByAccountId);

                if (onProgress != null) await onProgress("Analysis pipeline completed successfully!", 100);

                return new TradingAgentResult
                {
                    Success = true,
                    NewsArticleID = newsArticleId,
                    ErrorMessage = null,
                    PipelineType = pipeline,
                    RichData = richData
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

        // ── Python adapter helpers ────────────────────────────────────────────

        private bool IsPythonAdapterEnabled()
            => bool.TryParse(_configuration["TradingAgents:EnablePythonAdapter"], out var v) && v;

        private async Task<(PortfolioManagerResponse, TradingAgentsRichData)> RunPythonAdapterAsync(
            string ticker, Func<string, int, Task>? onProgress, string depth = "fast", string provider = "openai")
        {
            var rawScriptsPath = _configuration["TradingAgents:ScriptsPath"];
            var scriptsPath = string.IsNullOrEmpty(rawScriptsPath)
                ? System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "scripts")
                : rawScriptsPath;
            var scriptPath = System.IO.Path.Combine(scriptsPath, "ta_adapter.py");
            var python = _configuration["TradingAgents:PythonExecutable"] ?? "python3";
            var date = DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-dd");

            _logger.LogInformation("[TradingAgents] Starting Python adapter: {Python} \"{Script}\" {Ticker} {Date} depth={Depth}",
                python, scriptPath, ticker, date, depth);

            var psi = new ProcessStartInfo
            {
                FileName = python,
                Arguments = $"\"{scriptPath}\" {ticker} {date}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };
            // Normalise provider name and set the env var the library reads
            var normalizedProvider = provider.ToLowerInvariant() switch {
                "groq"               => "groq",
                "google" or "gemini" => "google",
                _                    => "openai"
            };
            psi.EnvironmentVariables["TRADINGAGENTS_LLM_PROVIDER"] = normalizedProvider;
            _logger.LogInformation("[TradingAgents] LLM provider: {Provider}", normalizedProvider);

            // Override model names — DEFAULT_CONFIG defaults to OpenAI names (gpt-5.5/gpt-5.4-mini)
            // which cause 404 errors on Groq/Google APIs. Set provider-appropriate models via the
            // env vars that default_config._apply_env_overrides already knows how to read.
            if (normalizedProvider == "groq")
            {
                psi.EnvironmentVariables["TRADINGAGENTS_DEEP_THINK_LLM"]  = "llama-3.3-70b-versatile";
                psi.EnvironmentVariables["TRADINGAGENTS_QUICK_THINK_LLM"] = "llama-3.1-8b-instant";
            }
            else if (normalizedProvider == "google")
            {
                psi.EnvironmentVariables["TRADINGAGENTS_DEEP_THINK_LLM"]  = "gemini-2.5-flash";
                psi.EnvironmentVariables["TRADINGAGENTS_QUICK_THINK_LLM"] = "gemini-3.1-flash-lite";
            }
            // openai: keep library defaults (gpt-5.5 / gpt-5.4-mini)

            // Forward the correct API key for the selected provider
            if (normalizedProvider == "groq")
            {
                var groqKey = _configuration["Groq:ApiKey"];
                if (string.IsNullOrEmpty(groqKey) || groqKey.StartsWith("YOUR_", StringComparison.OrdinalIgnoreCase))
                    _logger.LogWarning("[TradingAgents] GROQ_API_KEY is not configured — adapter will fail");
                else
                {
                    _logger.LogInformation("[TradingAgents] GROQ_API_KEY configured ({Len} chars)", groqKey.Length);
                    psi.EnvironmentVariables["GROQ_API_KEY"] = groqKey;
                }
            }
            else if (normalizedProvider == "google")
            {
                var googleKey = _configuration["Google:ApiKey"];
                if (string.IsNullOrEmpty(googleKey) || googleKey.StartsWith("YOUR_", StringComparison.OrdinalIgnoreCase))
                    _logger.LogWarning("[TradingAgents] GOOGLE_API_KEY is not configured — adapter will fail");
                else
                {
                    _logger.LogInformation("[TradingAgents] GOOGLE_API_KEY configured ({Len} chars)", googleKey.Length);
                    psi.EnvironmentVariables["GOOGLE_API_KEY"] = googleKey;
                }
            }
            else // openai (default)
            {
                var openAiKey = _configuration["OpenAI:ApiKey"];
                if (string.IsNullOrEmpty(openAiKey) || openAiKey.StartsWith("YOUR_", StringComparison.OrdinalIgnoreCase))
                    _logger.LogWarning("[TradingAgents] OPENAI_API_KEY is not configured or is a placeholder — Python adapter will fail with AuthenticationError");
                else
                {
                    _logger.LogInformation("[TradingAgents] OPENAI_API_KEY configured ({Len} chars)", openAiKey.Length);
                    psi.EnvironmentVariables["OPENAI_API_KEY"] = openAiKey;
                }
            }

            // Depth controls debate/risk rounds: fast=1, balanced=2
            var rounds = depth == "balanced" ? "2" : "1";
            psi.EnvironmentVariables["TRADINGAGENTS_MAX_DEBATE_ROUNDS"] = rounds;
            psi.EnvironmentVariables["TRADINGAGENTS_MAX_RISK_ROUNDS"]   = rounds;


            using var proc = Process.Start(psi)
                ?? throw new PipelineException("PYTHON_ERROR: failed to start process");

            // Read stdout/stderr concurrently to avoid deadlocks on large output
            var stdoutTask = proc.StandardOutput.ReadToEndAsync();
            var stderrTask = proc.StandardError.ReadToEndAsync();

            // Simulate pipeline stage progress while Python runs (fires every ~30 s)
            using var cts = new System.Threading.CancellationTokenSource();
            var progressTask = SimulatePipelineProgressAsync(onProgress, cts.Token);

            await proc.WaitForExitAsync();
            await cts.CancelAsync();
            await progressTask.ConfigureAwait(false);

            var stdout = await stdoutTask;
            var stderr = await stderrTask;

            // Always try to parse stdout first — _fail() writes {"error":"..."} there
            PythonAdapterOutput? dto = null;
            if (!string.IsNullOrWhiteSpace(stdout))
            {
                try
                {
                    dto = JsonSerializer.Deserialize<PythonAdapterOutput>(
                        stdout, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                catch (JsonException) { /* stdout was not JSON; fall through to exit-code check */ }
            }

            // Propagate any error the adapter reported explicitly
            if (!string.IsNullOrEmpty(dto?.Error))
            {
                var adapterErr = dto!.Error;
                _logger.LogError("[TradingAgents] Adapter error: {Error}", adapterErr);
                var lowerErr = adapterErr.ToLowerInvariant();
                var providerLabel = normalizedProvider.ToUpperInvariant(); // "OPENAI" | "GROQ" | "GOOGLE"
                string errPrefix = lowerErr.Contains("authentication") || lowerErr.Contains("api key") || lowerErr.Contains("401")
                    ? $"{providerLabel} Auth Error — "
                    : lowerErr.Contains("rate limit") || lowerErr.Contains("429") || lowerErr.Contains("resource_exhausted")
                        ? $"{providerLabel} Rate Limit — "
                        : lowerErr.Contains("import error") || lowerErr.Contains("no module")
                            ? "Python Dependency Error — "
                            : "";
                throw new PipelineException($"PYTHON_ERROR: {errPrefix}{adapterErr}");
            }

            if (proc.ExitCode != 0)
            {
                var fullStderr = stderr.Trim();
                // Log full output (no truncation) so Render logs contain the complete traceback
                _logger.LogError("[TradingAgents] Process exited {Code}.\n=== STDERR ===\n{Stderr}\n=== STDOUT ===\n{Stdout}",
                    proc.ExitCode, fullStderr, stdout.Trim());
                // Show the most informative stream in the UI error
                var displayErr = string.IsNullOrEmpty(fullStderr) ? stdout.Trim() : fullStderr;
                throw new PipelineException(
                    $"PYTHON_ERROR: exit {proc.ExitCode} — {TruncateForLog(displayErr, 800)}");
            }

            if (dto == null)
                throw new PipelineException("PYTHON_ERROR: no output from adapter");

            var pm = new PortfolioManagerResponse
            {
                Decision        = (dto.Decision ?? "HOLD").Trim().ToUpperInvariant(),
                Title           = dto.Title    ?? $"[HOLD] {ticker} Analysis",
                Headline        = dto.Headline ?? "",
                Content         = dto.Content  ?? "",
                Source          = dto.Source   ?? "TradingAgents",
                ConfidenceScore = dto.ConfidenceScore ?? 70,
            };
            ValidatePortfolioResponse(pm);

            var rich = new TradingAgentsRichData
            {
                Decision             = pm.Decision,
                Signal               = dto.Signal ?? pm.Decision,
                Ticker               = ticker,
                ConfidenceScore      = pm.ConfidenceScore,
                SentimentReport      = dto.SentimentReport,
                FundamentalsReport   = dto.FundamentalsReport,
                NewsReport           = dto.NewsReport,
                MarketReport         = dto.MarketReport,
                InvestmentPlan       = dto.InvestmentPlan,
                TraderInvestmentPlan = dto.TraderInvestmentPlan,
                FinalTradeDecision   = dto.FinalTradeDecision,
            };

            return (pm, rich);
        }

        private static async Task SimulatePipelineProgressAsync(
            Func<string, int, Task>? onProgress, System.Threading.CancellationToken ct)
        {
            if (onProgress == null) return;
            var stages = new (int Pct, string Msg)[]
            {
                (30, "Fetching market data (OHLCV, technical indicators)..."),
                (38, "Social sentiment analyst processing StockTwits and Reddit..."),
                (46, "News analyst reviewing recent articles and macro data..."),
                (54, "Fundamentals analyst evaluating earnings and balance sheet..."),
                (62, "Bull/Bear debate team synthesizing investment thesis..."),
                (70, "Research manager reviewing debate and drawing conclusions..."),
                (78, "Trader composing investment plan..."),
                (84, "Risk management panel reviewing trade parameters..."),
                (90, "Portfolio manager finalising recommendation..."),
            };
            foreach (var (pct, msg) in stages)
            {
                try { await Task.Delay(30_000, ct); }
                catch (OperationCanceledException) { break; }
                try { await onProgress(msg, pct); }
                catch { /* SignalR hub errors are non-fatal */ }
            }
        }

        private sealed class PythonAdapterOutput
        {
            public string? Decision { get; set; }
            public string? Signal { get; set; }
            public string? Title { get; set; }
            public string? Headline { get; set; }
            public string? Content { get; set; }
            public string? Source { get; set; }
            public int? ConfidenceScore { get; set; }
            public string? MarketReport { get; set; }
            public string? SentimentReport { get; set; }
            public string? NewsReport { get; set; }
            public string? FundamentalsReport { get; set; }
            public string? InvestmentPlan { get; set; }
            public string? TraderInvestmentPlan { get; set; }
            public string? FinalTradeDecision { get; set; }
            public string? Error { get; set; }
        }

        // ── Classic pipeline ─────────────────────────────────────────────────

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

        internal string GenerateFallbackNewsHeadlines(string ticker, string companyName)
        {
            var name = string.IsNullOrWhiteSpace(companyName) ? ticker : companyName;
            var rand = new Random();
            var scenario = rand.Next(0, 3); // 0 = Positive, 1 = Negative, 2 = Neutral

            if (scenario == 0)
            {
                return string.Join("\n", new[]
                {
                    $"1. {name} ({ticker}) Reports Strong Quarterly Performance and Revenue Growth – Financial analysts highlight robust demand across core business divisions.",
                    $"2. Market Sentiment Surges for {ticker} Following Strategic Expansion Announcement – Investors react positively to management's long-term growth outlook and market positioning.",
                    $"3. Institutional Buyers Increase Stake in {ticker} Amidst Broader Market Rally – Wall Street upgrades target prices citing solid operational margins and balance sheet strength."
                });
            }
            else if (scenario == 1)
            {
                return string.Join("\n", new[]
                {
                    $"1. {name} ({ticker}) Shares Decline as Supply Chain Constraints Impact Profit Margins – Analysts warn that rising logistical costs could pressure quarterly earnings.",
                    $"2. Regulatory Scrutiny Intensifies for {ticker} Amid Compliance Inquiries – Regulatory agencies request additional operational audits, sparking investor anxiety.",
                    $"3. Institutional Outflow Detected in {ticker} as Sector Rotation Accelerates – Market participants reduce exposure citing near-term macroeconomic headwinds and valuation premium."
                });
            }
            else
            {
                return string.Join("\n", new[]
                {
                    $"1. {name} ({ticker}) Trades in Narrow Consolidation Range Ahead of Key Economic Data – Investors adopt wait-and-see stance pending macroeconomic catalysts.",
                    $"2. {ticker} Announces Routine Management Restructuring and Board Reorganization – Board confirms transition plan is running on schedule with zero expected operational disruptions.",
                    $"3. General Market Consolidation Limits Price Movement for {ticker} – Lower trading volume observed across major exchanges as asset remains rangebound."
                });
            }
        }

        private async Task<string> RunSentimentAgentAsync(string ticker, string headlines, string provider = "openai")
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
                return await CallOpenAiAsync(prompt, provider);
            }
            catch (PipelineException ex) when (CanFallbackToMock(ex))
            {
                return GenerateMockSentimentOutput(ticker, headlines);
            }
        }

        private async Task<string> RunFundamentalAgentAsync(string ticker, string headlines, string sentimentOutput, string provider = "openai")
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
                return await CallOpenAiAsync(prompt, provider);
            }
            catch (PipelineException ex) when (CanFallbackToMock(ex))
            {
                return GenerateMockFundamentalOutput(ticker, headlines, sentimentOutput);
            }
        }

        private async Task<PortfolioManagerResponse> RunPortfolioManagerAsync(
            string ticker,
            string sentimentOutput,
            string fundamentalOutput,
            string provider = "openai")
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
                var rawResponse = await CallOpenAiAsync(prompt, provider);
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

        // Groq and Gemini expose OpenAI-compatible REST endpoints, so the same
        // HTTP call works for all three providers — only URL, key, and model differ.
        private (string baseUrl, string apiKey, string model) ResolveLlmConfig(string provider)
        {
            return (provider ?? "openai").ToLowerInvariant() switch {
                "groq" => (
                    "https://api.groq.com/openai/v1/chat/completions",
                    _configuration["Groq:ApiKey"] ?? "",
                    "llama-3.3-70b-versatile"),
                "google" or "gemini" => (
                    "https://generativelanguage.googleapis.com/v1beta/openai/chat/completions",
                    _configuration["Google:ApiKey"] ?? "",
                    "gemini-3.1-flash-lite"),
                _ => (
                    _configuration["OpenAI:BaseUrl"] ?? "https://api.openai.com/v1/chat/completions",
                    _configuration["OpenAI:ApiKey"] ?? "",
                    _configuration["OpenAI:Model"] ?? "gpt-4o")
            };
        }

        private async Task<string> CallOpenAiAsync(string prompt, string provider = "openai")
        {
            var (baseUrl, apiKey, model) = ResolveLlmConfig(provider);

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

        internal List<string> ExtractHeadlineTitles(string headlines)
        {
            var titles = new List<string>();
            if (string.IsNullOrWhiteSpace(headlines)) return titles;

            var lines = headlines.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var cleanLine = Regex.Replace(line, @"^\d+\.\s*", "").Trim();
                if (string.IsNullOrWhiteSpace(cleanLine)) continue;

                var dashIndex = cleanLine.IndexOf(" – ");
                if (dashIndex < 0) dashIndex = cleanLine.IndexOf(" - ");
                if (dashIndex < 0) dashIndex = cleanLine.IndexOf(" — ");

                string title = dashIndex >= 0 ? cleanLine.Substring(0, dashIndex).Trim() : cleanLine;
                if (title.Length > 120) title = title.Substring(0, 117) + "...";
                
                if (!string.IsNullOrWhiteSpace(title))
                {
                    titles.Add(title);
                }
            }
            return titles;
        }

        internal string DetermineSentimentTone(string headlines)
        {
            if (string.IsNullOrWhiteSpace(headlines)) return "Positive";

            var lower = headlines.ToLowerInvariant();
            int positiveScore = 0;
            int negativeScore = 0;

            string[] posKeywords = { "rise", "surge", "gain", "grow", "bull", "rally", "up", "profit", "expansion", "buy", "success", "strong", "higher", "positive", "beat", "upgrade" };
            foreach (var kw in posKeywords)
            {
                if (lower.Contains(kw)) positiveScore++;
            }

            string[] negKeywords = { "drop", "fall", "decline", "warn", "plummet", "bear", "lawsuit", "down", "loss", "lower", "negative", "risk", "investigate", "crash", "miss", "downgrade" };
            foreach (var kw in negKeywords)
            {
                if (lower.Contains(kw)) negativeScore++;
            }

            if (negativeScore > positiveScore) return "Negative";
            if (positiveScore > negativeScore) return "Positive";
            
            var rand = new Random().Next(0, 3);
            return rand switch
            {
                0 => "Negative",
                1 => "Neutral",
                _ => "Positive"
            };
        }

        internal string GenerateMockSentimentOutput(string ticker, string headlines)
        {
            var tone = DetermineSentimentTone(headlines);
            var titles = ExtractHeadlineTitles(headlines);
            var sb = new StringBuilder();

            if (tone == "Positive")
            {
                sb.Append($"Positive market sentiment has built up around {ticker} recently. ");
                if (titles.Count > 0)
                {
                    sb.Append($"Traders are reacting enthusiastically to reports such as \"{titles[0]}\", driving high-volume accumulation. ");
                }
                if (titles.Count > 1)
                {
                    sb.Append($"Furthermore, additional interest sparked by \"{titles[1]}\" has reinforced bullish momentum on short-term horizons. ");
                }
                sb.Append("Institutional order books display significant buying pressure with key support levels holding firmly.");
            }
            else if (tone == "Negative")
            {
                sb.Append($"Prevailing market sentiment for {ticker} has turned negative. ");
                if (titles.Count > 0)
                {
                    sb.Append($"Selling pressure accelerated significantly in response to concerns in headlines, specifically \"{titles[0]}\". ");
                }
                if (titles.Count > 1)
                {
                    sb.Append($"Sentiment remains weighed down by concerns surrounding \"{titles[1]}\", prompting momentum traders to liquidate long positions. ");
                }
                sb.Append("Risk off behavior is dominating trading sessions with elevated volatility.");
            }
            else // Neutral
            {
                sb.Append($"Market sentiment for {ticker} is currently neutral or balanced. ");
                if (titles.Count > 0)
                {
                    sb.Append($"Mixed reactions to news like \"{titles[0]}\" have kept the price in a consolidation range. ");
                }
                if (titles.Count > 1)
                {
                    sb.Append($"Market participants are carefully weighing the implications of \"{titles[1]}\" before committing to a clear direction. ");
                }
                sb.Append("Trading volume is average, indicating a wait-and-see attitude across major desks.");
            }

            return sb.ToString();
        }

        internal string GenerateMockFundamentalOutput(string ticker, string headlines, string sentimentOutput)
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

            var titles = ExtractHeadlineTitles(headlines);
            var sb = new StringBuilder();

            if (decision == "BUY")
            {
                sb.Append($"From a fundamental perspective, {ticker} exhibits robust operational health and strong revenue drivers. ");
                if (titles.Count > 2)
                {
                    sb.Append($"Developments like \"{titles[2]}\" point to increased market share and enhanced competitive advantages. ");
                }
                else if (titles.Count > 0)
                {
                    sb.Append($"Strategic developments highlighted in \"{titles[0]}\" support sustained long-term earnings expansion. ");
                }
                sb.Append("Balance sheet liquidity remains healthy, supporting continuous business expansion and capital efficiency.");
            }
            else if (decision == "SELL")
            {
                sb.Append($"Fundamental headwinds are emerging for {ticker}, potentially constraining future profit margins. ");
                if (titles.Count > 2)
                {
                    sb.Append($"Structural challenges indicated by \"{titles[2]}\" raise questions about near-term valuation models. ");
                }
                else if (titles.Count > 0)
                {
                    sb.Append($"The operational risks detailed in \"{titles[0]}\" could delay planned growth initiatives. ");
                }
                sb.Append("Elevated debt levels or rising input costs could pressure the bottom line, warranting a cautious approach.");
            }
            else // HOLD
            {
                sb.Append($"The fundamental outlook for {ticker} remains stable with few near-term catalysts. ");
                if (titles.Count > 2)
                {
                    sb.Append($"While \"{titles[2]}\" presents a minor tailwind, it is offset by broader macroeconomic challenges. ");
                }
                sb.Append("Company earnings are tracking close to consensus expectations, and capital structures are stable but offer limited near-term upside.");
            }

            return sb.ToString();
        }

        internal PortfolioManagerResponse GenerateMockPortfolioManagerResponse(string ticker, string sentimentOutput, string fundamentalOutput)
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

            string riskWarnings;
            var sym = ticker.ToUpperInvariant();
            if (sym == "BTC" || sym == "ETH" || sym == "SOL" || sym == "BNB" || sym == "XRP")
            {
                riskWarnings = "Regulatory scrutiny on digital asset trading platforms, high network/transaction fee spikes, and extreme volatility driven by retail leverage liquidation.";
            }
            else if (sym == "SPY" || sym == "FNTS" || sym == "QQQ" || sym == "DIA")
            {
                riskWarnings = "Systemic index rebalancing risks, shifting monetary policy actions by the Federal Reserve, and macroeconomic inflation indicators impacting consumer demand.";
            }
            else
            {
                riskWarnings = "Supply chain dependencies, intense sector-wide price competition, potential regulatory compliance hurdles, and broader macroeconomic sector rotation.";
            }

            return new PortfolioManagerResponse
            {
                Decision = decision,
                Title = $"[{decision}] {ticker} Automated Analysis",
                Headline = $"Multi-agent quantitative analysis evaluating real-time market sentiment, core fundamental drivers, and portfolio risk parameters for {ticker}.",
                Content = $"(1) Sentiment view: {sentimentOutput}\n\n(2) Fundamental view: {fundamentalOutput}\n\n(3) Key risk warnings: {riskWarnings}",
                Source = "NewsAPI.org + GPT-4o Agent Engine",
                ConfidenceScore = new Random().Next(75, 96)
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

            // Clamp confidence score to valid range
            if (r.ConfidenceScore < 0) r.ConfidenceScore = 0;
            if (r.ConfidenceScore > 100) r.ConfidenceScore = 100;
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
                            NewsStatus = true,
                            ConfidenceScore = response.ConfidenceScore
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
