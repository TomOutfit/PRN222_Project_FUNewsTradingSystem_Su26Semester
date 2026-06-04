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
// builder.Services.AddScoped<IRepositories.Interfaces.ISystemAccountRepository, Repositories.Implements.SystemAccountRepository>();
// builder.Services.AddScoped<IRepositories.Interfaces.ICategoryRepository, Repositories.Implements.CategoryRepository>();
// builder.Services.AddScoped<IRepositories.Interfaces.ITagRepository, Repositories.Implements.TagRepository>();
// builder.Services.AddScoped<IRepositories.Interfaces.INewsArticleRepository, Repositories.Implements.NewsArticleRepository>();
// builder.Services.AddScoped<IRepositories.Interfaces.INewsTagRepository, Repositories.Implements.NewsTagRepository>();

// Services — Scoped
// builder.Services.AddScoped<IServices.Interfaces.ISystemAccountService, Services.Implements.SystemAccountService>();
// builder.Services.AddScoped<IServices.Interfaces.ICategoryService, Services.Implements.CategoryService>();
// builder.Services.AddScoped<IServices.Interfaces.ITagService, Services.Implements.TagService>();
// builder.Services.AddScoped<IServices.Interfaces.INewsArticleService, Services.Implements.NewsArticleService>();

// TradingAgentService — Singleton (reuses HttpClient, thread-safe)
// builder.Services.AddSingleton<HttpClient>(sp => new HttpClient { Timeout = TimeSpan.FromSeconds(10) });
// builder.Services.AddSingleton<IServices.Interfaces.ITradingAgentService, Services.Implements.TradingAgentService>();

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
