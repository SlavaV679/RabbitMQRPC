using ProjectName.Domain.Options;
using RPCServer;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddOptions<RabbitMqOptions>().BindConfiguration(nameof(RabbitMqOptions));

builder.Services.AddSingleton<RabbitMqOptions>();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
