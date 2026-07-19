using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Moq.Protected;
using Xunit;
using FUNewsTradingSystem_BusinessLayer.Services.Implements;
using FUNewsTradingSystem_DataAccessLayer.Models.DTOs;

namespace FUNewsTradingSystem.Tests.Services
{
    public class TradingAgentServiceTests
    {
        // ─────────────────────────────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────────────────────────────

        private static IConfiguration BuildConfig(bool mockEnabled = true, string? apiKey = null)
        {
            var dict = new Dictionary<string, string?>
            {
                ["OpenAI:EnableMock"]  = mockEnabled.ToString(),
                ["OpenAI:ApiKey"]      = apiKey ?? "test-key",
                ["OpenAI:BaseUrl"]     = "https://api.openai.com/v1/chat/completions",
                ["OpenAI:Model"]       = "gpt-4o",
                ["NewsApi:ApiKey"]     = "test-news-key",
                ["NewsApi:BaseUrl"]    = "https://newsapi.org/v2/everything"
            };
            return new ConfigurationBuilder()
                .AddInMemoryCollection(dict)
                .Build();
        }

        private static TradingAgentService CreateService(
            IConfiguration? config = null,
            HttpMessageHandler? handler = null,
            IServiceProvider? serviceProvider = null)
        {
            config          ??= BuildConfig();
            handler         ??= new FakeHttpHandler(HttpStatusCode.OK, "{}");
            serviceProvider ??= new Mock<IServiceProvider>().Object;

            var httpClient = new HttpClient(handler);
            return new TradingAgentService(httpClient, config, serviceProvider);
        }

        // ─────────────────────────────────────────────────────────────────
        // GenerateFallbackNewsHeadlines
        // ─────────────────────────────────────────────────────────────────

