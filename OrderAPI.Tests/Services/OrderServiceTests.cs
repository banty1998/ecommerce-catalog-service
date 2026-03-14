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

            // UPDATED TO USE ITEMS LIST
            var orderDto = new OrderCreateDto
            {
                Items = new List<OrderItemDto> { new OrderItemDto { ProductId = 1, Quantity = 2 } },
                GrandTotal = 50
            };

            _mockProductClient.Setup(c => c.GetProductByIdAsync(1)).ReturnsAsync((ProductDto)null!);

            // Act & Assert - Added Fake User ID
            var ex = await Assert.ThrowsAsync<Exception>(() => service.PlaceOrderAsync(orderDto, "test-user"));
            Assert.Contains("does not exist", ex.Message);

            _mockPublishEndpoint.Verify(p => p.Publish(It.IsAny<OrderPlacedEvent>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task PlaceOrderAsync_WhenInsufficientStock_ThrowsException()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var service = new OrderService(context, _mockMapper.Object, _mockProductClient.Object, _mockLogger.Object, _mockPublishEndpoint.Object);

            var orderDto = new OrderCreateDto
            {
                Items = new List<OrderItemDto> { new OrderItemDto { ProductId = 1, Quantity = 10 } },
                GrandTotal = 50
            };

            var fakeProduct = new ProductDto { Id = 1, StockQuantity = 5 };
            _mockProductClient.Setup(c => c.GetProductByIdAsync(1)).ReturnsAsync(fakeProduct);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<Exception>(() => service.PlaceOrderAsync(orderDto, "test-user"));
            Assert.Contains("Insufficient stock", ex.Message);
        }

        [Fact]
        public async Task PlaceOrderAsync_WhenValid_SavesToDbAndPublishesEvent()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var service = new OrderService(context, _mockMapper.Object, _mockProductClient.Object, _mockLogger.Object, _mockPublishEndpoint.Object);

            var orderDto = new OrderCreateDto
            {
                Items = new List<OrderItemDto> { new OrderItemDto { ProductId = 1, Quantity = 2 } },
                GrandTotal = 50
            };

            var fakeProduct = new ProductDto { Id = 1, StockQuantity = 10 };

            _mockProductClient
                .Setup(c => c.GetProductByIdAsync(It.IsAny<int>()))
                .ReturnsAsync(fakeProduct);

            // UPDATED MODELS TO USE ITEMS AND USERID
            var mappedOrder = new Order
            {
                Id = 100,
                UserId = "test-user",
                Items = new List<OrderItem> { new OrderItem { ProductId = 1, Quantity = 2 } }
            };
            var responseDto = new OrderResponseDto
            {
                Id = 100,
                UserId = "test-user",
                Items = new List<OrderItemDto> { new OrderItemDto { ProductId = 1, Quantity = 2 } }
            };

            _mockMapper.Setup(m => m.Map<Order>(orderDto)).Returns(mappedOrder);
            _mockMapper.Setup(m => m.Map<OrderResponseDto>(mappedOrder)).Returns(responseDto);

            // Act
            var result = await service.PlaceOrderAsync(orderDto, "test-user");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(100, result.Id);
            Assert.Equal(1, await context.Orders.CountAsync());

            // Verify MassTransit published the event (assuming property is Quantity in your event, adjust if it's QuantityDeducted)
            _mockPublishEndpoint.Verify(p => p.Publish(It.Is<OrderPlacedEvent>(e => e.ProductId == 1), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
