using AutoMapper;
using OrderAPI.DTOs;
using OrderAPI.Models;

namespace OrderAPI.Profiles
{
    public class OrderMappingProfile : Profile
    {
        public OrderMappingProfile()
        {
            // Main Order mapping
            CreateMap<OrderCreateDto, Order>();
            CreateMap<Order, OrderResponseDto>();

            // Nested Items mapping
            CreateMap<OrderItemDto, OrderItem>();
            CreateMap<OrderItem, OrderItemDto>();
        }
    }
}
