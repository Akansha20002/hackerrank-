//using Xunit;
//using Moq;
//using HackerRank.Controllers;
//using HackerRank.Models;
//using HackerRank.Services;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.Configuration;
//using System.Collections.Generic;
//using System.Threading.Tasks;

//namespace HackerRank.Tests
//{
//    public class SectionTests
//    {
//        private SectionController CreateControllerWithInMemoryDb(out AppDbContext context)
//        {
//            var options = new DbContextOptionsBuilder<AppDbContext>()
//                .UseInMemoryDatabase("HackerRankTestDb")
//                .Options;

//            context = new AppDbContext(options);

//            var configurationMock = new Mock<IConfiguration>();
//            var fileStorageMock = new Mock<FileStorageService>();

//            return new SectionController(context, configurationMock.Object, fileStorageMock.Object);
//        }

//        [Fact]
//        public async Task Index_ReturnsViewWithSections()
//        {
//            // Arrange
//            var controller = CreateControllerWithInMemoryDb(out var context);
//            context.Sections.Add(new SectionModel { Name = "Section 1" });
//            await context.SaveChangesAsync();

//            // Act
//            var result = await controller.Index();

//            // Assert
//            var viewResult = Assert.IsType<ViewResult>(result);
//            var model = Assert.IsAssignableFrom<List<SectionModel>>(viewResult.Model);
//            Assert.Single(model);
//            Assert.Equal("Section 1", model[0].Name);
//        }

//        [Fact]
//        public async Task Create_ValidModel_ReturnsOk()
//        {
//            // Arrange
//            var controller = CreateControllerWithInMemoryDb(out var context);
//            var model = new SectionModel { Name = "New Section" };

//            // Act
//            var result = await controller.Create(model);

//            // Assert
//            var okResult = Assert.IsType<OkObjectResult>(result);
//            var returnedModel = Assert.IsType<SectionModel>(okResult.Value);
//            Assert.Equal("New Section", returnedModel.Name);
//        }

//        [Fact]
//        public async Task Create_EmptyName_ReturnsBadRequest()
//        {
//            // Arrange
//            var controller = CreateControllerWithInMemoryDb(out var context);
//            var model = new SectionModel { Name = "" };

//            // Act
//            var result = await controller.Create(model);

//            // Assert
//            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
//            Assert.Contains("Section name is required", badRequest.Value.ToString());
//        }

//        [Fact]
//        public async Task Details_IdIsNull_ReturnsNotFound()
//        {
//            var controller = CreateControllerWithInMemoryDb(out _);
//            var result = await controller.Details(null);
//            Assert.IsType<NotFoundResult>(result);
//        }
//    }
//}
