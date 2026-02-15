namespace OrderService.Events
{
    public class OrderCreatedEvent
    {
        public Guid EventId { get; set; } = Guid.NewGuid();  // For idempotency
        public int OrderId { get; set; }
        public int ProductId { get; set; }                   // IMPORTANT
        public int Quantity { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
