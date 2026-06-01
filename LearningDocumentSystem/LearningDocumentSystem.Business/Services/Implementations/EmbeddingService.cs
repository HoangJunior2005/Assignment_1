using LearningDocumentSystem.Business.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace LearningDocumentSystem.Business.Services.Implementations
{
    public class EmbeddingService : IEmbeddingService
    {
        private readonly ILogger<EmbeddingService> _logger;

        private const int VectorDimension = 512;

        private static readonly HashSet<string> VietnameseStopWords = new(StringComparer.OrdinalIgnoreCase)
        {
            "và", "của", "là", "có", "trong", "cho", "với", "các", "được", "không",
            "này", "đó", "một", "những", "để", "hay", "hoặc", "thì", "mà", "khi",
            "về", "theo", "từ", "tại", "bởi", "vì", "nên", "đã", "sẽ", "đang",
            "rằng", "như", "cũng", "chỉ", "vào", "ra", "lên", "xuống", "qua",
            "trên", "dưới", "sau", "trước", "nếu", "vậy", "thế", "còn", "nhiều",
            "hơn", "nhất", "rất", "quá", "cả", "mọi", "bao", "gồm", "tất", "cần",
            "the", "is", "in", "of", "and", "to", "a", "an", "for", "on", "at",
            "by", "with", "as", "be", "are", "was", "were", "has", "have", "had"
        };

        public EmbeddingService(ILogger<EmbeddingService> logger)
        {
            _logger = logger;
        }

        public Task<string> GenerateEmbeddingAsync(string text)
        {
            _logger.LogDebug("Generating embedding for text (length={Len})", text.Length);

            if (string.IsNullOrWhiteSpace(text))
            {
                var zeroVec = new float[VectorDimension];
                return Task.FromResult(JsonSerializer.Serialize(zeroVec));
            }

            var tokens = Tokenize(text);

            if (tokens.Count == 0)
            {
                var zeroVec = new float[VectorDimension];
                return Task.FromResult(JsonSerializer.Serialize(zeroVec));
            }

            var termFrequency = ComputeTermFrequency(tokens);

            var vector = new float[VectorDimension];

            foreach (var (term, rawTf) in termFrequency)
            {
                float tfWeight = (float)(Math.Log(1.0 + rawTf) / Math.Log(1.0 + tokens.Count));

                int hash = GetStableHash(term);
                int index = Math.Abs(hash % VectorDimension);

                float sign = (hash >= 0) ? 1f : -1f;
                vector[index] += sign * tfWeight;
            }

            NormalizeL2(vector);

            var json = JsonSerializer.Serialize(vector);
            return Task.FromResult(json);
        }

        private static List<string> Tokenize(string text)
        {
            var lower = text.ToLowerInvariant();

            var wordMatches = Regex.Matches(lower, @"[\p{L}\p{N}]+");
            var words = wordMatches
                .Select(m => m.Value)
                .Where(w => w.Length >= 2 && !VietnameseStopWords.Contains(w) && !VietnameseStopWords.Contains(RemoveDiacritics(w)))
                .ToList();

            var wordsNoAccents = wordMatches
                .Select(m => RemoveDiacritics(m.Value))
                .Where(w => w.Length >= 2 && !VietnameseStopWords.Contains(w) && !VietnameseStopWords.Contains(RemoveDiacritics(w)))
                .ToList();

            if (words.Count == 0 && wordsNoAccents.Count == 0) return new List<string>();

            var tokens = new List<string>();
            tokens.AddRange(words);
            tokens.AddRange(wordsNoAccents);

            // Add bigrams
            for (int i = 0; i < words.Count - 1; i++)
            {
                tokens.Add($"{words[i]}_{words[i + 1]}");
                tokens.Add($"{wordsNoAccents[i]}_{wordsNoAccents[i + 1]}");
            }

            return tokens.Distinct().ToList();
        }

        private static Dictionary<string, int> ComputeTermFrequency(List<string> tokens)
        {
            var tf = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var token in tokens)
            {
                tf.TryGetValue(token, out int current);
                tf[token] = current + 1;
            }
            return tf;
        }

        private static int GetStableHash(string term)
        {
            const uint FnvPrime = 16777619u;
            const uint FnvOffsetBasis = 2166136261u;

            uint hash = FnvOffsetBasis;
            foreach (char c in term)
            {
                hash ^= (uint)c;
                hash *= FnvPrime;
            }

            return (int)hash;
        }

        private static void NormalizeL2(float[] vector)
        {
            float sumOfSquares = 0f;
            foreach (var v in vector)
                sumOfSquares += v * v;

            if (sumOfSquares <= 1e-12f) return;

            float magnitude = (float)Math.Sqrt(sumOfSquares);
            for (int i = 0; i < vector.Length; i++)
                vector[i] /= magnitude;
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
