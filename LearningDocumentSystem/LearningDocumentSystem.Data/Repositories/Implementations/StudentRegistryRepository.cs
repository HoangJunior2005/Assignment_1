using LearningDocumentSystem.Common.Helpers;
using LearningDocumentSystem.Data.DbContexts;
using LearningDocumentSystem.Data.Repositories.Interfaces;
using LearningDocumentSystem.Entities.Models;
using Microsoft.EntityFrameworkCore;

namespace LearningDocumentSystem.Data.Repositories.Implementations
{
    public class StudentRegistryRepository : GenericRepository<StudentRegistry>, IStudentRegistryRepository
    {
        public StudentRegistryRepository(AppDbContext context) : base(context) { }

        public async Task<StudentRegistry?> GetByStudentCodeAsync(string studentCode)
        {
            var normalized = StringHelper.NormalizeStudentCode(studentCode);
            return await _context.StudentRegistries
                .Include(r => r.School)
                .FirstOrDefaultAsync(r => r.StudentCode == normalized);
        }
    }
}
