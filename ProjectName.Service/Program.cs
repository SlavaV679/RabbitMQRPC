using ProjectName.Domain.Options;
using ProjectName.Service;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Consumer>();

builder.Services.AddOptions<RabbitMqOptions>().BindConfiguration(nameof(RabbitMqOptions));

builder.Services.AddSingleton<RabbitMqOptions>();

var host = builder.Build();
host.Run();
