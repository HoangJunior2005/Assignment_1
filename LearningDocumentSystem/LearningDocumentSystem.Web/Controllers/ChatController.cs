using LearningDocumentSystem.Business.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LearningDocumentSystem.Web.Controllers
{
    [Authorize]
    public class ChatController : Controller
    {
        private readonly IChatService _chatService;
        private readonly ISubjectService _subjectService;
        private readonly ILogger<ChatController> _logger;

        public ChatController(
            IChatService chatService,
            ISubjectService subjectService,
            ILogger<ChatController> logger)
        {
            _chatService = chatService;
            _subjectService = subjectService;
            _logger = logger;
        }

        // GET: /Chat
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var subjects = await _subjectService.GetAllAsync();
            ViewBag.Subjects = subjects;
            return View();
        }

        // POST: /Chat/Ask
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Ask(string question, int? subjectId, int? chapterId)
        {
            if (string.IsNullOrWhiteSpace(question))
            {
                return Json(new { answer = "Vui lòng nhập câu hỏi hợp lệ." });
            }

            try
            {
                var result = await _chatService.AskQuestionAsync(question.Trim(), subjectId, chapterId);
                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing chat question in Controller.");
                return Json(new { answer = "Đã xảy ra lỗi hệ thống khi xử lý câu hỏi của bạn. Vui lòng thử lại sau." });
            }
        }
    }
}
