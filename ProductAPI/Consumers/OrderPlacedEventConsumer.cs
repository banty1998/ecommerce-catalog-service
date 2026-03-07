using MassTransit;
using ProductAPI.Data;
using Shared.IntegrationEvents;

namespace ProductAPI.Consumers
{
    public class OrderPlacedEventConsumer : IConsumer<OrderPlacedEvent>
    {
        private readonly AppDbContext _context;

        public OrderPlacedEventConsumer(AppDbContext context)
        {
            _context = context;
        }

        public async Task Consume(ConsumeContext<OrderPlacedEvent> context)
        {
            var message = context.Message;
            var product = await _context.Products.FindAsync(message.ProductId);

            if (product != null)
            {
                product.StockQuantity -= message.QuantityDeducted;
                await _context.SaveChangesAsync();
            }
        }
    }
}