using AutoMapper;
using Microsoft.EntityFrameworkCore;
using ProductAPI.Data;
using ProductAPI.Interface;
using ProductAPI.Models;
using ProductAPI.Models.DTO;
using ProductAPI.Models.Pagination;
using static ProductAPI.Models.DTO.ProductDTO;

namespace ProductAPI.Services
{
    public class ProductService : IProductServices
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        public ProductService(AppDbContext context, IMapper mapper) 
        {
            _context = context;
            _mapper = mapper;
        }
        public async Task<Product> CreateAsync(CreateProductDTO dto)
        {
            var product = _mapper.Map<Product>(dto);
            await _context.Products.AddAsync(product);
            await _context.SaveChangesAsync();
            return product;
        }

        public async Task DeleteAsync(int id)
        {
            var product = await _context.Products.FindAsync(id);

            if(product != null)
            {
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<PaginatedResult<Product>> GetAllAsync(PaginationQuery query)
        {
            var productsQuery = _context.Products.AsQueryable();
            if(!string.IsNullOrEmpty(query.SearchTerm))
            {
                productsQuery = productsQuery.Where(p => p.Name.Contains(query.SearchTerm));
            }

            if(!string.IsNullOrWhiteSpace(query.SortColumn))
            {
                productsQuery = query.SortColumn.ToLower() switch
                {
                    "name" => query.SortDescending 
                    ? productsQuery.OrderByDescending(p => p.Name) 
                    : productsQuery.OrderBy(p => p.Name),
                    "price" => query.SortDescending 
                    ? productsQuery.OrderByDescending(p => p.Price) 
                    : productsQuery.OrderBy(p => p.Price),
                    "stockquantity" => query.SortDescending 
                    ? productsQuery.OrderByDescending(p => p.StockQuantity) 
                    : productsQuery.OrderBy(p => p.StockQuantity),
                    _ => productsQuery.OrderBy(p => p.Id)
                };
            }
            else
            {
                productsQuery = productsQuery.OrderBy(p => p.Id);
            }

            var totalRecords = await productsQuery.CountAsync();

            var products = await productsQuery
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();

            return new PaginatedResult<Product>
            {
                Items = products,
                TotalCount = totalRecords,
                PageNumber = query.PageNumber,
                PageSize = query.PageSize
            };
        }

        public async Task<Product?> GetByIdAsync(int id)
        {
            return await _context.Products.FindAsync(id);
        }

        public async Task UpdateAsync(int id, UpdateProductDTO dto)
        {
            var product = await _context.Products.FindAsync(id);
            if(product == null)
            {
                throw new KeyNotFoundException("Product Not Found");
            }
            _mapper.Map(dto, product);
            await _context.SaveChangesAsync();
        }
    }
}
