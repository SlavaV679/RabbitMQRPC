using Microsoft.Extensions.Options;
using ProjectName.Domain.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Threading.Channels;

namespace RPCServer
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly RabbitMqOptions _rabbitMqOptions;
        private readonly IModel _channel;
        private readonly string _queueName;

        public Worker(
            ILogger<Worker> logger,
            IOptions<RabbitMqOptions> rabbitMqOptions
            )
        {
            _logger = logger;
            _rabbitMqOptions = rabbitMqOptions.Value;

            var factory = new ConnectionFactory
            {
                UserName = _rabbitMqOptions.UserName,
                Password = _rabbitMqOptions.Password,
                VirtualHost = _rabbitMqOptions.VirtualHost,
                DispatchConsumersAsync = true
            };
            var connection = factory.CreateConnection(_rabbitMqOptions.ServerHostNames);
            _channel = connection.CreateModel();


            //_channel.ExchangeDeclare(exchange: _rabbitMqOptions.ExchangeName, type: ExchangeType.Direct);

            _queueName = _channel.QueueDeclare(
                queue: _rabbitMqOptions.QueueName,
                durable: false,//true,
                exclusive: false,
                autoDelete: false,
                arguments: null).QueueName;

            _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);
            //_channel.QueueBind(
            //    queue: _queueName,
            //    exchange: _rabbitMqOptions.ExchangeName,
            //    routingKey: _rabbitMqOptions.RoutingKey);



        }




        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();

            var consumer = new AsyncEventingBasicConsumer(_channel);
            _channel.BasicConsume(queue: _rabbitMqOptions.QueueName,
                                 autoAck: false,
                                 consumer: consumer);
            Console.WriteLine(" [x] Awaiting RPC requests");

            consumer.Received += async (model, ea) =>
            {
                string response = string.Empty;

                var body = ea.Body.ToArray();
                var props = ea.BasicProperties;
                var replyProps = _channel.CreateBasicProperties();
                replyProps.CorrelationId = props.CorrelationId;

                try
                {
                    var message = Encoding.UTF8.GetString(body);
                    int n = int.Parse(message);
                    Console.WriteLine($" [.] Fib({message})");
                    response = Fib(n).ToString();
                }
                catch (Exception e)
                {
                    Console.WriteLine($" [.] {e.Message}");
                    response = string.Empty;
                }
                finally
                {
                    var responseBytes = Encoding.UTF8.GetBytes(response);
                    _channel.BasicPublish(exchange: string.Empty,
                                         routingKey: props.ReplyTo,
                                         basicProperties: replyProps,
                                         body: responseBytes);
                    _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                }
            };

            Console.WriteLine(" Press [enter] to exit.");
            Console.ReadLine();

            // Assumes only valid positive integer input.
            // Don't expect this one to work for big numbers, and it's probably the slowest recursive implementation possible.
            static int Fib(int n)
            {
                if (n is 0 or 1)
                {
                    return n;
                }

                return Fib(n - 1) + Fib(n - 2);
            }

            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            }
             Task.Delay(1000, stoppingToken);

            return Task.CompletedTask;
        }
    }
}
