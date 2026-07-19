using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Xunit;
using FUNewsTradingSystem_BusinessLayer.Repositories.Interfaces;
using FUNewsTradingSystem_BusinessLayer.Services.Implements;
using FUNewsTradingSystem_DataAccessLayer.Models;

namespace FUNewsTradingSystem.Tests.Services
{
    public class CategoryServiceTests
    {
        // ─────────────────────────────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────────────────────────────

        private static (CategoryService svc, Mock<ICategoryRepository> repoMock) Build()
        {
            var mock = new Mock<ICategoryRepository>();
            var svc  = new CategoryService(mock.Object);
            return (svc, mock);
        }

        // ─────────────────────────────────────────────────────────────────
        // GetAllCategoriesAsync
        // ─────────────────────────────────────────────────────────────────

        [Fact]
        public async Task GetAllCategoriesAsync_ReturnsAllFromRepository()
        {
            var (svc, repo) = Build();
            var expected    = new List<Category>
            {
                new() { CategoryID = 1, CategoryName = "Technology" },
                new() { CategoryID = 2, CategoryName = "Finance" }
            };
            repo.Setup(r => r.GetAllAsync()).ReturnsAsync(expected);

            var result = await svc.GetAllCategoriesAsync();

            result.Should().BeEquivalentTo(expected);
        }

        [Fact]
        public async Task GetAllCategoriesAsync_EmptyList_ReturnsEmpty()
        {
            var (svc, repo) = Build();
            repo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Category>());

            var result = await svc.GetAllCategoriesAsync();

            result.Should().BeEmpty();
        }

        // ─────────────────────────────────────────────────────────────────
        // GetActiveAsync
        // ─────────────────────────────────────────────────────────────────

        [Fact]
        public async Task GetActiveAsync_DelegatesToRepository()
        {
            var (svc, repo) = Build();
            var active      = new List<Category> { new() { CategoryID = 1, IsActive = true } };
            repo.Setup(r => r.GetActiveAsync()).ReturnsAsync(active);

            var result = await svc.GetActiveAsync();

            result.Should().BeEquivalentTo(active);
            repo.Verify(r => r.GetActiveAsync(), Times.Once);
        }

        // ─────────────────────────────────────────────────────────────────
        // GetTopLevelAsync
        // ─────────────────────────────────────────────────────────────────

        [Fact]
        public async Task GetTopLevelAsync_DelegatesToRepository()
        {
            var (svc, repo) = Build();
            var topLevel    = new List<Category> { new() { CategoryID = 1, ParentCategoryID = null } };
            repo.Setup(r => r.GetTopLevelAsync()).ReturnsAsync(topLevel);

            var result = await svc.GetTopLevelAsync();

            result.Should().BeEquivalentTo(topLevel);
        }

        // ─────────────────────────────────────────────────────────────────
        // GetCategoryByIdAsync
        // ─────────────────────────────────────────────────────────────────

        [Fact]
        public async Task GetCategoryByIdAsync_ExistingId_ReturnsCategory()
        {
            var (svc, repo) = Build();
            var category    = new Category { CategoryID = 5, CategoryName = "Crypto" };
            repo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(category);

            var result = await svc.GetCategoryByIdAsync(5);

            result.Should().NotBeNull();
            result!.CategoryID.Should().Be(5);
        }

