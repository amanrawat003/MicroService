namespace OrderService.Events
{
    public class OrderFailedEvent
    {
        public int OrderId { get; set; }
        public string Reason { get; set; }
    }

}
