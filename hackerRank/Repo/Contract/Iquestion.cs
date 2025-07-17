
using HackerRank.Models;
namespace HackerRank.Repo.Contract
{
    public interface Iquestion
    {
        QuestionModel GetQuestionById(int questionId);
        IEnumerable<QuestionModel> GetAllQuestionsByTestId(int testId);
        IEnumerable<QuestionModel> GetAllQuestionsBySectionId(int sectionId);
        void DeleteQuestionById(int questionId);
    }
}
