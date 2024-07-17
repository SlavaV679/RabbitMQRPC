using Microsoft.Extensions.Options;
using ProjectName.Domain.Options;
using RabbitMQ.Client;
using System.Text;

namespace Runpay.Payouts.FakePublisher.Helpers
{
    public class RabbitMqPublisher
    {
        private readonly RabbitMqOptions _rabbitMqOptions;

        public RabbitMqPublisher(IOptions<RabbitMqOptions> rabbitMqOptions)
        {
            _rabbitMqOptions = rabbitMqOptions.Value;
        }

        public void SendMessage(string message)
        {
            var factory = new ConnectionFactory
            {
                UserName = _rabbitMqOptions.UserName,
                Password = _rabbitMqOptions.Password,
                VirtualHost = _rabbitMqOptions.VirtualHost
            };

            var connection = factory.CreateConnection(_rabbitMqOptions.ServerHostNames);
            using var channel = connection.CreateModel();

            channel.ExchangeDeclare(exchange: _rabbitMqOptions.ExchangeName, type: ExchangeType.Direct);

            var body = Encoding.UTF8.GetBytes(message);

            var props = channel.CreateBasicProperties();
            props.Persistent = true;// or props.DeliveryMode = 2;

            channel.BasicPublish(exchange: _rabbitMqOptions.ExchangeName,
                                routingKey: _rabbitMqOptions.RoutingKey,
                                basicProperties: props,
                                body: body);
        }
    }
}
