using AutoMapper;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using OrderAPI.Data;
using OrderAPI.DTOs;
using OrderAPI.HttpClients;
using OrderAPI.Models;
using OrderAPI.Services.IServices;
using Shared.IntegrationEvents;

namespace OrderAPI.Services
{
    public class OrderService : IOrderService
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly IProductApiClient _productClient;
        private readonly ILogger<OrderService> _logger;
        private readonly IPublishEndpoint _publishEndpoint;

        public OrderService(
            AppDbContext context,
            IMapper mapper,
            IProductApiClient productClient,
            ILogger<OrderService> logger, IPublishEndpoint publishEndpoint)
        {
            _context = context;
            _mapper = mapper;
            _productClient = productClient;
            _logger = logger;
            _publishEndpoint = publishEndpoint;
        }

        public async Task<OrderResponseDto> PlaceOrderAsync(OrderCreateDto orderDto)
        {
            _logger.LogInformation("Attempting to place order for ProductId: {ProductId} with Quantity: {Quantity}", orderDto.ProductId, orderDto.Quantity);

            // 1. Fetch product details from ProductAPI
            var product = await _productClient.GetProductByIdAsync(orderDto.ProductId);

            // 2. Validate product existence and stock
            if (product == null)
            {
                _logger.LogWarning("Order failed. ProductId {ProductId} was not found in the Catalog.", orderDto.ProductId);
                throw new Exception($"Product with ID {orderDto.ProductId} does not exist.");
            }

            if (product.StockQuantity < orderDto.Quantity)
            {
                _logger.LogWarning("Order failed. Insufficient stock for ProductId {ProductId}. Requested: {RequestedQuantity}, Available: {AvailableQuantity}",
                    orderDto.ProductId, orderDto.Quantity, product.StockQuantity);
                throw new Exception($"Insufficient stock available for Product ID {orderDto.ProductId}.");
            }

            // 3. Map and save the order
            var order = _mapper.Map<Order>(orderDto);
            order.OrderDate = DateTime.UtcNow;

            try
            {
                _context.Orders.Add(order);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Successfully placed OrderId: {OrderId} for ProductId: {ProductId}", order.Id, order.ProductId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while saving the order for ProductId: {ProductId} to the database.", orderDto.ProductId);
                throw;
            }

            await _publishEndpoint.Publish(new OrderPlacedEvent(order.ProductId, order.Quantity));
            _logger.LogInformation("Published OrderPlacedEvent to RabbitMQ for ProductId: {ProductId}", order.ProductId);

            return _mapper.Map<OrderResponseDto>(order);
        }

        public async Task<OrderResponseDto?> GetOrderByIdAsync(int id)
        {
            _logger.LogInformation("Retrieving order with ID: {OrderId}", id);
            var order = await _context.Orders.FindAsync(id);

            if (order == null)
            {
                _logger.LogWarning("Order with ID: {OrderId} was not found.", id);
                return null;
            }

            return _mapper.Map<OrderResponseDto>(order);
        }

        public async Task<IEnumerable<OrderResponseDto>> GetAllOrdersAsync()
        {
            _logger.LogInformation("Retrieving all orders from the database.");
            var orders = await _context.Orders.ToListAsync();
            _logger.LogInformation("Successfully retrieved {OrderCount} orders.", orders.Count);

            return _mapper.Map<IEnumerable<OrderResponseDto>>(orders);
        }
    }
}
