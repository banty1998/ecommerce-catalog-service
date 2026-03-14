using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OrderAPI.Models
{
    public class Order
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty; // Links order to the logged-in user

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal GrandTotal { get; set; }

        public DateTime OrderDate { get; set; }

        // Navigation property for Entity Framework
        public List<OrderItem> Items { get; set; } = new List<OrderItem>();
    }
}
