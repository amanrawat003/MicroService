using Microsoft.AspNetCore.Connections;
using Microsoft.EntityFrameworkCore;
using ProductService.Data;
using ProductService.Events;
using ProductService.Models;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace ProductService.Consumer
{
    public class OrderCreatedConsumer : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public OrderCreatedConsumer(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            IConnection? connection = null;
            IModel? channel = null;

            var factory = new ConnectionFactory()
            {
                HostName = "rabbitmq"
            };

            // Retry until RabbitMQ is ready
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    connection = factory.CreateConnection();
                    channel = connection.CreateModel();
                    Console.WriteLine("Connected to RabbitMQ (ProductService).");
                    break;
                }
                catch
                {
                    Console.WriteLine("RabbitMQ not ready (ProductService), retrying in 5 seconds...");
                    await Task.Delay(5000, stoppingToken);
                }
            }

            if (channel == null)
                return;

            // Durable exchange
            channel.ExchangeDeclare(
                exchange: "order-exchange",
                type: ExchangeType.Topic,
                durable: true);

            // Durable named queue
            channel.QueueDeclare(
                queue: "product-order-created-queue",
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            channel.QueueBind(
                queue: "product-order-created-queue",
                exchange: "order-exchange",
                routingKey: "order.created");

            var consumer = new EventingBasicConsumer(channel);

            consumer.Received += async (sender, e) =>
            {
                var message = Encoding.UTF8.GetString(e.Body.ToArray());
                Console.WriteLine($"[ProductService] Event received: {message}");

                var orderEvent = JsonSerializer.Deserialize<OrderCreatedEvent>(message);

                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ProductDbContext>();

                var product = await db.Products
                    .FirstOrDefaultAsync(p => p.Id == orderEvent.ProductId);

                if (product == null)
                {
                    Console.WriteLine("Product not found.");
                    //channel.BasicAck(e.DeliveryTag, false);
                    //return;

                    PublishOrderFailed(channel, orderEvent.OrderId, "Product not found");

                    channel.BasicAck(e.DeliveryTag, false);
                    return;
                }

                if (product.Stock < orderEvent.Quantity)
                {
                    Console.WriteLine("Insufficient stock.");
                    //channel.BasicAck(e.DeliveryTag, false);
                    //return;

                    PublishOrderFailed(channel, orderEvent.OrderId, "Insufficient stock");

                    channel.BasicAck(e.DeliveryTag, false);
                    return;
                }

                product.Stock -= orderEvent.Quantity;
                await db.SaveChangesAsync();

                Console.WriteLine("Stock updated successfully.");
                PublishStockReduced(channel, orderEvent.OrderId);

                channel.BasicAck(e.DeliveryTag, false);
            };

            channel.BasicConsume(
                queue: "product-order-created-queue",
                autoAck: false,
                consumer: consumer);

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        private void PublishStockReduced(IModel channel, int orderId)
        {
            var stockReducedEvent = new StockReducedEvent
            {
                OrderId = orderId
            };

            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(stockReducedEvent));

            channel.BasicPublish(
                exchange: "order-exchange",
                routingKey: "order.stockreduced",
                basicProperties: null,
                body: body);

            Console.WriteLine($"Published StockReduced for Order {orderId}");
        }

        private void PublishOrderFailed(IModel channel, int orderId, string reason)
        {
            var failedEvent = new OrderFailedEvent
            {
                OrderId = orderId,
                Reason = reason
            };

            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(failedEvent));

            channel.BasicPublish(
                exchange: "order-exchange",
                routingKey: "order.failed",
                basicProperties: null,
                body: body);

            Console.WriteLine($"Published OrderFailed for Order {orderId}");
        }



    }


}
