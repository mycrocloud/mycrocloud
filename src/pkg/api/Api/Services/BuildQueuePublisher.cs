using RabbitMQ.Client;
using System.Text;

namespace Api.Services
{
    public class RabbitMqConnectionFactory : IDisposable
    {
        private readonly Lazy<IConnection> _connection;

        public RabbitMqConnectionFactory(IConfiguration configuration)
        {
            _connection = new Lazy<IConnection>(() =>
            {
                var factory = new ConnectionFactory
                {
                    Uri = new Uri(configuration.GetConnectionString("RabbitMq")!),
                };
                return factory.CreateConnection();
            });
        }

        public IModel CreateChannel() => _connection.Value.CreateModel();

        public void Dispose()
        {
            if (_connection.IsValueCreated)
            {
                _connection.Value.Close();
                _connection.Value.Dispose();
            }
        }
    }

    public class BuildQueuePublisher : IDisposable
    {
        private const string BuildQueueName = "build_queue";
        private readonly Lazy<IModel> _channel;
        private readonly ILogger<BuildQueuePublisher> _logger;

        public BuildQueuePublisher(RabbitMqConnectionFactory connectionFactory, ILogger<BuildQueuePublisher> logger)
        {
            _logger = logger;
            _channel = new Lazy<IModel>(connectionFactory.CreateChannel);
        }

        public void PublishMessage(string message)
        {
            var channel = _channel.Value;

            channel.QueueDeclare(queue: BuildQueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            var body = Encoding.UTF8.GetBytes(message);

            channel.BasicPublish(exchange: "",
                routingKey: BuildQueueName,
                basicProperties: null,
                body: body);

            _logger.LogDebug("Sent message to build_queue: {Message}", message);
        }

        public void Dispose()
        {
            if (_channel.IsValueCreated)
            {
                _channel.Value.Close();
                _channel.Value.Dispose();
            }
        }
    }
}
