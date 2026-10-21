
using Hỗ_trợ_và_quản_lí_khóa_học_trực_tuyến.Data;
using Hỗ_trợ_và_quản_lí_khóa_học_trực_tuyến.Models;
using Hỗ_trợ_và_quản_lí_khóa_học_trực_tuyến.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Kết nối SQL Server
builder.Services.AddDbContext<NTULearnContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// Đăng ký dịch vụ băm và kiểm tra mật khẩu
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

// Lưu trữ tài liệu bài học (ngoài wwwroot)
builder.Services.AddSingleton<LessonFileStorage>();

// Cấu hình xác thực bằng Cookie
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.AccessDeniedPath = "/Auth/AccessDenied";

        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;

        options.ExpireTimeSpan = TimeSpan.FromHours(2);
        options.SlidingExpiration = true;

        options.Events.OnValidatePrincipal = async context =>
        {
            var userIdString = context.Principal?
                .FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdString, out int userId))
            {
                context.RejectPrincipal();

                await context.HttpContext.SignOutAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme);

                return;
            }

            var db = context.HttpContext.RequestServices
                .GetRequiredService<NTULearnContext>();

            var user = await db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null || !user.IsActive)
            {
                context.RejectPrincipal();

                await context.HttpContext.SignOutAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme);
            }


            var currentRoleName = await db.Roles
                .AsNoTracking()
                .Where(r => r.RoleId == user.RoleId)
                .Select(r => r.RoleName)
                .FirstOrDefaultAsync();

            var cookieRoleName = context.Principal?
                .FindFirstValue(ClaimTypes.Role);

            if (string.IsNullOrEmpty(currentRoleName) ||
                currentRoleName != cookieRoleName)
            {
                context.RejectPrincipal();

                await context.HttpContext.SignOutAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme);

                return;
            }

        };

    });

// Đăng ký MVC
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Cấu hình xử lý lỗi
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Thứ tự rất quan trọng:
// Authentication xác định người dùng là ai.
// Authorization kiểm tra người dùng có quyền hay không.
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();