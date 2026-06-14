using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SagaEcommerce.Order.Application.Interfaces;
using SagaEcommerce.Order.Application.Services;
using SagaEcommerce.Order.Application.Validators;
using SagaEcommerce.Order.Domain.Repositories;
using SagaEcommerce.Order.Infrastructure.Data;
using SagaEcommerce.Order.Infrastructure.Messaging;
using SagaEcommerce.Order.Infrastructure.Repositories;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Configure PostgreSQL Database (EF Core)
var connectionString = builder.Configuration.GetConnectionString("OrderDbConnection");
builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseNpgsql(connectionString));

// 2. Configure RabbitMQ Settings
builder.Services.Configure<RabbitMqSettings>(builder.Configuration.GetSection("RabbitMq"));

// 3. Register Repositories and Application Services
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOrderAppService, OrderAppService>();
builder.Services.AddScoped<IOrderEventPublisher, RabbitMqMessagePublisher>();

// 4. Register Validators (FluentValidation)
builder.Services.AddValidatorsFromAssemblyContaining<CreateOrderDtoValidator>();

// 5. Configure Controllers and OpenAPI (Scalar)
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

// 6. Run database migrations automatically on startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<OrderDbContext>();
        app.Logger.LogInformation("Applying database migrations...");
        await context.Database.MigrateAsync();
        app.Logger.LogInformation("Database updated successfully.");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "An error occurred while applying migrations to the database.");
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
