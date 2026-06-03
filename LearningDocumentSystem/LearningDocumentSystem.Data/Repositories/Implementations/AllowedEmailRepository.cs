using LearningDocumentSystem.Data.DbContexts;
using LearningDocumentSystem.Data.Repositories.Interfaces;
using LearningDocumentSystem.Entities.Models;
using Microsoft.EntityFrameworkCore;

namespace LearningDocumentSystem.Data.Repositories.Implementations
{
    public class AllowedEmailRepository : GenericRepository<AllowedEmail>, IAllowedEmailRepository
    {
        public AllowedEmailRepository(AppDbContext context) : base(context) { }

        public async Task<AllowedEmail?> GetByEmailAsync(string email)
            => await _context.AllowedEmails.FirstOrDefaultAsync(ae => ae.Email == email);

        public async Task<bool> IsEmailWhitelistedAsync(string email)
            => await _context.AllowedEmails.AnyAsync(ae => ae.Email == email);
    }
}
