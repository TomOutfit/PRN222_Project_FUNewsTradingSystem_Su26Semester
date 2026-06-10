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
            return await _context.Tags
                .OrderBy(t => t.TagID)
                .ToListAsync();
        }

        public async Task<Tag?> GetByIdAsync(int id)
        {
            return await _context.Tags
                .FirstOrDefaultAsync(t => t.TagID == id);
        }

        public async Task AddAsync(Tag tag)
        {
            await _context.Tags.AddAsync(tag);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Tag tag)
        {
            var existing = await _context.Tags
                .FirstOrDefaultAsync(t => t.TagID == tag.TagID);

            if (existing == null)
                return;

            existing.TagName = tag.TagName;
            existing.Note = tag.Note;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var tag = await _context.Tags.FindAsync(id);

            if (tag == null)
                return;

            _context.Tags.Remove(tag);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> ExistsByNameAsync(string tagName)
        {
            return await _context.Tags
                .AnyAsync(t => t.TagName == tagName);
        }
    }
}