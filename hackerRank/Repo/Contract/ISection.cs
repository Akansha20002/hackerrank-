using HackerRank.Models;

namespace HackerRank.Repo.Contract
{
    public interface ISection
    {
        Task<SectionModel> CreateSection(string name);
        Task<List<SectionModel>> GetAllSections();
        Task<SectionModel?> GetSectionById(int id);
    }
}
