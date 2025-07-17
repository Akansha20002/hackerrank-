namespace HackerRank.Models;

public class TestModel
{
        public int Id { get; set; }
        public string TestName { get; set; }
        public DateTime TestDate { get; set; }
        public int TimeAllowed { get; set; } // in minutes

        public int AdminId { get; set; }
        public Admin Admin { get; set; }

        public ICollection<TestQuestionModel> TestQuestions { get; set; }
}
