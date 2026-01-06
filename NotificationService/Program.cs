using NotificationService.Services;
using NotificationService.Storage;

var builder = WebApplication.CreateBuilder(args);

// Register application services
builder.Services.AddScoped<INotificationService, NotificationServiceImpl>();
builder.Services.AddSingleton<INotificationStore, InMemoryNotificationStore>();

// NEW: RabbitMQ consumer background service (v2.0.0)
builder.Services.AddHostedService<RabbitMQConsumerService>();

// Configure API
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure middleware pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();