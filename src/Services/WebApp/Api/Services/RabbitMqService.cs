using RabbitMQ.Client;
using System.Text;

namespace Api.Services
{
    public class RabbitMqService
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;

        public RabbitMqService(IConfiguration configuration)
        {
            // Create a connection factory
            var factory = new ConnectionFactory
            {
                Uri = new Uri(configuration.GetConnectionString("RabbitMq")!),
            };

            // Create a connection and a channel
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            // Declare a queue (ensure the queue exists)
            _channel.QueueDeclare(queue: "job_queue", // Name of the queue
                durable: true, // Durable queue (persists)
                exclusive: false, // Not exclusive to one consumer
                autoDelete: false, // Do not auto-delete the queue
                arguments: null); // No additional arguments
        }

        // Method to publish a message to the RabbitMQ queue
        public void PublishMessage(string message)
        {
            var body = Encoding.UTF8.GetBytes(message);

            _channel.BasicPublish(exchange: "", // Default exchange
                routingKey: "job_queue", // Queue name
                basicProperties: null, // No custom properties
                body: body); // Message body

            Console.WriteLine($" [x] Sent '{message}'");
        }

        public void Close()
        {
            _channel.Close();
            _connection.Close();
        }
    }
}