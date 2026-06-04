using FUNewsTradingSystem_BusinessLayer.Repositories.Interfaces;
using FUNewsTradingSystem_DataAccessLayer.Models;
using Microsoft.EntityFrameworkCore;

namespace FUNewsTradingSystem_BusinessLayer.Repositories.Implements
{
    public class TagRepository : ITagRepository
    {
        private readonly FUNewsManagementContext _context;

        public TagRepository(FUNewsManagementContext context)
        {
            _context = context;
        }

        public async Task<List<Tag>> GetAllAsync()
        {
            return await _context.Tags.ToListAsync();
        }

        public async Task<List<Tag>> GetAllForDropdownAsync()
        {
            return await _context.Tags.OrderBy(t => t.TagName).ToListAsync();
        }

        public async Task<Tag?> GetByIdAsync(int id)
        {
            return await _context.Tags.FindAsync(id);
        }

        public async Task<bool> TagNameExistsAsync(string name, int? excludeId = null)
        {
            if (excludeId.HasValue)
            {
                return await _context.Tags.AnyAsync(t => t.TagName == name && t.TagID != excludeId.Value);
            }
            return await _context.Tags.AnyAsync(t => t.TagName == name);
        }

        public async Task<bool> IsReferencedByAnyArticleAsync(int tagId)
        {
            return await _context.NewsTags.AnyAsync(nt => nt.TagID == tagId);
        }

        public async Task<Tag> CreateAsync(Tag tag)
        {
            tag.TagName = tag.TagName.ToUpperInvariant();
            _context.Tags.Add(tag);
            await _context.SaveChangesAsync();
            return tag;
        }

        public async Task UpdateAsync(Tag tag)
        {
            tag.TagName = tag.TagName.ToUpperInvariant();
            _context.Tags.Update(tag);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var tag = await _context.Tags.FindAsync(id);
            if (tag != null)
            {
                _context.Tags.Remove(tag);
                await _context.SaveChangesAsync();
            }
        }
    }
}
