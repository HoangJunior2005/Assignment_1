using LearningDocumentSystem.Entities.Models;

namespace LearningDocumentSystem.Data.Repositories.Interfaces
{
    public interface IUserRepository : IGenericRepository<User>
    {
        Task<User?> GetByUsernameAsync(string username);
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetWithRolesAsync(int userId);
        Task<IEnumerable<User>> GetAllWithRolesAsync();
        Task<IEnumerable<string>> GetUserRolesAsync(int userId);
        Task<bool> IsUsernameExistsAsync(string username);
        Task<bool> IsEmailExistsAsync(string email);
    }

    public interface IRoleRepository : IGenericRepository<Role>
    {
        Task<Role?> GetByNameAsync(string roleName);
    }

    public interface IUserRoleRepository : IGenericRepository<UserRole>
    {
        Task<bool> HasRoleAsync(int userId, string roleName);
        Task AssignRoleAsync(int userId, int roleId);
        Task RemoveRoleAsync(int userId, int roleId);
    }
}
