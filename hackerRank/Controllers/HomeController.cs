using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using HackerRank.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

namespace HackerRank.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly AppDbContext _tables;
    private readonly IConfiguration _configuration;

    public HomeController(ILogger<HomeController> logger, AppDbContext tables, IConfiguration configuration)
    {
        _logger = logger;
        _tables = tables;
        _configuration = configuration;
    }
    public IActionResult Index()
    {
        return View();
    }
    public IActionResult Privacy()
    {
        return View();
    }
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
    // Login:
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Login()
    {
        if (User.Identity != null && User.Identity.IsAuthenticated)
        {
            return RedirectToAction("Dashboard");
        }
        return View();
    }
 
    [HttpPost]
    public async Task<IActionResult> Login(Admin temp)
    {
        var user = await _tables.Admins
            .FirstOrDefaultAsync(x => x.Username == temp.Username);

        if (user == null || !BCrypt.Net.BCrypt.Verify(temp.PasswordHash, user.PasswordHash))
        {
            TempData["Error"] = "Invalid username or password";
            return RedirectToAction("Login");
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim("Role", "Admin")
        };

        var claimsIdentity = new ClaimsIdentity(claims, "CustomCookieAuth");
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true, 
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(24)
        };

        await HttpContext.SignInAsync("CustomCookieAuth", new ClaimsPrincipal(claimsIdentity), authProperties);

        return RedirectToAction("Dashboard");
    }

    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync("CustomCookieAuth");
        return RedirectToAction("Index");
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }

 
    [Microsoft.AspNetCore.Authorization.Authorize(AuthenticationSchemes = "CustomCookieAuth")]
    public async Task<IActionResult> Dashboard()
    {
        var test = await _tables.Tests.ToListAsync();
        
        return View(test);
    }


    public IActionResult Signup()
    {
        return View();
    }


    [HttpPost]
    public async Task<IActionResult> Signup(Admin model, string confirmPassword)
    {
        if (model.PasswordHash != confirmPassword)
        {
            TempData["Error"] = "Passwords do not match";
            return RedirectToAction("Signup");
        }

        var existingUser = await _tables.Admins
            .FirstOrDefaultAsync(x => x.Username == model.Username);

        if (existingUser != null)
        {
            TempData["Error"] = "Username already exists";
            return RedirectToAction("Signup");
        }

        // Hash the password
        model.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.PasswordHash);

        // Save the new admin
        await _tables.Admins.AddAsync(model);
        await _tables.SaveChangesAsync();

        TempData["Success"] = "Account created successfully! Please login.";
        return RedirectToAction("Login");
    }
   
    public IActionResult Comingsoon()
    {
        Console.WriteLine("fine till comoing soon");
        return View("Comingsoon");
    }
}
