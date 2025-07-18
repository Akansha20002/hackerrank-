using Xunit;
using Moq;
using HackerRank.Controllers;
using HackerRank.Models;
using HackerRank.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace HackerRank.Tests
{
    public class SectionTests
    {
        private SectionController CreateControllerWithInMemoryDb(out AppDbContext context)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            context = new AppDbContext(options);

            var configurationMock = new Mock<IConfiguration>();
            configurationMock.Setup(x => x["FileStorage:BaseUrl"]).Returns("http://test.com");
            var fileStorageMock = new Mock<FileStorageService>(configurationMock.Object, Mock.Of<ILogger<FileStorageService>>());

            // Mock successful file upload
            fileStorageMock
                .Setup(f => f.UploadFile(It.IsAny<IFormFile>(), It.IsAny<string>()))
                .ReturnsAsync("testImageUrl");

            // Mock successful file deletion
            fileStorageMock
                .Setup(f => f.DeleteFile(It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            var controller = new SectionController(context, configurationMock.Object, fileStorageMock.Object);
            
            // Setup ControllerContext
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            return controller;
        }

        [Fact]
        public async Task Index_ReturnsViewWithSections()
        {
            // Arrange
            var controller = CreateControllerWithInMemoryDb(out var context);
            context.Sections.Add(new SectionModel { Name = "Section 1" });
            await context.SaveChangesAsync();

            // Act
            var result = await controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<List<SectionModel>>(viewResult.Model);
            Assert.Single(model);
            Assert.Equal("Section 1", model[0].Name);
        }

        [Fact]
        public async Task Create_ValidModel_ReturnsOk()
        {
            // Arrange
            var controller = CreateControllerWithInMemoryDb(out var context);
            var model = new SectionModel { Name = "New Section" };

            // Act
            var result = await controller.Create(model);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedModel = Assert.IsType<SectionModel>(okResult.Value);
            Assert.Equal("New Section", returnedModel.Name);
        }

        [Fact]
        public async Task Create_EmptyName_ReturnsBadRequest()
        {
            // Arrange
            var controller = CreateControllerWithInMemoryDb(out var context);
            var model = new SectionModel { Name = "" };

            // Act
            var result = await controller.Create(model);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("Section name is required", badRequest.Value?.ToString() ?? "");
        }

        [Fact]
        public async Task Details_ValidId_ReturnsViewWithSection()
        {
            // Arrange
            var controller = CreateControllerWithInMemoryDb(out var context);
            var section = new SectionModel { Id = 1, Name = "Test Section" };
            context.Sections.Add(section);
            await context.SaveChangesAsync();

            // Act
            var result = await controller.Details(1);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<SectionModel>(viewResult.Model);
            Assert.Equal("Test Section", model.Name);
        }

        [Theory]
        [InlineData(null)]
        [InlineData(999)]
        public async Task Details_InvalidId_ReturnsNotFound(int? id)
        {
            // Arrange
            var controller = CreateControllerWithInMemoryDb(out var context);

            // Act
            var result = await controller.Details(id);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task CreateQuestion_ValidData_RedirectsToDetails()
        {
            // Arrange
            var controller = CreateControllerWithInMemoryDb(out var context);
            var section = new SectionModel { Id = 1, Name = "Test Section" };
            context.Sections.Add(section);
            await context.SaveChangesAsync();

            var question = new QuestionModel 
            { 
                SectionId = 1,
                Description = "Test Question",
                Option1Text = "Option 1",
                Option2Text = "Option 2",
                Option3Text = "Option 3",
                Option4Text = "Option 4",
                CorrectOption = 1
            };

            var questionImage = new Mock<IFormFile>();
            questionImage.Setup(f => f.Length).Returns(1024);

            // Act
            var result = await controller.CreateQuestion(question, questionImage.Object, null);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(controller.Details), redirectResult.ActionName);
            Assert.Equal(1, redirectResult.RouteValues["id"]);
            Assert.Single(context.Questions);
        }

        [Fact]
        public async Task CreateQuestion_InvalidModelState_ReturnsViewWithModel()
        {
            // Arrange
            var controller = CreateControllerWithInMemoryDb(out var context);
            var section = new SectionModel { Id = 1, Name = "Test Section" };
            context.Sections.Add(section);
            await context.SaveChangesAsync();

            var question = new QuestionModel { SectionId = 1 }; // Missing required fields
            controller.ModelState.AddModelError("Description", "Description is required");

            // Act
            var result = await controller.CreateQuestion(question, null, null);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.IsType<QuestionModel>(viewResult.Model);
            Assert.Empty(context.Questions);
        }

        [Fact]
        public async Task CreateQuestion_WithImages_SavesImages()
        {
            // Arrange
            var controller = CreateControllerWithInMemoryDb(out var context);
            var section = new SectionModel { Id = 1, Name = "Test Section" };
            context.Sections.Add(section);
            await context.SaveChangesAsync();

            var question = new QuestionModel 
            { 
                SectionId = 1,
                Description = "Test Question",
                Option1Text = "Option 1",
                Option2Text = "Option 2",
                Option3Text = "Option 3",
                Option4Text = "Option 4",
                CorrectOption = 1
            };

            var questionImage = new Mock<IFormFile>();
            questionImage.Setup(f => f.Length).Returns(1024);

            var optionImages = new List<IFormFile> { questionImage.Object };

            // Act
            var result = await controller.CreateQuestion(question, questionImage.Object, optionImages);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            var savedQuestion = await context.Questions.FirstOrDefaultAsync();
            Assert.NotNull(savedQuestion);
            Assert.True(savedQuestion.HasImage);
            Assert.Equal("testImageUrl", savedQuestion.ImageUrl);
            Assert.True(savedQuestion.IsOption1Image);
            Assert.Equal("testImageUrl", savedQuestion.Option1Image);
        }

        [Fact]
        public async Task Details_IdIsNull_ReturnsNotFound()
        {
            var controller = CreateControllerWithInMemoryDb(out _);
            var result = await controller.Details(null);
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Details_ValidId_ReturnsViewWithSection()
        {
            // Arrange
            var controller = CreateControllerWithInMemoryDb(out var context);
            var section = new SectionModel { Id = 1, Name = "Test Section" };
            context.Sections.Add(section);
            await context.SaveChangesAsync();

            // Act
            var result = await controller.Details(1);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<SectionModel>(viewResult.Model);
            Assert.Equal("Test Section", model.Name);
        }

        [Fact]
        public void CreateQuestion_InvalidSectionId_ReturnsNotFound()
        {
            // Arrange
            var controller = CreateControllerWithInMemoryDb(out var context);

            // Act
            var result = controller.CreateQuestion(null);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task CreateQuestion_ValidSectionId_ReturnsViewWithModel()
        {
            // Arrange
            var controller = CreateControllerWithInMemoryDb(out var context);
            var section = new SectionModel { Id = 1, Name = "Test Section" };
            context.Sections.Add(section);
            await context.SaveChangesAsync();

            // Act
            var result = controller.CreateQuestion(1);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<QuestionModel>(viewResult.Model);
            Assert.Equal(1, model.SectionId);
        }

        [Fact]
        public async Task EditQuestion_InvalidId_ReturnsNotFound()
        {
            // Arrange
            var controller = CreateControllerWithInMemoryDb(out var context);

            // Act
            var result = await controller.EditQuestion(null);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task EditQuestion_ValidId_ReturnsViewWithQuestion()
        {
            // Arrange
            var controller = CreateControllerWithInMemoryDb(out var context);
            var section = new SectionModel { Id = 1, Name = "Test Section" };
            var question = new QuestionModel 
            { 
                Id = 1, 
                SectionId = 1,
                Description = "Test Question",
                Option1Text = "Option 1",
                Option2Text = "Option 2",
                Option3Text = "Option 3",
                Option4Text = "Option 4",
                Option5Text = "Option 5",
                CorrectOption = 1
            };
            context.Sections.Add(section);
            context.Questions.Add(question);
            await context.SaveChangesAsync();

            // Act
            var result = await controller.EditQuestion(1);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<QuestionModel>(viewResult.Model);
            Assert.Equal("Test Question", model.Description);
        }

        [Fact]
        public async Task DeleteQuestion_NonExistingQuestion_ReturnsNotFound()
        {
            // Arrange
            var controller = CreateControllerWithInMemoryDb(out var context);

            // Act
            var result = await controller.DeleteQuestion(999);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task GetSections_EmptyDatabase_ReturnsEmptyList()
        {
            // Arrange
            var controller = CreateControllerWithInMemoryDb(out var context);

            // Act
            var result = await controller.GetSections();

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            var sections = jsonResult.Value as System.Collections.IEnumerable;
            Assert.NotNull(sections);
            Assert.Empty(sections);
        }

        [Fact]
        public async Task GetQuestions_InvalidSectionId_ReturnsEmptyList()
        {
            // Arrange
            var controller = CreateControllerWithInMemoryDb(out var context);

            // Act
            var result = await controller.GetQuestions(999);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            var questions = jsonResult.Value as System.Collections.IEnumerable;
            Assert.NotNull(questions);
            Assert.Empty(questions);
        }
    }
}
