using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Hỗ_trợ_và_quản_lí_khóa_học_trực_tuyến.Data;
using Hỗ_trợ_và_quản_lí_khóa_học_trực_tuyến.Models;
using Hỗ_trợ_và_quản_lí_khóa_học_trực_tuyến.ViewModels;

namespace Hỗ_trợ_và_quản_lí_khóa_học_trực_tuyến.Controllers
{
    public class AuthController : Controller
    {
        private readonly NTULearnContext _context;
        private readonly IPasswordHasher<User> _passwordHasher;

        public AuthController(
            NTULearnContext context,
            IPasswordHasher<User> passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }


        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(
            LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
                return View(model);

            string email = model.Email.Trim().ToLowerInvariant();

            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                ModelState.AddModelError(
                    "",
                    "Email hoặc mật khẩu không chính xác.");

                return View(model);
            }

            if (!user.IsActive)
            {
                ModelState.AddModelError(
                    "",
                    "Tài khoản đã bị khóa. Vui lòng liên hệ quản trị viên.");

                return View(model);
            }

            var result = _passwordHasher.VerifyHashedPassword(
                user, user.PasswordHash, model.Password);

            if (result == PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Email hoặc mật khẩu không chính xác.");

                return View(model);
            }

            if (user.ApprovalStatus == "Pending")
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Tài khoản đang chờ duyệt. Vui lòng chờ quản trị viên phê duyệt.");

                return View(model);
            }

            if (user.ApprovalStatus == "Rejected")
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Tài khoản đã bị từ chối duyệt. Vui lòng liên hệ quản trị viên.");

                return View(model);
            }

            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
        new Claim(ClaimTypes.Name, user.FullName),
        new Claim(ClaimTypes.Email, user.Email),
        new Claim(ClaimTypes.Role, user.Role.RoleName)
    };

            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme);

            var principal = new ClaimsPrincipal(identity);

            var properties = new AuthenticationProperties
            {
                IsPersistent = model.RememberMe
            };

            if (model.RememberMe)
            {
                properties.ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7);
            }

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                properties);

            if (!string.IsNullOrEmpty(returnUrl) &&
                Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(
            RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string email = model.Email.Trim().ToLowerInvariant();

            bool emailExists = await _context.Users
                .AnyAsync(u => u.Email == email);

            if (emailExists)
            {
                ModelState.AddModelError(
                    nameof(model.Email),
                    "Email này đã được sử dụng.");

                return View(model);
            }

            bool isInstructor = model.RoleName == "GiangVien";

            Role? role = await _context.Roles
                .FirstOrDefaultAsync(r => r.RoleName == model.RoleName);

            if (role == null)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Không tìm thấy vai trò trong database.");

                return View(model);
            }

            var user = new User
            {
                FullName = model.FullName.Trim(),
                Email = email,
                RoleId = role.RoleId,
                CreatedAt = DateTime.Now,
                IsActive = true,
                // Giảng viên phải chờ Admin duyệt mới đăng nhập được
                ApprovalStatus = isInstructor ? "Pending" : "Approved"
            };

            user.PasswordHash = _passwordHasher.HashPassword(
                user, model.Password);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            TempData["Success"] = isInstructor
                ? "Đăng ký thành công. Tài khoản giảng viên đang chờ quản trị viên duyệt."
                : "Đăng ký thành công. Bạn có thể đăng nhập.";

            return RedirectToAction("Login");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);

            return RedirectToAction("Index", "Home");
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}