using ProjectName.Domain.Options;
using RPCClient.Helpers;
using System.Net.Sockets;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddOptions<RabbitMqOptions>().BindConfiguration(nameof(RabbitMqOptions));

builder.Services.AddSingleton<RabbitMqOptions>();
builder.Services.AddTransient<RabbitMqClient>();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapPost("/", async (HttpRequest request, ILoggerFactory loggerFactory, RabbitMqClient rpcClient) =>
{
    try
    {
        Console.WriteLine("RPC Client");
        string n = "30";
        string response = null;

        Console.WriteLine(" Press [enter] to exit.");


        int timeout = 30000;
        var task = InvokeAsync(n, rpcClient);
        if (await Task.WhenAny(task, Task.Delay(timeout)) == task)
        {
            // task completed within timeout
            return task.Result;
        }
        else
        {
            // timeout logic
            return "timeout";
        }


        //DateTime timeout = DateTime.Now.AddSeconds(5); // Set the timeout to 10 seconds
        //while (DateTime.Now < timeout && response == null)
        //{
        //    // Wait for response or timeout
        //    response = InvokeAsync(n, rpcClient).Result;
        //}

        //if (response == null)
        //{
        //    // Handle timeout
        //    return "timeout";
        //}
        //else
        //{
        //    // Handle response
        //    return response;
        //}

        //Console.ReadLine();
    }
    catch (Exception)
    {

        throw;
    }
});


app.Run();

async Task<string> InvokeAsync(string n, RabbitMqClient rpcClient)
{
    Console.WriteLine(" [x] Requesting fib({0})", n);
    var response = await rpcClient.CallAsync(n);
    Console.WriteLine(" [.] Got '{0}'", response);

    return response;
}
