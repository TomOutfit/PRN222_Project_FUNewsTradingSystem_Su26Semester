using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using FUNewsTradingSystem_DataAccessLayer.Models;
using FUNewsTradingSystem_MVC.Services;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
var builder = WebApplication.CreateBuilder(args);

// ─────────────────────────────────────────────
// 1. Authentication — Cookie-based, sliding 60 min
// ─────────────────────────────────────────────
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.LogoutPath = "/Account/Logout";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.None
            : CookieSecurePolicy.Always;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
        options.SlidingExpiration = true;
        options.ReturnUrlParameter = "returnUrl";
    });

// ─────────────────────────────────────────────
// 1.5 Database Configuration
// ─────────────────────────────────────────────
string MaskConnectionString(string? connStr)
{
    if (string.IsNullOrEmpty(connStr)) return "";
    var masked = System.Text.RegularExpressions.Regex.Replace(connStr, @"(postgres(?:ql)?://[^:]+:)([^@]+)(@)", "$1******$3");
    masked = System.Text.RegularExpressions.Regex.Replace(masked, @"Password=[^;]+", "Password=******", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    masked = System.Text.RegularExpressions.Regex.Replace(masked, @"pwd=[^;]+", "pwd=******", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    return masked;
}

string ConvertPostgresUrlToConnectionString(string? url)
{
    if (string.IsNullOrEmpty(url)) return "";
    if (!url.StartsWith("postgres://") && !url.StartsWith("postgresql://"))
    {
        return url;
    }
    var uri = new Uri(url);
    var userInfo = uri.UserInfo.Split(':');
    var username = userInfo[0];
    var password = userInfo.Length > 1 ? userInfo[1] : "";
    var host = uri.Host;
    var port = uri.Port > 0 ? uri.Port : 5432;
    var database = uri.AbsolutePath.TrimStart('/');
    return $"Host={host};Port={port};Database={database};Username={username};Password={password};SSL Mode=Require;Trust Server Certificate=true;Pooling=true;Minimum Pool Size=0;Maximum Pool Size=20;Connection Lifetime=300;Keepalive=30;Timeout=30;Command Timeout=30;";
}

builder.Services.AddDbContext<FUNewsManagementContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    var env = builder.Environment.EnvironmentName;
    
    Console.WriteLine($"[DIAGNOSTIC] Environment: '{env}'");
    Console.WriteLine($"[DIAGNOSTIC] Loaded Connection String (Masked): '{MaskConnectionString(connectionString)}'");
    
    if (env == "Production")
    {
        connectionString = ConvertPostgresUrlToConnectionString(connectionString);
        Console.WriteLine($"[DIAGNOSTIC] Converted Connection String (Masked): '{MaskConnectionString(connectionString)}'");
        options.UseNpgsql(connectionString, npgsqlOptions =>
        {
            npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorCodesToAdd: null);
            npgsqlOptions.CommandTimeout(30);
        });
    }
    else
    {
        options.UseSqlServer(connectionString);
    }
});

// ─────────────────────────────────────────────
// 2. Authorization — Role-based policies
// ─────────────────────────────────────────────
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("StaffOnly", policy =>
        policy.RequireClaim(ClaimTypes.Role, "1"));

    options.AddPolicy("AdminOnly", policy =>
        policy.RequireClaim(ClaimTypes.Role, "3"));

    options.AddPolicy("StaffOrLecturer", policy =>
        policy.RequireClaim(ClaimTypes.Role, "1", "2"));
});

