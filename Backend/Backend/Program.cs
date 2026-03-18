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

builder.Services.AddHostedService<TokenRefreshService>();
builder.Services.AddScoped<TokenService>();

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
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Schedule API v1");
        c.RoutePrefix = "swagger";
    });
    app.UseCors("DevPolicy");  
}
else
{
    app.UseCors("AllowAngularApp");
}

// ИНИЦИАЛИЗАЦИЯ БАЗЫ ДАННЫХ - ТОЛЬКО ОДИН РАЗ!
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await dbContext.Database.EnsureCreatedAsync();

    // Заполняем тестовыми данными
    await app.SeedDatabaseAsync();

    // Добавляем админа если нужно (SeedDatabaseAsync уже добавляет)
    // await app.ApplyMigrationsAndSeedAdmin();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();