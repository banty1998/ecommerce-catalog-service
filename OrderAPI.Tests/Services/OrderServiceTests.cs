using AutoMapper;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using OrderAPI.Data;
using OrderAPI.DTOs;
using OrderAPI.HttpClients;
using OrderAPI.Models;
using OrderAPI.Services;
using Shared.IntegrationEvents;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderAPI.Tests.Services
{
    public class OrderServiceTests
    {
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<IProductApiClient> _mockProductClient;
        private readonly Mock<ILogger<OrderService>> _mockLogger;
        private readonly Mock<IPublishEndpoint> _mockPublishEndpoint;

        public OrderServiceTests()
        {
            _mockMapper = new Mock<IMapper>();
            _mockProductClient = new Mock<IProductApiClient>();
            _mockLogger = new Mock<ILogger<OrderService>>();
            _mockPublishEndpoint = new Mock<IPublishEndpoint>();
        }

        private AppDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        [Fact]
        public async Task PlaceOrderAsync_WhenProductNotFound_ThrowsException()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var service = new OrderService(context, _mockMapper.Object, _mockProductClient.Object, _mockLogger.Object, _mockPublishEndpoint.Object);
            var orderDto = new OrderCreateDto { ProductId = 1, Quantity = 2 };

            // Simulate the HTTP client returning null (product doesn't exist)
            _mockProductClient.Setup(c => c.GetProductByIdAsync(orderDto.ProductId)).ReturnsAsync((ProductDto)null!);
            // Act & Assert
            var ex = await Assert.ThrowsAsync<Exception>(() => service.PlaceOrderAsync(orderDto));
            Assert.Contains("does not exist", ex.Message);

            // Verify MassTransit was NEVER called
            _mockPublishEndpoint.Verify(p => p.Publish(It.IsAny<OrderPlacedEvent>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task PlaceOrderAsync_WhenInsufficientStock_ThrowsException()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var service = new OrderService(context, _mockMapper.Object, _mockProductClient.Object, _mockLogger.Object, _mockPublishEndpoint.Object);
            var orderDto = new OrderCreateDto { ProductId = 1, Quantity = 10 }; // Ordering 10

            // Simulate the HTTP client returning a product with only 5 in stock
            var fakeProduct = new ProductDto { Id = 1, StockQuantity = 5 }; // Replace with your actual product DTO type
            _mockProductClient.Setup(c => c.GetProductByIdAsync(orderDto.ProductId)).ReturnsAsync(fakeProduct);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<Exception>(() => service.PlaceOrderAsync(orderDto));
            Assert.Contains("Insufficient stock", ex.Message);
        }

        [Fact]
        public async Task PlaceOrderAsync_WhenValid_SavesToDbAndPublishesEvent()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var service = new OrderService(context, _mockMapper.Object, _mockProductClient.Object, _mockLogger.Object, _mockPublishEndpoint.Object);

            var orderDto = new OrderCreateDto { ProductId = 1, Quantity = 2 };

            // 1. Create the fake ProductDto that the client should return
            var fakeProduct = new ProductDto { Id = 1, StockQuantity = 10 };

            // 2. Setup Moq to return the fake product for ANY integer passed to it
            _mockProductClient
                .Setup(c => c.GetProductByIdAsync(It.IsAny<int>()))
                .ReturnsAsync(fakeProduct);

            var mappedOrder = new Order { Id = 100, ProductId = 1, Quantity = 2 };
            var responseDto = new OrderResponseDto { Id = 100, ProductId = 1, Quantity = 2 };

            _mockMapper.Setup(m => m.Map<Order>(orderDto)).Returns(mappedOrder);
            _mockMapper.Setup(m => m.Map<OrderResponseDto>(mappedOrder)).Returns(responseDto);

            // Act
            var result = await service.PlaceOrderAsync(orderDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(100, result.Id);
            Assert.Equal(1, await context.Orders.CountAsync());

            // Verify MassTransit published the event with the correct QuantityDeducted property
            _mockPublishEndpoint.Verify(p => p.Publish(It.Is<OrderPlacedEvent>(e => e.ProductId == 1 && e.QuantityDeducted == 2), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
