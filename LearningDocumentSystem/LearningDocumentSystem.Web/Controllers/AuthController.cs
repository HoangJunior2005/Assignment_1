using LearningDocumentSystem.Business.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LearningDocumentSystem.Web.Controllers
{
    [ApiController]
    [Route("auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                var user = await _authService.RegisterAsync(request.Email, request.Password);
                return Ok(new { message = "Đăng ký tài khoản thành công.", user });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering user: {Email}", request.Email);
                return StatusCode(500, new { error = "Đã xảy ra lỗi hệ thống khi đăng ký tài khoản." });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                var user = await _authService.LoginAsync(request.Email, request.Password);
                if (user == null)
                {
                    return BadRequest(new { error = "Email hoặc mật khẩu không chính xác." });
                }

                var claims = new List<Claim>
                {
                    new(ClaimTypes.NameIdentifier, user.UserID.ToString()),
                    new(ClaimTypes.Name, user.Username),
                    new("FullName", user.FullName),
                    new(ClaimTypes.Email, user.Email),
                    new("CanUpload", user.CanUpload.ToString())
                };

                foreach (var role in user.Roles)
                    claims.Add(new Claim(ClaimTypes.Role, role));

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    principal,
                    new AuthenticationProperties
                    {
                        IsPersistent = true,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
                    });

                return Ok(new { message = "Đăng nhập thành công.", user });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging in user: {Email}", request.Email);
                return StatusCode(500, new { error = "Đã xảy ra lỗi hệ thống khi đăng nhập." });
            }
        }
    }

    public class RegisterRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
