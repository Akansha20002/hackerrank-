using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication;
using HackerRank.Controllers;
using HackerRank.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
//this it test file for HomeController in HackerRank project
namespace HackerRank.Tests
{
    public class HomeTests
    {
        public HomeController GetControllerWithContext(AppDbContext dbContext, bool isAuthenticated = false)
        {
            var logger = new Mock<ILogger<HomeController>>();
            var config = new Mock<IConfiguration>();
            var controller = new HomeController(logger.Object, dbContext, config.Object);

            var user = new ClaimsPrincipal(new ClaimsIdentity(
                isAuthenticated ? new[] { new Claim(ClaimTypes.Name, "admin"), new Claim(ClaimTypes.NameIdentifier, "1") } : null,
                isAuthenticated ? "CustomCookieAuth" : null
            ));

            var httpContext = new DefaultHttpContext { User = user };
            
         
            var authServiceMock = new Mock<IAuthenticationService>();
            authServiceMock
                .Setup(x => x.SignInAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<ClaimsPrincipal>(), It.IsAny<AuthenticationProperties>()))
                .Returns(Task.CompletedTask);
            authServiceMock
                .Setup(x => x.SignOutAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<AuthenticationProperties>()))
                .Returns(Task.CompletedTask);

            // Setup URL helper
            var urlHelperFactory = new Mock<IUrlHelperFactory>();
            var urlHelper = new Mock<IUrlHelper>();
            urlHelperFactory
                .Setup(_ => _.GetUrlHelper(It.IsAny<ActionContext>()))
                .Returns(urlHelper.Object);
            urlHelper
                .Setup(x => x.Action(It.IsAny<UrlActionContext>()))
                .Returns("someUrl");

            // Setup TempData
            var tempDataProvider = new Mock<ITempDataProvider>();
            var tempDataDictionaryFactory = new Mock<ITempDataDictionaryFactory>();
            tempDataDictionaryFactory
                .Setup(x => x.GetTempData(It.IsAny<HttpContext>()))
                .Returns(new TempDataDictionary(httpContext, tempDataProvider.Object));

            var serviceProvider = new ServiceCollection()
                .AddSingleton(authServiceMock.Object)
                .AddSingleton(urlHelperFactory.Object)
                .AddSingleton(tempDataDictionaryFactory.Object)
                .BuildServiceProvider();

