using LearningDocumentSystem.Business.DTOs;
using LearningDocumentSystem.Business.Services.Interfaces;
using LearningDocumentSystem.Common.Constants;
using LearningDocumentSystem.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Security.Claims;
using System.Linq;

namespace LearningDocumentSystem.Web.Controllers
{
    [Authorize]
    public class DocumentController : Controller
    {
        private readonly IDocumentService _documentService;
        private readonly ISubjectService  _subjectService;
        private readonly IChapterService  _chapterService;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _config;
        private readonly ILogger<DocumentController> _logger;

        public DocumentController(
            IDocumentService documentService,
            ISubjectService subjectService,
            IChapterService chapterService,
            IWebHostEnvironment env,
            IConfiguration config,
            ILogger<DocumentController> logger)
        {
            _documentService = documentService;
            _subjectService  = subjectService;
            _chapterService  = chapterService;
            _env             = env;
            _config          = config;
            _logger          = logger;
        }

        // GET: /Document
        [HttpGet]
        public async Task<IActionResult> Index(
            string? keyword, int? subjectId, int? chapterId, string? status, int page = 1)
        {
            // Nếu chỉ truyền chapterId (vd: click từ màn Chapter) thì resolve subjectId
            // để dropdown Môn/Chương hiển thị đúng dữ liệu.
            if (!subjectId.HasValue && chapterId.HasValue)
            {
                var selectedChapter = await _chapterService.GetByIdAsync(chapterId.Value);
                if (selectedChapter != null)
                {
                    subjectId = selectedChapter.SubjectID;
                }
            }

            var (items, total) = await _documentService.GetPagedAsync(
                keyword, subjectId, chapterId, status, page, AppConstants.DefaultPageSize);

            var subjects = await _subjectService.GetAllAsync();
            var chapters = subjectId.HasValue
                ? await _chapterService.GetBySubjectAsync(subjectId.Value)
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
        public async Task<IActionResult> Upload(int? subjectId = null)
        {
            PopulateUploadUiSettings();
            var subjects = (await _subjectService.GetAllAsync()).ToList();

            // Demo 1 môn: nếu DB chỉ có 1 môn thì auto select.
            var selectedSubjectId = subjectId;
            if (!selectedSubjectId.HasValue && subjects.Count == 1)
            {
                selectedSubjectId = subjects[0].SubjectID;
            }

            var chapters = selectedSubjectId.HasValue
                ? await _chapterService.GetBySubjectAsync(selectedSubjectId.Value)
                : [];

            return View(new DocumentUploadViewModel
            {
                Subjects = subjects,
                Chapters = chapters,
                SelectedSubjectId = selectedSubjectId
            });
        }

        // POST: /Document/Upload
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "TeacherUp")]
        public async Task<IActionResult> Upload(DocumentUploadViewModel model)
        {
            PopulateUploadUiSettings();
            var allowedFileTypes = GetAllowedFileTypes();
            var maxFileSizeBytes = GetMaxFileSizeBytes();

            if (model.File != null)
            {
                var extension = Path.GetExtension(model.File.FileName)
                    .TrimStart('.')
                    .ToLowerInvariant();

                if (string.IsNullOrWhiteSpace(extension)
                    || !allowedFileTypes.Contains(extension, StringComparer.OrdinalIgnoreCase))
                {
                    ModelState.AddModelError(nameof(model.File), AppMessages.MsgInvalidFileType);
                }

                if (model.File.Length > maxFileSizeBytes)
                {
                    ModelState.AddModelError(nameof(model.File), AppMessages.MsgFileSizeExceeded);
                }
            }

            var selectedChapter = model.ChapterId > 0
                ? await _chapterService.GetByIdAsync(model.ChapterId)
                : null;

            if (model.ChapterId > 0 && selectedChapter == null)
            {
                ModelState.AddModelError(nameof(model.ChapterId), "Chương không tồn tại.");
            }
            else if (selectedChapter != null
                     && model.SelectedSubjectId.HasValue
                     && selectedChapter.SubjectID != model.SelectedSubjectId.Value)
            {
                ModelState.AddModelError(nameof(model.ChapterId), "Chương đã chọn không thuộc môn học này.");
            }

            if (!ModelState.IsValid)
            {
                await PopulateUploadDropdownsAsync(model);
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
                await PopulateUploadDropdownsAsync(model);
                return View(model);
            }
        }

        private async Task PopulateUploadDropdownsAsync(DocumentUploadViewModel model)
        {
            var subjects = (await _subjectService.GetAllAsync()).ToList();
            model.Subjects = subjects;

            int? selectedSubjectId = model.SelectedSubjectId;
            if (!selectedSubjectId.HasValue || selectedSubjectId.Value <= 0)
            {
                // Nếu chỉ có ChapterId, suy ra SubjectID từ Chapter
                if (model.ChapterId > 0)
                {
                    var chapter = await _chapterService.GetByIdAsync(model.ChapterId);
                    selectedSubjectId = chapter?.SubjectID;
                }
                else if (subjects.Count == 1)
                {
                    selectedSubjectId = subjects[0].SubjectID;
                }
            }

            model.SelectedSubjectId = selectedSubjectId;
            model.Chapters = selectedSubjectId.HasValue
                ? await _chapterService.GetBySubjectAsync(selectedSubjectId.Value)
                : [];
        }

        private void PopulateUploadUiSettings()
        {
            var allowedFileTypes = GetAllowedFileTypes();
            var maxFileSizeBytes = GetMaxFileSizeBytes();

            ViewBag.AllowedFileTypes = allowedFileTypes;
            ViewBag.MaxFileSizeBytes = maxFileSizeBytes;
            ViewBag.MaxFileSizeMB = Math.Max(1, (long)Math.Ceiling(maxFileSizeBytes / (1024d * 1024d)));
        }

        private string[] GetAllowedFileTypes()
        {
            var configuredTypes = _config
                .GetSection("AppSettings:AllowedFileTypes")
                .GetChildren()
                .Select(x => x.Value?.Trim().TrimStart('.').ToLowerInvariant())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Cast<string>()
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            return configuredTypes.Length > 0
                ? configuredTypes
                : AppConstants.AllowedFileTypes;
        }

        private long GetMaxFileSizeBytes()
        {
            if (long.TryParse(_config["AppSettings:MaxFileSizeMB"], out var maxFileSizeMb)
                && maxFileSizeMb > 0)
            {
                return maxFileSizeMb * 1024 * 1024;
            }

            return AppConstants.MaxFileSizeBytes;
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
