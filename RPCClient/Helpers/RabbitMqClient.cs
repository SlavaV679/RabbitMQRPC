using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using System.Collections.Concurrent;
using System.Text;
using Microsoft.Extensions.Options;
using ProjectName.Domain.Options;

namespace RPCClient.Helpers
{
    public class RabbitMqClient
    {
        private readonly RabbitMqOptions _rabbitMqOptions;

        private readonly IConnection connection;
        private readonly IModel channel;
        private readonly string replyQueueName;
        private readonly ConcurrentDictionary<string, TaskCompletionSource<string>> callbackMapper = new();

        public RabbitMqClient(IOptions<RabbitMqOptions> rabbitMqOptions)
        {
            _rabbitMqOptions = rabbitMqOptions.Value;
            var factory = new ConnectionFactory
            {
                UserName = _rabbitMqOptions.UserName,
                Password = _rabbitMqOptions.Password,
                VirtualHost = _rabbitMqOptions.VirtualHost
            };

            connection = factory.CreateConnection(_rabbitMqOptions.ServerHostNames);
            channel = connection.CreateModel();
            var properties = channel.CreateBasicProperties();
            properties.Expiration = "3000"; // Set the timeout value to 10 seconds (in milliseconds)

            // declare a server-named queue
            replyQueueName = channel.QueueDeclare(
                //queue: "_rabbitMqOptions.QueueName"
                //durable: true,
                //exclusive: false,
                //autoDelete: false,
                //arguments: null
                ).QueueName;
            var consumer = new EventingBasicConsumer(channel);
            string response = null;
            consumer.Received += (model, ea) =>
            {
                if (!callbackMapper.TryRemove(ea.BasicProperties.CorrelationId, out var tcs))
                    return;
                var body = ea.Body.ToArray();
                response = Encoding.UTF8.GetString(body);
                tcs.TrySetResult(response);
            };

            channel.BasicConsume(consumer: consumer,
                                 queue: replyQueueName,
                                 autoAck: true);


            //channel.ExchangeDeclare(exchange: _rabbitMqOptions.ExchangeName, type: ExchangeType.Direct);

            //var body = Encoding.UTF8.GetBytes(message);

            //var props = channel.CreateBasicProperties();
            //props.Persistent = true;// or props.DeliveryMode = 2;

            //channel.BasicPublish(exchange: _rabbitMqOptions.ExchangeName,
            //                    routingKey: _rabbitMqOptions.RoutingKey,
            //                    basicProperties: props,
            //                    body: body);
        }    

        public Task<string> CallAsync(string message, CancellationToken cancellationToken = default)
        {
            IBasicProperties props = channel.CreateBasicProperties();
            var correlationId = Guid.NewGuid().ToString();
            props.CorrelationId = correlationId;
            props.ReplyTo = replyQueueName;
            var messageBytes = Encoding.UTF8.GetBytes(message);
            var tcs = new TaskCompletionSource<string>();
            callbackMapper.TryAdd(correlationId, tcs);

            channel.BasicPublish(exchange: string.Empty,
                                 routingKey: _rabbitMqOptions.QueueName,
                                 basicProperties: props,
                                 body: messageBytes);

            cancellationToken.Register(() => callbackMapper.TryRemove(correlationId, out _));
            return tcs.Task;
        }

        public void Dispose()
        {
            // closing a connection will also close all channels on it
            connection.Close();
        }
    }
}
