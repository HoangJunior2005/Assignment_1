using LearningDocumentSystem.Entities.Models;

namespace LearningDocumentSystem.Data.Repositories.Interfaces
{
    public interface IStudentRegistryRepository : IGenericRepository<StudentRegistry>
    {
        Task<StudentRegistry?> GetByStudentCodeAsync(string studentCode);
    }
}
