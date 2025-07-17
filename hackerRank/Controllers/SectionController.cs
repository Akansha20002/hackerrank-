using Microsoft.AspNetCore.Mvc;
using HackerRank.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using HackerRank.Services;
using HackerRank.Repo;

namespace HackerRank.Controllers;

[Authorize(AuthenticationSchemes = "CustomCookieAuth")]
public class SectionController : Controller
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly FileStorageService _fileStorage;

    public SectionController(AppDbContext context, IConfiguration configuration, FileStorageService fileStorage)
    {
        _context = context;
        _configuration = configuration;
        _fileStorage = fileStorage;
    }
    public async Task<IActionResult> Index()
    {
        //ViewBag.sections = 
        return View(await _context.Sections
            .Include(s => s.Questions)
            .ToListAsync());
    }
    // POST: Section/Create
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SectionModel model)
    {
        if (string.IsNullOrEmpty(model.Name))
        {
            return BadRequest(new { error = "Section name is required" });
        }
        try 
        {
            _context.Sections.Add(model);
            await _context.SaveChangesAsync();
            return Ok(model);
        }
        catch (Exception)
        {
            return StatusCode(500, new { error = "Failed to create section" });
        }
    }

    // GET: Section/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var section = await _context.Sections
            .Include(s => s.Questions)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (section == null)
        {
            return NotFound();
        }
        
        return View(section);
    }

    // GET: Section/
    // /5
    public IActionResult CreateQuestion(int? sectionId)
    {
        if (sectionId == null)
        {
            return NotFound();
        }

        var section = _context.Sections.FirstOrDefault(s => s.Id == sectionId);
        if (section == null)
        {
            return NotFound();
        }

        //ViewBag.SectionId = sectionId;
        var model = new QuestionModel { SectionId = sectionId.Value };
        return View(model);
    }

    // POST: Section/CreateQuestion
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateQuestion(QuestionModel question, IFormFile? questionImage, List<IFormFile>? optionImages)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                foreach (var modelError in ModelState.Values.SelectMany(v => v.Errors))
                {
                    ModelState.AddModelError(string.Empty, modelError.ErrorMessage);
                }
                //ViewBag.SectionId = question.SectionId;
                return View(question);
            }

            // Handle question image upload
            if (questionImage != null && questionImage.Length > 0)
            {
                try
                {
                    var imageUrl = await _fileStorage.UploadFile(questionImage, "images");
                   
                    if (imageUrl != null)
                    {
                        question.HasImage = true;
                        question.ImageUrl = imageUrl;
                    }
                    else
                    {
                        ModelState.AddModelError("", "Failed to upload question image.");
                        //ViewBag.SectionId = question.SectionId;
                        return View(question);
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error uploading question image: {ex.Message}");
                    //ViewBag.SectionId = question.SectionId;
                    return View(question);
                }
            }
           
            // Handle option images upload
            if (optionImages != null && optionImages.Count > 0)
            {
               
                for (int i = 0; i < optionImages.Count; i++)
                {
                    
                    var image = optionImages[i];
                    if (image != null && image.Length > 0)
                    {
                        
                        try
                        {
                            var imageUrl = await _fileStorage.UploadFile(image, "images");
                   
                            if (imageUrl != null)
                            {
                                switch (i)
                                {
                                    case 0:
                                        question.IsOption1Image = true;
                                        question.Option1Image = imageUrl;
                                        break;
                                    case 1:
                                        question.IsOption2Image = true;
                                        question.Option2Image = imageUrl;
                                        break;
                                    case 2:
                                        question.IsOption3Image = true;
                                        question.Option3Image = imageUrl;
                                        break;
                                    case 3:
                                        question.IsOption4Image = true;
                                        question.Option4Image = imageUrl;
                                        break;
                                    case 4:
                                        question.IsOption5Image = true;
                                        question.Option5Image = imageUrl;
                                        break;
                                }
                            }
                            else
                            {
                                ModelState.AddModelError("", $"Failed to upload image for option {i + 1}");
                                //ViewBag.SectionId = question.SectionId;
                                return View(question);
                            }
                        }
                        catch (Exception ex)
                        {
                            ModelState.AddModelError("", $"Error uploading image for option {i + 1}: {ex.Message}");
                            //ViewBag.SectionId = question.SectionId;
                            return View(question);
                        }
                    }
                }
            }

            question.TestQuestions = new List<TestQuestionModel>();
            _context.Questions.Add(question);
            await _context.SaveChangesAsync();
    
            return RedirectToAction(nameof(Details), new { id = question.SectionId });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", "An error occurred while saving the question: " + ex.Message);
            //ViewBag.SectionId = question.SectionId;
            return View(question);
        }
    }

    // GET: Section/EditQuestion/5
    public async Task<IActionResult> EditQuestion(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var question = await _context.Questions
            .Include(q => q.Section)
            .FirstOrDefaultAsync(q => q.Id == id);
        //var sectionId = await _context.Questions
        //.Where(q => q.Id == id)
        //.Select(q => q.SectionId)
        //.FirstOrDefaultAsync();

        Console.WriteLine(question);
            
        if (question == null)
        {
            return NotFound();
        }
        //ViewBag.secid = sectionId;
        return View(question);
    }

    // POST: Section/EditQuestion/5
    [HttpPost]
    // [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditQuestion(int id, QuestionModel question, IFormFile questionImage, List<IFormFile> optionImages)
    {
        if (id != question.Id)
        {
            return NotFound();
        }

        //if (ModelState.IsValid)
        //{
            try
            {
                var existingQuestion = await _context.Questions.AsNoTracking().FirstOrDefaultAsync(q => q.Id == id);
                if (existingQuestion == null)
                {
                    return NotFound();
                }

                if (questionImage != null)
                {
                    if (!string.IsNullOrEmpty(existingQuestion.ImageUrl))
                    {
                        await _fileStorage.DeleteFile(existingQuestion.ImageUrl);
                    }
                    var imageUrl = await _fileStorage.UploadFile(questionImage, "images");
                    if (imageUrl != null)
                    {
                        question.HasImage = true;
                        question.ImageUrl = imageUrl;
                    }
                }
                else
                {
                    question.HasImage = existingQuestion.HasImage;
                    question.ImageUrl = existingQuestion.ImageUrl;
                }

                for (int i = 0; i < optionImages?.Count; i++)
                {
                    var image = optionImages[i];
                    if (image != null)
                    {
                        var imageUrl = await _fileStorage.UploadFile(image, "images");
                        if (imageUrl != null)
                        {
                            switch (i)
                            {
                                case 0:
                                    question.IsOption1Image = true;
                                    question.Option1Image = imageUrl;
                                    break;
                                case 1:
                                    question.IsOption2Image = true;
                                    question.Option2Image = imageUrl;
                                    break;
                                case 2:
                                    question.IsOption3Image = true;
                                    question.Option3Image = imageUrl;
                                    break;
                                case 3:
                                    question.IsOption4Image = true;
                                    question.Option4Image = imageUrl;
                                    break;
                                case 4:
                                    question.IsOption5Image = true;
                                    question.Option5Image = imageUrl;
                                    break;
                            }

                            // If there's an existing image, delete it
                            string? currentImage = i switch
                            {
                                0 => existingQuestion.Option1Image,
                                1 => existingQuestion.Option2Image,
                                2 => existingQuestion.Option3Image,
                                3 => existingQuestion.Option4Image,
                                4 => existingQuestion.Option5Image,
                                _ => null
                            };

                            if (!string.IsNullOrEmpty(currentImage))
                            {
                                await _fileStorage.DeleteFile(currentImage);
                            }
                        }
                    }
                }
                _context.Update(question);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!QuestionExists(question.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction(nameof(Details), new { id = question.SectionId });
        //}
        //return View(question);
    }

    // POST: Section/DeleteQuestion/5
    [HttpPost, ActionName("DeleteQuestion")]
        // [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteQuestion(int id)
    {
        var question = await _context.Questions.FindAsync(id);
        if (question == null)
        {
            return NotFound();
        }

        // Delete associated images
        if (!string.IsNullOrEmpty(question.ImageUrl))
        {
            await _fileStorage.DeleteFile(question.ImageUrl!);
        }

        var optionImages = new[]
        {
            question.Option1Image,
            question.Option2Image,
            question.Option3Image,
            question.Option4Image,
            question.Option5Image
        };

        foreach (var imageUrl in optionImages.Where(url => !string.IsNullOrEmpty(url)))
        {
            await _fileStorage.DeleteFile(imageUrl!);
        }

        _context.Questions.Remove(question);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id = question.SectionId });
    }

    // POST: Section/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var section = await _context.Sections
            .Include(s => s.Questions)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (section == null)
        {
            return NotFound();
        }

        // Delete all questions associated with this section
        foreach (var question in section.Questions)
        {
            // Delete question images if they exist
            if (!string.IsNullOrEmpty(question.ImageUrl))
            {
                await _fileStorage.DeleteFile(question.ImageUrl!);
            }

            // Delete option images if they exist
            var optionImages = new[]
            {
                question.Option1Image,
                question.Option2Image,
                question.Option3Image,
                question.Option4Image,
                question.Option5Image
            };

            foreach (var imageUrl in optionImages)
            {
                if (!string.IsNullOrEmpty(imageUrl))
                {
                    await _fileStorage.DeleteFile(imageUrl!);
                }
            }
        }

        _context.Sections.Remove(section);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool QuestionExists(int id)
    {
        return _context.Questions.Any(e => e.Id == id);
    }

    [HttpGet]
    public async Task<IActionResult> GetSections()
    {
        var sections = await _context.Sections
            .Select(s => new { id = s.Id, name = s.Name })
            .ToListAsync();
        return Json(sections);
    }

    [HttpGet]
    [Route("GetQuestions/{sectionId}")]
    public async Task<IActionResult> GetQuestions(int sectionId)
    {
        var questions = await _context.Questions
            .Where(q => q.SectionId == sectionId)
            .Select(q => new { 
                id = q.Id, 
                description = q.Description,
                hasImage = q.HasImage,
                imageUrl = q.ImageUrl,
                options = new[] {
                    new { text = q.Option1Text, isImage = q.IsOption1Image, image = q.Option1Image },
                    new { text = q.Option2Text, isImage = q.IsOption2Image, image = q.Option2Image },
                    new { text = q.Option3Text, isImage = q.IsOption3Image, image = q.Option3Image },
                    new { text = q.Option4Text, isImage = q.IsOption4Image, image = q.Option4Image },
                    new { text = q.Option5Text, isImage = q.IsOption5Image, image = q.Option5Image }
                },
                correctOption = q.CorrectOption
            })
            .ToListAsync();
        return Json(questions);
    }
}
