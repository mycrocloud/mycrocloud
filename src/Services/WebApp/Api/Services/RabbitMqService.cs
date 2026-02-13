using RabbitMQ.Client;
using System.Text;

namespace Api.Services
{
    public class RabbitMqService : IDisposable
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly ILogger<RabbitMqService> _logger;

        public RabbitMqService(IConfiguration configuration, ILogger<RabbitMqService> logger)
        {
            _logger = logger;

            var factory = new ConnectionFactory
            {
                Uri = new Uri(configuration.GetConnectionString("RabbitMq")!),
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            _channel.QueueDeclare(queue: "job_queue",
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);
        }

        public void PublishMessage(string message)
        {
            var body = Encoding.UTF8.GetBytes(message);

            _channel.BasicPublish(exchange: "",
                routingKey: "job_queue",
                basicProperties: null,
                body: body);

            _logger.LogDebug("Sent message to job_queue: {Message}", message);
        }

        /// <summary>
        /// Creates a new channel from the shared connection.
        /// Caller is responsible for closing the returned channel.
        /// </summary>
        public IModel CreateChannel() => _connection.CreateModel();

        public void Dispose()
        {
            _channel.Close();
            _channel.Dispose();
            _connection.Close();
            _connection.Dispose();
        }
    }
}
