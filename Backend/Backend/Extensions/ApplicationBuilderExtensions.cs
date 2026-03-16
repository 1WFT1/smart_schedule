using Backend.API.Data;
using Backend.API.Interfaces;
using Backend.API.Models;
using Backend.API.Services;
using Microsoft.EntityFrameworkCore;

namespace Backend.API.Extensions
{
    public static class ApplicationBuilderExtensions
    {
        /// <summary>
        /// Применяет миграции и добавляет тестового админа
        /// </summary>
        public static async Task ApplyMigrationsAndSeedAdmin(this IApplicationBuilder app)
        {
            using var scope = app.ApplicationServices.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

            try
            {
                logger.LogInformation("Применение миграций...");
                await dbContext.Database.MigrateAsync();
                logger.LogInformation("Миграции успешно применены");

                // Проверяем, есть ли админ
                var adminExists = await dbContext.Users
                    .AnyAsync(u => u.Role == UserRole.admin);

                if (!adminExists)
                {
                    logger.LogInformation("Создание тестового администратора...");

                    var admin = new User
                    {
                        Username = "adminH_nw08",
                        PasswordHash = passwordHasher.Hash("adminQ54hb6b7"),
                        FullName = "Главный администратор",
                        Role = UserRole.admin,
                        CreatedAt = DateTime.UtcNow
                    };

                    dbContext.Users.Add(admin);
                    await dbContext.SaveChangesAsync();

                    logger.LogInformation("Тестовый администратор создан");
                }
                else
                {
                    logger.LogInformation("Администратор уже существует");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка при применении миграций");
            }
        }

        /// <summary>
        /// Настройка для среды разработки
        /// </summary>
        public static void UseDevelopmentEnvironment(this IApplicationBuilder app)
        {
            var env = app.ApplicationServices.GetRequiredService<IWebHostEnvironment>();

            if (env.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "API v1");
                    c.RoutePrefix = "swagger";
                });

                app.UseCors(policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyHeader()
                          .AllowAnyMethod();
                });
            }
        }

        /// <summary>
        /// Настройка для продакшн среды
        /// </summary>
        public static void UseProductionEnvironment(this IApplicationBuilder app)
        {
            var env = app.ApplicationServices.GetRequiredService<IWebHostEnvironment>();

            if (!env.IsDevelopment())
            {
                app.UseHsts();

                app.UseCors(policy =>
                {
                    policy.WithOrigins("https://yourdomain.com")
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();
                });
            }
        }

        /// <summary>
        /// Заполнение базы данных тестовыми данными
        /// </summary>
        public static async Task SeedDatabaseAsync(this IApplicationBuilder app)
        {
            using var scope = app.ApplicationServices.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

            try
            {
                if (!await dbContext.Users.AnyAsync())
                {
                    logger.LogInformation("Создание тестовых пользователей...");

                    // Тестовый студент
                    var student = new User
                    {
                        JournalLogin = "test_student",
                        EncryptedJournalPassword = Convert.ToBase64String(
                            System.Text.Encoding.UTF8.GetBytes("test123")),
                        FullName = "Тестовый Студент",
                        Group = "ТЕСТ-01",
                        Role = UserRole.student,
                        CreatedAt = DateTime.UtcNow
                    };
                    dbContext.Users.Add(student);

                    // Тестовый админ
                    var admin = new User
                    {
                        Username = "admin",
                        PasswordHash = passwordHasher.Hash("admin123"),
                        FullName = "Тестовый Администратор",
                        Role = UserRole.admin,
                        CreatedAt = DateTime.UtcNow
                    };
                    dbContext.Users.Add(admin);

                    // Тестовый учитель
                    var teacher = new User
                    {
                        Username = "teacher",
                        PasswordHash = passwordHasher.Hash("teacher123"),
                        FullName = "Тестовый Учитель",
                        Role = UserRole.teacher,
                        CreatedAt = DateTime.UtcNow
                    };
                    dbContext.Users.Add(teacher);

                    await dbContext.SaveChangesAsync();
                    logger.LogInformation("Тестовые пользователи успешно созданы");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка при заполнении базы данных");
            }
        }
    }
}