using OrderAPI.DTOs;

namespace OrderAPI.Services.IServices
{
    public interface IOrderService
    {
        Task<OrderResponseDto> PlaceOrderAsync(OrderCreateDto orderDto);
        Task<OrderResponseDto?> GetOrderByIdAsync(int id);
        Task<IEnumerable<OrderResponseDto>> GetAllOrdersAsync();
    }
}
