using System.ComponentModel.DataAnnotations;

namespace ProjectName.Domain.Options
{

    public class RabbitMqOptions
    {
        public List<string> ServerHostNames { get; set; }

        [Required]
        public string UserName { get; set; }

        public string Password { get; set; }

        public string VirtualHost { get; set; }

        public string QueueName { get; set; }

        public string ExchangeName { get; set; }

        public string RoutingKey { get; set; }

        /// <summary>
        /// Routing key for queue, which execute Transaction
        /// </summary>
        public string TransactionRoutingKey { get; set; }
    }
}