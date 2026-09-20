using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Configuration
var configuration = builder.Configuration;

// Add services
builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();
// Use the lightweight OpenAPI helper package included in ASP.NET Core templates
builder.Services.AddOpenApi();

// Choose DB provider by configuration. Default to InMemory for demo.
var connection = configuration.GetConnectionString("DefaultConnection");
if (!string.IsNullOrWhiteSpace(connection))
{
    // If a real connection string is provided, user likely intends to use SQL Server or other provider
    builder.Services.AddDbContext<KafkaConsumerMicroService.Data.AppDbContext>(opts => opts.UseSqlServer(connection));
}
else
{
    builder.Services.AddDbContext<KafkaConsumerMicroService.Data.AppDbContext>(opts => opts.UseInMemoryDatabase("MarketDb"));
}

// Application services
builder.Services.AddScoped<KafkaConsumerMicroService.Services.ValidationService>();
builder.Services.AddHostedService<KafkaConsumerMicroService.Services.KafkaConsumerService>();

var app = builder.Build();

// Ensure DB created (for demo). In production migrations should be applied via tools
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<KafkaConsumerMicroService.Data.AppDbContext>();
    db.Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    // Map OpenAPI endpoints for development UI
    app.MapOpenApi();
}

app.UseRouting();
app.UseAuthorization();

app.MapControllers();
app.MapHub<KafkaConsumerMicroService.Hubs.DataHub>("/hubs/data");

app.Run();
