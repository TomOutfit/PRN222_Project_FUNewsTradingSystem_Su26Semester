using FUNewsTradingSystem_BusinessLayer.Repositories.Implements;
using FUNewsTradingSystem_BusinessLayer.Repositories.Interfaces;
using FUNewsTradingSystem_BusinessLayer.Services.Interfaces;
using FUNewsTradingSystem_DataAccessLayer.Models;

namespace FUNewsTradingSystem_BusinessLayer.Services.Implements
{
    public class TagService : ITagService
    {
        private readonly ITagRepository _repo;

        public TagService(ITagRepository repo)
        {
            _repo = repo;
        }

        public async Task<List<Tag>> GetAllTagsAsync()
            => await _repo.GetAllAsync();

        public async Task<Tag?> GetTagByIdAsync(int id)
            => await _repo.GetByIdAsync(id);

        public async Task CreateTagAsync(Tag tag)
        {
            if (await _repo.ExistsByNameAsync(tag.TagName))
            {
                throw new InvalidOperationException(
                    "Tag name already exists.");
            }

            await _repo.AddAsync(tag);
        }

        public async Task UpdateTagAsync(Tag tag)
        {
            var allTags = await _repo.GetAllAsync();

            var duplicate = allTags.Any(t =>
                t.TagID != tag.TagID &&
                t.TagName.ToLower() == tag.TagName.ToLower());

            if (duplicate)
            {
                throw new InvalidOperationException(
                    "Tag name already exists.");
            }

            await _repo.UpdateAsync(tag);
        }

        public async Task DeleteTagAsync(int id)
            => await _repo.DeleteAsync(id);
    }
}
