namespace HackerRank.Models;

public class SectionModel
{
  public int Id { get; set; }
        public string Name { get; set; }

        public ICollection<QuestionModel> Questions { get; set; }
}
