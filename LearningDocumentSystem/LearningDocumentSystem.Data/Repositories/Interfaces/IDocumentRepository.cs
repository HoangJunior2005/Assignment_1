using LearningDocumentSystem.Entities.Models;

namespace LearningDocumentSystem.Data.Repositories.Interfaces
{
    public interface ISubjectRepository : IGenericRepository<Subject>
    {
        Task<Subject?> GetWithChaptersAsync(int subjectId);
        Task<bool> IsCodeExistsAsync(string code, int? excludeId = null);
        Task<IEnumerable<Subject>> GetAllActiveAsync();
    }

    public interface IChapterRepository : IGenericRepository<Chapter>
    {
        Task<IEnumerable<Chapter>> GetBySubjectIdAsync(int subjectId);
        Task<Chapter?> GetWithDocumentsAsync(int chapterId);
        Task<bool> IsChapterNumberExistsAsync(int subjectId, int chapterNumber, int? excludeId = null);
    }

    public interface IDocumentRepository : IGenericRepository<Document>
    {
        Task<Document?> GetWithDetailsAsync(int documentId);
        Task<IEnumerable<Document>> GetByChapterIdAsync(int chapterId);
        Task<IEnumerable<Document>> SearchAsync(string? keyword, int? subjectId, int? chapterId);
        Task<(IEnumerable<Document> Items, int TotalCount)> GetPagedAsync(
            string? keyword, int? subjectId, int? chapterId, string? status,
            int page, int pageSize);
        Task<int> CountByStatusAsync(string status);
        Task UpdateStatusAsync(int documentId, string status);
    }

    public interface IDocumentChunkRepository : IGenericRepository<DocumentChunk>
    {
        Task<IEnumerable<DocumentChunk>> GetByDocumentIdAsync(int documentId);
        Task<int> CountByDocumentIdAsync(int documentId);
        Task DeleteByDocumentIdAsync(int documentId);
        Task<IEnumerable<DocumentChunk>> GetChunksForRAGAsync(int? subjectId, int? chapterId);
    }

    public interface IEmbeddingRepository : IGenericRepository<Embedding>
    {
        Task<Embedding?> GetByChunkIdAsync(int chunkId);
    }
}
