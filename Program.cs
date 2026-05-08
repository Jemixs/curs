using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using SportClub.Application.Interfaces;
using SportClub.Application.Services;
using SportClub.Data;
using SportClub.Domain;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Налаштування бази даних SQLite
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")
                     ?? "Data Source=sportclub.db"));

// Налаштування аутентифікації через Cookies
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/login";
        options.AccessDeniedPath = "/login";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddMemoryCache();
// Реєстрація сервісів бізнес-логіки
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ISmsService, MockSmsService>();
builder.Services.AddScoped<IClientProfileService, ClientProfileService>();
builder.Services.AddScoped<IPlanService, PlanService>();
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();
builder.Services.AddScoped<ICheckInService, CheckInService>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
builder.Services.AddSingleton<IMonobankPaymentService, MonobankPaymentService>();

// Фоновий сервіс для обслуговування абонементів
builder.Services.AddHostedService<SubscriptionMaintenanceService>();

// Налаштування MudBlazor
builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass = MudBlazor.Defaults.Classes.Position.BottomRight;
    config.SnackbarConfiguration.ShowTransitionDuration = 300;
    config.SnackbarConfiguration.HideTransitionDuration = 300;
    config.SnackbarConfiguration.VisibleStateDuration = 3000;
    config.SnackbarConfiguration.PreventDuplicates = false;
});

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

// Обробка входу для адміністраторів
app.MapPost("/auth/login", async (
    HttpContext ctx,
    ApplicationDbContext db,
    [Microsoft.AspNetCore.Mvc.FromForm] string email,
    [Microsoft.AspNetCore.Mvc.FromForm] string password) =>
{
    var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);

    if (user is null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        return Results.Redirect("/login?error=1");

    var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new(ClaimTypes.Name, user.FullName),
        new(ClaimTypes.Email, user.Email),
        new(ClaimTypes.Role, user.Role.ToString())
    };

    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

    await ctx.SignInAsync(
        CookieAuthenticationDefaults.AuthenticationScheme,
        new ClaimsPrincipal(identity),
        new AuthenticationProperties { IsPersistent = true });

    return user.Role switch
    {
        UserRole.Admin or UserRole.Receptionist => Results.Redirect("/admin"),
        UserRole.Client => Results.Redirect("/client"),
        _ => Results.Redirect("/")
    };
}).DisableAntiforgery();

app.MapPost("/auth/logout", async (HttpContext ctx) =>
{
    await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/login");
}).DisableAntiforgery();

// Генерація фінансових звітів в Excel
app.MapGet("/api/reports/excel", async (
    HttpContext ctx,
    IAnalyticsService analytics,
    DateTime from,
    DateTime to) =>
{
    var result = await analytics.ExportToExcelAsync(from, to);
    if (!result.IsSuccess)
        return Results.BadRequest(result.FirstError);

    return Results.File(result.Value!, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Report_{from:yyyyMMdd}_{to:yyyyMMdd}.xlsx");
}).RequireAuthorization(policy => policy.RequireRole(UserRole.Admin.ToString()));

app.MapRazorComponents<SportClub.Components.App>()
    .AddInteractiveServerRenderMode();

// Endpoint для завершення авторизації (встановлення Cookies)
app.MapGet("/auth/signin-callback", async (
    string token, 
    string? returnUrl, 
    Microsoft.Extensions.Caching.Memory.IMemoryCache cache, 
    SportClub.Data.ApplicationDbContext db,
    Microsoft.AspNetCore.Http.HttpContext context) => 
{
    var cacheKey = $"LOGIN_TOKEN_{token}";
    if (!cache.TryGetValue(cacheKey, out object? userIdObj) || userIdObj is not int userId)
    {
        return Results.Redirect("/auth?error=expired");
    }
    
    cache.Remove(cacheKey);
    
    var user = await db.Users.FindAsync(userId);
    if (user is null) return Results.Redirect("/auth?error=usernotfound");

    var claims = new List<System.Security.Claims.Claim>
    {
        new(System.Security.Claims.ClaimTypes.NameIdentifier, user.Id.ToString()),
        new(System.Security.Claims.ClaimTypes.Name, user.FullName),
        new(System.Security.Claims.ClaimTypes.Email, user.Email),
        new(System.Security.Claims.ClaimTypes.Role, user.Role.ToString())
    };

    var identity = new System.Security.Claims.ClaimsIdentity(claims, Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme);
    var principal = new System.Security.Claims.ClaimsPrincipal(identity);

    await context.SignInAsync(
        Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme,
        principal,
        new Microsoft.AspNetCore.Authentication.AuthenticationProperties { IsPersistent = true });

    return Results.Redirect(string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl);
});

// Ініціалізація бази даних при запуску
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();
    
    // Наповнення бази даних фейковими даними
    DbInitializer.Initialize(db);
}

app.Run();
