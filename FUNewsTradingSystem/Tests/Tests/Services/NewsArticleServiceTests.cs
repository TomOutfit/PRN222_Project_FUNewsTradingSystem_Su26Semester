using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Xunit;
using FUNewsTradingSystem_BusinessLayer.Repositories.Interfaces;
using FUNewsTradingSystem_BusinessLayer.Services;
using FUNewsTradingSystem_BusinessLayer.Services.Implements;
using FUNewsTradingSystem_DataAccessLayer.Models;

namespace FUNewsTradingSystem.Tests.Services
{
    public class NewsArticleServiceTests
    {
        // ─────────────────────────────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────────────────────────────

        private static (NewsArticleService svc,
                        Mock<INewsArticleRepository> articleRepo,
                        Mock<ICategoryRepository> catRepo,
                        Mock<ITagRepository> tagRepo) Build()
        {
            var articleRepoMock = new Mock<INewsArticleRepository>();
            var catRepoMock     = new Mock<ICategoryRepository>();
            var tagRepoMock     = new Mock<ITagRepository>();

            var svc = new NewsArticleService(
                articleRepoMock.Object,
                catRepoMock.Object,
                tagRepoMock.Object);

            return (svc, articleRepoMock, catRepoMock, tagRepoMock);
        }

        private static NewsArticle ValidArticle() => new()
        {
            NewsTitle   = "AAPL Q3 Earnings Beat Expectations",
            NewsContent = "Apple reports record quarterly profits...",
            CategoryID  = 1
        };

        // ─────────────────────────────────────────────────────────────────
        // GetActiveAsync
        // ─────────────────────────────────────────────────────────────────

        [Fact]
        public async Task GetActiveAsync_ReturnsAllActiveArticles()
        {
            var (svc, ar, _, _) = Build();
            var articles        = new List<NewsArticle>
            {
                new() { NewsArticleID = 1, NewsStatus = true },
                new() { NewsArticleID = 2, NewsStatus = true }
            };
            ar.Setup(r => r.GetActiveAsync()).ReturnsAsync(articles);

            var result = await svc.GetActiveAsync();

            result.Should().HaveCount(2);
        }

        // ─────────────────────────────────────────────────────────────────
        // GetByIdAsync
        // ─────────────────────────────────────────────────────────────────

        [Fact]
        public async Task GetByIdAsync_ExistingId_ReturnsArticle()
        {
            var (svc, ar, _, _) = Build();
            ar.Setup(r => r.GetByIdAsync(7))
              .ReturnsAsync(new NewsArticle { NewsArticleID = 7 });

            var result = await svc.GetByIdAsync(7);

            result.Should().NotBeNull();
            result!.NewsArticleID.Should().Be(7);
        }

        [Fact]
        public async Task GetByIdAsync_NonExistingId_ReturnsNull()
        {
            var (svc, ar, _, _) = Build();
            ar.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((NewsArticle?)null);

            var result = await svc.GetByIdAsync(9999);

            result.Should().BeNull();
        }

        // ─────────────────────────────────────────────────────────────────
        // GetByCreatorAsync
        // ─────────────────────────────────────────────────────────────────

        [Fact]
        public async Task GetByCreatorAsync_ValidAccountId_ReturnsList()
        {
            var (svc, ar, _, _) = Build();
            ar.Setup(r => r.GetByCreatorAsync(3))
              .ReturnsAsync(new List<NewsArticle> { new() { CreatedByID = 3 } });

            var result = await svc.GetByCreatorAsync(3);

            result.Should().ContainSingle();
            result[0].CreatedByID.Should().Be(3);
        }

        // ─────────────────────────────────────────────────────────────────
        // GetByDateRangeAsync
        // ─────────────────────────────────────────────────────────────────

        [Fact]
        public async Task GetByDateRangeAsync_ValidRange_DelegatesToRepository()
        {
            var (svc, ar, _, _) = Build();
            var start           = DateTime.UtcNow.AddDays(-7);
            var end             = DateTime.UtcNow;

            ar.Setup(r => r.GetByDateRangeAsync(start, end))
              .ReturnsAsync(new List<NewsArticle>());

            var result = await svc.GetByDateRangeAsync(start, end);

            ar.Verify(r => r.GetByDateRangeAsync(start, end), Times.Once);
            result.Should().BeEmpty();
        }

        // ─────────────────────────────────────────────────────────────────
        // CreateWithTagAsync
        // ─────────────────────────────────────────────────────────────────

        [Fact]
        public async Task CreateWithTagAsync_MissingTitle_ReturnsValidationError()
        {
            var (svc, _, _, _) = Build();
            var article         = new NewsArticle { NewsTitle = "", NewsContent = "Content", CategoryID = 1 };

            var result = await svc.CreateWithTagAsync(article, 1);

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Contain("required");
        }

        [Fact]
        public async Task CreateWithTagAsync_MissingContent_ReturnsValidationError()
        {
            var (svc, _, _, _) = Build();
            var article         = new NewsArticle { NewsTitle = "Title", NewsContent = "", CategoryID = 1 };

            var result = await svc.CreateWithTagAsync(article, 1);

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Contain("required");
        }

