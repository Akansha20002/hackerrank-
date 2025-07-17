using System.ComponentModel.DataAnnotations;

namespace HackerRank.Models;

public class QuestionModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Section is required")]
    public int SectionId { get; set; }
    public SectionModel? Section { get; set; } 
    [Required(ErrorMessage = "Question text is required")]
    public string Description { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public bool HasImage { get; set; }

    [Required(ErrorMessage = "Answer type is required")]
    public string AnswerControlType { get; set; } = "Radio";

    [Required(ErrorMessage = "Option 1 text is required")]
    public string Option1Text { get; set; } = string.Empty;
    public string? Option1Image { get; set; }
    public bool IsOption1Image { get; set; }

    [Required(ErrorMessage = "Option 2 text is required")]
    public string Option2Text { get; set; } = string.Empty;
    public string? Option2Image { get; set; }
    public bool IsOption2Image { get; set; }

    [Required(ErrorMessage = "Option 3 text is required")]
    public string Option3Text { get; set; } = string.Empty;
    public string? Option3Image { get; set; }
    public bool IsOption3Image { get; set; }

    [Required(ErrorMessage = "Option 4 text is required")]
    public string Option4Text { get; set; } = string.Empty;
    public string? Option4Image { get; set; }
    public bool IsOption4Image { get; set; }

    [Required(ErrorMessage = "Option 5 text is required")]
    public string Option5Text { get; set; } = string.Empty;
    public string? Option5Image { get; set; }
    public bool IsOption5Image { get; set; }

    [Required(ErrorMessage = "Correct option is required")]
    [Range(1, 5, ErrorMessage = "Correct option must be between 1 and 5")]
    public int CorrectOption { get; set; }

    public ICollection<TestQuestionModel> TestQuestions { get; set; } = new List<TestQuestionModel>();
}
