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
                // Применяем миграции
                await dbContext.Database.MigrateAsync();
                logger.LogInformation("Миграции успешно применены");

                // Создаём тестового админа, если его нет
                if (!await dbContext.Users.AnyAsync(u => u.Username == "admin"))
                {
                    var admin = new User
                    {
                        Username = "admin",
                        PasswordHash = passwordHasher.Hash("admin123"),
                        FullName = "Тестовый Администратор",
                        Role = UserRole.admin,
                        CreatedAt = DateTime.UtcNow
                    };
                    dbContext.Users.Add(admin);
                    logger.LogInformation("Тестовый администратор создан");
                }

                // Создаём тестового учителя, если его нет
                if (!await dbContext.Users.AnyAsync(u => u.Username == "teacher"))
                {
                    var teacher = new User
                    {
                        Username = "teacher",
                        PasswordHash = passwordHasher.Hash("teacher123"),
                        FullName = "Тестовый Учитель",
                        Role = UserRole.teacher,
                        CreatedAt = DateTime.UtcNow
                    };
                    dbContext.Users.Add(teacher);
                    logger.LogInformation("Тестовый учитель создан");
                }

                await dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка при применении миграций");
            }
        }

        /// <summary>
        /// Заполнение базы данных тестовыми данными (устаревший метод)
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

                    // Тестовый студент - теперь без пароля
                    var student = new User
                    {
                        JournalLogin = "test_student",
                        FullName = "Тестовый Студент",
                        Group = "ТЕСТ-01",
                        Role = UserRole.student,
                        CreatedAt = DateTime.UtcNow,
                        AccessToken = null,
                        RefreshToken = null,
                        TokenExpiresAt = null
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