        [Fact]
        public async Task GetCategoryByIdAsync_NonExistingId_ReturnsNull()
        {
            var (svc, repo) = Build();
            repo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Category?)null);

            var result = await svc.GetCategoryByIdAsync(999);

            result.Should().BeNull();
        }

        // ─────────────────────────────────────────────────────────────────
        // CreateCategoryAsync
        // ─────────────────────────────────────────────────────────────────

        [Fact]
        public async Task CreateCategoryAsync_Success_ReturnsTrue()
        {
            var (svc, repo) = Build();
            var cat         = new Category { CategoryName = "New Category" };
            repo.Setup(r => r.CreateAsync(cat)).ReturnsAsync(new Category { CategoryID = 10 });

            var result = await svc.CreateCategoryAsync(cat);

            result.Should().BeTrue();
        }

        [Fact]
        public async Task CreateCategoryAsync_WhenRepositoryReturnsNull_ReturnsFalse()
        {
            var (svc, repo) = Build();
            var cat         = new Category { CategoryName = "Bad Category" };
            repo.Setup(r => r.CreateAsync(cat)).ReturnsAsync((Category)null!);

            var result = await svc.CreateCategoryAsync(cat);

            result.Should().BeFalse();
        }

        [Fact]
        public async Task CreateCategoryAsync_WhenRepositoryReturnsIdZero_ReturnsFalse()
        {
            var (svc, repo) = Build();
            var cat         = new Category { CategoryName = "Zero ID" };
            repo.Setup(r => r.CreateAsync(cat)).ReturnsAsync(new Category { CategoryID = 0 });

            var result = await svc.CreateCategoryAsync(cat);

            result.Should().BeFalse();
        }

        // ─────────────────────────────────────────────────────────────────
        // UpdateCategoryAsync
        // ─────────────────────────────────────────────────────────────────

        [Fact]
        public async Task UpdateCategoryAsync_ExistingCategory_CallsUpdateAndReturnsTrue()
        {
            var (svc, repo) = Build();
            var cat         = new Category { CategoryID = 3, CategoryName = "Updated" };
            repo.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(new Category { CategoryID = 3 });
            repo.Setup(r => r.UpdateAsync(cat)).Returns(Task.CompletedTask);

            var result = await svc.UpdateCategoryAsync(cat);

            result.Should().BeTrue();
            repo.Verify(r => r.UpdateAsync(cat), Times.Once);
        }

        [Fact]
        public async Task UpdateCategoryAsync_NonExistingCategory_ReturnsFalse()
        {
            var (svc, repo) = Build();
            var cat         = new Category { CategoryID = 999, CategoryName = "Ghost" };
            repo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Category?)null);

            var result = await svc.UpdateCategoryAsync(cat);

            result.Should().BeFalse();
            repo.Verify(r => r.UpdateAsync(It.IsAny<Category>()), Times.Never);
        }

        // ─────────────────────────────────────────────────────────────────
        // ToggleActiveAsync
        // ─────────────────────────────────────────────────────────────────

        [Fact]
        public async Task ToggleActiveAsync_ActiveCategory_DeactivatesAndReturnsFalse()
        {
            var (svc, repo) = Build();
            var cat         = new Category { CategoryID = 1, IsActive = true };
            repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(cat);
            repo.Setup(r => r.UpdateAsync(It.IsAny<Category>())).Returns(Task.CompletedTask);

            var result = await svc.ToggleActiveAsync(1);

            result.Should().BeFalse(because: "toggling an active category should deactivate it");
        }

        [Fact]
        public async Task ToggleActiveAsync_InactiveCategory_ActivatesAndReturnsTrue()
        {
            var (svc, repo) = Build();
            var cat         = new Category { CategoryID = 2, IsActive = false };
            repo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(cat);
            repo.Setup(r => r.UpdateAsync(It.IsAny<Category>())).Returns(Task.CompletedTask);

            var result = await svc.ToggleActiveAsync(2);

            result.Should().BeTrue(because: "toggling an inactive category should activate it");
        }

        [Fact]
        public async Task ToggleActiveAsync_NonExistingId_ReturnsFalse()
        {
            var (svc, repo) = Build();
            repo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Category?)null);

            var result = await svc.ToggleActiveAsync(999);

            result.Should().BeFalse();
        }

        // ─────────────────────────────────────────────────────────────────
        // DeleteCategoryAsync
        // ─────────────────────────────────────────────────────────────────

        [Fact]
        public async Task DeleteCategoryAsync_NotReferencedAndNoChildren_DeletesAndReturnsTrue()
        {
            var (svc, repo) = Build();
            repo.Setup(r => r.IsReferencedByAnyArticleAsync(1)).ReturnsAsync(false);
            repo.Setup(r => r.HasChildCategoriesReferencedByArticlesAsync(1)).ReturnsAsync(false);
            repo.Setup(r => r.DeleteWithReparentChildrenAsync(1)).Returns(Task.CompletedTask);

            var result = await svc.DeleteCategoryAsync(1);

            result.Should().BeTrue();
            repo.Verify(r => r.DeleteWithReparentChildrenAsync(1), Times.Once);
        }

        [Fact]
        public async Task DeleteCategoryAsync_ReferencedByArticle_ReturnsFalseWithoutDeleting()
        {
            var (svc, repo) = Build();
            repo.Setup(r => r.IsReferencedByAnyArticleAsync(2)).ReturnsAsync(true);

            var result = await svc.DeleteCategoryAsync(2);

            result.Should().BeFalse();
            repo.Verify(r => r.DeleteWithReparentChildrenAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task DeleteCategoryAsync_ChildrenReferencedByArticle_ReturnsFalseWithoutDeleting()
        {
            var (svc, repo) = Build();
            repo.Setup(r => r.IsReferencedByAnyArticleAsync(3)).ReturnsAsync(false);
            repo.Setup(r => r.HasChildCategoriesReferencedByArticlesAsync(3)).ReturnsAsync(true);

            var result = await svc.DeleteCategoryAsync(3);

            result.Should().BeFalse();
            repo.Verify(r => r.DeleteWithReparentChildrenAsync(It.IsAny<int>()), Times.Never);
        }
    }
}
