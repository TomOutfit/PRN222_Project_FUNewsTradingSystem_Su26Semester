using System;
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
    public class TagServiceTests
    {
        // ─────────────────────────────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────────────────────────────

        private static (TagService svc, Mock<ITagRepository> repoMock) Build()
        {
            var mock = new Mock<ITagRepository>();
            var svc  = new TagService(mock.Object);
            return (svc, mock);
        }

        // ─────────────────────────────────────────────────────────────────
        // GetAllTagsAsync
        // ─────────────────────────────────────────────────────────────────

        [Fact]
        public async Task GetAllTagsAsync_ReturnsAllTagsFromRepository()
        {
            var (svc, repo) = Build();
            var tags        = new List<Tag>
            {
                new() { TagID = 1, TagName = "AAPL" },
                new() { TagID = 2, TagName = "MSFT" }
            };
            repo.Setup(r => r.GetAllAsync()).ReturnsAsync(tags);

            var result = await svc.GetAllTagsAsync();

            result.Should().HaveCount(2);
            result.Should().ContainSingle(t => t.TagName == "AAPL");
        }

        [Fact]
        public async Task GetAllTagsAsync_EmptyList_ReturnsEmpty()
        {
            var (svc, repo) = Build();
            repo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Tag>());

            var result = await svc.GetAllTagsAsync();

            result.Should().BeEmpty();
        }

        // ─────────────────────────────────────────────────────────────────
        // GetTagByIdAsync
        // ─────────────────────────────────────────────────────────────────

        [Fact]
        public async Task GetTagByIdAsync_ExistingId_ReturnsTag()
        {
            var (svc, repo) = Build();
            repo.Setup(r => r.GetByIdAsync(3))
                .ReturnsAsync(new Tag { TagID = 3, TagName = "BTC" });

            var result = await svc.GetTagByIdAsync(3);

            result.Should().NotBeNull();
            result!.TagName.Should().Be("BTC");
        }

        [Fact]
        public async Task GetTagByIdAsync_NonExistingId_ReturnsNull()
        {
            var (svc, repo) = Build();
            repo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Tag?)null);

            var result = await svc.GetTagByIdAsync(999);

            result.Should().BeNull();
        }

        // ─────────────────────────────────────────────────────────────────
        // CreateTagAsync
        // ─────────────────────────────────────────────────────────────────

        [Fact]
        public async Task CreateTagAsync_UniqueTagName_CreatesSuccessfully()
        {
            var (svc, repo) = Build();
            var tag         = new Tag { TagName = "NVDA" };
            repo.Setup(r => r.ExistsByNameAsync("NVDA")).ReturnsAsync(false);
            repo.Setup(r => r.AddAsync(tag)).Returns(Task.CompletedTask);

            var act = async () => await svc.CreateTagAsync(tag);

            await act.Should().NotThrowAsync();
            repo.Verify(r => r.AddAsync(tag), Times.Once);
        }

        [Fact]
        public async Task CreateTagAsync_DuplicateTagName_ThrowsInvalidOperationException()
        {
            var (svc, repo) = Build();
            var tag         = new Tag { TagName = "AAPL" };
            repo.Setup(r => r.ExistsByNameAsync("AAPL")).ReturnsAsync(true);

            var act = async () => await svc.CreateTagAsync(tag);

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*already exists*");
        }

        // ─────────────────────────────────────────────────────────────────
        // UpdateTagAsync
        // ─────────────────────────────────────────────────────────────────

        [Fact]
        public async Task UpdateTagAsync_UniqueNameAmongOtherTags_UpdatesSuccessfully()
        {
            var (svc, repo) = Build();
            var updatedTag  = new Tag { TagID = 2, TagName = "TSLA" };
            var allTags     = new List<Tag>
            {
                new() { TagID = 1, TagName = "AAPL" },
                new() { TagID = 2, TagName = "MSFT" }  // will be updated → TSLA
            };
            repo.Setup(r => r.GetAllAsync()).ReturnsAsync(allTags);
            repo.Setup(r => r.UpdateAsync(updatedTag)).Returns(Task.CompletedTask);

            var act = async () => await svc.UpdateTagAsync(updatedTag);

            await act.Should().NotThrowAsync();
            repo.Verify(r => r.UpdateAsync(updatedTag), Times.Once);
        }

        [Fact]
        public async Task UpdateTagAsync_DuplicateNameWithDifferentId_ThrowsInvalidOperationException()
        {
            var (svc, repo) = Build();
            var updatedTag  = new Tag { TagID = 2, TagName = "aapl" }; // same as tag 1 (case-insensitive)
            var allTags     = new List<Tag>
            {
                new() { TagID = 1, TagName = "AAPL" },
                new() { TagID = 2, TagName = "MSFT" }
            };
            repo.Setup(r => r.GetAllAsync()).ReturnsAsync(allTags);

            var act = async () => await svc.UpdateTagAsync(updatedTag);

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*already exists*");
        }

        [Fact]
        public async Task UpdateTagAsync_SameTagSameId_NoDuplicateConflict()
        {
            var (svc, repo) = Build();
            var updatedTag  = new Tag { TagID = 1, TagName = "AAPL" }; // no change, same ID
            var allTags     = new List<Tag>
            {
                new() { TagID = 1, TagName = "AAPL" },
                new() { TagID = 2, TagName = "MSFT" }
            };
            repo.Setup(r => r.GetAllAsync()).ReturnsAsync(allTags);
            repo.Setup(r => r.UpdateAsync(updatedTag)).Returns(Task.CompletedTask);

            var act = async () => await svc.UpdateTagAsync(updatedTag);

            await act.Should().NotThrowAsync();
        }

        // ─────────────────────────────────────────────────────────────────
        // DeleteTagAsync
        // ─────────────────────────────────────────────────────────────────

        [Fact]
        public async Task DeleteTagAsync_DelegatesToRepository()
        {
            var (svc, repo) = Build();
            repo.Setup(r => r.DeleteAsync(5)).Returns(Task.CompletedTask);

            await svc.DeleteTagAsync(5);

            repo.Verify(r => r.DeleteAsync(5), Times.Once);
        }
    }
}
