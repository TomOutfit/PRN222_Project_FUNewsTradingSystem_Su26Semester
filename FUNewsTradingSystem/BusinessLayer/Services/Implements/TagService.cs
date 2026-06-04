using FUNewsTradingSystem_BusinessLayer.Repositories.Interfaces;
using FUNewsTradingSystem_DataAccessLayer.Models;

namespace FUNewsTradingSystem_BusinessLayer.Services.Implements
{
    public class TagService : Interfaces.ITagService
    {
        private readonly ITagRepository _repository;

        public TagService(ITagRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<Tag>> GetAllAsync() => await _repository.GetAllAsync();
        public async Task<Tag?> GetByIdAsync(int id) => await _repository.GetByIdAsync(id);

        public async Task<ServiceResult> CreateAsync(Tag tag)
        {
            if (await _repository.TagNameExistsAsync(tag.TagName))
            {
                return new ServiceResult { Success = false, ErrorMessage = "Tag name already exists." };
            }

            var created = await _repository.CreateAsync(tag);
            return new ServiceResult { Success = true, EntityId = created.TagID };
        }

        public async Task<ServiceResult> UpdateAsync(Tag tag)
        {
            if (await _repository.TagNameExistsAsync(tag.TagName, tag.TagID))
            {
                return new ServiceResult { Success = false, ErrorMessage = "Tag name already exists." };
            }

            await _repository.UpdateAsync(tag);
            return new ServiceResult { Success = true, EntityId = tag.TagID };
        }

        public async Task<ServiceResult> DeleteAsync(int id)
        {
            if (await _repository.IsReferencedByAnyArticleAsync(id))
            {
                return new ServiceResult { Success = false, ErrorMessage = "Cannot delete tag: it is referenced by one or more articles." };
            }

            await _repository.DeleteAsync(id);
            return new ServiceResult { Success = true };
        }
    }
}
