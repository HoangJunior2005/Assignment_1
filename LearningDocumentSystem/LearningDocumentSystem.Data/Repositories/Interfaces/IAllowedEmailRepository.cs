using LearningDocumentSystem.Entities.Models;

namespace LearningDocumentSystem.Data.Repositories.Interfaces
{
    public interface IAllowedEmailRepository : IGenericRepository<AllowedEmail>
    {
        Task<AllowedEmail?> GetByEmailAsync(string email);
        Task<bool> IsEmailWhitelistedAsync(string email);
    }
}
