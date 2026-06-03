using AutoMapper;
using LearningDocumentSystem.Business.DTOs;
using LearningDocumentSystem.Business.Services.Interfaces;
using LearningDocumentSystem.Common.Constants;
using LearningDocumentSystem.Common.Exceptions;
using LearningDocumentSystem.Common.Helpers;
using LearningDocumentSystem.Data.Repositories.Interfaces;
using LearningDocumentSystem.Entities.Models;
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

        public async Task<UserDto> RegisterStudentAsync(RegisterStudentDto dto)
        {
            if (!string.Equals(dto.Password, dto.ConfirmPassword, StringComparison.Ordinal))
                throw new BusinessException(AppMessages.MsgPasswordMismatch);

            var studentCode = StringHelper.NormalizeStudentCode(dto.StudentCode);
            var registry = await _uow.StudentRegistries.GetByStudentCodeAsync(studentCode);

            if (registry == null)
                throw new BusinessException(AppMessages.MsgStudentCodeNotFound);

            if (registry.IsActivated)
                throw new BusinessException(AppMessages.MsgStudentAlreadyExists);

            if (!StringHelper.NamesMatch(dto.FullName, registry.FullName))
                throw new BusinessException(AppMessages.MsgStudentInfoInvalid);

            if (await _uow.Users.IsUsernameExistsAsync(studentCode))
                throw new BusinessException(AppMessages.MsgStudentAlreadyExists);

            var studentRole = await _uow.Roles.GetByNameAsync(AppConstants.RoleStudent)
                ?? throw new BusinessException("Vai trò sinh viên chưa được cấu hình trong hệ thống.");

            var email = $"{studentCode.ToLowerInvariant()}@student.edu.vn";
            if (await _uow.Users.IsEmailExistsAsync(email))
                throw new BusinessException(AppMessages.MsgStudentAlreadyExists);

            await _uow.BeginTransactionAsync();
            try
            {
                var user = new User
                {
                    Username     = studentCode,
                    PasswordHash = PasswordHelper.HashPassword(dto.Password),
                    FullName     = dto.FullName.Trim(),
                    Email        = email,
                    SchoolID     = registry.SchoolID,
                    IsActive     = true,
                    CreatedAt    = DateTime.UtcNow
                };

                await _uow.Users.AddAsync(user);
                await _uow.SaveChangesAsync();

                await _uow.UserRoles.AssignRoleAsync(user.UserID, studentRole.RoleID);

                registry.IsActivated = true;
                registry.ActivatedAt = DateTime.UtcNow;
                registry.UserID = user.UserID;
                registry.FullName = dto.FullName.Trim();
                _uow.StudentRegistries.Update(registry);

                await _uow.SaveChangesAsync();
                await _uow.CommitAsync();

                _logger.LogInformation("Student registered: {StudentCode}", studentCode);

                var created = await _uow.Users.GetWithRolesAsync(user.UserID)
                    ?? throw new BusinessException("Không thể tải tài khoản sau khi đăng ký.");
                return _mapper.Map<UserDto>(created);
            }
            catch
            {
                await _uow.RollbackAsync();
                throw;
            }
        }
    }
}
