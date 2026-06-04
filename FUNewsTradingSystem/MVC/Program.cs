using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using FUNewsTradingSystem_DataAccessLayer.Models;

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
builder.Services.AddDbContext<FUNewsManagementContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

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

// Services — Scoped
builder.Services.AddScoped<FUNewsTradingSystem_BusinessLayer.Services.Interfaces.ISystemAccountService, FUNewsTradingSystem_BusinessLayer.Services.Implements.SystemAccountService>();
builder.Services.AddScoped<FUNewsTradingSystem_BusinessLayer.Services.Interfaces.ICategoryService, FUNewsTradingSystem_BusinessLayer.Services.Implements.CategoryService>();
builder.Services.AddScoped<FUNewsTradingSystem_BusinessLayer.Services.Interfaces.ITagService, FUNewsTradingSystem_BusinessLayer.Services.Implements.TagService>();
builder.Services.AddScoped<FUNewsTradingSystem_BusinessLayer.Services.Interfaces.INewsArticleService, FUNewsTradingSystem_BusinessLayer.Services.Implements.NewsArticleService>();

// TradingAgentService — Singleton (reuses HttpClient, thread-safe)
builder.Services.AddSingleton<HttpClient>(sp => new HttpClient { Timeout = TimeSpan.FromSeconds(10) });
builder.Services.AddSingleton<FUNewsTradingSystem_BusinessLayer.Services.Interfaces.ITradingAgentService, FUNewsTradingSystem_BusinessLayer.Services.Implements.TradingAgentService>();

// ─────────────────────────────────────────────
// 4. MVC + Views
// ─────────────────────────────────────────────
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// ─────────────────────────────────────────────
// 5. HTTP Pipeline
// ─────────────────────────────────────────────
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}")
    .WithStaticAssets();

app.Run();

public partial class Program { }
