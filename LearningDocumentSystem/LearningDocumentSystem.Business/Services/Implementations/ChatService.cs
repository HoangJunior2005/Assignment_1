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

                var validChunks = topChunks.Where(tc => tc.Score > 0.01f).ToList();

                if (!validChunks.Any())
                {
                    _logger.LogWarning("No compatible 512-dim embeddings found. Falling back to keyword-only search.");

                    var keywordScored = chunksInDb
                        .Select(c => (Score: ComputeKeywordBoost(question, c.ContentText), Chunk: c))
                        .Where(x => x.Score > 0f)
                        .OrderByDescending(x => x.Score)
                        .Take(3)
                        .ToList();

                    validChunks = keywordScored;
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

                if (!string.IsNullOrEmpty(response.Answer) && 
                    (response.Answer.Contains("không tìm thấy", StringComparison.OrdinalIgnoreCase) ||
                     response.Answer.Contains("chưa được cấu hình", StringComparison.OrdinalIgnoreCase) ||
                     response.Answer.Contains("đã xảy ra lỗi", StringComparison.OrdinalIgnoreCase) ||
                     response.Answer.Contains("lỗi nội bộ", StringComparison.OrdinalIgnoreCase) ||
                     response.Answer.Contains("không nhận được", StringComparison.OrdinalIgnoreCase) ||
                     response.Answer.Contains("không thể trích xuất", StringComparison.OrdinalIgnoreCase)))
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
            var questionWords = Regex.Matches(question.ToLowerInvariant(), @"[\p{L}\p{N}]+")
                .Select(m => m.Value)
                .Where(w => w.Length >= 3)
                .Distinct()
                .ToList();

            if (!questionWords.Any()) return 0f;

            var contentLower = chunkContent.ToLowerInvariant();
            int matches = questionWords.Count(word => contentLower.Contains(word));

            return 0.25f * ((float)matches / questionWords.Count);
        }

        private static string GenerateAnswerFromContext(
            string question,
            List<(float Score, Entities.Models.DocumentChunk Chunk)> matchedChunks)
        {
            var bestChunk = matchedChunks.First().Chunk;
            var docTitle = bestChunk.Document.Title;
            var pageInfo = bestChunk.PageNumber.HasValue ? $"trang {bestChunk.PageNumber.Value}" : "tài liệu";
            var chapterName = bestChunk.Document.Chapter?.ChapterName ?? "tài liệu";
            var chapterNum = bestChunk.Document.Chapter?.ChapterNumber ?? 1;

            var keywords = Regex.Matches(question.ToLowerInvariant(), @"[\p{L}\p{N}]+")
                .Select(m => m.Value)
                .Where(w => w.Length >= 3)
                .ToHashSet();

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Dựa trên tài liệu **\"{docTitle}\"** (Chương {chapterNum}: *{chapterName}*, {pageInfo}):\n");

            var relevantSentences = ExtractRelevantSentences(bestChunk.ContentText, keywords, topN: 4);

            if (relevantSentences.Any())
            {
                var cleaned = relevantSentences
                    .Select(CleanFragmentStart)
                    .Where(s => s.Length >= 10)
                    .ToList();

                sb.AppendLine(string.Join(" ", cleaned.Any() ? cleaned : relevantSentences));
            }
            else
            {
                var cleanText = NormalizeLineBreaks(bestChunk.ContentText);
                var preview = cleanText.Length > 500 ? cleanText[..500] + "..." : cleanText;
                sb.AppendLine(preview);
            }

            if (matchedChunks.Count > 1)
            {
                var secondChunk = matchedChunks[1].Chunk;
                var extraSentences = ExtractRelevantSentences(secondChunk.ContentText, keywords, topN: 2);

                if (extraSentences.Any())
                {
                    sb.AppendLine($"\nThông tin bổ sung từ tài liệu liên quan:");
                    foreach (var es in extraSentences)
                    {
                        sb.AppendLine($"> *\"{es}\"*");
                    }
                }
            }

            sb.AppendLine("\n*Hy vọng thông tin này giúp ích cho việc ôn tập của bạn!*");
            return sb.ToString();
        }

        private static string NormalizeLineBreaks(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            var normalized = Regex.Replace(text, @"(?<![.!?:;\n])\n", " ");
            normalized = Regex.Replace(normalized, @" {2,}", " ");

            return normalized.Trim();
        }

        private static string CleanFragmentStart(string sentence)
        {
            if (string.IsNullOrWhiteSpace(sentence)) return sentence;

            sentence = sentence.Trim();
            if (sentence.Length == 0) return sentence;

            char first = sentence[0];

            if (char.IsLower(first))
            {
                int firstSpace = sentence.IndexOf(' ');
                if (firstSpace > 0 && firstSpace < sentence.Length - 1)
                {
                    string rest = sentence[(firstSpace + 1)..].TrimStart();
                    if (rest.Length > 0 && char.IsLower(rest[0]))
                        return char.ToUpperInvariant(rest[0]) + rest[1..];
                    return rest;
                }
                return string.Empty;
            }

            return sentence;
        }

        private static List<string> ExtractRelevantSentences(string text, HashSet<string> keywords, int topN)
        {
            if (string.IsNullOrWhiteSpace(text) || keywords.Count == 0)
                return new List<string>();

            var normalized = NormalizeLineBreaks(text);

            var sentences = Regex.Split(normalized, @"(?<=[.!?])\s+")
                .Select(s => s.Trim())
                .Where(s => s.Length >= 20)
                .ToList();

            if (!sentences.Any()) return new List<string>();

            var scored = sentences.Select((sentence, originalIndex) =>
            {
                var sentenceLower = sentence.ToLowerInvariant();
                int keywordHits = keywords.Count(kw => sentenceLower.Contains(kw));
                return (Score: keywordHits, Index: originalIndex, Sentence: sentence);
            })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .Take(topN)
            .OrderBy(x => x.Index)
            .Select(x => x.Sentence)
            .ToList();

            if (!scored.Any())
            {
                scored = sentences.Take(topN).ToList();
            }

            return scored;
        }
    }
}
