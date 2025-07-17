using HackerRank.Models;
using HackerRank.Repo.Contract;
using Microsoft.EntityFrameworkCore;

namespace HackerRank.Repo
{
    public class Section : ISection
    {
        private readonly AppDbContext _context;

        public Section(AppDbContext context)
        {
            _context = context;
        }

        public async Task<SectionModel> CreateSection(string name)
        {
            var section = new SectionModel { Name = name };
            _context.Sections.Add(section);
            await _context.SaveChangesAsync();
            return section;
        }

        public async Task<List<SectionModel>> GetAllSections()
        {
            return await _context.Sections.ToListAsync();
        }

        public async Task<SectionModel?> GetSectionById(int id)
        {
            return await _context.Sections.FindAsync(id);
        }
    }
}
