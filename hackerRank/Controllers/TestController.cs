using Microsoft.AspNetCore.Mvc;
using HackerRank.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
namespace HackerRank.Controllers;

[Authorize(AuthenticationSchemes = "CustomCookieAuth")]
public class TestController : Controller
{
    private readonly AppDbContext _context;
    public TestController(AppDbContext context)
    {
        _context = context;
    }

    // GET: Test/Create
    public IActionResult Create()
    {
        return View();
    }
        
    // POST: Test/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromBody] TestModel test)
    {
        Console.WriteLine("Added ");
            _context.Add(test);
            await _context.SaveChangesAsync();
            //perfactly fatching the test id and send back to view , after ok report
            return Ok(new { message = "Test saved successfully", testId = test.Id }); ;
    }

    public async Task<IActionResult> Index()
    {
        var tests = await _context.Tests.ToListAsync();
        return View(tests);
    }
    public async Task<IActionResult> Details(int id)
    {
        var test = await _context.Tests
            .Include(t => t.TestQuestions)
            .ThenInclude(tq => tq.Question)
            .ThenInclude(q => q.Section)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (test == null)
        {
            return NotFound();
        }
        return View(test);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveQuestion(int testId, int questionId)
    {
        var testQuestion = await _context.Set<TestQuestionModel>()
            .FirstOrDefaultAsync(tq => tq.TestId == testId && tq.QuestionId == questionId);

        if (testQuestion != null)
        {
            _context.Remove(testQuestion);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Details), new { id = testId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddQuestion([FromBody] AddQuestionViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var test = await _context.Tests.FindAsync(model.TestId);
        if (test == null)
        {
            return NotFound();
        }

        foreach (var questionId in model.QuestionIds)
        {
            if (!await _context.Set<TestQuestionModel>()
                .AnyAsync(tq => tq.TestId == model.TestId && tq.QuestionId == questionId))
            {
                _context.Add(new TestQuestionModel
                {
                    TestId = model.TestId,
                    QuestionId = questionId
                });
            }
        }

        await _context.SaveChangesAsync();
        return Ok();
    }

    // POST: Test/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var test = await _context.Tests
            .Include(t => t.TestQuestions)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (test == null)
        {
            return NotFound();
        }

        // Remove all test questions
        _context.TestQuestions.RemoveRange(test.TestQuestions);
        
        // Remove the test
        _context.Tests.Remove(test);
        await _context.SaveChangesAsync();
        var tests = await _context.Tests.ToListAsync();
        return View("~/Views/Home/Dashboard.cshtml", tests);

    }

    //after test creation , 
    //[ValidateAntiForgeryToken] this only used with post actions not with get actions 
    public async Task<IActionResult> ShowSections(int testId)
    {
        var sections = await _context.Sections
           .Include(s => s.Questions)
           .ToListAsync();
        ViewBag.testId = testId;
        return View(sections);
    }
    [HttpPost("Test/Each_section")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Each_section(int testId , string secIds)
    {
        var ids = JsonSerializer.Deserialize<List<int>>(secIds);  // this fixes [6] format issue

        var allsections = new List<SectionModel>();
        foreach (var x in ids)
        {
            var sec = await _context.Sections
                .Where(s => s.Id == x)
                .Include(s => s.Questions)
                .ToListAsync();
            allsections.AddRange(sec);
        }
        ViewBag.TestId = testId;
        //ViewBag.Sections = allsections;
        return View(allsections);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save_Test_Questions([FromBody] SaveTestQuestionsRequest request)
    {
        if (request == null || request.TestId <= 0 || request.SelectedQuestions == null)
        {
            return BadRequest(new { message = "Invalid request data." });
        }

        try
        {
            // Find the test
            var test = await _context.Tests.FindAsync(request.TestId);
            if (test == null)
            {
                return NotFound(new { message = "Test not found." });
            }

            foreach (var sectionQuestions in request.SelectedQuestions)
            {
                foreach (var questionId in sectionQuestions)
                {
                    var testQuestion = new TestQuestionModel
                    {
                        TestId = request.TestId,
                        QuestionId = questionId
                    };
                    _context.TestQuestions.Add(testQuestion);
                }
            }

            // Save changes to the database
            await _context.SaveChangesAsync();
            
            return Json(new { message = "Test questions saved successfully."  , TestId = request.TestId});
        }
        catch (Exception ex)
        {
            // Log the exception (optional)
            return StatusCode(500, new { message = "An error occurred while saving test questions.", error = ex.Message });
        }


    }
    public async Task<IActionResult> DisplayTest(int testId)
    {
        var test = await _context.Tests
            .Include(t => t.TestQuestions)
            .ThenInclude(tq => tq.Question)
            .FirstOrDefaultAsync(t => t.Id == testId);
        Console.WriteLine(test);
        if (test == null)
        {
            return NotFound();
        }

        return View(test);
    }

}

// Request model for Save_Test_Questions
public class SaveTestQuestionsRequest
{
    public int TestId { get; set; }
    public List<List<int>> SelectedQuestions { get; set; } = new List<List<int>>();
}

public class AddQuestionViewModel
{
    public int TestId { get; set; }
    public List<int> QuestionIds { get; set; } = new();
}