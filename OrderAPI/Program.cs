using MassTransit;
using Microsoft.EntityFrameworkCore;
using OrderAPI.Data;
using OrderAPI.Handler;
using OrderAPI.HttpClients;
using OrderAPI.Services;
using OrderAPI.Services.IServices;
using Polly;
using Polly.Extensions.Http;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IOrderService, OrderService>();
// Register AutoMapper
builder.Services.AddAutoMapper(cfg => { }, typeof(Program));
builder.Services.AddHttpClient<IProductApiClient, ProductApiClient>(client =>
{
    // The Docker internal DNS! It routes "productapi" directly to your other container.
    // Check your docker-compose.yml: if your service is named 'product-api', use that instead!
    client.BaseAddress = new Uri("http://product-api:8080");
})
.AddPolicyHandler(HttpPolicyExtensions
    .HandleTransientHttpError() // Catches 5xx errors and network failures
    .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)))); // Retries after 2s, 4s, 8s

builder.Services.AddControllers();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        // "rabbitmq" is the name of the container in docker-compose!
        cfg.Host("rabbitmq", "/", h => {
            h.Username("guest");
            h.Password("guest");
        });
    });
});
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>(); // Use your actual DbContext name here
    db.Database.Migrate();
}

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();

app.UseAuthorization();
app.UseExceptionHandler();

app.MapControllers();

app.Run();
