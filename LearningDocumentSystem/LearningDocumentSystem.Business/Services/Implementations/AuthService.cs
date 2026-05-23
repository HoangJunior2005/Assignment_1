using AutoMapper;
using LearningDocumentSystem.Business.DTOs;
using LearningDocumentSystem.Business.Services.Interfaces;
using LearningDocumentSystem.Common.Helpers;
using LearningDocumentSystem.Data.Repositories.Interfaces;
using Microsoft.Extensions.Logging;

namespace LearningDocumentSystem.Business.Services.Implementations
{
    public class AuthService : IAuthService
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private readonly ILogger<AuthService> _logger;

        public AuthService(IUnitOfWork uow, IMapper mapper, ILogger<AuthService> logger)
        {
            _uow    = uow;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<UserDto?> LoginAsync(string username, string password)
        {
            var user = await _uow.Users.GetByUsernameAsync(username);
            if (user == null || !user.IsActive)
            {
                _logger.LogWarning("Login failed for username: {Username}", username);
                return null;
            }

            if (!PasswordHelper.VerifyPassword(password, user.PasswordHash))
            {
                _logger.LogWarning("Invalid password for username: {Username}", username);
                return null;
            }

            _logger.LogInformation("User {Username} logged in successfully.", username);
            return _mapper.Map<UserDto>(user);
        }

        public async Task<bool> IsUsernameAvailableAsync(string username)
            => !await _uow.Users.IsUsernameExistsAsync(username);
    }
}
