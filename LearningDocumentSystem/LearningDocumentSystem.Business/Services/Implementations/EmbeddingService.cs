using LearningDocumentSystem.Business.Services.Interfaces;
using LearningDocumentSystem.Common.Constants;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace LearningDocumentSystem.Business.Services.Implementations
{
    public class EmbeddingService : IEmbeddingService
    {
        private readonly ILogger<EmbeddingService> _logger;
        private readonly Random _random = new();

        public EmbeddingService(ILogger<EmbeddingService> logger)
        {
            _logger = logger;
        }

        public Task<string> GenerateFakeEmbeddingAsync(string text)
        {
            _logger.LogDebug("Generating fake embedding for text length: {Len}", text.Length);

            // Tạo vector ngẫu nhiên dim 1536
            var vector = new float[AppConstants.EmbeddingDimension];
            for (int i = 0; i < vector.Length; i++)
            {
                // Giá trị trong khoảng [-1, 1] như embedding thật
                vector[i] = (float)(_random.NextDouble() * 2 - 1);
            }

            // Normalize về unit vector (cosine similarity cần)
            var magnitude = (float)Math.Sqrt(vector.Sum(v => v * v));
            if (magnitude > 0)
                for (int i = 0; i < vector.Length; i++)
                    vector[i] /= magnitude;

            // Serialize thành JSON string để lưu DB
            var json = JsonSerializer.Serialize(vector);
            return Task.FromResult(json);
        }
    }
}