        [Fact]
        public async Task CreateWithTagAsync_InvalidCategoryId_ReturnsError()
        {
            var (svc, _, catRepo, _) = Build();
            var article              = new NewsArticle { NewsTitle = "Title", NewsContent = "Content", CategoryID = 99 };
            catRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Category?)null);

            var result = await svc.CreateWithTagAsync(article, 1);

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Category");
        }

        [Fact]
        public async Task CreateWithTagAsync_ZeroCategoryId_ReturnsError()
        {
            var (svc, _, _, _) = Build();
            var article         = new NewsArticle { NewsTitle = "Title", NewsContent = "Content", CategoryID = 0 };

            var result = await svc.CreateWithTagAsync(article, 1);

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Category is required");
        }

        [Fact]
        public async Task CreateWithTagAsync_InvalidTag_ReturnsError()
        {
            var (svc, _, catRepo, tagRepo) = Build();
            var article                    = ValidArticle();
            catRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Category { CategoryID = 1 });
            tagRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Tag?)null);

            var result = await svc.CreateWithTagAsync(article, 999);

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Tag");
        }

        [Fact]
        public async Task CreateWithTagAsync_ValidInputs_ReturnsSuccessWithEntityId()
        {
            var (svc, ar, catRepo, tagRepo) = Build();
            var article                     = ValidArticle();
            catRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Category { CategoryID = 1 });
            tagRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(new Tag { TagID = 5 });
            ar.Setup(r => r.CreateWithTagAsync(article, 5)).ReturnsAsync(42);

            var result = await svc.CreateWithTagAsync(article, 5);

            result.Success.Should().BeTrue();
            result.EntityId.Should().Be(42);
        }

        // ─────────────────────────────────────────────────────────────────
        // GetActiveReportsAsync
        // ─────────────────────────────────────────────────────────────────

        [Fact]
        public async Task GetActiveReportsAsync_DelegatesToRepository()
        {
            var (svc, ar, _, _) = Build();
            ar.Setup(r => r.GetActiveReportsAsync(null, null, null))
              .ReturnsAsync(new List<NewsArticle>());

            var result = await svc.GetActiveReportsAsync();

            ar.Verify(r => r.GetActiveReportsAsync(null, null, null), Times.Once);
        }

        [Fact]
        public async Task GetActiveReportsAsync_WithFilters_PassesFiltersToRepository()
        {
            var (svc, ar, _, _) = Build();
            ar.Setup(r => r.GetActiveReportsAsync(1, 2, "BUY"))
              .ReturnsAsync(new List<NewsArticle> { new() { NewsArticleID = 1 } });

            var result = await svc.GetActiveReportsAsync(1, 2, "BUY");

            result.Should().ContainSingle();
            ar.Verify(r => r.GetActiveReportsAsync(1, 2, "BUY"), Times.Once);
        }

        // ─────────────────────────────────────────────────────────────────
        // GetReportDetailAsync
        // ─────────────────────────────────────────────────────────────────

        [Fact]
        public async Task GetReportDetailAsync_ExistingId_ReturnsArticle()
        {
            var (svc, ar, _, _) = Build();
            ar.Setup(r => r.GetReportDetailAsync(10))
              .ReturnsAsync(new NewsArticle { NewsArticleID = 10 });

            var result = await svc.GetReportDetailAsync(10);

            result.Should().NotBeNull();
        }

        [Fact]
        public async Task GetReportDetailAsync_NonExisting_ReturnsNull()
        {
            var (svc, ar, _, _) = Build();
            ar.Setup(r => r.GetReportDetailAsync(It.IsAny<int>()))
              .ReturnsAsync((NewsArticle?)null);

            var result = await svc.GetReportDetailAsync(9999);

            result.Should().BeNull();
        }

        // ─────────────────────────────────────────────────────────────────
        // GetReportsByCreatorAsync
        // ─────────────────────────────────────────────────────────────────

        [Fact]
        public async Task GetReportsByCreatorAsync_DelegatesToRepository()
        {
            var (svc, ar, _, _) = Build();
            ar.Setup(r => r.GetReportsByCreatorAsync(5, null, null, null))
              .ReturnsAsync(new List<NewsArticle>());

            await svc.GetReportsByCreatorAsync(5);

            ar.Verify(r => r.GetReportsByCreatorAsync(5, null, null, null), Times.Once);
        }

        // ─────────────────────────────────────────────────────────────────
        // ToggleStatusAsync
        // ─────────────────────────────────────────────────────────────────

        [Fact]
        public async Task ToggleStatusAsync_DelegatesToRepository()
        {
            var (svc, ar, _, _) = Build();
            ar.Setup(r => r.ToggleStatusAsync(3, 7)).ReturnsAsync(false);

            var result = await svc.ToggleStatusAsync(3, 7);

            result.Should().BeFalse();
            ar.Verify(r => r.ToggleStatusAsync(3, 7), Times.Once);
        }

        [Fact]
        public async Task ToggleStatusAsync_WhenRepositoryReturnsTrue_ReturnsTrue()
        {
            var (svc, ar, _, _) = Build();
            ar.Setup(r => r.ToggleStatusAsync(It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(true);

            var result = await svc.ToggleStatusAsync(1, 2);

            result.Should().BeTrue();
        }
    }
}
