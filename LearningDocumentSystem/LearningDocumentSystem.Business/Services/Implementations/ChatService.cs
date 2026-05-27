using LearningDocumentSystem.Business.DTOs;
using LearningDocumentSystem.Business.Services.Interfaces;
using LearningDocumentSystem.Data.Repositories.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace LearningDocumentSystem.Business.Services.Implementations
{
    public class ChatService : IChatService
    {
        private readonly IUnitOfWork _uow;
        private readonly IEmbeddingService _embeddingService;
        private readonly ILogger<ChatService> _logger;

        public ChatService(
            IUnitOfWork uow,
            IEmbeddingService embeddingService,
            ILogger<ChatService> logger)
        {
            _uow = uow;
            _embeddingService = embeddingService;
            _logger = logger;
        }

        public async Task<ChatResponseDto> AskQuestionAsync(string question, int? subjectId = null, int? chapterId = null)
        {
            _logger.LogInformation("Processing question: '{Question}' with filter sub={SubId}, chap={ChapId}", question, subjectId, chapterId);

            var response = new ChatResponseDto();

            if (string.IsNullOrWhiteSpace(question))
            {
                response.Answer = "Vui lòng nhập câu hỏi để tôi có thể hỗ trợ bạn.";
                return response;
            }

            try
            {
                // Step 1: Sinh vector embedding giả lập cho câu hỏi của người dùng
                var questionEmbJson = await _embeddingService.GenerateFakeEmbeddingAsync(question);
                var questionVector = JsonSerializer.Deserialize<float[]>(questionEmbJson);

                if (questionVector == null || questionVector.Length == 0)
                {
                    response.Answer = "Đã xảy ra lỗi khi tạo mã nhúng câu hỏi. Vui lòng thử lại.";
                    return response;
                }

                // Step 2: Lấy tất cả chunks hoạt động từ cơ sở dữ liệu (có lọc theo Môn học / Chương học)
                var chunksInDb = await _uow.DocumentChunks.GetChunksForRAGAsync(subjectId, chapterId);
                var scoredChunks = new List<(float Score, Entities.Models.DocumentChunk Chunk)>();

                // Step 3: Tính độ tương đồng Cosine (Dot product vì vector đã chuẩn hóa magnitude = 1)
                foreach (var chunk in chunksInDb)
                {
                    if (chunk.Embedding == null || string.IsNullOrWhiteSpace(chunk.Embedding.VectorData))
                        continue;

                    try
                    {
                        var chunkVector = JsonSerializer.Deserialize<float[]>(chunk.Embedding.VectorData);
                        if (chunkVector == null || chunkVector.Length != questionVector.Length)
                            continue;

                        // Tính Tích vô hướng (Dot Product)
                        float score = 0f;
                        for (int i = 0; i < questionVector.Length; i++)
                        {
                            score += questionVector[i] * chunkVector[i];
                        }

                        // Điều chỉnh điểm số chút ít nếu câu hỏi chứa từ khóa của chunk (tăng tính phù hợp ngữ cảnh thực tế)
                        var words = question.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        int matches = 0;
                        foreach (var word in words)
                        {
                            if (word.Length > 2 && chunk.ContentText.ToLowerInvariant().Contains(word))
                            {
                                matches++;
                            }
                        }
                        if (matches > 0)
                        {
                            score += 0.05f * Math.Min(matches, 5); // Boost điểm nếu khớp từ khóa
                        }

                        scoredChunks.Add((score, chunk));
                    }
                    catch (Exception ex)
                      {
                        _logger.LogWarning(ex, "Error parsing vector for chunk ID {ChunkId}", chunk.ChunkID);
                    }
                }

                // Step 4: Sắp xếp giảm dần và lấy tối đa top 3 phân đoạn phù hợp nhất
                var topChunks = scoredChunks
                    .OrderByDescending(sc => sc.Score)
                    .Take(3)
                    .ToList();

                // Chỉ giữ các phân đoạn có độ tương đồng tích cực
                var validChunks = topChunks.Where(tc => tc.Score > 0.05f).ToList();

                if (!validChunks.Any())
                {
                    response.Answer = "Xin lỗi, tôi không tìm thấy tài liệu liên quan nào trong phạm vi môn học hoặc chương học đã chọn để trả lời câu hỏi của bạn. Bạn vui lòng cung cấp câu hỏi chi tiết hơn hoặc tải thêm tài liệu nhé!";
                    return response;
                }

                // Step 5: Đóng gói nguồn trích dẫn
                foreach (var item in validChunks)
                {
                    var source = new ChatSourceDto
                    {
                        DocumentID = item.Chunk.DocumentID,
                        DocumentTitle = item.Chunk.Document.Title,
                        PageNumber = item.Chunk.PageNumber,
                        SimilarityScore = Math.Clamp(item.Score * 100f, 10f, 99.9f), // Quy đổi sang tỷ lệ phần trăm trực quan
                        ContentSnippet = item.Chunk.ContentText.Length > 300 
                            ? item.Chunk.ContentText[..300] + "..." 
                            : item.Chunk.ContentText
                    };
                    response.Sources.Add(source);
                }

                // Step 6: Tạo câu trả lời AI chất lượng cao (Mock RAG Engine)
                response.Answer = GenerateRagResponse(question, validChunks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating chatbot answer.");
                response.Answer = "Xin lỗi, đã xảy ra lỗi trong quá trình xử lý câu hỏi. Vui lòng tải lại trang và thử lại.";
            }

            return response;
        }

        /// <summary>
        /// Tạo câu trả lời RAG thông minh bằng cách phân tích văn cảnh của các chunks được truy xuất
        /// </summary>
        private string GenerateRagResponse(string question, List<(float Score, Entities.Models.DocumentChunk Chunk)> matchedChunks)
        {
            var bestChunk = matchedChunks.First().Chunk;
            var docTitle = bestChunk.Document.Title;
            var pageInfo = bestChunk.PageNumber.HasValue ? $"trang {bestChunk.PageNumber.Value}" : "văn bản";
            var chapterName = bestChunk.Document.Chapter?.ChapterName ?? "tài liệu";
            var chapterNum = bestChunk.Document.Chapter?.ChapterNumber ?? 1;

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Dựa trên tài liệu **\"{docTitle}\"** (Chương {chapterNum}: *{chapterName}*, {pageInfo}), tôi xin trả lời câu hỏi của bạn như sau:\n");

            // Tạo nội dung trả lời dựa trên văn cảnh trích xuất được
            var contextText = bestChunk.ContentText;
            
            // Tìm các đoạn văn ngắn chứa từ khóa từ văn cảnh hoặc trả về đoạn văn chất lượng cao
            var segments = contextText.Split(new[] { '.', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                                     .Select(s => s.Trim())
                                     .Where(s => s.Length > 25)
                                     .Take(4)
                                     .ToList();

            if (segments.Any())
            {
                sb.AppendLine("Dưới đây là các luận điểm chính từ tài liệu học tập:");
                foreach (var seg in segments)
                {
                    sb.AppendLine($"- **{seg.Split(':').First()}**: {string.Join(':', seg.Split(':').Skip(1))}");
                }
            }
            else
            {
                sb.AppendLine(contextText);
            }

            // Nếu có nhiều hơn 1 nguồn trích dẫn phù hợp, mở rộng câu trả lời thêm thông tin bổ trợ
            if (matchedChunks.Count > 1)
            {
                var secondChunk = matchedChunks[1].Chunk;
                sb.AppendLine($"\nNgoài ra, tài liệu cũng bổ sung thêm thông tin tại chương học liên quan:");
                
                var extraSegments = secondChunk.ContentText.Split(new[] { '.', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                                                          .Select(s => s.Trim())
                                                          .Where(s => s.Length > 30)
                                                          .Take(2)
                                                          .ToList();
                foreach (var es in extraSegments)
                {
                    sb.AppendLine($"> *\"{es}\"*");
                }
            }

            sb.AppendLine("\n*Hy vọng thông tin này giúp ích cho quá trình ôn tập môn học của bạn!*");

            return sb.ToString();
        }
    }
}
