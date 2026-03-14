using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using OrderAPI.Controllers;
using OrderAPI.DTOs;
using OrderAPI.Services.IServices;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;

namespace OrderAPI.Tests.Controllers
{
    public class OrdersControllerTests
    {
        private readonly Mock<IOrderService> _mockService;
        private readonly OrdersController _controller;
        private readonly string _fakeUserId = "test-user-123";

        public OrdersControllerTests()
        {
            _mockService = new Mock<IOrderService>();
            _controller = new OrdersController(_mockService.Object);

            // NEW: We must mock a logged-in user so User.FindFirstValue() works!
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, _fakeUserId),
            }, "mock"));

            _controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = user }
            };
        }

        [Fact]
        public async Task PlaceOrder_WhenValid_ReturnsCreatedAtActionResult()
        {
            // Arrange
            var createDto = new OrderCreateDto
            {
                Items = new List<OrderItemDto> { new OrderItemDto { ProductId = 1, Quantity = 2 } }
            };
            var responseDto = new OrderResponseDto
            {
                Id = 100,
                UserId = _fakeUserId,
                Items = new List<OrderItemDto> { new OrderItemDto { ProductId = 1, Quantity = 2 } }
            };

            // Setup the mock to expect the fakeUserId we injected above
            _mockService.Setup(s => s.PlaceOrderAsync(createDto, _fakeUserId)).ReturnsAsync(responseDto);

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
            var responseDto = new OrderResponseDto
            {
                Id = orderId,
                UserId = _fakeUserId,
                Items = new List<OrderItemDto> { new OrderItemDto { ProductId = 1, Quantity = 2 } }
            };
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
