using MassTransit;
using Microsoft.Extensions.Caching.Distributed;
using ProductAPI.Data;
using Shared.IntegrationEvents;

namespace ProductAPI.Consumers
{
    public class OrderPlacedEventConsumer : IConsumer<OrderPlacedEvent>
    {
        private readonly AppDbContext _context;
        private readonly IDistributedCache _cache; // 1. Inject the Redis Cache

        public OrderPlacedEventConsumer(AppDbContext context, IDistributedCache cache)
        {
            _context = context;
            _cache = cache;
        }

        public async Task Consume(ConsumeContext<OrderPlacedEvent> context)
        {
            var message = context.Message;
            var product = await _context.Products.FindAsync(message.ProductId);

            if (product != null)
            {
                // 2. Deduct the stock in SQL
                product.StockQuantity -= message.QuantityDeducted;

                // Safety net to prevent negative inventory
                if (product.StockQuantity < 0) product.StockQuantity = 0;

                await _context.SaveChangesAsync();

                // 3. CACHE INVALIDATION: Remove the single item cache
                await _cache.RemoveAsync($"product_{message.ProductId}");

                // 4. CACHE INVALIDATION: Wipe the paginated list cache (Matching your Controller)
                // Note: If your Angular UI requests a different size (like size 5 or 20), 
                // you must ensure the size number matches here!
                for (int i = 1; i <= 10; i++)
                {
                    await _cache.RemoveAsync($"products_page_{i}_size_10");
                }
            }
        }
    }
}