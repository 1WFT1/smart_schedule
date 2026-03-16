using Backend.API.Data;
using Backend.API.Extensions;
using Backend.API.Options;
using Backend.API.Services;

var builder = WebApplication.CreateBuilder(args);

// Настройка логирования
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.SetMinimumLevel(LogLevel.Debug);

// Добавление сервисов
builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddCorsPolicies();
builder.Services.AddSwaggerWithJwt();
builder.Services.AddApplicationServices();

// Настройка Telegram бота
builder.Services.Configure<TelegramBotConfiguration>(
    builder.Configuration.GetSection("TelegramBot"));
builder.Services.AddHostedService<TelegramBotService>();

// Настройка JSON
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

var app = builder.Build();

// Настройка pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDevelopmentEnvironment();
}
else
{
    app.UseProductionEnvironment();
}

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.EnsureCreated();

    // Вызываем метод заполнения тестовыми данными
    await app.SeedDatabaseAsync();
}



//app.UseHttpsRedirection();
//app.UseHttpsRedirectionWithHeaders();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Инициализация базы данных
await app.ApplyMigrationsAndSeedAdmin();

app.Run();

//