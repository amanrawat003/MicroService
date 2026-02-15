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

        // 🔁 Retry until RabbitMQ is ready
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                connection = factory.CreateConnection();
                channel = connection.CreateModel();
                Console.WriteLine("Connected to RabbitMQ.");
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

        channel.ExchangeDeclare("order-exchange", ExchangeType.Fanout);
        var queue = channel.QueueDeclare().QueueName;

        channel.QueueBind(queue, "order-exchange", "");

        var consumer = new EventingBasicConsumer(channel);

        consumer.Received += (sender, e) =>
        {
            var message = Encoding.UTF8.GetString(e.Body.ToArray());
            Console.WriteLine($"[NotificationService] Order event received: {message}");
        };

        channel.BasicConsume(queue, true, consumer);

        // 🔥 Keep the service alive
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

}
