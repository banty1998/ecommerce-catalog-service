using ProductAPI.Models;
using ProductAPI.Models.Pagination;
using static ProductAPI.Models.DTO.ProductDTO;

namespace ProductAPI.Interface
{
    public interface IProductServices
    {
        Task<PaginatedResult<Product>> GetAllAsync(PaginationQuery query);
        Task<Product?> GetByIdAsync(int id);
        Task<Product> CreateAsync(CreateProductDTO dto);
        Task UpdateAsync(int id, UpdateProductDTO product);
        Task DeleteAsync(int id);

    }
}
