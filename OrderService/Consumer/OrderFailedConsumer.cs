namespace OrderService.Consumer
{
    using global::OrderService.Data;
    using global::OrderService.Events;
    using global::OrderService.Models;
    using Microsoft.EntityFrameworkCore;

    using RabbitMQ.Client;
    using RabbitMQ.Client.Events;
    using System.Text;
    using System.Text.Json;

    namespace OrderService.Consumer
    {
        public class OrderFailedConsumer : BackgroundService
        {
            private readonly IServiceScopeFactory _scopeFactory;

            public OrderFailedConsumer(IServiceScopeFactory scopeFactory)
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

                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        connection = factory.CreateConnection();
                        channel = connection.CreateModel();
                        Console.WriteLine("Connected to RabbitMQ (OrderService - OrderFailed).");
                        break;
                    }
                    catch
                    {
                        Console.WriteLine("RabbitMQ not ready (OrderService), retrying...");
                        await Task.Delay(5000, stoppingToken);
                    }
                }

                if (channel == null) return;

                channel.ExchangeDeclare(
                    exchange: "order-exchange",
                    type: ExchangeType.Topic,
                    durable: true);

                channel.QueueDeclare(
                    queue: "order-failed-queue",
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);

                channel.QueueBind(
                    queue: "order-failed-queue",
                    exchange: "order-exchange",
                    routingKey: "order.failed");

                var consumer = new EventingBasicConsumer(channel);

                consumer.Received += async (sender, e) =>
                {
                    try
                    {
                        var message = Encoding.UTF8.GetString(e.Body.ToArray());
                        var failedEvent = JsonSerializer.Deserialize<OrderFailedEvent>(message);

                        using var scope = _scopeFactory.CreateScope();
                        var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();

                        var order = await db.Orders
                            .FirstOrDefaultAsync(o => o.Id == failedEvent.OrderId);

                        if (order != null)
                        {
                            order.Status = OrderStatus.Cancelled;
                            await db.SaveChangesAsync();
                            Console.WriteLine($"Order {order.Id} marked Cancelled.");
                        }

                        channel.BasicAck(e.DeliveryTag, false);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error: {ex.Message}");
                        channel.BasicNack(e.DeliveryTag, false, true);
                    }
                };

                channel.BasicConsume(
                    queue: "order-failed-queue",
                    autoAck: false,
                    consumer: consumer);

                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
        }
    }

}
