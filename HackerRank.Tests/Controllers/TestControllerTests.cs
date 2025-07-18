using Xunit;
using Moq;
using HackerRank.Controllers;
using HackerRank.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HackerRank.Tests
{
    public class TestControllerTests
    {
        private TestController CreateControllerWithInMemoryDb(out AppDbContext context)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            context = new AppDbContext(options);
            return new TestController(context);
        }

        [Fact]
        public async Task Create_ValidModel_ReturnsOkResult()
        {
            // Arrange
            var controller = CreateControllerWithInMemoryDb(out var context);
            var test = new TestModel { TestName = "Test 1", TestDate = DateTime.Now, TimeAllowed = 60 };

            // Act
            var result = await controller.Create(test);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
            var jsonDoc = System.Text.Json.JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(okResult.Value));
            Assert.Equal("Test saved successfully", jsonDoc.RootElement.GetProperty("message").GetString());
            Assert.True(jsonDoc.RootElement.GetProperty("testId").GetInt32() > 0, "Test ID should be greater than 0");
        }

        [Fact]
        public async Task Index_ReturnsViewWithTests()
        {
            // Arrange
            var controller = CreateControllerWithInMemoryDb(out var context);
            context.Tests.Add(new TestModel { TestName = "Test 1", TestDate = DateTime.Now, TimeAllowed = 60 });
            await context.SaveChangesAsync();

            // Act
            var result = await controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<List<TestModel>>(viewResult.Model);
            Assert.Single(model);
            Assert.Equal("Test 1", model[0].TestName);
        }

        [Fact]
        public async Task Details_ValidId_ReturnsViewWithTest()
        {
            // Arrange
            var controller = CreateControllerWithInMemoryDb(out var context);
            var test = new TestModel { Id = 1, TestName = "Test 1" };
            context.Tests.Add(test);
            await context.SaveChangesAsync();

            // Act
            var result = await controller.Details(1);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<TestModel>(viewResult.Model);
            Assert.Equal("Test 1", model.TestName);
        }

        [Fact]
        public async Task Details_InvalidId_ReturnsNotFound()
        {
            // Arrange
            var controller = CreateControllerWithInMemoryDb(out var context);

            // Act
            var result = await controller.Details(999);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task RemoveQuestion_ValidIds_RemovesQuestionAndRedirects()
        {
            // Arrange
            var controller = CreateControllerWithInMemoryDb(out var context);
            var test = new TestModel { Id = 1, TestName = "Test 1" };
            var question = new QuestionModel { Id = 1 };
            var testQuestion = new TestQuestionModel { TestId = 1, QuestionId = 1 };
            
            context.Tests.Add(test);
            context.Questions.Add(question);
            context.TestQuestions.Add(testQuestion);
            await context.SaveChangesAsync();

            // Act
            var result = await controller.RemoveQuestion(1, 1);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirectResult.ActionName);
            Assert.NotNull(redirectResult.RouteValues);
            Assert.Equal(1, redirectResult.RouteValues!["id"]);
            Assert.Empty(context.TestQuestions);
        }

        [Fact]
        public async Task AddQuestion_ValidModel_ReturnsOkResult()
        {
            // Arrange
            var controller = CreateControllerWithInMemoryDb(out var context);
            var test = new TestModel { Id = 1, TestName = "Test 1" };
            context.Tests.Add(test);
            await context.SaveChangesAsync();

            var model = new AddQuestionViewModel
            {
                TestId = 1,
                QuestionIds = new List<int> { 1, 2 }
            };

            // Act
            var result = await controller.AddQuestion(model);

            // Assert
            Assert.IsType<OkResult>(result);
            Assert.Equal(2, await context.TestQuestions.CountAsync());
        }

        [Fact]
        public async Task AddQuestion_InvalidTestId_ReturnsNotFound()
        {
            // Arrange
            var controller = CreateControllerWithInMemoryDb(out var context);
            var model = new AddQuestionViewModel
            {
                TestId = 999,
                QuestionIds = new List<int> { 1, 2 }
            };

            // Act
            var result = await controller.AddQuestion(model);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task DeleteConfirmed_ValidId_DeletesTestAndRedirects()
        {
            // Arrange
            var controller = CreateControllerWithInMemoryDb(out var context);
            var test = new TestModel { Id = 1, TestName = "Test 1" };
            context.Tests.Add(test);
            await context.SaveChangesAsync();

            // Act
            var result = await controller.DeleteConfirmed(1);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("~/Views/Home/Dashboard.cshtml", viewResult.ViewName);
            Assert.Empty(context.Tests);
        }

        [Fact]
        public async Task DeleteConfirmed_InvalidId_ReturnsNotFound()
        {
            // Arrange
            var controller = CreateControllerWithInMemoryDb(out var context);

            // Act
            var result = await controller.DeleteConfirmed(999);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task ShowSections_ReturnsViewWithSections()
        {
            // Arrange
            var controller = CreateControllerWithInMemoryDb(out var context);
            var section = new SectionModel { Id = 1, Name = "Section 1" };
            context.Sections.Add(section);
            await context.SaveChangesAsync();

            // Act
            var result = await controller.ShowSections(1);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<List<SectionModel>>(viewResult.Model);
            Assert.Single(model);
            Assert.Equal(1, viewResult.ViewData["testId"]);
        }

        [Fact]
        public async Task Save_Test_Questions_ValidRequest_ReturnsSuccess()
        {
            // Arrange
            var controller = CreateControllerWithInMemoryDb(out var context);
            var test = new TestModel { Id = 1 ,  TestName = "Test 1" };
            context.Tests.Add(test);
            await context.SaveChangesAsync();

            var request = new SaveTestQuestionsRequest
            {
                TestId = 1,
                SelectedQuestions = new List<List<int>> { new List<int> { 1, 2 } }
            };

            // Act
            var result = await controller.Save_Test_Questions(request);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            Assert.NotNull(jsonResult.Value);
            var jsonDoc = System.Text.Json.JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(jsonResult.Value));
            Assert.Equal("Test questions saved successfully.", jsonDoc.RootElement.GetProperty("message").GetString());
            Assert.Equal(1, jsonDoc.RootElement.GetProperty("TestId").GetInt32());
            Assert.Equal(2, await context.TestQuestions.CountAsync());
        }

        [Fact]
        public async Task Save_Test_Questions_InvalidTestId_ReturnsNotFound()
        {
            // Arrange
            var controller = CreateControllerWithInMemoryDb(out var context);
            var request = new SaveTestQuestionsRequest
            {
                TestId = 999,
                SelectedQuestions = new List<List<int>> { new List<int> { 1, 2 } }
            };

            // Act
            var result = await controller.Save_Test_Questions(request);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task DisplayTest_ValidId_ReturnsViewWithTest()
        {
            // Arrange
            var controller = CreateControllerWithInMemoryDb(out var context);
            var test = new TestModel { Id = 1, TestName = "Test 1" };
            context.Tests.Add(test);
            await context.SaveChangesAsync();

            // Act
            var result = await controller.DisplayTest(1);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<TestModel>(viewResult.Model);
            Assert.Equal("Test 1", model.TestName);
        }

        [Fact]
        public async Task DisplayTest_InvalidId_ReturnsNotFound()
        {
            // Arrange
            var controller = CreateControllerWithInMemoryDb(out var context);

            // Act
            var result = await controller.DisplayTest(999);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }
    }
}
