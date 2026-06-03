using LearningDocumentSystem.Business.DTOs;
using LearningDocumentSystem.Business.Services.Interfaces;
using LearningDocumentSystem.Common.Constants;
using LearningDocumentSystem.Common.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LearningDocumentSystem.Web.Controllers.Api
{
    [ApiController]
    [Route("api/auth")]
    [AllowAnonymous]
    public class AuthApiController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthApiController> _logger;

        public AuthApiController(IAuthService authService, ILogger<AuthApiController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [HttpPost("register-student")]
        public async Task<IActionResult> RegisterStudent([FromBody] RegisterStudentDto dto)
        {
            if (!ModelState.IsValid)
            {
                var firstError = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .FirstOrDefault() ?? "Dữ liệu không hợp lệ.";
                return BadRequest(new { message = firstError });
            }

            try
            {
                await _authService.RegisterStudentAsync(dto);
                return Ok(new { message = AppMessages.MsgRegisterSuccess });
            }
            catch (BusinessException ex)
            {
                _logger.LogWarning("Student registration rejected: {Message}", ex.Message);
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
