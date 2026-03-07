using AutoMapper;
using OrderAPI.DTOs;
using OrderAPI.Models;

namespace OrderAPI.Profiles
{
    public class OrderMappingProfile : Profile
    {
        public OrderMappingProfile()
        {
            // Source -> Destination
            CreateMap<OrderCreateDto, Order>();
            CreateMap<Order, OrderResponseDto>();
        }
    }
}