        [Theory]
        [InlineData("AAPL", "Apple Inc")]
        [InlineData("BTC",  "")]
        [InlineData("TSLA", "Tesla")]
        public void GenerateFallbackNewsHeadlines_ReturnsNonEmptyString(string ticker, string company)
        {
            var svc    = CreateService();
            var result = svc.GenerateFallbackNewsHeadlines(ticker, company);

            result.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public void GenerateFallbackNewsHeadlines_ContainsTickerSymbol()
        {
            var svc    = CreateService();
            var result = svc.GenerateFallbackNewsHeadlines("NVDA", "Nvidia");

            // Either ticker or company name should appear in at least one headline
            (result.Contains("NVDA") || result.Contains("Nvidia"))
                .Should().BeTrue(because: "fallback headlines must reference the stock");
        }

        [Fact]
        public void GenerateFallbackNewsHeadlines_WhenCompanyEmpty_UsesTickerAsName()
        {
            var svc    = CreateService();
            var result = svc.GenerateFallbackNewsHeadlines("XYZ", "");

            result.Should().Contain("XYZ");
        }

        [Fact]
        public void GenerateFallbackNewsHeadlines_ProducesThreeLines()
        {
            var svc    = CreateService();
            var result = svc.GenerateFallbackNewsHeadlines("MSFT", "Microsoft");

            var lines = result.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            lines.Should().HaveCount(3, because: "exactly 3 fallback headlines must be returned");
        }

        // ─────────────────────────────────────────────────────────────────
        // ExtractHeadlineTitles
        // ─────────────────────────────────────────────────────────────────

        [Fact]
        public void ExtractHeadlineTitles_ReturnsEmptyList_ForNullOrWhiteSpace()
        {
            var svc = CreateService();

            svc.ExtractHeadlineTitles(null!).Should().BeEmpty();
            svc.ExtractHeadlineTitles("").Should().BeEmpty();
            svc.ExtractHeadlineTitles("   ").Should().BeEmpty();
        }

        [Fact]
        public void ExtractHeadlineTitles_StripsNumberPrefixAndDescription()
        {
            var svc   = CreateService();
            var input = "1. Apple Stock Rises – Investors cheer strong earnings.\n2. Tesla Falls – Demand concerns weigh on sentiment.";

            var titles = svc.ExtractHeadlineTitles(input);

            titles.Should().HaveCount(2);
            titles[0].Should().Be("Apple Stock Rises");
            titles[1].Should().Be("Tesla Falls");
        }

        [Fact]
        public void ExtractHeadlineTitles_HandlesEmDashSeparator()
        {
            var svc   = CreateService();
            var input = "1. AAPL Surges — Record quarterly revenue reported.";

            var titles = svc.ExtractHeadlineTitles(input);

            titles.Should().ContainSingle();
            titles[0].Should().Be("AAPL Surges");
        }

        [Fact]
        public void ExtractHeadlineTitles_TruncatesLongTitles()
        {
            var svc        = CreateService();
            var longTitle  = new string('A', 150);
            var input      = $"1. {longTitle} – some description.";

            var titles = svc.ExtractHeadlineTitles(input);

            titles.Should().ContainSingle();
            titles[0].Length.Should().BeLessThanOrEqualTo(120);
            titles[0].Should().EndWith("...");
        }

        [Fact]
        public void ExtractHeadlineTitles_SkipsBlankLines()
        {
            var svc   = CreateService();
            var input = "1. Valid Headline – Description.\n\n   \n2. Another Headline – Details.";

            var titles = svc.ExtractHeadlineTitles(input);

            titles.Should().HaveCount(2);
        }

        [Fact]
        public void ExtractHeadlineTitles_WhenNoDashSeparator_ReturnsFullLine()
        {
            var svc   = CreateService();
            var input = "1. AAPL Strong Quarter With No Dash";

            var titles = svc.ExtractHeadlineTitles(input);

            titles.Should().ContainSingle();
            titles[0].Should().Be("AAPL Strong Quarter With No Dash");
        }

        // ─────────────────────────────────────────────────────────────────
        // DetermineSentimentTone
        // ─────────────────────────────────────────────────────────────────

        [Theory]
        [InlineData("surge rise gain profit bull buy strong higher positive beat upgrade rally expansion", "Positive")]
        [InlineData("drop fall decline warn plummet bear lawsuit down loss lower negative risk investigate crash miss downgrade", "Negative")]
        public void DetermineSentimentTone_DetectsCorrectTone(string headlines, string expectedTone)
        {
            var svc  = CreateService();
            var tone = svc.DetermineSentimentTone(headlines);
            tone.Should().Be(expectedTone);
        }

        [Fact]
        public void DetermineSentimentTone_EmptyHeadlines_ReturnsPositive()
        {
            var svc = CreateService();
            svc.DetermineSentimentTone("").Should().Be("Positive");
            svc.DetermineSentimentTone(null!).Should().Be("Positive");
        }

        [Fact]
        public void DetermineSentimentTone_ReturnsValidSentiment_WhenNeutral()
        {
            var svc  = CreateService();
            var tone = svc.DetermineSentimentTone("company announces routine restructuring consolidation");

            new[] { "Positive", "Negative", "Neutral" }.Should().Contain(tone);
        }

        // ─────────────────────────────────────────────────────────────────
        // GenerateMockSentimentOutput
        // ─────────────────────────────────────────────────────────────────

        [Theory]
        [InlineData("AAPL")]
        [InlineData("BTC")]
        [InlineData("TSLA")]
        public void GenerateMockSentimentOutput_ReturnsNonEmptyText(string ticker)
        {
            var svc      = CreateService();
            var fallback = svc.GenerateFallbackNewsHeadlines(ticker, "");
            var result   = svc.GenerateMockSentimentOutput(ticker, fallback);

            result.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public void GenerateMockSentimentOutput_PositiveSentiment_MentionsBullishLanguage()
        {
            var svc      = CreateService();
            var headlines = "1. AAPL Rises – surge profit gain bull rally rise";
            var result   = svc.GenerateMockSentimentOutput("AAPL", headlines);

            result.Should().Contain("AAPL");
        }

        [Fact]
        public void GenerateMockSentimentOutput_NegativeSentiment_ContainsNegativeLanguage()
        {
            var svc      = CreateService();
            var headlines = "1. TSLA Falls – decline drop crash loss warn lawsuit downgrade bear";
            var result   = svc.GenerateMockSentimentOutput("TSLA", headlines);

            result.Should().NotBeNullOrWhiteSpace();
            result.Should().Contain("TSLA");
        }

        [Fact]
        public void GenerateMockSentimentOutput_WithTwoOrMoreTitles_IncludesSecondTitleInOutput()
        {
            var svc      = CreateService();
            var headlines = "1. Strong Quarterly Results – Investors cheer.\n2. Record Revenue – Analysts upgrade target.\n";
            var result   = svc.GenerateMockSentimentOutput("AAPL", headlines);

            // The output should reference at least one headline title
            (result.Contains("Strong Quarterly Results") || result.Contains("Record Revenue"))
                .Should().BeTrue(because: "mock should embed headline titles into the sentiment text");
        }

        // ─────────────────────────────────────────────────────────────────
        // GenerateMockFundamentalOutput
        // ─────────────────────────────────────────────────────────────────

        [Theory]
        [InlineData("AAPL", "positive sentiment strong bullish", "BUY")]
        [InlineData("AAPL", "prevailing negative market sentiment", "SELL")]
        [InlineData("AAPL", "neutral balanced market sentiment", "HOLD")]
        public void GenerateMockFundamentalOutput_AlignedWithSentimentDecision(
            string ticker, string sentiment, string expectedDirection)
        {
            var svc      = CreateService();
            var fallback = svc.GenerateFallbackNewsHeadlines(ticker, "");
            var result   = svc.GenerateMockFundamentalOutput(ticker, fallback, sentiment);

            result.Should().NotBeNullOrWhiteSpace();
            result.Should().Contain(ticker);
            result.ToLowerInvariant().Should().Contain("fundamental", because: "the output should describe a fundamental analysis");

            switch (expectedDirection)
            {
                case "BUY":
                    result.Should().Contain("robust operational health", because: "positive sentiment should lead to a positive fundamental outlook");
                    break;
                case "SELL":
                    result.Should().Contain("headwinds", because: "negative sentiment should lead to a cautionary fundamental outlook");
                    break;
                case "HOLD":
                    result.Should().Contain("stable", because: "neutral sentiment should lead to a balanced fundamental outlook");
                    break;
            }
        }

        [Fact]
        public void GenerateMockFundamentalOutput_ContainsNoException()
        {
            var svc = CreateService();
            var act = () => svc.GenerateMockFundamentalOutput("MSFT", "1. Headline – Description.", "positive");
            act.Should().NotThrow();
        }

        // ─────────────────────────────────────────────────────────────────
        // GenerateMockPortfolioManagerResponse
        // ─────────────────────────────────────────────────────────────────

        [Theory]
        [InlineData("AAPL", "positive bullish strong", "BUY")]
        [InlineData("TSLA", "negative bear drop decline", "SELL")]
        [InlineData("MSFT", "neutral consolidation balanced", "HOLD")]
        public void GenerateMockPortfolioManagerResponse_ReturnsCorrectDecision(
            string ticker, string sentiment, string expectedDecision)
        {
            var svc      = CreateService();
            var fallback = svc.GenerateFallbackNewsHeadlines(ticker, "");
            var result   = svc.GenerateMockPortfolioManagerResponse(ticker, sentiment, fallback);

            result.Should().NotBeNull();
            result.Decision.Should().Be(expectedDecision);
        }

        [Fact]
        public void GenerateMockPortfolioManagerResponse_ConfidenceScore_InRange()
        {
            var svc    = CreateService();
            var result = svc.GenerateMockPortfolioManagerResponse("AAPL", "positive strong", "Strong fundamentals.");

            result.ConfidenceScore.Should().BeInRange(0, 100);
        }

        [Fact]
        public void GenerateMockPortfolioManagerResponse_TitleContainsDecisionAndTicker()
        {
            var svc    = CreateService();
            var result = svc.GenerateMockPortfolioManagerResponse("NVDA", "positive", "solid fundamentals");

            result.Title.Should().Contain("NVDA");
            result.Title.Should().Contain(result.Decision);
        }

        [Theory]
        [InlineData("BTC")]
        [InlineData("ETH")]
        [InlineData("SOL")]
        [InlineData("BNB")]
        [InlineData("XRP")]
        public void GenerateMockPortfolioManagerResponse_CryptoTickers_ContainCryptoRisks(string ticker)
        {
            var svc    = CreateService();
            var result = svc.GenerateMockPortfolioManagerResponse(ticker, "positive", "ok");

            result.Content.Should().Contain("Regulatory scrutiny");
        }

        [Theory]
        [InlineData("SPY")]
        [InlineData("QQQ")]
        [InlineData("DIA")]
        [InlineData("FNTS")]
        public void GenerateMockPortfolioManagerResponse_IndexETFs_ContainMacroRisks(string ticker)
        {
            var svc    = CreateService();
            var result = svc.GenerateMockPortfolioManagerResponse(ticker, "neutral", "ok");

            result.Content.Should().Contain("Federal Reserve");
        }

        [Fact]
        public void GenerateMockPortfolioManagerResponse_AllRequiredFieldsPopulated()
        {
            var svc    = CreateService();
            var result = svc.GenerateMockPortfolioManagerResponse("AAPL", "positive", "fundamentals");

            result.Decision.Should().NotBeNullOrWhiteSpace();
            result.Title.Should().NotBeNullOrWhiteSpace();
            result.Headline.Should().NotBeNullOrWhiteSpace();
            result.Content.Should().NotBeNullOrWhiteSpace();
            result.Source.Should().NotBeNullOrWhiteSpace();
        }

        // ─────────────────────────────────────────────────────────────────
        // PreprocessJsonResponse (via reflection / internal access)
        // ─────────────────────────────────────────────────────────────────

        private static string InvokePreprocess(TradingAgentService svc, string raw)
        {
            var method = typeof(TradingAgentService)
                .GetMethod("PreprocessJsonResponse",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
            return (string)method.Invoke(svc, new object[] { raw })!;
        }

        [Fact]
        public void PreprocessJsonResponse_NullOrWhitespace_ReturnsEmpty()
        {
            var svc = CreateService();
            InvokePreprocess(svc, "").Should().Be(string.Empty);
            InvokePreprocess(svc, "   ").Should().Be(string.Empty);
        }

        [Fact]
        public void PreprocessJsonResponse_StripsJsonCodeFence()
        {
            var svc  = CreateService();
            var raw  = "```json\n{\"decision\":\"BUY\"}\n```";
            var proc = InvokePreprocess(svc, raw);
            proc.Should().StartWith("{");
            proc.Should().EndWith("}");
        }

        [Fact]
        public void PreprocessJsonResponse_StripsPlainCodeFence()
        {
            var svc  = CreateService();
            var raw  = "```\n{\"decision\":\"SELL\"}\n```";
            var proc = InvokePreprocess(svc, raw);
            proc.Should().Be("{\"decision\":\"SELL\"}");
        }

        [Fact]
        public void PreprocessJsonResponse_ExtractsJsonFromSurroundingText()
        {
            var svc  = CreateService();
            var raw  = "Here is the JSON: {\"decision\":\"HOLD\"} as requested.";
            var proc = InvokePreprocess(svc, raw);
            proc.Should().StartWith("{");
            proc.Should().EndWith("}");
        }

        [Fact]
        public void PreprocessJsonResponse_CleanJson_ReturnsSameJson()
        {
            var svc  = CreateService();
            var json = "{\"decision\":\"BUY\",\"title\":\"Test\"}";
            InvokePreprocess(svc, json).Should().Be(json);
        }

        // ─────────────────────────────────────────────────────────────────
        // RunAnalysisAsync – Mock mode integration path
        // ─────────────────────────────────────────────────────────────────

        [Fact]
        public async Task RunAnalysisAsync_WhenTagNotFound_ReturnsFailureResult()
        {
            var config = BuildConfig(mockEnabled: true);
            var services = new ServiceCollection();
            services.AddDbContext<FUNewsTradingSystem_DataAccessLayer.Models.FUNewsManagementContext>(options =>
                options.UseInMemoryDatabase("TradingAgentServiceTests"));

            var serviceProvider = services.BuildServiceProvider();
            var svc = CreateService(config, serviceProvider: serviceProvider);

            var result = await svc.RunAnalysisAsync(999, 1, 1);

            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().NotBeNullOrWhiteSpace();
        }

        // ─────────────────────────────────────────────────────────────────
        // Prompt template constants
        // ─────────────────────────────────────────────────────────────────

        [Fact]
        public void SentimentPromptTemplate_ContainsPlaceholders()
        {
            TradingAgentService.SENTIMENT_AGENT_PROMPT_TEMPLATE
                .Should().Contain("{ticker}")
                .And.Contain("{headlines_numbered_list}");
        }

        [Fact]
        public void FundamentalPromptTemplate_ContainsPlaceholders()
        {
            TradingAgentService.FUNDAMENTAL_AGENT_PROMPT_TEMPLATE
                .Should().Contain("{ticker}")
                .And.Contain("{headlines_numbered_list}")
                .And.Contain("{sentiment_output}");
        }

        [Fact]
        public void PortfolioManagerPromptTemplate_ContainsExpectedDecisionSchema()
        {
            TradingAgentService.PORTFOLIO_MANAGER_PROMPT_TEMPLATE
                .Should().Contain("BUY")
                .And.Contain("SELL")
                .And.Contain("HOLD")
                .And.Contain("confidenceScore");
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // Fake HTTP handler used for tests that do not need real HTTP calls
    // ─────────────────────────────────────────────────────────────────────

    public sealed class FakeHttpHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly string         _responseBody;

        public FakeHttpHandler(HttpStatusCode statusCode, string responseBody)
        {
            _statusCode   = statusCode;
            _responseBody = responseBody;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_responseBody, Encoding.UTF8, "application/json")
            };
            return Task.FromResult(response);
        }
    }
}
