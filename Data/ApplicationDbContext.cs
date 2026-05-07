using Microsoft.EntityFrameworkCore;
using SportClub.Domain;
using BCrypt.Net;

namespace SportClub.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    // ── DbSets ───────────────────────────────────────────────
    public DbSet<User> Users => Set<User>();
    public DbSet<ClientProfile> ClientProfiles => Set<ClientProfile>();
    public DbSet<Plan> Plans => Set<Plan>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<CheckIn> CheckIns => Set<CheckIn>();

    // ────────────────────────────────────────────────────────
    protected override void OnModelCreating(ModelBuilder mb)
    {
        base.OnModelCreating(mb);

        // ── Global query filters (soft-delete) ───────────────
        mb.Entity<Plan>()
            .HasQueryFilter(p => !p.IsArchived);

        mb.Entity<ClientProfile>()
            .HasQueryFilter(cp => !EF.Property<bool>(cp, "IsDeleted"));

        // ── User ─────────────────────────────────────────────
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

            e.Property(u => u.PasswordHash)
                .IsRequired();

            e.Property(u => u.FullName)
                .IsRequired()
                .HasMaxLength(256);

            e.Property(u => u.Role)
                .HasConversion<string>()
                .HasMaxLength(32);

            e.Property(u => u.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            // Relationship: User 1—0..1 ClientProfile
            e.HasOne(u => u.ClientProfile)
                .WithOne(cp => cp.User)
                .HasForeignKey<ClientProfile>(cp => cp.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── ClientProfile ─────────────────────────────────────
        mb.Entity<ClientProfile>(e =>
        {
            e.ToTable("ClientProfiles");
            e.HasKey(cp => cp.Id);

            // Shadow property for soft-delete
            e.Property<bool>("IsDeleted")
                .HasDefaultValue(false);

            e.Property(cp => cp.Phone)
                .IsRequired()
                .HasMaxLength(20);

            // Unique index on Phone for O(1) lookup
            e.HasIndex(cp => cp.Phone)
                .IsUnique()
                .HasDatabaseName("IX_ClientProfiles_Phone");

            e.Property(cp => cp.Barcode)
                .IsRequired()
                .HasMaxLength(128);

            // Unique index on Barcode for O(1) QR scan
            e.HasIndex(cp => cp.Barcode)
                .IsUnique()
                .HasDatabaseName("IX_ClientProfiles_Barcode");

            e.Property(cp => cp.Notes)
                .HasMaxLength(1000);

            e.Property(cp => cp.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // ── Plan ─────────────────────────────────────────────
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

        // ── Subscription ─────────────────────────────────────
        mb.Entity<Subscription>(e =>
        {
            e.ToTable("Subscriptions");
            e.HasKey(s => s.Id);

            e.Property(s => s.PurchaseDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            e.Property(s => s.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            // Ignore computed property — not stored in DB
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

        // ── CheckIn ───────────────────────────────────────────
        mb.Entity<CheckIn>(e =>
        {
            e.ToTable("CheckIns");
            e.HasKey(ci => ci.Id);

            e.Property(ci => ci.BarcodeSnapshot)
                .IsRequired()
                .HasMaxLength(128);

            e.Property(ci => ci.CheckedInAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            // Index for debounce lookup: (ClientProfileId, CheckedInAt DESC)
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

        // ── Seed data ─────────────────────────────────────────
        SeedData(mb);
    }

    // ────────────────────────────────────────────────────────
    private static void SeedData(ModelBuilder mb)
    {
        var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // ── Admin user ───────────────────────────────────────
        mb.Entity<User>().HasData(new User
        {
            Id = 1,
            Email = "admin@sportclub.ua",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin1234!"),
            Role = UserRole.Admin,
            FullName = "Адміністратор",
            CreatedAt = now
        });

        // ── Receptionist ─────────────────────────────────────
        mb.Entity<User>().HasData(new User
        {
            Id = 2,
            Email = "reception@sportclub.ua",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Recept1234!"),
            Role = UserRole.Receptionist,
            FullName = "Рецепціоніст",
            CreatedAt = now
        });

        // ── Sample client user ───────────────────────────────
        mb.Entity<User>().HasData(new User
        {
            Id = 3,
            Email = "client@sportclub.ua",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Client1234!"),
            Role = UserRole.Client,
            FullName = "Іван Петренко",
            CreatedAt = now
        });

        // ── Sample client profile ─────────────────────────────
        // We use an anonymous object to set shadow property IsDeleted
        mb.Entity<ClientProfile>().HasData(new
        {
            Id = 1,
            UserId = 3,
            Phone = "+380501234567",
            Barcode = "SC-000001",
            DateOfBirth = new DateTime(1990, 5, 15, 0, 0, 0, DateTimeKind.Utc),
            Notes = (string?)null,
            IsBlocked = false,
            CreatedAt = now,
            IsDeleted = false
        });

        // ── Plans ────────────────────────────────────────────
        mb.Entity<Plan>().HasData(
            new Plan
            {
                Id = 1,
                Name = "Місячний безлімітний",
                Description = "Необмежені відвідування протягом 30 днів",
                Price = 1200m,
                PlanType = PlanType.Unlimited,
                MaxVisits = null,
                DurationValue = 1,
                DurationUnit = PlanDurationUnit.Months,
                IsArchived = false,
                CreatedAt = now
            },
            new Plan
            {
                Id = 2,
                Name = "8 занять",
                Description = "8 відвідувань, дійсний 60 днів",
                Price = 800m,
                PlanType = PlanType.LimitedVisits,
                MaxVisits = 8,
                DurationValue = 60,
                DurationUnit = PlanDurationUnit.Days,
                IsArchived = false,
                CreatedAt = now
            },
            new Plan
            {
                Id = 3,
                Name = "12 занять",
                Description = "12 відвідувань, дійсний 90 днів",
                Price = 1100m,
                PlanType = PlanType.LimitedVisits,
                MaxVisits = 12,
                DurationValue = 90,
                DurationUnit = PlanDurationUnit.Days,
                IsArchived = false,
                CreatedAt = now
            },
            new Plan
            {
                Id = 4,
                Name = "Квартальний безлімітний",
                Description = "Необмежені відвідування протягом 3 місяців",
                Price = 3000m,
                PlanType = PlanType.Unlimited,
                MaxVisits = null,
                DurationValue = 3,
                DurationUnit = PlanDurationUnit.Months,
                IsArchived = false,
                CreatedAt = now
            }
        );
    }
}
