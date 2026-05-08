using Microsoft.EntityFrameworkCore;
using SportClub.Domain;
using BCrypt.Net;

namespace SportClub.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    // Таблиці БД
    public DbSet<User> Users => Set<User>();
    public DbSet<ClientProfile> ClientProfiles => Set<ClientProfile>();
    public DbSet<Plan> Plans => Set<Plan>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<CheckIn> CheckIns => Set<CheckIn>();

    // Додаткові модулі
    public DbSet<Trainer> Trainers => Set<Trainer>();
    public DbSet<ClassSession> ClassSessions => Set<ClassSession>();
    public DbSet<ClassBooking> ClassBookings => Set<ClassBooking>();
    public DbSet<PromoCode> PromoCodes => Set<PromoCode>();


    protected override void OnModelCreating(ModelBuilder mb)
    {
        base.OnModelCreating(mb);

        // Глобальні фільтри (м'яке видалення)
        mb.Entity<Plan>()
            .HasQueryFilter(p => !p.IsArchived);

        mb.Entity<ClientProfile>()
            .HasQueryFilter(cp => !EF.Property<bool>(cp, "IsDeleted"));

        // Налаштування користувача
        mb.Entity<User>(e =>
        {
            e.ToTable("Users");
            e.HasKey(u => u.Id);

            e.Property(u => u.Email)
                .IsRequired()
                .HasMaxLength(256);

            e.HasIndex(u => u.Email)
                .IsUnique()
                .HasDatabaseName("IX_Users_Email");

            e.Property(u => u.PasswordHash);

            e.Property(u => u.FullName)
                .IsRequired()
                .HasMaxLength(256);

            e.Property(u => u.Role)
                .HasConversion<string>()
                .HasMaxLength(32);

            e.Property(u => u.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            // Зв'язок 1 до 0..1 між користувачем та профілем клієнта
            e.HasOne(u => u.ClientProfile)
                .WithOne(cp => cp.User)
                .HasForeignKey<ClientProfile>(cp => cp.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Налаштування профілю клієнта
        mb.Entity<ClientProfile>(e =>
        {
            e.ToTable("ClientProfiles");
            e.HasKey(cp => cp.Id);

            e.Property(cp => cp.BonusBalance)
                .HasColumnType("decimal(10,2)")
                .HasDefaultValue(0m);

            // Тіньова властивість для м'якого видалення
            e.Property<bool>("IsDeleted")
                .HasDefaultValue(false);

            e.Property(cp => cp.Phone)
                .IsRequired()
                .HasMaxLength(20);

            // Індекс для швидкого пошуку за номером телефону
            e.HasIndex(cp => cp.Phone)
                .IsUnique()
                .HasDatabaseName("IX_ClientProfiles_Phone");

            e.Property(cp => cp.Barcode)
                .IsRequired()
                .HasMaxLength(128);

            // Індекс для швидкого пошуку за баркодом
            e.HasIndex(cp => cp.Barcode)
                .IsUnique()
                .HasDatabaseName("IX_ClientProfiles_Barcode");

            e.Property(cp => cp.Notes)
                .HasMaxLength(1000);

            e.Property(cp => cp.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // Налаштування тарифів
        mb.Entity<Plan>(e =>
        {
            e.ToTable("Plans");
            e.HasKey(p => p.Id);

            e.Property(p => p.Name)
                .IsRequired()
                .HasMaxLength(128);

            e.Property(p => p.Description)
                .HasMaxLength(512);

            e.Property(p => p.Price)
                .HasColumnType("decimal(10,2)");

            e.Property(p => p.PlanType)
                .HasConversion<string>()
                .HasMaxLength(32);

            e.Property(p => p.DurationUnit)
                .HasConversion<string>()
                .HasMaxLength(16);

            e.Property(p => p.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // Налаштування абонементів
        mb.Entity<Subscription>(e =>
        {
            e.ToTable("Subscriptions");
            e.HasKey(s => s.Id);

            e.Property(s => s.PurchaseDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            e.Property(s => s.FinalPrice)
                .HasColumnType("decimal(10,2)")
                .HasDefaultValue(0m);

            e.Property(s => s.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            // Ігноруємо обчислювану властивість
            e.Ignore(s => s.IsActive);

            e.HasOne(s => s.ClientProfile)
                .WithMany(cp => cp.Subscriptions)
                .HasForeignKey(s => s.ClientProfileId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(s => s.Plan)
                .WithMany(p => p.Subscriptions)
                .HasForeignKey(s => s.PlanId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Налаштування відвідувань
        mb.Entity<CheckIn>(e =>
        {
            e.ToTable("CheckIns");
            e.HasKey(ci => ci.Id);

            e.Property(ci => ci.BarcodeSnapshot)
                .IsRequired()
                .HasMaxLength(128);

            e.Property(ci => ci.CheckedInAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            // Індекс для перевірки дублікатів входу
            e.HasIndex(ci => new { ci.ClientProfileId, ci.CheckedInAt })
                .HasDatabaseName("IX_CheckIns_ClientProfile_Time");

            e.HasOne(ci => ci.ClientProfile)
                .WithMany(cp => cp.CheckIns)
                .HasForeignKey(ci => ci.ClientProfileId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(ci => ci.Subscription)
                .WithMany(s => s.CheckIns)
                .HasForeignKey(ci => ci.SubscriptionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Тренери
        mb.Entity<Trainer>(e =>
        {
            e.ToTable("Trainers");
            e.HasKey(t => t.Id);

            e.Property(t => t.FullName)
                .IsRequired()
                .HasMaxLength(256);

            e.Property(t => t.Specialization)
                .HasMaxLength(256);
        });

        // Групові заняття
        mb.Entity<ClassSession>(e =>
        {
            e.ToTable("ClassSessions");
            e.HasKey(cs => cs.Id);

            e.Property(cs => cs.Title)
                .IsRequired()
                .HasMaxLength(256);

            e.Property(cs => cs.Version)
                .IsConcurrencyToken();

            e.HasOne(cs => cs.Trainer)
                .WithMany(t => t.Sessions)
                .HasForeignKey(cs => cs.TrainerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Бронювання занять
        mb.Entity<ClassBooking>(e =>
        {
            e.ToTable("ClassBookings");
            e.HasKey(cb => cb.Id);

            e.Property(cb => cb.Status)
                .HasConversion<string>()
                .HasMaxLength(32);

            e.Property(cb => cb.BookingTime)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            // Обмеження, щоб клієнт не міг записатися двічі на одне заняття
            e.HasIndex(cb => new { cb.SessionId, cb.ClientProfileId })
                .IsUnique()
                .HasDatabaseName("IX_ClassBookings_Session_Client");

            e.HasOne(cb => cb.Session)
                .WithMany(cs => cs.Bookings)
                .HasForeignKey(cb => cb.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(cb => cb.Client)
                .WithMany(cp => cp.ClassBookings)
                .HasForeignKey(cb => cb.ClientProfileId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Промокоди
        mb.Entity<PromoCode>(e =>
        {
            e.ToTable("PromoCodes");
            e.HasKey(pc => pc.Id);

            e.Property(pc => pc.Code)
                .IsRequired()
                .HasMaxLength(64);

            e.HasIndex(pc => pc.Code)
                .IsUnique()
                .HasDatabaseName("IX_PromoCodes_Code");

            e.Property(pc => pc.Version)
                .IsConcurrencyToken();
        });

        // Початкові дані тепер обробляються через DbInitializer
    }

}
