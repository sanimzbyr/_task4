
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Threading.Tasks;
using System.Linq;
using System;
//using BCrypt.Net;

public class AccountController : Controller
{
    private readonly AppDbContext _db;

    public AccountController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public IActionResult Login() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string Email, string Password)
    {
        bool wantsJson = Request.Headers["X-Requested-With"] == "XMLHttpRequest";

        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrEmpty(Password))
        {
            if (wantsJson) return Json(new { success = false, message = "E-mail and password are required." });
            ViewBag.ErrorMessage = "E-mail and password are required.";
            return View();
        }

        var user = _db.Users.FirstOrDefault(x => x.Email == Email);
        if (user == null || user.Status == UserStatus.Blocked || user.Status == UserStatus.Deleted)
        {
            var msg = user == null ? "Invalid credentials." :
                      user.Status == UserStatus.Blocked ? "Account is blocked." :
                      "Account deleted. Please register again.";

            if (wantsJson) return Json(new { success = false, message = msg });
            ViewBag.ErrorMessage = msg;
            return View();
        }
        if (!BCrypt.Net.BCrypt.Verify(Password, user.PasswordHash))
        {
            if (wantsJson) return Json(new { success = false, message = "Invalid credentials." });
            ViewBag.ErrorMessage = "Invalid credentials.";
            return View();
        }

        user.LastLoginTime = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var claims = new[] { new Claim(ClaimTypes.Name, user.Email), new Claim("FullName", user.Name) };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

        if (wantsJson) return Json(new { success = true, message = "Login successful.", redirectUrl = Url.Action("Index", "UserManagement") });
        return RedirectToAction("Index", "UserManagement");
    }

    [HttpGet]
    public IActionResult Register() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(string FirstName, string LastName, string Email, string Password, string ConfirmPassword)
    {
        bool wantsJson = Request.Headers["X-Requested-With"] == "XMLHttpRequest";

        if (string.IsNullOrWhiteSpace(FirstName) || string.IsNullOrWhiteSpace(LastName) ||
            string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
        {
            var msg = "All fields are required.";
            if (wantsJson) return Json(new { success = false, message = msg });
            ViewBag.ErrorMessage = msg;
            return View();
        }
        if (Password != ConfirmPassword)
        {
            var msg = "Passwords do not match.";
            if (wantsJson) return Json(new { success = false, message = msg });
            ViewBag.ErrorMessage = msg;
            return View();
        }

        if (_db.Users.Any(u => u.Email == Email))
        {
            var msg = "Email is already registered.";
            if (wantsJson) return Json(new { success = false, message = msg });
            ViewBag.ErrorMessage = msg;
            return View();
        }

        var user = new User
        {
            Name = $"{FirstName} {LastName}".Trim(),
            Email = Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(Password),
            RegistrationTime = DateTime.UtcNow,
            LastLoginTime = DateTime.UtcNow,
            Status = UserStatus.Active
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        if (wantsJson) return Json(new { success = true, message = "Registration successful. Please log in.", redirectUrl = Url.Action("Login", "Account") });
        TempData["StatusMessage"] = "Registration successful. Please log in.";
        return RedirectToAction("Login");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login");
    }
}
