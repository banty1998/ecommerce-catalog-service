using OrderAPI.DTOs;

namespace OrderAPI.Services.IServices
{
    public interface IOrderService
    {// Added userId parameter here
        Task<OrderResponseDto> PlaceOrderAsync(OrderCreateDto orderDto, string userId);

        Task<OrderResponseDto?> GetOrderByIdAsync(int id);
        Task<IEnumerable<OrderResponseDto>> GetAllOrdersAsync();

        // Added this new method for the My Orders page
        Task<IEnumerable<OrderResponseDto>> GetOrdersByUserIdAsync(string userId);
    }
}
