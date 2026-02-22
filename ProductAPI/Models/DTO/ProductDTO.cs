using System.ComponentModel.DataAnnotations;

namespace ProductAPI.Models.DTO
{
    public class ProductDTO
    {
        public record CreateProductDTO(
            [Required(ErrorMessage = "Product name is required.")]
            [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
            string Name,
            [Required(ErrorMessage = "Price is required.")]
            [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0.")]
            decimal Price,
            [Required(ErrorMessage = "Stock quantity is required.")]
            [Range(0, int.MaxValue, ErrorMessage = "Stock quantity cannot be negative.")]
            int StockQuantity);
        public record UpdateProductDTO(
            [Required]
            int Id,
            [Required(ErrorMessage = "Product name is required.")]
            [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
            string Name,
            [Required(ErrorMessage = "Price is required.")]
            [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0.")]
            decimal Price,
            [Required(ErrorMessage = "Stock quantity is required.")]
            [Range(0, int.MaxValue, ErrorMessage = "Stock quantity cannot be negative.")]
            int StockQuantity);
    }
}
