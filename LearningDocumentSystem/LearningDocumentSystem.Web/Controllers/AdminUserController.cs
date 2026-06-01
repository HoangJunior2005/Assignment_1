using LearningDocumentSystem.Business.Services.Interfaces;
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
                    Roles = u.Roles,
                    AssignedRoleIds = roles
                        .Where(r => u.Roles.Contains(r.RoleName))
                        .Select(r => r.RoleID)
                        .ToList()
                })
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateRoles(int userId, List<int> roleIds)
        {
            await _adminUserService.UpdateUserRolesAsync(userId, roleIds);
            TempData["Success"] = "Cập nhật quyền thành công.";
            return RedirectToAction(nameof(Index));
        }
    }
}
