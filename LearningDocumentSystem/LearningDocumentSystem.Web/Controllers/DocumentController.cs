using LearningDocumentSystem.Business.DTOs;
using LearningDocumentSystem.Business.Services.Interfaces;
using LearningDocumentSystem.Common.Constants;
using LearningDocumentSystem.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Security.Claims;

namespace LearningDocumentSystem.Web.Controllers
{
    [Authorize]
    public class DocumentController : Controller
    {
        private readonly IDocumentService _documentService;
        private readonly ISubjectService  _subjectService;
        private readonly IChapterService  _chapterService;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<DocumentController> _logger;

        public DocumentController(
            IDocumentService documentService,
            ISubjectService subjectService,
            IChapterService chapterService,
            IWebHostEnvironment env,
            ILogger<DocumentController> logger)
        {
            _documentService = documentService;
            _subjectService  = subjectService;
            _chapterService  = chapterService;
            _env             = env;
            _logger          = logger;
        }

        // GET: /Document
        [HttpGet]
        public async Task<IActionResult> Index(
            string? keyword, int? subjectId, int? chapterId, string? status, int page = 1)
        {
            var (items, total) = await _documentService.GetPagedAsync(
                keyword, subjectId, chapterId, status, page, AppConstants.DefaultPageSize);

            var subjects = await _subjectService.GetAllAsync();
            var chapters = chapterId.HasValue || subjectId.HasValue
                ? await _chapterService.GetBySubjectAsync(subjectId ?? 0)
                : [];

            var vm = new DocumentListViewModel
            {
                Documents         = items,
                Subjects          = subjects,
                Chapters          = chapters,
                Keyword           = keyword,
                SelectedSubjectId = subjectId,
                SelectedChapterId = chapterId,
                SelectedStatus    = status,
                CurrentPage       = page,
                TotalCount        = total,
                TotalPages        = (int)Math.Ceiling(total / (double)AppConstants.DefaultPageSize),
                PageSize          = AppConstants.DefaultPageSize
            };

            return View(vm);
        }

        // GET: /Document/Upload
        [HttpGet]
        [Authorize(Policy = "TeacherUp")]
        public async Task<IActionResult> Upload()
        {
            var subjects = await _subjectService.GetAllAsync();
            var chapters = await _chapterService.GetAllAsync();
            return View(new DocumentUploadViewModel
            {
                Subjects = subjects,
                Chapters = chapters
            });
        }

        // POST: /Document/Upload
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "TeacherUp")]
        public async Task<IActionResult> Upload(DocumentUploadViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Subjects = await _subjectService.GetAllAsync();
                model.Chapters = await _chapterService.GetAllAsync();
                return View(model);
            }

            try
            {
                // Lấy UserID từ claims
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!int.TryParse(userIdStr, out int userId))
                {
                    TempData["Error"] = "Phiên đăng nhập không hợp lệ.";
                    return RedirectToAction("Login", "Account");
                }

                var doc = await _documentService.UploadAsync(
                    model.File!, model.ChapterId, model.Title, userId);

                TempData["Success"] = AppMessages.MsgUploadSuccess;
                return RedirectToAction("Detail", new { id = doc.DocumentID });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Upload failed.");
                ModelState.AddModelError(string.Empty, ex.Message);
                model.Subjects = await _subjectService.GetAllAsync();
                model.Chapters = await _chapterService.GetAllAsync();
                return View(model);
            }
        }

        // GET: /Document/Detail/5
        [HttpGet]
        public async Task<IActionResult> Detail(int id)
        {
            var doc = await _documentService.GetDetailAsync(id);
            if (doc == null)
            {
                TempData["Error"] = AppMessages.MsgNotFound;
                return RedirectToAction("Index");
            }
            return View(doc);
        }

        // GET: /Document/Download/5
        [HttpGet]
        public async Task<IActionResult> Download(int id)
        {
            var doc = await _documentService.GetDetailAsync(id);
            if (doc == null)
            {
                TempData["Error"] = AppMessages.MsgNotFound;
                return RedirectToAction("Index");
            }

            var filePath = Path.Combine(_env.WebRootPath, AppConstants.UploadFolder, doc.StoragePath);
            if (!System.IO.File.Exists(filePath))
            {
                TempData["Error"] = "File vật lý không tồn tại trên hệ thống.";
                return RedirectToAction("Detail", new { id = id });
            }

            var contentType = doc.FileType.ToLowerInvariant() switch
            {
                "pdf"  => "application/pdf",
                "docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                "pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                _      => "application/octet-stream"
            };

            var downloadName = $"{doc.Title}.{doc.FileType}";
            return PhysicalFile(filePath, contentType, downloadName);
        }


        // POST: /Document/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "TeacherUp")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _documentService.DeleteAsync(id);
                TempData["Success"] = AppMessages.MsgDeleteSuccess;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Delete failed for doc {Id}.", id);
                TempData["Error"] = ex.Message;
            }
            return RedirectToAction("Index");
        }

        // AJAX: /Document/GetChapters?subjectId=1
        [HttpGet]
        public async Task<IActionResult> GetChapters(int subjectId)
        {
            var chapters = await _chapterService.GetBySubjectAsync(subjectId);
            return Json(chapters.Select(c => new { c.ChapterID, c.ChapterName, c.ChapterNumber }));
        }
    }
}
