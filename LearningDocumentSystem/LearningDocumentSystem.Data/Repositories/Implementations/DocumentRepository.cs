using System;
using LearningDocumentSystem.Data.DbContexts;
using LearningDocumentSystem.Data.Repositories.Interfaces;
using LearningDocumentSystem.Entities.Models;
using Microsoft.EntityFrameworkCore;

namespace LearningDocumentSystem.Data.Repositories.Implementations
{
    public class SubjectRepository : GenericRepository<Subject>, ISubjectRepository
    {
        public SubjectRepository(AppDbContext context) : base(context) { }

        public override async Task<IEnumerable<Subject>> GetAllAsync()
            => await _context.Subjects
                .Include(s => s.Chapters)
                    .ThenInclude(c => c.Documents)
                .OrderBy(s => s.SubjectName)
                .ToListAsync();

        public async Task<Subject?> GetWithChaptersAsync(int subjectId)
            => await _context.Subjects
                .Include(s => s.Chapters.OrderBy(c => c.ChapterNumber))
                    .ThenInclude(c => c.Documents)
                .FirstOrDefaultAsync(s => s.SubjectID == subjectId);

        public async Task<bool> IsCodeExistsAsync(string code, int? excludeId = null)
            => await _context.Subjects
                .AnyAsync(s => s.SubjectCode == code && (!excludeId.HasValue || s.SubjectID != excludeId));

        public async Task<IEnumerable<Subject>> GetAllActiveAsync()
            => await GetAllAsync();
    }

    public class ChapterRepository : GenericRepository<Chapter>, IChapterRepository
    {
        public ChapterRepository(AppDbContext context) : base(context) { }

        public override async Task<IEnumerable<Chapter>> GetAllAsync()
            => await _context.Chapters
                .Include(c => c.Subject)
                .Include(c => c.Documents)
                .OrderBy(c => c.Subject.SubjectName)
                .ThenBy(c => c.ChapterNumber)
                .ToListAsync();

        public async Task<IEnumerable<Chapter>> GetBySubjectIdAsync(int subjectId)
            => await _context.Chapters
                .Include(c => c.Subject)
                .Include(c => c.Documents)
                .Where(c => c.SubjectID == subjectId)
                .OrderBy(c => c.ChapterNumber)
                .ToListAsync();

        public async Task<Chapter?> GetWithDocumentsAsync(int chapterId)
            => await _context.Chapters
                .Include(c => c.Subject)
                .Include(c => c.Documents)
                .FirstOrDefaultAsync(c => c.ChapterID == chapterId);

        public async Task<bool> IsChapterNumberExistsAsync(int subjectId, int chapterNumber, int? excludeId = null)
            => await _context.Chapters
                .AnyAsync(c => c.SubjectID == subjectId
                            && c.ChapterNumber == chapterNumber
                            && (!excludeId.HasValue || c.ChapterID != excludeId));
    }

    public class DocumentRepository : GenericRepository<Document>, IDocumentRepository
    {
        public DocumentRepository(AppDbContext context) : base(context) { }

        public async Task<Document?> GetWithDetailsAsync(int documentId)
            => await _context.Documents
                .Include(d => d.Chapter).ThenInclude(c => c.Subject)
                .Include(d => d.UploadedByUser)
                .Include(d => d.Chunks).ThenInclude(c => c.Embedding)
                .FirstOrDefaultAsync(d => d.DocumentID == documentId);

        public async Task<IEnumerable<Document>> GetByChapterIdAsync(int chapterId)
            => await _context.Documents
                .Include(d => d.Chapter).ThenInclude(c => c.Subject)
                .Include(d => d.UploadedByUser)
                .Where(d => d.ChapterID == chapterId)
                .OrderByDescending(d => d.UploadedAt)
                .ToListAsync();

        public async Task<IEnumerable<Document>> SearchAsync(string? keyword, int? subjectId, int? chapterId)
        {
            var query = _context.Documents
                .Include(d => d.Chapter).ThenInclude(c => c.Subject)
                .Include(d => d.UploadedByUser)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
                query = query.Where(d => d.Title.Contains(keyword));
            if (subjectId.HasValue)
                query = query.Where(d => d.Chapter.SubjectID == subjectId);
            if (chapterId.HasValue)
                query = query.Where(d => d.ChapterID == chapterId);

            return await query.OrderByDescending(d => d.UploadedAt).ToListAsync();
        }

        public async Task<(IEnumerable<Document> Items, int TotalCount)> GetPagedAsync(
            string? keyword, int? subjectId, int? chapterId, string? status, int page, int pageSize)
        {
            var query = _context.Documents
                .Include(d => d.Chapter).ThenInclude(c => c.Subject)
                .Include(d => d.UploadedByUser)
                .Include(d => d.Chunks)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
                query = query.Where(d => d.Title.Contains(keyword));
            if (subjectId.HasValue)
                query = query.Where(d => d.Chapter.SubjectID == subjectId);
            if (chapterId.HasValue)
                query = query.Where(d => d.ChapterID == chapterId);
            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(d => d.IndexStatus == status);

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(d => d.UploadedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, total);
        }

        public async Task<int> CountByStatusAsync(string status)
            => await _context.Documents.CountAsync(d => d.IndexStatus == status);

        public async Task UpdateStatusAsync(int documentId, string status)
        {
            var doc = await _context.Documents.FindAsync(documentId);
            if (doc != null)
            {
                doc.IndexStatus = status;
                doc.IndexedAt = string.Equals(status, "Indexed", StringComparison.OrdinalIgnoreCase)
                    ? DateTime.UtcNow
                    : null;
                _context.Documents.Update(doc);
            }
        }
    }

    public class DocumentChunkRepository : GenericRepository<DocumentChunk>, IDocumentChunkRepository
    {
        public DocumentChunkRepository(AppDbContext context) : base(context) { }

        public async Task<IEnumerable<DocumentChunk>> GetByDocumentIdAsync(int documentId)
            => await _context.DocumentChunks
                .Include(c => c.Embedding)
                .Where(c => c.DocumentID == documentId)
                .OrderBy(c => c.ChunkIndex)
                .ToListAsync();

        public async Task<int> CountByDocumentIdAsync(int documentId)
            => await _context.DocumentChunks.CountAsync(c => c.DocumentID == documentId);

        public async Task DeleteByDocumentIdAsync(int documentId)
        {
            var chunks = await _context.DocumentChunks
                .Where(c => c.DocumentID == documentId)
                .ToListAsync();
            _context.DocumentChunks.RemoveRange(chunks);
        }

        public async Task<IEnumerable<DocumentChunk>> GetChunksForRAGAsync(int? subjectId, int? chapterId)
        {
            var query = _context.DocumentChunks
                .Include(c => c.Embedding)
                .Include(c => c.Document).ThenInclude(d => d.Chapter)
                .Where(c => c.Document.IndexStatus == "Indexed")
                .AsQueryable();

            if (subjectId.HasValue)
                query = query.Where(c => c.Document.Chapter.SubjectID == subjectId.Value);

            if (chapterId.HasValue)
                query = query.Where(c => c.Document.ChapterID == chapterId.Value);

            return await query.ToListAsync();
        }
    }

    public class EmbeddingRepository : GenericRepository<Embedding>, IEmbeddingRepository
    {
        public EmbeddingRepository(AppDbContext context) : base(context) { }

        public async Task<Embedding?> GetByChunkIdAsync(int chunkId)
            => await _context.Embeddings.FirstOrDefaultAsync(e => e.ChunkID == chunkId);
    }
}
