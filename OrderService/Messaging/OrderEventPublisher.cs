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

            channel.ExchangeDeclare("order-exchange", ExchangeType.Fanout);

            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(evt));

            channel.BasicPublish(
                exchange: "order-exchange",
                routingKey: "",
                basicProperties: null,
                body: body
            );
        }
    }
}
