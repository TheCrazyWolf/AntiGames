using System.Net;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.WebHost.ConfigureKestrel((httpClient, options) =>
{
    options.Listen(IPAddress.Any, httpClient.Configuration.GetValue<int?>("PortServer") ?? 5000);
});

var app = builder.Build();
// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();