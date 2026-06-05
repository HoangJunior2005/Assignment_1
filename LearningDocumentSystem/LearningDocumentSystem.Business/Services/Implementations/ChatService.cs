using LearningDocumentSystem.Business.DTOs;
using LearningDocumentSystem.Business.Services.Interfaces;
using LearningDocumentSystem.Data.Repositories.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace LearningDocumentSystem.Business.Services.Implementations
{
    public class ChatService : IChatService
    {
        private readonly IUnitOfWork _uow;
        private readonly IEmbeddingService _embeddingService;
        private readonly IGeminiService _geminiService;
        private readonly ILogger<ChatService> _logger;

        private const int ExpectedDimension = 512;

        public ChatService(
            IUnitOfWork uow,
            IEmbeddingService embeddingService,
            IGeminiService geminiService,
            ILogger<ChatService> logger)
        {
            _uow = uow;
            _embeddingService = embeddingService;
            _geminiService = geminiService;
            _logger = logger;
        }

        public async Task<ChatResponseDto> AskQuestionAsync(string question, int? subjectId = null, int? chapterId = null)
        {
            _logger.LogInformation("Processing question: '{Question}' | sub={SubId}, chap={ChapId}", question, subjectId, chapterId);

            var response = new ChatResponseDto();

            if (string.IsNullOrWhiteSpace(question))
            {
                response.Answer = "Vui lòng nhập câu hỏi để tôi có thể hỗ trợ bạn.";
                return response;
            }

            try
            {
                var questionEmbJson = await _embeddingService.GenerateEmbeddingAsync(question);
                var questionVector = JsonSerializer.Deserialize<float[]>(questionEmbJson);

                if (questionVector == null || questionVector.Length == 0)
                {
                    response.Answer = "Đã xảy ra lỗi khi phân tích câu hỏi. Vui lòng thử lại.";
                    return response;
                }

                var chunksInDb = await _uow.DocumentChunks.GetChunksForRAGAsync(subjectId, chapterId);
                var scoredChunks = new List<(float Score, Entities.Models.DocumentChunk Chunk)>();

                foreach (var chunk in chunksInDb)
                {
                    if (chunk.Embedding == null || string.IsNullOrWhiteSpace(chunk.Embedding.VectorData))
                        continue;

                    try
                    {
                        var chunkVector = JsonSerializer.Deserialize<float[]>(chunk.Embedding.VectorData);

                        if (chunkVector == null || chunkVector.Length != ExpectedDimension)
                            continue;

                        float semanticScore = DotProduct(questionVector, chunkVector);
                        float keywordBoost = ComputeKeywordBoost(question, chunk.ContentText);
                        float finalScore = semanticScore + keywordBoost;
                        scoredChunks.Add((finalScore, chunk));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error parsing vector for chunk ID {ChunkId}", chunk.ChunkID);
                    }
                }

                var topChunks = scoredChunks
                    .OrderByDescending(sc => sc.Score)
                    .Take(3)
                    .ToList();

                List<(float Score, Entities.Models.DocumentChunk Chunk)> validChunks;
                if (topChunks.Any())
                {
                    float topScore = topChunks.First().Score;
                    // Require a minimum absolute semantic similarity of 0.05 to filter out
                    // completely unrelated documents, while still catching relevant chunks.
                    // 512-dim dot product scores are naturally much lower than cosine similarity.
                    validChunks = topChunks
                        .Where(tc => tc.Score >= 0.05f && tc.Score >= topScore * 0.5f)
                        .ToList();
                }
                else
                {
                    validChunks = new List<(float Score, Entities.Models.DocumentChunk Chunk)>();
                }

                if (!validChunks.Any())
                {
                    _logger.LogWarning("No compatible 512-dim embeddings found or scores too low. Falling back to keyword-only search.");

                    var keywordScored = chunksInDb
                        .Select(c => (Score: ComputeKeywordBoost(question, c.ContentText), Chunk: c))
                        // Require meaningful keyword match (>= 0.35) to avoid false citations from partial overlap
                        .Where(x => x.Score >= 0.35f)
                        .OrderByDescending(x => x.Score)
                        .Take(3)
                        .ToList();

                    if (keywordScored.Any())
                    {
                        float topScore = keywordScored.First().Score;
                        validChunks = keywordScored
                            .Where(x => x.Score >= topScore * 0.5f)
                            .ToList();
                    }
                    else
                    {
                        validChunks = new List<(float Score, Entities.Models.DocumentChunk Chunk)>();
                    }
                }

                if (!validChunks.Any())
                {
                    response.Answer = "Xin lỗi, tôi không tìm thấy nội dung liên quan trong tài liệu học tập. " +
                                      "Bạn thử chọn đúng môn học ở bên trái, hoặc đặt câu hỏi theo sát các khái niệm trong bài giảng nhé!";
                    return response;
                }

                var contextBuilder = new System.Text.StringBuilder();

                foreach (var item in validChunks)
                {
                    var source = new ChatSourceDto
                    {
                        DocumentID = item.Chunk.DocumentID,
                        DocumentTitle = item.Chunk.Document.Title,
                        PageNumber = item.Chunk.PageNumber,
                        SimilarityScore = Math.Clamp(item.Score * 100f, 5f, 99.9f),
                        ContentSnippet = item.Chunk.ContentText.Length > 300
                            ? item.Chunk.ContentText[..300] + "..."
                            : item.Chunk.ContentText
                    };
                    response.Sources.Add(source);
                    
                    contextBuilder.AppendLine($"[Tài liệu: {source.DocumentTitle}, Trang {source.PageNumber?.ToString() ?? "N/A"}]: {item.Chunk.ContentText}");
                }

                response.Answer = await _geminiService.GenerateAnswerAsync(question, contextBuilder.ToString());

                // If AI indicates it couldn't find relevant information in the context, clear
                // any sources that were tentatively attached (they would be misleading citations).
                if (!string.IsNullOrEmpty(response.Answer) && 
                    (response.Answer.Contains("không tìm thấy", StringComparison.OrdinalIgnoreCase) ||
                     response.Answer.Contains("không có thông tin", StringComparison.OrdinalIgnoreCase) ||
                     response.Answer.Contains("không có trong", StringComparison.OrdinalIgnoreCase) ||
                     response.Answer.Contains("chưa được cấu hình", StringComparison.OrdinalIgnoreCase) ||
                     response.Answer.Contains("đã xảy ra lỗi", StringComparison.OrdinalIgnoreCase) ||
                     response.Answer.Contains("lỗi nội bộ", StringComparison.OrdinalIgnoreCase) ||
                     response.Answer.Contains("không nhận được", StringComparison.OrdinalIgnoreCase) ||
                     response.Answer.Contains("không thể trích xuất", StringComparison.OrdinalIgnoreCase) ||
                     response.Answer.Contains("ngoài phạm vi", StringComparison.OrdinalIgnoreCase) ||
                     response.Answer.Contains("tài liệu không đề cập", StringComparison.OrdinalIgnoreCase)))
                {
                    response.Sources.Clear();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating chatbot answer.");
                response.Answer = "Xin lỗi, đã xảy ra lỗi trong quá trình xử lý câu hỏi. Vui lòng tải lại trang và thử lại.";
            }

            return response;
        }

        private static float DotProduct(float[] a, float[] b)
        {
            float sum = 0f;
            int len = Math.Min(a.Length, b.Length);
            for (int i = 0; i < len; i++)
                sum += a[i] * b[i];
            return sum;
        }

        private static float ComputeKeywordBoost(string question, string chunkContent)
        {
            if (string.IsNullOrWhiteSpace(question) || string.IsNullOrWhiteSpace(chunkContent))
                return 0f;

            var cleanQuestion = RemoveDiacritics(question).ToLowerInvariant();
            var cleanContent = RemoveDiacritics(chunkContent).ToLowerInvariant();

            // Extract all words from the question
            var questionWords = Regex.Matches(cleanQuestion, @"[\p{L}\p{N}]+")
                .Select(m => m.Value)
                .Where(w => w.Length >= 2)
                .Distinct()
                .ToList();

            if (!questionWords.Any()) return 0f;

            // Define common stop words in both Vietnamese (stripped of diacritics) and English
            var stopWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "who", "what", "where", "when", "why", "how", "is", "are", "the", "a", "an", "of", "in", "on", "at", "to", "for", "with", "by", "about",
                "la", "ai", "cua", "trong", "va", "co", "cac", "cho", "nhu", "nhung", "mot", "voi", "duoc", "nay", "khi", "de", "sau", "tai", "noi", "nao", "thi"
            };

            // Filter out stopwords
            var filteredWords = questionWords.Where(w => !stopWords.Contains(w)).ToList();
            if (!filteredWords.Any()) return 0f;

            float boost = 0f;

            // 1. Check exact contiguous phrase matching (Trigram and Bigram)
            // If the user query has consecutive important keywords, finding them together is an extremely strong match.
            if (filteredWords.Count >= 3)
            {
                for (int i = 0; i <= filteredWords.Count - 3; i++)
                {
                    var trigram = $"{filteredWords[i]} {filteredWords[i + 1]} {filteredWords[i + 2]}";
                    if (cleanContent.Contains(trigram))
                    {
                        boost += 1.5f; // High boost for matching full trigram phrase
                    }
                }
            }

            if (filteredWords.Count >= 2)
            {
                for (int i = 0; i <= filteredWords.Count - 2; i++)
                {
                    var bigram = $"{filteredWords[i]} {filteredWords[i + 1]}";
                    if (cleanContent.Contains(bigram))
                    {
                        boost += 0.6f; // Moderate boost for matching bigram phrase
                    }
                }
            }

            // 2. Check individual whole-word matches
            // We use Regex with word boundaries to ensure we only match whole words, preventing sub-word false matches
            int wordMatches = 0;
            foreach (var word in filteredWords)
            {
                var pattern = @"\b" + Regex.Escape(word) + @"\b";
                if (Regex.IsMatch(cleanContent, pattern))
                {
                    wordMatches++;
                }
            }

            boost += 0.15f * ((float)wordMatches / filteredWords.Count);

            return boost;
        }



        private static string RemoveDiacritics(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            string normalizedString = text.Normalize(System.Text.NormalizationForm.FormD);
            var stringBuilder = new System.Text.StringBuilder();

            foreach (char c in normalizedString)
            {
                var unicodeCategory = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != System.Globalization.UnicodeCategory.NonSpacingMark)
                {
                    if (c == 'đ')
                        stringBuilder.Append('d');
                    else if (c == 'Đ')
                        stringBuilder.Append('D');
                    else
                        stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString().Normalize(System.Text.NormalizationForm.FormC);
        }
    }
}
