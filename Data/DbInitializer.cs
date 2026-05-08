using Microsoft.EntityFrameworkCore;
using SportClub.Domain;

namespace SportClub.Data;

public static class DbInitializer
{
    public static void Initialize(ApplicationDbContext context)
    {
        // Перевіряємо чи є вже дані
        if (context.Users.Any())
        {
            return; // База вже наповнена
        }

        // Створення Адміністратора
        var admin = new User
        {
            Email = "admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
            Role = UserRole.Admin,
            FullName = "Головний Адміністратор",
            CreatedAt = DateTime.UtcNow
        };
        context.Users.Add(admin);

        // Створення Тарифних планів
        var plans = new List<Plan>
        {
            new Plan
            {
                Name = "Разовий візит",
                Description = "Доступ до залу на 1 день",
                Price = 300m,
                PlanType = PlanType.LimitedVisits,
                MaxVisits = 1,
                DurationValue = 1,
                DurationUnit = PlanDurationUnit.Days
            },
            new Plan
            {
                Name = "Базовий (12 занять)",
                Description = "Доступ до залу на 12 відвідувань протягом місяця",
                Price = 1200m,
                PlanType = PlanType.LimitedVisits,
                MaxVisits = 12,
                DurationValue = 1,
                DurationUnit = PlanDurationUnit.Months
            },
            new Plan
            {
                Name = "Безліміт 1 місяць",
                Description = "Необмежений доступ до тренажерного залу на 30 днів",
                Price = 1500m,
                PlanType = PlanType.Unlimited,
                DurationValue = 1,
                DurationUnit = PlanDurationUnit.Months
            },
            new Plan
            {
                Name = "Безліміт Рік",
                Description = "Необмежений доступ на 365 днів з максимальною вигодою",
                Price = 12000m,
                PlanType = PlanType.Unlimited,
                DurationValue = 12,
                DurationUnit = PlanDurationUnit.Months
            }
        };
        context.Plans.AddRange(plans);

        // Створення Тренерів
        var trainers = new List<Trainer>
        {
            new Trainer { FullName = "Олександр Іваненко", Specialization = "Кросфіт" },
            new Trainer { FullName = "Марія Петренко", Specialization = "Йога та Пілатес" },
            new Trainer { FullName = "Дмитро Коваленко", Specialization = "Бодібілдинг" },
            new Trainer { FullName = "Анна Сидоренко", Specialization = "TRX, Фітнес" },
            new Trainer { FullName = "Віталій Мельник", Specialization = "Бокс" }
        };
        context.Trainers.AddRange(trainers);
        context.SaveChanges();

        // Створення Клієнтів та Історії
        var random = new Random();
        var clients = new List<User>();
        var firstNames = new[] { "Іван", "Максим", "Олег", "Сергій", "Андрій", "Олена", "Наталія", "Ірина", "Тетяна", "Оксана" };
        var lastNames = new[] { "Шевченко", "Бойко", "Ковальчук", "Бондаренко", "Ткаченко", "Кравченко", "Олійник", "Мороз", "Лисенко", "Савченко" };

        for (int i = 1; i <= 15; i++)
        {
            var firstName = firstNames[random.Next(firstNames.Length)];
            var lastName = lastNames[random.Next(lastNames.Length)];
            var phone = $"+38050{random.Next(1000000, 9999999)}";
            
            var user = new User
            {
                Email = $"client{i}@test.com",
                Role = UserRole.Client,
                FullName = $"{firstName} {lastName}",
                CreatedAt = DateTime.UtcNow.AddDays(-random.Next(30, 365))
            };
            
            var profile = new ClientProfile
            {
                User = user,
                Phone = phone,
                Barcode = $"IMP{random.Next(10000, 99999)}",
                DateOfBirth = DateTime.UtcNow.AddYears(-random.Next(18, 50)).AddDays(-random.Next(0, 365)),
                CreatedAt = user.CreatedAt
            };
            
            context.Users.Add(user);
            context.ClientProfiles.Add(profile);
            context.SaveChanges();

            // + абонемент
            var plan = plans[random.Next(plans.Count)];
            var purchaseDate = DateTime.UtcNow.AddDays(-random.Next(5, 60));
            var isActivated = purchaseDate <= DateTime.UtcNow;
            var activationDate = isActivated ? (DateTime?)purchaseDate.AddDays(random.Next(0, 3)) : null;
            
            var expirationDate = activationDate.HasValue 
                ? (plan.DurationUnit == PlanDurationUnit.Months 
                    ? activationDate.Value.AddMonths(plan.DurationValue) 
                    : activationDate.Value.AddDays(plan.DurationValue))
                : (DateTime?)null;

            var subscription = new Subscription
            {
                ClientProfileId = profile.Id,
                PlanId = plan.Id,
                FinalPrice = plan.Price,
                PurchaseDate = purchaseDate,
                ActivationDate = activationDate,
                ExpirationDate = expirationDate,
                VisitsUsed = 0
            };
            context.Subscriptions.Add(subscription);
            context.SaveChanges();

            // Історія відвідувань
            if (activationDate.HasValue)
            {
                int visitsCount = random.Next(3, 6);
                for (int v = 0; v < visitsCount; v++)
                {
                    var visitDate = activationDate.Value.AddDays(random.Next(1, 30));
                    if (visitDate > DateTime.UtcNow) visitDate = DateTime.UtcNow.AddDays(-random.Next(1, 5));

                    var checkIn = new CheckIn
                    {
                        ClientProfileId = profile.Id,
                        SubscriptionId = subscription.Id,
                        CheckedInAt = visitDate
                    };
                    context.CheckIns.Add(checkIn);
                    subscription.VisitsUsed++;
                }
            }
        }
        context.SaveChanges();

        // Групові заняття на поточний тиждень
        var startOfWeek = DateTime.UtcNow.Date.AddDays(-(int)DateTime.UtcNow.DayOfWeek + (int)DayOfWeek.Monday);
        var classNames = new[] { "Ранкова Йога", "Інтенсивний Кросфіт", "Основи Боксу", "Пілатес Реформер", "Функціонал" };
        
        for (int i = 0; i < 10; i++)
        {
            var trainer = trainers[random.Next(trainers.Count)];
            var classDate = startOfWeek.AddDays(random.Next(0, 7)).AddHours(random.Next(8, 20));

            context.ClassSessions.Add(new ClassSession
            {
                TrainerId = trainer.Id,
                Title = classNames[random.Next(classNames.Length)],
                StartTime = classDate,
                EndTime = classDate.AddMinutes(60),
                MaxParticipants = random.Next(10, 20)
            });
        }
        context.SaveChanges();
    }
}
