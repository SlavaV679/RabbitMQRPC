using ProjectName.Domain.Models;
using ProjectName.Domain.Options;
using Runpay.Payouts.FakePublisher.Helpers;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true).Build();

builder.Services.Configure<RabbitMqOptions>(config.GetSection(nameof(RabbitMqOptions)));

builder.Services.AddSingleton<RabbitMqOptions>();
builder.Services.AddTransient<RabbitMqPublisher>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

//var summaries = new[]
//{
//    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
//};

//app.MapGet("/weatherforecast", () =>
//{
//    var forecast = Enumerable.Range(1, 5).Select(index =>
//        new WeatherForecast
//        (
//            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
//            Random.Shared.Next(-20, 55),
//            summaries[Random.Shared.Next(summaries.Length)]
//        ))
//        .ToArray();
//    return forecast;
//})
//.WithName("GetWeatherForecast")
//.WithOpenApi();

app.MapPost("/", async (HttpRequest request, ILoggerFactory loggerFactory, RabbitMqPublisher rabbitMqPublisher) =>
{
    try
    {
        var requestRaw = await new StreamReader(request.Body).ReadToEndAsync();
        if (string.IsNullOrWhiteSpace(requestRaw))
            requestRaw = "{\"ActId\":178614133,\"PaymentTypeId\":\"5\",\"PaymentDate\":\"2024-03-27T11:48:00.0589245+03:00\",\"Summa\":0,\"Currency\":\"MDL\",\"Notes\":\"note\",\"Inn\":\"inn\"}";
        var paymentRequest = JsonSerializer.Deserialize<PaymentRequest>(requestRaw);

        var message = JsonSerializer.Serialize(paymentRequest);
        rabbitMqPublisher.SendMessage(message);
    }
    catch (Exception)
    {

        throw;
    }
});

app.Run();

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
