using AutoMapper;
using ProductAPI.Models;
using static ProductAPI.Models.DTO.ProductDTO;

namespace ProductAPI.Mapping
{
    public class MappingProfiles : Profile
    {
        public MappingProfiles() 
        {
            CreateMap<CreateProductDTO, Product>();
            CreateMap<UpdateProductDTO, Product>();
        }
    }
}
