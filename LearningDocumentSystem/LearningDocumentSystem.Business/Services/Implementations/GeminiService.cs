using LearningDocumentSystem.Business.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace LearningDocumentSystem.Business.Services.Implementations
{
    public class GeminiService : IGeminiService
    {
        private readonly HttpClient _httpClient;
        private readonly string? _apiKey;
        private readonly string _modelName;
        private readonly ILogger<GeminiService> _logger;

        public GeminiService(HttpClient httpClient, IConfiguration configuration, ILogger<GeminiService> logger)
        {
            _httpClient = httpClient;
            _apiKey = configuration["Gemini:ApiKey"];
            _modelName = configuration["Gemini:ModelName"] ?? "gemini-1.5-pro";
            _logger = logger;
        }

        public async Task<string> GenerateAnswerAsync(string question, string context)
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                _logger.LogWarning("Gemini API Key is not configured. Falling back to default message.");
                return "Hệ thống chưa được cấu hình API Key của Gemini. Vui lòng liên hệ quản trị viên.";
            }

            string systemPrompt = @"Bạn là trợ lý học tập AI.
Vui lòng trả lời câu hỏi của người dùng CHỈ DỰA TRÊN ngữ cảnh (context) được cung cấp dưới đây.
Nếu thông tin không có trong ngữ cảnh, hãy nói 'Xin lỗi, tôi không tìm thấy thông tin liên quan trong tài liệu học tập.' Không tự bịa thêm thông tin.
Trả lời bằng tiếng Việt, định dạng rõ ràng, ngắn gọn và dễ hiểu.
KHÔNG tự động thêm phần chú thích trích dẫn nguồn ở cuối câu trả lời (như các câu mở ngoặc đơn dạng '(theo tài liệu..., trang...)'), vì hệ thống đã tự động hiển thị các thẻ nguồn ở giao diện bên dưới.

Ngữ cảnh:
";
            
            string fullPrompt = $"{systemPrompt}\n{context}\n\nCâu hỏi: {question}\nTrả lời:";

            var payload = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[] { new { text = fullPrompt } }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.2, // Low temperature for factual RAG
                    maxOutputTokens = 1024
                }
            };

            var jsonPayload = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            string url = $"https://generativelanguage.googleapis.com/v1beta/models/{_modelName}:generateContent?key={_apiKey}";

            try
            {
                var response = await _httpClient.PostAsync(url, content);
                
                if (!response.IsSuccessStatusCode)
                {
                    string errorBody = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Gemini API error. Status: {StatusCode}. Body: {Body}", response.StatusCode, errorBody);
                    return "Đã xảy ra lỗi khi kết nối tới mô hình AI.";
                }

                var responseString = await response.Content.ReadAsStringAsync();
                
                using var jsonDoc = JsonDocument.Parse(responseString);
                var candidates = jsonDoc.RootElement.GetProperty("candidates");
                
                if (candidates.GetArrayLength() > 0)
                {
                    var text = candidates[0]
                        .GetProperty("content")
                        .GetProperty("parts")[0]
                        .GetProperty("text")
                        .GetString();
                        
                    return text ?? "Không nhận được phản hồi từ AI.";
                }

                return "Không thể trích xuất câu trả lời từ hệ thống.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while calling Gemini API");
                return "Lỗi nội bộ khi xử lý câu trả lời với AI.";
            }
        }
    }
}
