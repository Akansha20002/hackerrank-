using HackerRank.Models;
using HackerRank.Repo.Contract;
using Microsoft.EntityFrameworkCore;
namespace HackerRank.Repo
{
    public class Question : Iquestion
    {
        private readonly AppDbContext _context;
        public Question(AppDbContext context)
        {
            _context = context;
        }
        public QuestionModel GetQuestionById(int questionId)
        {
            return _context.Questions
                .Include(q => q.Section)
                .FirstOrDefault(q => q.Id == questionId);
        }
        public IEnumerable<QuestionModel> GetAllQuestionsByTestId(int testId)
        {
            return _context.TestQuestions
                .Where(tq => tq.TestId == testId)
                .Select(tq => tq.Question)
                .Include(q => q.Section)
                .ToList();
        }
        public IEnumerable<QuestionModel> GetAllQuestionsBySectionId(int sectionId)
        {
            return _context.Questions
                .Where(q => q.SectionId == sectionId)
                .ToList();
        }
        public void DeleteQuestionById(int questionId)
        {
            var question = _context.Questions.Find(questionId);
            if (question != null)
            {
                _context.Questions.Remove(question);
                _context.SaveChanges();
            }
        }
    }
}
