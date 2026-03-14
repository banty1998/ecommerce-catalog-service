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
            ILogger<OrderService> logger,
            IPublishEndpoint publishEndpoint)
        {
            _context = context;
            _mapper = mapper;
            _productClient = productClient;
            _logger = logger;
            _publishEndpoint = publishEndpoint;
        }

        public async Task<OrderResponseDto> PlaceOrderAsync(OrderCreateDto orderDto, string userId)
        {
            _logger.LogInformation("Attempting to place order for UserId: {UserId} with {Count} items", userId, orderDto.Items.Count);

            // 1. All-or-Nothing Validation Loop
            foreach (var item in orderDto.Items)
            {
                var product = await _productClient.GetProductByIdAsync(item.ProductId);

                if (product == null)
                {
                    _logger.LogWarning("Order failed. ProductId {ProductId} was not found.", item.ProductId);
                    throw new Exception($"Product with ID {item.ProductId} does not exist.");
                }

                if (product.StockQuantity < item.Quantity)
                {
                    _logger.LogWarning("Order failed. Insufficient stock for ProductId {ProductId}. Requested: {RequestedQuantity}, Available: {AvailableQuantity}",
                        item.ProductId, item.Quantity, product.StockQuantity);
                    throw new Exception($"Insufficient stock available for Product ID {item.ProductId}.");
                }
            }

            // 2. Map and save the order
            var order = _mapper.Map<Order>(orderDto);
            order.UserId = userId; // Attach to the user!
            order.OrderDate = DateTime.UtcNow;

            try
            {
                _context.Orders.Add(order);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Successfully placed OrderId: {OrderId} for UserId: {UserId}", order.Id, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while saving the order to the database.");
                throw;
            }

            // 3. Publish an inventory deduction event for EVERY item in the cart
            foreach (var item in order.Items)
            {
                await _publishEndpoint.Publish(new OrderPlacedEvent(item.ProductId, item.Quantity));
                _logger.LogInformation("Published OrderPlacedEvent for ProductId: {ProductId}", item.ProductId);
            }

            return _mapper.Map<OrderResponseDto>(order);
        }

        public async Task<OrderResponseDto?> GetOrderByIdAsync(int id)
        {
            _logger.LogInformation("Retrieving order with ID: {OrderId}", id);

            var order = await _context.Orders
                                      .Include(o => o.Items) // Ensure Items are loaded
                                      .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                _logger.LogWarning("Order with ID: {OrderId} was not found.", id);
                return null;
            }

            return _mapper.Map<OrderResponseDto>(order);
        }

        public async Task<IEnumerable<OrderResponseDto>> GetAllOrdersAsync()
        {
            _logger.LogInformation("Retrieving all orders.");

            var orders = await _context.Orders
                                       .Include(o => o.Items) // Ensure Items are loaded
                                       .ToListAsync();

            return _mapper.Map<IEnumerable<OrderResponseDto>>(orders);
        }

        public async Task<IEnumerable<OrderResponseDto>> GetOrdersByUserIdAsync(string userId)
        {
            _logger.LogInformation("Retrieving orders for UserId: {UserId}", userId);

            var orders = await _context.Orders
                                       .Include(o => o.Items) // Ensure Items are loaded
                                       .Where(o => o.UserId == userId)
                                       .OrderByDescending(o => o.OrderDate)
                                       .ToListAsync();

            return _mapper.Map<IEnumerable<OrderResponseDto>>(orders);
        }
    }
}
