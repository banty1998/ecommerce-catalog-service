namespace OrderAPI.HttpClients
{
    public interface IProductApiClient
    {
        Task<ProductDto?> GetProductByIdAsync(int productId);
    }
}
