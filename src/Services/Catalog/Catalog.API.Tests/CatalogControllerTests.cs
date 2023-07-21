using Catalog.API.Controllers;
using Catalog.API.Entities;
using Catalog.API.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Catalog.API.Tests
{
    public class CatalogControllerTests
    {
        private readonly Mock<IProductRepository> _productRepositoryMock;
        private readonly Mock<ILogger<CatalogController>> _loggerMock;

        public CatalogControllerTests()
        {
            _productRepositoryMock = new Mock<IProductRepository>();
            _loggerMock = new Mock<ILogger<CatalogController>>();
        }

        [Fact]
        public async Task GetProductById_Null_Id_Returns_BadRequest()
        {
            // Arrange
            string? productId = null;
            var controller = new CatalogController(_productRepositoryMock.Object, _loggerMock.Object);

            // Act
            var actual = await controller.GetProductById(productId);

            // Assert
            Assert.IsType<BadRequestObjectResult>(actual.Result);
        }

        [Fact]
        public async Task GetProductById_Empty_Id_Returns_BadRequest()
        {
            // Arrange
            var productId = string.Empty;
            var controller = new CatalogController(_productRepositoryMock.Object, _loggerMock.Object);

            // Act
            var actual = await controller.GetProductById(productId);

            // Assert
            Assert.IsType<BadRequestObjectResult>(actual.Result);
        }

        [Fact]
        public async Task GetProductById_Product_NotFound_Returns_NotFound()
        {
            // Arrange
            var productId = "test_id";
            _productRepositoryMock.Setup(x => x.GetProductAsync(It.IsAny<string>()))
                .ReturnsAsync(() => null);

            var controller = new CatalogController(_productRepositoryMock.Object, _loggerMock.Object);

            // Act
            var actual = await controller.GetProductById(productId);

            // Assert
            Assert.IsType<NotFoundResult>(actual.Result);
        }

        [Fact]
        public async Task GetProductById_Returns_OK()
        {
            // Arrange
            var productId = "test";
            _productRepositoryMock.Setup(x => x.GetProductAsync(It.IsAny<string>()))
                .ReturnsAsync(() => new Product()
                {
                    Id = productId,
                    Category = "Test",
                    Name = "Test",
                    Description = "Test",
                    ImageFile = "Test"
                });
            var controller = new CatalogController(_productRepositoryMock.Object, _loggerMock.Object);

            // Act
            var actual = await controller.GetProductById(productId);

            // Assert
            Assert.NotNull(actual);
            Assert.IsType<ActionResult<Product>>(actual);
            Assert.Equal((actual.Result as OkObjectResult).StatusCode, (int)System.Net.HttpStatusCode.OK);
            Assert.Equal(productId, (((OkObjectResult)actual.Result).Value as Product).Id);
            _productRepositoryMock.Verify(x => x.GetProductAsync(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task GetProductByCategory_Null_CategoryId_Returns_BadRequest()
        {
            // Arrange
            string? productId = null;
            var controller = new CatalogController(_productRepositoryMock.Object, _loggerMock.Object);

            // Act
            var actual = await controller.GetProductByCategory(productId);

            // Assert
            Assert.IsType<BadRequestObjectResult>(actual.Result);
        }

        [Fact]
        public async Task GetProductByCategory_Empty_CategoryId_Returns_BadRequest()
        {
            // Arrange
            var productId = string.Empty;
            var controller = new CatalogController(_productRepositoryMock.Object, _loggerMock.Object);

            // Act
            var actual = await controller.GetProductByCategory(productId);

            // Assert
            Assert.IsType<BadRequestObjectResult>(actual.Result);
        }

        [Fact]
        public async Task GetProductByCategory_Returns_OK()
        {
            // Arrange
            var productId = "test_id";
            _productRepositoryMock.Setup(x => x.GetProductByCategoryAsync(It.IsAny<string>()))
                .ReturnsAsync(() => new List<Product>() { new Product { Id = productId, Name = "test", Category = "category" } });

            var controller = new CatalogController(_productRepositoryMock.Object, _loggerMock.Object);

            // Act
            var actual = await controller.GetProductByCategory(productId);

            // Assert
            Assert.NotNull(actual);
            Assert.IsType<ActionResult<IEnumerable<Product>>>(actual);
            Assert.Equal((actual.Result as OkObjectResult).StatusCode, (int)System.Net.HttpStatusCode.OK);
            _productRepositoryMock.Verify(x => x.GetProductByCategoryAsync(It.IsAny<string>()), Times.Once);

        }

        [Fact]
        public async Task DeleteProductById_Null_CategoryId_Returns_BadRequest()
        {
            // Arrange
            string? productId = null;
            var controller = new CatalogController(_productRepositoryMock.Object, _loggerMock.Object);

            // Act
            var actual = await controller.DeleteProductById(productId);

            // Assert
            Assert.IsType<BadRequestObjectResult>(actual);
        }

        [Fact]
        public async Task DeleteProductById_Empty_CategoryId_Returns_BadRequest()
        {
            // Arrange
            var productId = string.Empty;
            var controller = new CatalogController(_productRepositoryMock.Object, _loggerMock.Object);

            // Act
            var actual = await controller.DeleteProductById(productId);

            // Assert
            Assert.IsType<BadRequestObjectResult>(actual);
        }

    }
}
