using AutoMapper;
using LearningDocumentSystem.Business.DTOs;
using LearningDocumentSystem.Business.Services.Interfaces;
using LearningDocumentSystem.Common.Constants;
using LearningDocumentSystem.Common.Exceptions;
using LearningDocumentSystem.Common.Helpers;
using LearningDocumentSystem.Data.Repositories.Interfaces;
using LearningDocumentSystem.Entities.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace LearningDocumentSystem.Business.Services.Implementations
{
    public class DocumentService : IDocumentService
    {
        private readonly IUnitOfWork       _uow;
        private readonly IMapper           _mapper;
        private readonly IFileService      _fileService;
        private readonly IChunkingService  _chunkingService;
        private readonly IEmbeddingService _embeddingService;
        private readonly ILogger<DocumentService> _logger;

        // Inject upload path qua constructor (set bởi Web layer)
        private string _uploadPath = string.Empty;

        public DocumentService(
            IUnitOfWork uow,
            IMapper mapper,
            IFileService fileService,
            IChunkingService chunkingService,
            IEmbeddingService embeddingService,
            ILogger<DocumentService> logger)
        {
            _uow              = uow;
            _mapper           = mapper;
            _fileService      = fileService;
            _chunkingService  = chunkingService;
            _embeddingService = embeddingService;
            _logger           = logger;
        }

        public void SetUploadPath(string path) => _uploadPath = path;

        public async Task<(IEnumerable<DocumentDto> Items, int TotalCount)> GetPagedAsync(
            string? keyword, int? subjectId, int? chapterId, int page, int pageSize)
        {
            var (items, total) = await _uow.Documents.GetPagedAsync(
                keyword, subjectId, chapterId, page, pageSize);
            return (_mapper.Map<IEnumerable<DocumentDto>>(items), total);
        }

        public async Task<DocumentDetailDto?> GetDetailAsync(int id)
        {
            var doc = await _uow.Documents.GetWithDetailsAsync(id);
            if (doc == null) return null;

            var dto = _mapper.Map<DocumentDetailDto>(doc);
            dto.Chunks = _mapper.Map<List<ChunkDto>>(doc.Chunks);
            return dto;
        }

        public async Task<DocumentDto> UploadAsync(
            IFormFile file, int chapterId, string title, int uploadedByUserId)
        {
            // Validate chapter tồn tại
            var chapter = await _uow.Chapters.GetByIdAsync(chapterId)
                ?? throw new NotFoundException("Chapter", chapterId);

            // Step 1: Lưu file vật lý
            await _uow.BeginTransactionAsync();
            try
            {
                var storageName = await _fileService.SaveFileAsync(file, _uploadPath);

                // Step 2: Tạo Document record
                var document = new Document
                {
                    ChapterID      = chapterId,
                    Title          = title,
                    FileType       = FileHelper.GetFileType(file.FileName),
                    StoragePath    = storageName,
                    FileSizeInBytes = file.Length,
                    IndexStatus    = AppConstants.StatusPending,
                    UploadedBy     = uploadedByUserId,
                    UploadedAt     = DateTime.UtcNow
                };
                await _uow.Documents.AddAsync(document);
                await _uow.SaveChangesAsync();

                // Step 3: Cập nhật status → Processing
                await _uow.Documents.UpdateStatusAsync(document.DocumentID, AppConstants.StatusProcessing);
                await _uow.SaveChangesAsync();

                // Step 4: Chunking
                var fullPath = Path.Combine(_uploadPath, storageName);
                var chunks   = await _chunkingService.ExtractChunksAsync(fullPath, document.FileType);

                // Step 5: Lưu chunks + embeddings
                var chunkEntities = new List<DocumentChunk>();
                for (int i = 0; i < chunks.Count; i++)
                {
                    var (content, pageNum) = chunks[i];
                    var chunk = new DocumentChunk
                    {
                        DocumentID = document.DocumentID,
                        ChunkIndex = i,
                        PageNumber = pageNum,
                        ContentText = content
                    };
                    chunkEntities.Add(chunk);
                }
                await _uow.DocumentChunks.AddRangeAsync(chunkEntities);
                await _uow.SaveChangesAsync();

                // Step 6: Sinh embedding cho từng chunk
                var embeddings = new List<Embedding>();
                foreach (var chunk in chunkEntities)
                {
                    var vectorJson = await _embeddingService.GenerateFakeEmbeddingAsync(chunk.ContentText);
                    embeddings.Add(new Embedding
                    {
                        ChunkID    = chunk.ChunkID,
                        VectorData = vectorJson,
                        CreatedAt  = DateTime.UtcNow
                    });
                }
                await _uow.Embeddings.AddRangeAsync(embeddings);

                // Step 7: Cập nhật status → Indexed
                await _uow.Documents.UpdateStatusAsync(document.DocumentID, AppConstants.StatusIndexed);
                await _uow.SaveChangesAsync();

                await _uow.CommitAsync();

                _logger.LogInformation(
                    "Document uploaded: {Title}, {ChunkCount} chunks, {EmbCount} embeddings.",
                    title, chunkEntities.Count, embeddings.Count);

                // Reload với full details để map
                var result = await _uow.Documents.GetWithDetailsAsync(document.DocumentID);
                return _mapper.Map<DocumentDto>(result!);
            }
            catch (Exception ex)
            {
                await _uow.RollbackAsync();
                _logger.LogError(ex, "Upload failed for: {Title}", title);
                throw;
            }
        }

        public async Task DeleteAsync(int id)
        {
            var doc = await _uow.Documents.GetByIdAsync(id)
                ?? throw new NotFoundException("Document", id);

            // Xóa file vật lý
            _fileService.DeleteFile(doc.StoragePath, _uploadPath);

            _uow.Documents.Remove(doc);
            await _uow.SaveChangesAsync();
            _logger.LogInformation("Document deleted: {Id}", id);
        }

        public async Task<DashboardDto> GetDashboardAsync()
        {
            var totalDocs    = await _uow.Documents.CountAsync();
            var totalChunks  = await _uow.DocumentChunks.CountAsync();
            var totalSubjects = await _uow.Subjects.CountAsync();
            var totalUsers   = await _uow.Users.CountAsync();
            var indexed      = await _uow.Documents.CountByStatusAsync(AppConstants.StatusIndexed);
            var pending      = await _uow.Documents.CountByStatusAsync(AppConstants.StatusPending);
            var processing   = await _uow.Documents.CountByStatusAsync(AppConstants.StatusProcessing);
            var failed       = await _uow.Documents.CountByStatusAsync(AppConstants.StatusFailed);

            var (recent, _) = await _uow.Documents.GetPagedAsync(null, null, null, 1, 5);

            return new DashboardDto
            {
                TotalDocuments    = totalDocs,
                TotalChunks       = totalChunks,
                TotalSubjects     = totalSubjects,
                TotalUsers        = totalUsers,
                IndexedDocuments  = indexed,
                PendingDocuments  = pending,
                ProcessingDocuments = processing,
                FailedDocuments   = failed,
                RecentDocuments   = _mapper.Map<List<DocumentDto>>(recent)
            };
        }
    }
}
