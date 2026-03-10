using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Moq;
using ProductAPI.Controllers;
using ProductAPI.Interface;
using ProductAPI.Models;
using ProductAPI.Models.Pagination;
using static ProductAPI.Models.DTO.ProductDTO;

namespace ProductAPI.Tests.Controllers
{
    public class ProductControllerTests
    {
        private readonly Mock<IProductServices> _mockService;
        private readonly Mock<IDistributedCache> _mockCache; // 1. Add the cache mock field
        private readonly ProductController _controller;

        public ProductControllerTests()
        {
            _mockService = new Mock<IProductServices>();
            _mockCache = new Mock<IDistributedCache>(); // 2. Instantiate the mock

            // 3. Inject BOTH mocks into the controller
            _controller = new ProductController(_mockService.Object, _mockCache.Object);
        }

        #region GetById Tests

        [Fact]
        public async Task GetById_WhenProductExists_ReturnsOkResult()
        {
            // Arrange
            int productId = 1;
            // FIXED: Using the actual Product entity instead of an anonymous object
            var expectedProduct = new Product { Id = productId, Name = "Test", Price = 10.99m, StockQuantity = 5 };

            _mockService.Setup(s => s.GetByIdAsync(productId)).ReturnsAsync(expectedProduct);

            // Act
            var result = await _controller.GetById(productId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expectedProduct, okResult.Value);
        }

        [Fact]
        public async Task GetById_WhenProductDoesNotExist_ReturnsNotFoundResult()
        {
            // Arrange
            int productId = 99;
            // FIXED: Explicitly casting null to Product to satisfy strict nullability and Moq
            _mockService.Setup(s => s.GetByIdAsync(productId)).ReturnsAsync((Product)null!);

            // Act
            var result = await _controller.GetById(productId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        #endregion

        #region CreateProduct Tests

        [Fact]
        public async Task CreateProduct_WhenValidDto_ReturnsCreatedAtActionResult()
        {
            // Arrange
            var createDto = new CreateProductDTO("New Product", 25.50m, 10);

            // FIXED: Using the actual Product entity
            var createdProduct = new Product { Id = 1, Name = "New Product", Price = 25.50m, StockQuantity = 10 };

            _mockService.Setup(s => s.CreateAsync(createDto)).ReturnsAsync(createdProduct);

            // Act
            var result = await _controller.CreateProduct(createDto);

            // Assert
            var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(nameof(_controller.GetById), createdAtActionResult.ActionName);
            Assert.Equal(createdProduct, createdAtActionResult.Value);
        }

        #endregion

        #region UpdateProduct Tests

        [Fact]
        public async Task UpdateProduct_WhenSuccessful_ReturnsNoContentResult()
        {
            // Arrange
            int productId = 1;
            var updateDto = new UpdateProductDTO(productId, "Updated Name", 30.00m, 15);

            _mockService.Setup(s => s.UpdateAsync(productId, updateDto)).Returns(Task.CompletedTask);

            // Act
            var result = await _controller.UpdateProduct(productId, updateDto);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task UpdateProduct_WhenProductDoesNotExist_ReturnsNotFoundResult()
        {
            // Arrange
            int productId = 99;
            var updateDto = new UpdateProductDTO(productId, "Updated Name", 30.00m, 15);

            _mockService.Setup(s => s.UpdateAsync(productId, updateDto)).ThrowsAsync(new KeyNotFoundException());

            // Act
            var result = await _controller.UpdateProduct(productId, updateDto);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        #endregion

        #region DeleteProduct Tests

        [Fact]
        public async Task DeleteProduct_WhenSuccessful_ReturnsNoContentResult()
        {
            // Arrange
            int productId = 1;
            _mockService.Setup(s => s.DeleteAsync(productId)).Returns(Task.CompletedTask);

            // Act
            var result = await _controller.DeleteProduct(productId);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task DeleteProduct_WhenProductDoesNotExist_ReturnsNotFoundResult()
        {
            // Arrange
            int productId = 99;
            _mockService.Setup(s => s.DeleteAsync(productId)).ThrowsAsync(new KeyNotFoundException());

            // Act
            var result = await _controller.DeleteProduct(productId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        #endregion

        #region GetAll Tests

        [Fact]
        public async Task GetAll_WhenCalled_ReturnsOkResultWithData()
        {
            // Arrange
            var query = new PaginationQuery { PageNumber = 1, PageSize = 10 };

            // FIXED: Using the actual PaginatedResult type
            var expectedResponse = new PaginatedResult<Product>
            {
                TotalCount = 1,
                Items = new List<Product> { new Product { Id = 1, Name = "Test", Price = 10.99m, StockQuantity = 5 } }
            };

            _mockService.Setup(s => s.GetAllAsync(query)).ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.GetAll(query);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expectedResponse, okResult.Value);
        }

        #endregion
    }
}