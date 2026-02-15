using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

public class OrderCreatedConsumer : BackgroundService
{
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
                Console.WriteLine("Connected to RabbitMQ (NotificationService).");
                break;
            }
            catch
            {
                Console.WriteLine("RabbitMQ not ready, retrying in 5 seconds...");
                await Task.Delay(5000, stoppingToken);
            }
        }

        if (channel == null)
            return;

        // Durable exchange (MUST MATCH other services)
        channel.ExchangeDeclare(
            exchange: "order-exchange",
            type: ExchangeType.Topic,
            durable: true);

        // Durable queue for notifications
        channel.QueueDeclare(
            queue: "notification-order-created-queue",
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        channel.QueueBind(
            queue: "notification-order-created-queue",
            exchange: "order-exchange",
            routingKey: "order.*");

        var consumer = new EventingBasicConsumer(channel);

        consumer.Received += (sender, e) =>
        {
            var message = Encoding.UTF8.GetString(e.Body.ToArray());
            Console.WriteLine($"[NotificationService] Order event received: {message}");

            channel.BasicAck(e.DeliveryTag, false);
        };

        channel.BasicConsume(
            queue: "notification-order-created-queue",
            autoAck: false,
            consumer: consumer);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}
