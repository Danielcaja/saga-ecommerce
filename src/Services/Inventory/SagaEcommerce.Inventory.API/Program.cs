using Microsoft.EntityFrameworkCore;
using SagaEcommerce.Inventory.Application.Interfaces;
using SagaEcommerce.Inventory.Application.Services;
using SagaEcommerce.Inventory.Domain.Repositories;
using SagaEcommerce.Inventory.Infrastructure.Data;
using SagaEcommerce.Inventory.Infrastructure.Messaging;
using SagaEcommerce.Inventory.Infrastructure.Repositories;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Configure PostgreSQL Database (EF Core)
var connectionString = builder.Configuration.GetConnectionString("InventoryDbConnection");
builder.Services.AddDbContext<InventoryDbContext>(options =>
    options.UseNpgsql(connectionString));

// 2. Configure RabbitMQ Settings & Persistent Connection
builder.Services.Configure<RabbitMqSettings>(builder.Configuration.GetSection("RabbitMq"));
builder.Services.AddSingleton<IRabbitMqPersistentConnection, RabbitMqPersistentConnection>();

// 3. Register Repositories and Application Services
builder.Services.AddScoped<IInventoryRepository, InventoryRepository>();
builder.Services.AddScoped<IInventoryAppService, InventoryAppService>();
builder.Services.AddScoped<IInventoryEventPublisher, RabbitMqMessagePublisher>();

// 4. Register RabbitMQ consumer background service
builder.Services.AddHostedService<OrderCreatedConsumer>();

// 5. Configure Controllers and OpenAPI (Scalar)
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

// 6. Run database migrations and seed databases automatically on startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<InventoryDbContext>();
        app.Logger.LogInformation("Applying inventory database migrations and seeding...");
        await InventoryDbSeeder.SeedAsync(context);
        app.Logger.LogInformation("Inventory database updated and seeded successfully.");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "An error occurred while migrating or seeding the inventory database.");
    }
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
