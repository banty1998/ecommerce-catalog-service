namespace OrderAPI.HttpClients
{
    public class ProductApiClient : IProductApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ProductApiClient> _logger;

        public ProductApiClient(HttpClient httpClient, ILogger<ProductApiClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<ProductDto?> GetProductByIdAsync(int productId)
        {
            _logger.LogInformation("Attempting to fetch product {ProductId} from ProductAPI...", productId);

            var response = await _httpClient.GetAsync($"/api/product/{productId}");

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully retrieved product {ProductId}.", productId);
                return await response.Content.ReadFromJsonAsync<ProductDto>();
            }

            _logger.LogWarning("ProductAPI returned status {StatusCode} for product {ProductId}.", response.StatusCode, productId);
            return null;
        }
    }
}
