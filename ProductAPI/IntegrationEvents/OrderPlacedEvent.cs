namespace Shared.IntegrationEvents
{
    // Notice we use the word 'record' instead of 'class'
    public record OrderPlacedEvent(int ProductId, int QuantityDeducted);
}