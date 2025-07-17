using HackerRank.Models;

namespace HackerRank.Repo.Contract
{
    public interface ITest
    {
        public IEnumerable<TestModel> GetTestsByAdminId(int adminId);
        public void DeleteTestById(int testId);
        public void AddTest(TestModel test);

    }
}