// ─────────────────────────────────────────────
// 3. Register BusinessLayer services
//    (uncomment and wire up once service implementations are available)
// ─────────────────────────────────────────────
// Repositories — Scoped
builder.Services.AddScoped<FUNewsTradingSystem_BusinessLayer.Repositories.Interfaces.ISystemAccountRepository, FUNewsTradingSystem_BusinessLayer.Repositories.Implements.SystemAccountRepository>();
builder.Services.AddScoped<FUNewsTradingSystem_BusinessLayer.Repositories.Interfaces.ICategoryRepository, FUNewsTradingSystem_BusinessLayer.Repositories.Implements.CategoryRepository>();
builder.Services.AddScoped<FUNewsTradingSystem_BusinessLayer.Repositories.Interfaces.ITagRepository, FUNewsTradingSystem_BusinessLayer.Repositories.Implements.TagRepository>();
builder.Services.AddScoped<FUNewsTradingSystem_BusinessLayer.Repositories.Interfaces.INewsArticleRepository, FUNewsTradingSystem_BusinessLayer.Repositories.Implements.NewsArticleRepository>();
builder.Services.AddScoped<FUNewsTradingSystem_BusinessLayer.Repositories.Interfaces.ISavedReportRepository, FUNewsTradingSystem_BusinessLayer.Repositories.Implements.SavedReportRepository>();

// Services — Scoped
builder.Services.AddScoped<FUNewsTradingSystem_BusinessLayer.Services.Interfaces.ISystemAccountService, FUNewsTradingSystem_BusinessLayer.Services.Implements.SystemAccountService>();
builder.Services.AddScoped<FUNewsTradingSystem_BusinessLayer.Services.Interfaces.ICategoryService, FUNewsTradingSystem_BusinessLayer.Services.Implements.CategoryService>();
builder.Services.AddScoped<FUNewsTradingSystem_BusinessLayer.Services.Interfaces.ITagService, FUNewsTradingSystem_BusinessLayer.Services.Implements.TagService>();
builder.Services.AddScoped<FUNewsTradingSystem_BusinessLayer.Services.Interfaces.INewsArticleService, FUNewsTradingSystem_BusinessLayer.Services.Implements.NewsArticleService>();
builder.Services.AddScoped<FUNewsTradingSystem_BusinessLayer.Services.Interfaces.ISavedReportService, FUNewsTradingSystem_BusinessLayer.Services.Implements.SavedReportService>();

// MarketDataService — singleton shared between BackgroundService and API
builder.Services.AddSingleton<IMarketDataService, MarketDataService>();
builder.Services.AddSingleton<HttpClient>(sp => new HttpClient { Timeout = TimeSpan.FromSeconds(10) });
builder.Services.AddSingleton<FUNewsTradingSystem_BusinessLayer.Services.Interfaces.ITradingAgentService, FUNewsTradingSystem_BusinessLayer.Services.Implements.TradingAgentService>();

// ─────────────────────────────────────────────
// 4. MVC + Views
// ─────────────────────────────────────────────
builder.Services.AddSignalR();
builder.Services.AddHostedService<FUNewsTradingSystem_MVC.Services.MarketDataBackgroundService>();
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();

// IPasswordHasher — used by AccountController for login verification
builder.Services.AddScoped<IPasswordHasher<SystemAccount>, PasswordHasher<SystemAccount>>();

var app = builder.Build();

// ─────────────────────────────────────────────
// 5. HTTP Pipeline
// ─────────────────────────────────────────────
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/Home/Error/{0}");

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

// Map Hub Endpoints
app.MapHub<FUNewsTradingSystem_MVC.Hubs.AnalysisProgressHub>("/hubs/analysisProgress");
app.MapHub<FUNewsTradingSystem_MVC.Hubs.ReportHub>("/hubs/reports");
app.MapHub<FUNewsTradingSystem_MVC.Hubs.MarketTickerHub>("/hubs/marketTicker");
app.MapHub<FUNewsTradingSystem_MVC.Hubs.PresenceHub>("/hubs/presence");
app.MapHub<FUNewsTradingSystem_MVC.Hubs.NotificationHub>("/hubs/notifications");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();
// ─────────────────────────────────────────────
// 6. Auto-apply pending EF migrations on startup (best-effort)
// ─────────────────────────────────────────────
try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<FUNewsManagementContext>();
    db.Database.Migrate();
}
catch (Exception ex)
{
    // Log warning but don't crash — DB may already be up-to-date
    // or LocalDB isn't reachable from this shell context.
    var loggerFactory = app.Services.GetRequiredService<ILoggerFactory>();
    var logger = loggerFactory.CreateLogger("Startup");
    logger.LogWarning(ex, "Auto-migration skipped: {Message}", ex.Message);
}

app.Run();

public partial class Program { }