            httpContext.RequestServices = serviceProvider;

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            return controller;
        }

        private AppDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: System.Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        [Fact]
        public void Index_ReturnsViewResult()
        {
            var db = GetInMemoryDbContext();
            var controller = GetControllerWithContext(db);

            var result = controller.Index();

            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public void Privacy_ReturnsViewResult()
        {
            var db = GetInMemoryDbContext();
            var controller = GetControllerWithContext(db);

            var result = controller.Privacy();

            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public void Error_ReturnsViewResultWithModel()
        {
            var db = GetInMemoryDbContext();
            var controller = GetControllerWithContext(db);

            var result = controller.Error() as ViewResult;

            Assert.NotNull(result);
            Assert.IsType<ErrorViewModel>(result.Model);
        }

        [Fact]
        public void Login_Get_AuthenticatedUser_RedirectsToDashboard()
        {
            var db = GetInMemoryDbContext();
            var controller = GetControllerWithContext(db, isAuthenticated: true);

            var result = controller.Login();

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Dashboard", redirect.ActionName);
        }

        [Fact]
        public void Login_Get_UnauthenticatedUser_ReturnsView()
        {
            var db = GetInMemoryDbContext();
            var controller = GetControllerWithContext(db, isAuthenticated: false);

            var result = controller.Login();

            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public async Task Login_Post_InvalidUser_RedirectsToLoginWithError()
        {
            var db = GetInMemoryDbContext();
            var controller = GetControllerWithContext(db);

            var tempAdmin = new Admin { Username = "notfound", PasswordHash = "wrong" };

            var result = await controller.Login(tempAdmin);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Login", redirect.ActionName);
            //Assert.NotNull(controller.TempData["Error"]);
        }

        //[Fact]
        //public async Task Login_Post_ValidUser_RedirectsToDashboard()
        //{
        //    var db = GetInMemoryDbContext();
        //    var password = BCrypt.Net.BCrypt.HashPassword("pass");
        //    var admin = new Admin { Username = "admin", PasswordHash = password };
        //    db.Admins.Add(admin);
        //    db.SaveChanges();

        //    var controller = GetControllerWithContext(db);

        //    // Mock SignInAsync
        //    var authService = new Mock<IAuthenticationService>();
        //    controller.ControllerContext.HttpContext.RequestServices = new ServiceCollection()
        //        .AddSingleton(authService.Object)
        //        .BuildServiceProvider();

        //    var tempAdmin = new Admin { Username = "admin", PasswordHash = "pass" };

        //    var result = await controller.Login(tempAdmin);

        //    var redirect = Assert.IsType<RedirectToActionResult>(result);
        //    Assert.Equal("Dashboard", redirect.ActionName);
        //}

        //[Fact]
        //public async Task Logout_SignsOutAndRedirectsToIndex()
        //{
        //    var db = GetInMemoryDbContext();
        //    var controller = GetControllerWithContext(db);

        //    // Mock SignOutAsync
        //    var authService = new Mock<IAuthenticationService>();
        //    controller.ControllerContext.HttpContext.RequestServices = new ServiceCollection()
        //        .AddSingleton(authService.Object)
        //        .BuildServiceProvider();

        //    var result = await controller.Logout();

        //    var redirect = Assert.IsType<RedirectToActionResult>(result);
        //    Assert.Equal("Index", redirect.ActionName);
        //}

        [Fact]
        public void AccessDenied_ReturnsView()
        {
            var db = GetInMemoryDbContext();
            var controller = GetControllerWithContext(db);

            var result = controller.AccessDenied();

            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public async Task Dashboard_ReturnsViewWithTests()
        {
            var db = GetInMemoryDbContext();
            db.Tests.Add(new TestModel { Id = 1, TestName = "Test1", TestDate = System.DateTime.Now, TimeAllowed = 60, AdminId = 1 });
            db.SaveChanges();

            var controller = GetControllerWithContext(db, isAuthenticated: true);

            var result = await controller.Dashboard();

            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<List<TestModel>>(view.Model);
            Assert.Single(model);
        }

        [Fact]
        public void Signup_Get_ReturnsView()
        {
            var db = GetInMemoryDbContext();
            var controller = GetControllerWithContext(db);

            var result = controller.Signup();

            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public async Task Signup_Post_PasswordsDoNotMatch_RedirectsWithError()
        {
            var db = GetInMemoryDbContext();
            var controller = GetControllerWithContext(db);

            var admin = new Admin { Username = "user", PasswordHash = "pass1" };

            var result = await controller.Signup(admin, "pass2");

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Signup", redirect.ActionName);
            Assert.NotNull(controller.TempData["Error"]);
        }

        [Fact]
        public async Task Signup_Post_UsernameExists_RedirectsWithError()
        {
            var db = GetInMemoryDbContext();
            db.Admins.Add(new Admin { Username = "user", PasswordHash = "pass" });
            db.SaveChanges();

            var controller = GetControllerWithContext(db);

            var admin = new Admin { Username = "user", PasswordHash = "pass" };

            var result = await controller.Signup(admin, "pass");

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Signup", redirect.ActionName);
            Assert.NotNull(controller.TempData["Error"]);
        }

        [Fact]
        public async Task Signup_Post_Valid_RedirectsToLoginWithSuccess()
        {
            var db = GetInMemoryDbContext();
            var controller = GetControllerWithContext(db);

            var admin = new Admin { Username = "newuser", PasswordHash = "pass" };

            var result = await controller.Signup(admin, "pass");

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Login", redirect.ActionName);
            Assert.NotNull(controller.TempData["Success"]);
        }

        [Fact]
        public void Comingsoon_ReturnsView()
        {
            var db = GetInMemoryDbContext();
            var controller = GetControllerWithContext(db);

            var result = controller.Comingsoon();

            var view = Assert.IsType<ViewResult>(result);
            Assert.Equal("Comingsoon", view.ViewName);
        }

        //[Fact]
        //public async Task Dashboard_UnauthenticatedUser_ThrowsException()
        //{
        //    var db = GetInMemoryDbContext();
        //    var controller = GetControllerWithContext(db, isAuthenticated: false);

        //    await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        //    {
        //        await controller.Dashboard();
        //    });
        //}

        [Fact]
        public async Task Login_Post_InvalidPassword_RedirectsToLoginWithError()
        {
            var db = GetInMemoryDbContext();
            var password = BCrypt.Net.BCrypt.HashPassword("correctpass");
            var admin = new Admin { Username = "admin", PasswordHash = password };
            db.Admins.Add(admin);
            db.SaveChanges();

            var controller = GetControllerWithContext(db);
            var tempAdmin = new Admin { Username = "admin", PasswordHash = "wrongpass" };

            var result = await controller.Login(tempAdmin);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Login", redirect.ActionName);
            Assert.Equal("Invalid username or password", controller.TempData["Error"]);
        }

        [Fact]
        public async Task Dashboard_EmptyTestList_ReturnsViewWithEmptyList()
        {
            var db = GetInMemoryDbContext();
            var controller = GetControllerWithContext(db, isAuthenticated: true);

            var result = await controller.Dashboard();

            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<List<TestModel>>(view.Model);
            Assert.Empty(model);
        }

        [Fact]
        public async Task Signup_Post_SavesHashedPassword()
        {
            var db = GetInMemoryDbContext();
            var controller = GetControllerWithContext(db);
            var plainPassword = "testPassword123";
            var admin = new Admin { Username = "newuser", PasswordHash = plainPassword };

            await controller.Signup(admin, plainPassword);

            var savedAdmin = await db.Admins.FirstOrDefaultAsync(a => a.Username == "newuser");
            Assert.NotNull(savedAdmin);
            Assert.NotEqual(plainPassword, savedAdmin.PasswordHash);
            Assert.True(BCrypt.Net.BCrypt.Verify(plainPassword, savedAdmin.PasswordHash));
        }
    }
}