namespace OrderAPI.HttpClients
{
    public class ProductDto
    {
        public int Id { get; set; }
        public int StockQuantity { get; set; } // We only care about stock!
    }
}
