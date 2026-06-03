using LearningDocumentSystem.Business.DTOs;
using LearningDocumentSystem.Business.Services.Interfaces;
using LearningDocumentSystem.Common.Constants;
using LearningDocumentSystem.Common.Exceptions;
using LearningDocumentSystem.Web.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LearningDocumentSystem.Web.Controllers
{
    [AllowAnonymous]
    public class AccountController : Controller
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AccountController> _logger;

        public AccountController(IAuthService authService, ILogger<AccountController> logger)
        {
            _authService = authService;
            _logger      = logger;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _authService.LoginAsync(model.Username, model.Password);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, AppMessages.MsgLoginFailed);
                return View(model);
            }

            await SignInUserAsync(user, model.RememberMe);

            _logger.LogInformation("User {Username} logged in.", user.Username);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");

            return View(new RegisterStudentViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterStudentViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var dto = new RegisterStudentDto
                {
                    StudentCode     = model.StudentCode,
                    FullName        = model.FullName,
                    Password        = model.Password,
                    ConfirmPassword = model.ConfirmPassword
                };

                var user = await _authService.RegisterStudentAsync(dto);
                await SignInUserAsync(user, rememberMe: false);

                _logger.LogInformation("Student {StudentCode} registered and signed in.", user.Username);
                TempData["Success"] = AppMessages.MsgRegisterSuccess;
                return RedirectToAction("Index", "Home");
            }
            catch (BusinessException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            _logger.LogInformation("User logged out.");
            return RedirectToAction("Login");
        }

        public IActionResult AccessDenied()
        {
            return View();
        }

        private async Task SignInUserAsync(UserDto user, bool rememberMe)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.UserID.ToString()),
                new(ClaimTypes.Name, user.Username),
                new("FullName", user.FullName),
                new(ClaimTypes.Email, user.Email)
            };

            foreach (var role in user.Roles)
                claims.Add(new Claim(ClaimTypes.Role, role));

            var identity  = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = rememberMe,
                    ExpiresUtc   = DateTimeOffset.UtcNow.AddHours(8)
                });
        }
    }
}
