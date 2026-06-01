namespace LearningDocumentSystem.Business.Services.Interfaces
{
    public interface IGeminiService
    {
        Task<string> GenerateAnswerAsync(string question, string context);
    }
}
