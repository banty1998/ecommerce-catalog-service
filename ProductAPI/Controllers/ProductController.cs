using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using ProductAPI.Interface;
using ProductAPI.Models.Pagination;
using System.Text.Json;
using static ProductAPI.Models.DTO.ProductDTO;

namespace ProductAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly IProductServices _productService;
        private readonly IDistributedCache _cache; // 1. Add Cache Field

        // 2. Inject IDistributedCache via constructor
        public ProductController(IProductServices productServices, IDistributedCache cache)
        {
            _productService = productServices;
            _cache = cache;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] PaginationQuery query)
        {
            // Create a unique cache key for this specific page and size
            string cacheKey = $"products_page_{query.PageNumber}_size_{query.PageSize}";

            // Check if we have this page in Redis
            var cachedProducts = await _cache.GetStringAsync(cacheKey);

            if (!string.IsNullOrEmpty(cachedProducts))
            {
                // CACHE HIT! Return instantly without touching the SQL database
                return Ok(JsonSerializer.Deserialize<object>(cachedProducts));
            }

            // CACHE MISS! Fetch from SQL database
            var paginatedProducts = await _productService.GetAllAsync(query);

            // Store in Redis for future requests (Expires in 5 minutes)
            var cacheOptions = new DistributedCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(5));

            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(paginatedProducts), cacheOptions);

            return Ok(paginatedProducts);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            string cacheKey = $"product_{id}";
            var cachedProduct = await _cache.GetStringAsync(cacheKey);

            if (!string.IsNullOrEmpty(cachedProduct))
            {
                // CACHE HIT!
                return Ok(JsonSerializer.Deserialize<object>(cachedProduct));
            }

            // CACHE MISS!
            var product = await _productService.GetByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            var cacheOptions = new DistributedCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(10));

            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(product), cacheOptions);

            return Ok(product);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateProduct(CreateProductDTO dto)
        {
            var createdProduct = await _productService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = createdProduct.Id }, createdProduct);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateProduct(int id, UpdateProductDTO dto)
        {
            try
            {
                await _productService.UpdateAsync(id, dto);

                // CACHE INVALIDATION: Remove the stale product from Redis
                await _cache.RemoveAsync($"product_{id}");

                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            try
            {
                await _productService.DeleteAsync(id);

                // CACHE INVALIDATION: Remove the deleted product from Redis
                await _cache.RemoveAsync($"product_{id}");

                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }
    }
}
