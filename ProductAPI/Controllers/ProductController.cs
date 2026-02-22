using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProductAPI.Interface;
using ProductAPI.Models.Pagination;
using static ProductAPI.Models.DTO.ProductDTO;

namespace ProductAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly IProductServices _productService;

        public ProductController(IProductServices productServices)
        {
            _productService = productServices;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] PaginationQuery query)
        {

            var paginatedProducts = await _productService.GetAllAsync(query);
            return Ok(paginatedProducts);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var product = await _productService.GetByIdAsync(id);
            if(product == null)
            {
                return NotFound();
            }
            return Ok(product);
        }
        
        [HttpPost]
        public async Task<IActionResult> CreateProduct(CreateProductDTO dto)
        {
            var createdProduct = await _productService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = createdProduct.Id }, createdProduct);
        }
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProduct(int id, UpdateProductDTO dto)
        {
            try
            {
                await _productService.UpdateAsync(id, dto);
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            try
            {
                await _productService.DeleteAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }
    }
}
