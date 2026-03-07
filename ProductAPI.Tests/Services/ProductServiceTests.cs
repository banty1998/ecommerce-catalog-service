using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Moq;
using ProductAPI.Data;
using ProductAPI.Models;
using ProductAPI.Services;
using System;
using System.Collections.Generic;
using System.Text;
using static ProductAPI.Models.DTO.ProductDTO;

namespace ProductAPI.Tests.Services
{
    public class ProductServiceTests
    {
        private readonly Mock<IMapper> _mockMapper;

        public ProductServiceTests()
        {
            // We only mock AutoMapper, NOT the DbContext
            _mockMapper = new Mock<IMapper>();
        }

        // Helper method to create a fresh, isolated database for every test
        private AppDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new AppDbContext(options);
        }

        #region CreateAsync Tests

        [Fact]
        public async Task CreateAsync_AddsProductToDatabase()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var service = new ProductService(context, _mockMapper.Object);

            var createDto = new CreateProductDTO("Test Product", 10.99m, 5);
            var mappedProduct = new Product { Id = 1, Name = "Test Product", Price = 10.99m, StockQuantity = 5 };

            _mockMapper.Setup(m => m.Map<Product>(createDto)).Returns(mappedProduct);

            // Act
            var result = await service.CreateAsync(createDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, await context.Products.CountAsync()); // Verifies it actually saved to the DB
            Assert.Equal("Test Product", result.Name);
        }

        #endregion

        #region GetByIdAsync Tests

        [Fact]
        public async Task GetByIdAsync_WhenProductExists_ReturnsProduct()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var product = new Product { Id = 1, Name = "Existing Product", Price = 20.00m, StockQuantity = 10 };

            // Seed the in-memory database
            context.Products.Add(product);
            await context.SaveChangesAsync();

            var service = new ProductService(context, _mockMapper.Object);

            // Act
            var result = await service.GetByIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            Assert.Equal("Existing Product", result.Name);
        }

        [Fact]
        public async Task GetByIdAsync_WhenProductDoesNotExist_ReturnsNull()
        {
            // Arrange
            var context = GetInMemoryDbContext(); // Empty database
            var service = new ProductService(context, _mockMapper.Object);

            // Act
            var result = await service.GetByIdAsync(99);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region UpdateAsync Tests

        [Fact]
        public async Task UpdateAsync_WhenProductDoesNotExist_ThrowsKeyNotFoundException()
        {
            // Arrange
            var context = GetInMemoryDbContext(); // Empty database
            var service = new ProductService(context, _mockMapper.Object);
            var updateDto = new UpdateProductDTO(99, "Updated", 15.00m, 5);

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.UpdateAsync(99, updateDto));
        }

        [Fact]
        public async Task UpdateAsync_WhenProductExists_UpdatesDatabase()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var product = new Product { Id = 1, Name = "Old Name", Price = 10.00m, StockQuantity = 5 };
            context.Products.Add(product);
            await context.SaveChangesAsync();

            var service = new ProductService(context, _mockMapper.Object);
            var updateDto = new UpdateProductDTO(1, "New Name", 20.00m, 10);

            // Setup mapper to modify the existing product object
            _mockMapper.Setup(m => m.Map(updateDto, product)).Callback<UpdateProductDTO, Product>((dto, p) =>
            {
                p.Name = dto.Name;
                p.Price = dto.Price;
                p.StockQuantity = dto.StockQuantity;
            });

            // Act
            await service.UpdateAsync(1, updateDto);

            // Assert
            var updatedProduct = await context.Products.FindAsync(1);
            Assert.NotNull(updatedProduct);
            Assert.Equal("New Name", updatedProduct.Name);
            Assert.Equal(20.00m, updatedProduct.Price);
        }

        #endregion

        #region DeleteAsync Tests

        [Fact]
        public async Task DeleteAsync_WhenProductExists_RemovesFromDatabase()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var product = new Product { Id = 1, Name = "To Be Deleted", Price = 10.00m, StockQuantity = 5 };
            context.Products.Add(product);
            await context.SaveChangesAsync();

            var service = new ProductService(context, _mockMapper.Object);

            // Act
            await service.DeleteAsync(1);

            // Assert
            Assert.Equal(0, await context.Products.CountAsync()); // Verifies the DB is now empty
        }

        #endregion
    }
}
