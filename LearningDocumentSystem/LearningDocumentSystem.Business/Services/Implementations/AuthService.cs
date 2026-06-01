using AutoMapper;
using LearningDocumentSystem.Business.DTOs;
using LearningDocumentSystem.Business.Services.Interfaces;
using LearningDocumentSystem.Common.Constants;
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

    public class AdminUserService : IAdminUserService
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private readonly ILogger<AdminUserService> _logger;

        public AdminUserService(IUnitOfWork uow, IMapper mapper, ILogger<AdminUserService> logger)
        {
            _uow = uow;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<IEnumerable<UserDto>> GetAllUsersAsync()
        {
            var users = await _uow.Users.GetAllWithRolesAsync();
            return _mapper.Map<IEnumerable<UserDto>>(users)
                .Where(u => !u.Roles.Contains(AppConstants.RoleAdmin));
        }

        public async Task<IEnumerable<RoleDto>> GetAllRolesAsync()
        {
            var roles = await _uow.Roles.GetAllAsync();
            return _mapper.Map<IEnumerable<RoleDto>>(roles);
        }

        public async Task UpdateUserRolesAsync(int userId, IEnumerable<int> roleIds)
        {
            var user = await _uow.Users.GetWithRolesAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found for role update.", userId);
                return;
            }

            if (user.UserRoles.Any(ur => ur.Role.RoleName == AppConstants.RoleAdmin))
            {
                _logger.LogWarning("Admin user {UserId} cannot be modified.", userId);
                return;
            }

            var singleRoleId = roleIds.Distinct().FirstOrDefault();
            if (singleRoleId == 0)
            {
                _logger.LogWarning("No role selected for user {UserId}.", userId);
                return;
            }

            var selectedRole = await _uow.Roles.GetByIdAsync(singleRoleId);
            if (selectedRole?.RoleName == AppConstants.RoleAdmin)
            {
                _logger.LogWarning("Admin role cannot be assigned to user {UserId}.", userId);
                return;
            }

            var existingRoleIds = user.UserRoles.Select(ur => ur.RoleID).ToHashSet();
            var targetRoleIds = new HashSet<int> { singleRoleId };

            foreach (var roleId in existingRoleIds.Except(targetRoleIds))
            {
                await _uow.UserRoles.RemoveRoleAsync(userId, roleId);
            }

            foreach (var roleId in targetRoleIds.Except(existingRoleIds))
            {
                await _uow.UserRoles.AssignRoleAsync(userId, roleId);
            }

            await _uow.SaveChangesAsync();
        }
    }
}
