using HackerRank.Repo.Contract;
using HackerRank.Models;
namespace HackerRank.Repo
{
    public class Test : ITest
    {
        private readonly AppDbContext _context;
        public Test(AppDbContext context)
        {
            _context = context;
        }
        public void AddTest(TestModel test)
        {
            if (test == null)
                throw new ArgumentNullException(nameof(test));
            _context.Tests.Add(test);
            _context.SaveChanges();
        }
        public void DeleteTestById(int testId)
        {
            var test = _context.Tests.Find(testId);
            if (test != null)
            {
                _context.Tests.Remove(test);
                _context.SaveChanges();
            }
            else
            {
                throw new KeyNotFoundException($"Test with Id {testId} not found.");
            }
        }
        public IEnumerable<TestModel> GetTestsByAdminId(int adminId)
        {
            return _context.Tests.Where(t => t.AdminId == adminId).ToList();
        }
        
    }


}
