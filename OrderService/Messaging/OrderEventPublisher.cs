using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using OrderService.Events;

namespace OrderService.Messaging
{
    public class OrderEventPublisher
    {
        public void PublishOrderCreated(OrderCreatedEvent evt)
        {
            var factory = new ConnectionFactory
            {
                HostName = "rabbitmq"
            };

            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();

            // Durable exchange (IMPORTANT)
            channel.ExchangeDeclare(
                exchange: "order-exchange",
                type: ExchangeType.Topic,
                durable: true);

            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(evt));

            channel.BasicPublish(
                exchange: "order-exchange",
                routingKey: "order.created",
                basicProperties: null,
                body: body
            );

            Console.WriteLine("OrderCreated event published.");
        }
    }
}
