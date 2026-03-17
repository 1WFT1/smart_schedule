using Backend.API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Backend.API.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Таблицы в базе данных
        public DbSet<User> Users { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<Group> Groups { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Конвертер для DateTime в UTC
            var dateTimeConverter = new ValueConverter<DateTime, DateTime>(
                v => v.ToUniversalTime(),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

            var nullableDateTimeConverter = new ValueConverter<DateTime?, DateTime?>(
                v => v.HasValue ? v.Value.ToUniversalTime() : v,
                v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);

            // Применяем ко всем DateTime свойствам
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(DateTime))
                    {
                        property.SetValueConverter(dateTimeConverter);
                    }
                    else if (property.ClrType == typeof(DateTime?))
                    {
                        property.SetValueConverter(nullableDateTimeConverter);
                    }
                }
            }

            // Конвертер для List<string>
            var stringListConverter = new ValueConverter<List<string>, string>(
                v => string.Join(',', v),
                v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList());

            var stringListComparer = new ValueComparer<List<string>>(
                (c1, c2) => c1.SequenceEqual(c2),
                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                c => c.ToList());

            // Настройка для User
            modelBuilder.Entity<User>(entity =>
            {
                // Индекс для JournalLogin (студенты)
                modelBuilder.Entity<User>()
                    .HasIndex(u => u.JournalLogin)
                    .IsUnique()
                    .HasDatabaseName("IX_Users_JournalLogin");

                // Индекс для Username (админы)
                modelBuilder.Entity<User>()
                    .HasIndex(u => u.Username)
                    .IsUnique()
                    .HasDatabaseName("IX_Users_Username")
                    .HasFilter("\"Username\" IS NOT NULL");

                entity.Property(e => e.JournalLogin)
                    .HasMaxLength(100);

                //entity.Property(e => e.AdminUsername)
                    //.HasMaxLength(100);

                entity.Property(e => e.FullName)
                    .HasMaxLength(200);

                entity.Property(e => e.Group)
                    .HasMaxLength(50);

                entity.Property(e => e.Role)
                    .HasConversion<string>()
                    .HasMaxLength(20);
            });

            // Настройка для Event
            modelBuilder.Entity<Event>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasIndex(e => e.StartTime)
                    .HasDatabaseName("IX_Events_StartTime");

                entity.HasIndex(e => e.EndTime)
                    .HasDatabaseName("IX_Events_EndTime");

                entity.HasIndex(e => e.Category)
                    .HasDatabaseName("IX_Events_Category");

                entity.Property(e => e.Type)
                    .HasConversion<string>()
                    .HasMaxLength(20)
                    .IsRequired();

                entity.Property(e => e.Category)
                    .HasConversion<string>()
                    .HasMaxLength(20)
                    .IsRequired();

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(200);

                // Настройка для Tags
                entity.Property(e => e.Tags)
                    .HasConversion(stringListConverter)
                    .Metadata.SetValueComparer(stringListComparer);

                // Настройка для TargetGroups
                entity.Property(e => e.TargetGroups)
                    .HasConversion(stringListConverter)
                    .Metadata.SetValueComparer(stringListComparer);

                entity.HasOne(e => e.CreatedBy)
                    .WithMany()
                    .HasForeignKey(e => e.CreatedByUserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // НАСТРОЙКИ ДЛЯ GROUPS
            modelBuilder.Entity<Group>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasIndex(e => e.Name)
                    .IsUnique()
                    .HasDatabaseName("IX_Groups_Name");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.DisplayName)
                    .HasMaxLength(100);

                entity.Property(e => e.Source)
                    .HasMaxLength(20)
                    .HasDefaultValue("manual");

                // Связь с куратором
                entity.HasOne(g => g.Curator)
                    .WithMany(u => u.CuratedGroups)
                    .HasForeignKey(g => g.CuratorId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // НАСТРОЙКИ ДЛЯ USER (дополнительные)
            modelBuilder.Entity<User>(entity =>
            {
                // Связь студента с группой
                entity.HasOne(u => u.StudentGroup)
                    .WithMany(g => g.Students)
                    .HasForeignKey(u => u.StudentGroupId)
                    .OnDelete(DeleteBehavior.SetNull);
            });


        }
    }
}