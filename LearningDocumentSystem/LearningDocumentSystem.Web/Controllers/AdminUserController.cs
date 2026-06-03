using LearningDocumentSystem.Business.Services.Interfaces;
using LearningDocumentSystem.Business.DTOs;
using LearningDocumentSystem.Common.Constants;
using LearningDocumentSystem.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LearningDocumentSystem.Web.Controllers
{
    [Authorize(Policy = "AdminOnly")]
    public class AdminUserController : Controller
    {
        private readonly IAdminUserService _adminUserService;
        private readonly ILogger<AdminUserController> _logger;

        public AdminUserController(IAdminUserService adminUserService, ILogger<AdminUserController> logger)
        {
            _adminUserService = adminUserService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var users = await _adminUserService.GetAllUsersAsync();
            var roles = await _adminUserService.GetAllRolesAsync();

            var model = new UserRoleManageViewModel
            {
                Roles = roles,
                Users = users.Select(u => new UserRoleItemViewModel
                {
                    UserID = u.UserID,
                    Username = u.Username,
                    FullName = u.FullName,
                    Email = u.Email,
                    IsActive = u.IsActive,
                    CanUpload = u.CanUpload,
                    Roles = u.Roles,
                    AssignedRoleIds = roles
                        .Where(r => u.Roles.Contains(r.RoleName))
                        .Select(r => r.RoleID)
                        .ToList()
                })
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Whitelist()
        {
            var emails = await _adminUserService.GetAllowedEmailsAsync();
            return View(emails);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateRoles(int userId, List<int> roleIds, bool canUpload = false)
        {
            await _adminUserService.UpdateUserRolesAsync(userId, roleIds, canUpload);
            TempData["Success"] = "Cập nhật quyền thành công.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("/admin/create-teacher")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTeacher(string email, string fullName, string password)
        {
            try
            {
                await _adminUserService.CreateTeacherAccountAsync(email, fullName, password);
                TempData["Success"] = "Tạo tài khoản Giảng viên thành công.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating teacher account.");
                TempData["Error"] = ex.Message;
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("/admin/delete-email/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEmail(int id)
        {
            try
            {
                await _adminUserService.DeleteAllowedEmailAsync(id);
                TempData["Success"] = "Xóa email khỏi danh sách whitelist thành công.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting whitelisted email {Id}.", id);
                TempData["Error"] = "Đã xảy ra lỗi khi xóa email.";
            }
            return RedirectToAction(nameof(Whitelist));
        }

        [HttpPost("/admin/delete-user/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(int id)
        {
            try
            {
                await _adminUserService.DeleteUserAsync(id);
                TempData["Success"] = "Xóa người dùng thành công.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {Id}.", id);
                TempData["Error"] = ex.Message;
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("/admin/update-upload-permission")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateUploadPermission([FromForm] int userId, [FromForm] bool canUpload)
        {
            try
            {
                await _adminUserService.UpdateUploadPermissionAsync(userId, canUpload);
                return Ok(new { success = true, message = "Cập nhật quyền upload thành công." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating upload permission for user {UserId}.", userId);
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("/admin/import-emails")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> ImportEmails(IFormFile file)
        {
            bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";
            try
            {
                var count = await _adminUserService.ImportAllowedEmailsAsync(file);
                var message = $"Nhập whitelist thành công. Đã thêm {count} email mới.";
                if (isAjax)
                {
                    return Ok(new { message });
                }
                TempData["Success"] = message;
            }
            catch (ArgumentException ex)
            {
                if (isAjax) return BadRequest(new { error = ex.Message });
                TempData["Error"] = ex.Message;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing whitelisted emails.");
                var errorMsg = "Đã xảy ra lỗi hệ thống khi nhập email.";
                if (isAjax) return StatusCode(500, new { error = errorMsg });
                TempData["Error"] = errorMsg;
            }
            return RedirectToAction(nameof(Whitelist));
        }
    }
}
