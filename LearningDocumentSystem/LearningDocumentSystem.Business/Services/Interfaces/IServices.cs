using LearningDocumentSystem.Business.DTOs;
using Microsoft.AspNetCore.Http;

namespace LearningDocumentSystem.Business.Services.Interfaces
{
    public interface IAuthService
    {
        Task<UserDto?> LoginAsync(string username, string password);
        Task<bool> IsUsernameAvailableAsync(string username);
    }

    public interface ISubjectService
    {
        Task<IEnumerable<SubjectDto>> GetAllAsync();
        Task<SubjectDto?> GetByIdAsync(int id);
        Task<SubjectDto?> GetWithChaptersAsync(int id);
        Task<SubjectDto> CreateAsync(CreateSubjectDto dto);
        Task<SubjectDto> UpdateAsync(UpdateSubjectDto dto);
        Task DeleteAsync(int id);
    }

    public interface IChapterService
    {
        Task<IEnumerable<ChapterDto>> GetAllAsync();
        Task<IEnumerable<ChapterDto>> GetBySubjectAsync(int subjectId);
        Task<ChapterDto?> GetByIdAsync(int id);
        Task<ChapterDto> CreateAsync(CreateChapterDto dto);
        Task<ChapterDto> UpdateAsync(UpdateChapterDto dto);
        Task DeleteAsync(int id);
    }

    public interface IDocumentService
    {
        Task<(IEnumerable<DocumentDto> Items, int TotalCount)> GetPagedAsync(
            string? keyword, int? subjectId, int? chapterId, int page, int pageSize);
        Task<DocumentDetailDto?> GetDetailAsync(int id);
        Task<DocumentDto> UploadAsync(IFormFile file, int chapterId, string title, int uploadedByUserId);
        Task DeleteAsync(int id);
        Task<DashboardDto> GetDashboardAsync();
    }

    public interface IFileService
    {
        Task<string> SaveFileAsync(IFormFile file, string uploadFolder);
        void DeleteFile(string storagePath, string uploadFolder);
    }

    public interface IChunkingService
    {
        Task<List<(string Content, int PageNumber)>> ExtractChunksAsync(string filePath, string fileType);
    }

    public interface IEmbeddingService
    {
        Task<string> GenerateFakeEmbeddingAsync(string text);
    }
}
