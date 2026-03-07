using Microsoft.AspNetCore.Mvc;
using Moq;
using OrderAPI.Controllers;
using OrderAPI.DTOs;
using OrderAPI.Services.IServices;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderAPI.Tests.Controllers
{
    public class OrdersControllerTests
    {
        private readonly Mock<IOrderService> _mockService;
        private readonly OrdersController _controller;

        public OrdersControllerTests()
        {
            _mockService = new Mock<IOrderService>();
            _controller = new OrdersController(_mockService.Object);
        }

        [Fact]
        public async Task PlaceOrder_WhenValid_ReturnsCreatedAtActionResult()
        {
            // Arrange
            var createDto = new OrderCreateDto { ProductId = 1, Quantity = 2 };
            var responseDto = new OrderResponseDto { Id = 100, ProductId = 1, Quantity = 2 };

            _mockService.Setup(s => s.PlaceOrderAsync(createDto)).ReturnsAsync(responseDto);

            // Act
            var result = await _controller.PlaceOrder(createDto);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(nameof(_controller.GetOrderById), createdResult.ActionName);
            Assert.Equal(responseDto, createdResult.Value);
        }

        [Fact]
        public async Task GetOrderById_WhenExists_ReturnsOkResult()
        {
            // Arrange
            int orderId = 1;
            var responseDto = new OrderResponseDto { Id = orderId, ProductId = 1, Quantity = 2 };
            _mockService.Setup(s => s.GetOrderByIdAsync(orderId)).ReturnsAsync(responseDto);

            // Act
            var result = await _controller.GetOrderById(orderId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(responseDto, okResult.Value);
        }

        [Fact]
        public async Task GetOrderById_WhenDoesNotExist_ReturnsNotFound()
        {
            // Arrange
            int orderId = 99;
            _mockService.Setup(s => s.GetOrderByIdAsync(orderId)).ReturnsAsync((OrderResponseDto)null!);

            // Act
            var result = await _controller.GetOrderById(orderId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }
    }
}
