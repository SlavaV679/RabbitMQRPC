using Microsoft.Extensions.Options;
using ProjectName.Domain.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Threading;

namespace ProjectName.Service
{
    internal class Consumer : BackgroundService
    {
        private readonly ILogger<Consumer> _logger;
        private readonly RabbitMqOptions _rabbitMqOptions;
        private readonly IModel _channel;
        private readonly string _queueName;

        public Consumer(
            ILogger<Consumer> logger,
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

            _channel.ExchangeDeclare(exchange: _rabbitMqOptions.ExchangeName, type: ExchangeType.Direct);

            _queueName = _channel.QueueDeclare(
                queue: _rabbitMqOptions.QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null).QueueName;

            _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);
            _channel.QueueBind(
                queue: _queueName,
                exchange: _rabbitMqOptions.ExchangeName,
                routingKey: _rabbitMqOptions.RoutingKey);
        }

        protected override Task ExecuteAsync(CancellationToken cancellationToken)
        {

            cancellationToken.ThrowIfCancellationRequested();

            _logger.LogInformation($"ExecuteAsync of {nameof(Consumer)} started.");

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.Received += async (ch, ea) => //  (ch, ea)
            {
                var requestMessage = Encoding.UTF8.GetString(ea.Body.ToArray());
                _logger.LogInformation($"Request message: '{requestMessage}'");

                // work with received message
                try
                {
                    //await _paymentsLogic.MakePaymentAsync(requestMessage, _rabbitMqOptions.ActPaymentRoutingKey);

                    _channel.BasicAck(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    //HandleException(ex);
                    if (ea.Redelivered)
                    {
                        //var failedRequestId = _paymentsLogic.CreateFailedRequestAsync(requestMessage, _rabbitMqOptions.ActPaymentRoutingKey);

                        _logger.LogError($"Attention! Message was redelivered already!");

                        _channel.BasicAck(ea.DeliveryTag, false); // Consumer don’t send message back to the queue
                    }
                    else
                    {
                        //TODO сделать бы задерждку на повторное исполнение данного сообщения
                        _channel.BasicNack(ea.DeliveryTag, false, true); // Consumer return message in queue
                        _logger.LogError($"Message returned in queue. Error: '{ex.Message}'");
                    }
                }
            };

            _channel.BasicConsume(queue: _queueName, autoAck: false, consumer: consumer);

            return Task.CompletedTask;


            //while (!stoppingToken.IsCancellationRequested)
            //{
            //    if (_logger.IsEnabled(LogLevel.Information))
            //    {
            //        _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            //    }
            //    await Task.Delay(1000, stoppingToken);
            //}
        }
    }
}
