using LearningDocumentSystem.Business.DTOs;
using LearningDocumentSystem.Business.Services.Interfaces;
using LearningDocumentSystem.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LearningDocumentSystem.Web.Controllers
{
    [Authorize]
    public class ChapterController : Controller
    {
        private readonly IChapterService _chapterService;
        private readonly ISubjectService _subjectService;
        private readonly ILogger<ChapterController> _logger;

        public ChapterController(
            IChapterService chapterService,
            ISubjectService subjectService,
            ILogger<ChapterController> logger)
        {
            _chapterService = chapterService;
            _subjectService = subjectService;
            _logger         = logger;
        }

        public async Task<IActionResult> Index(int? subjectId)
        {
            var subjects = await _subjectService.GetAllAsync();
            var chapters = subjectId.HasValue
                ? await _chapterService.GetBySubjectAsync(subjectId.Value)
                : await _chapterService.GetAllAsync();

            return View(new ChapterListViewModel
            {
                Chapters          = chapters,
                Subjects          = subjects,
                SelectedSubjectId = subjectId
            });
        }

        [HttpGet]
        [Authorize(Policy = "TeacherUp")]
        public async Task<IActionResult> Create(int? subjectId)
        {
            var subjects = await _subjectService.GetAllAsync();
            return View(new ChapterFormViewModel
            {
                Subjects  = subjects,
                SubjectID = subjectId ?? 0
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "TeacherUp")]
        public async Task<IActionResult> Create(ChapterFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Subjects = await _subjectService.GetAllAsync();
                return View(model);
            }
            try
            {
                await _chapterService.CreateAsync(new CreateChapterDto
                {
                    SubjectID     = model.SubjectID,
                    ChapterNumber = model.ChapterNumber,
                    ChapterName   = model.ChapterName
                });
                TempData["Success"] = "Tạo chương học thành công.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                model.Subjects = await _subjectService.GetAllAsync();
                return View(model);
            }
        }

        [HttpGet]
        [Authorize(Policy = "TeacherUp")]
        public async Task<IActionResult> Edit(int id)
        {
            var c = await _chapterService.GetByIdAsync(id);
            if (c == null) return NotFound();
            var subjects = await _subjectService.GetAllAsync();
            return View(new ChapterFormViewModel
            {
                ChapterID     = c.ChapterID,
                SubjectID     = c.SubjectID,
                ChapterNumber = c.ChapterNumber,
                ChapterName   = c.ChapterName,
                Subjects      = subjects
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "TeacherUp")]
        public async Task<IActionResult> Edit(ChapterFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Subjects = await _subjectService.GetAllAsync();
                return View(model);
            }
            try
            {
                await _chapterService.UpdateAsync(new UpdateChapterDto
                {
                    ChapterID     = model.ChapterID,
                    SubjectID     = model.SubjectID,
                    ChapterNumber = model.ChapterNumber,
                    ChapterName   = model.ChapterName
                });
                TempData["Success"] = "Cập nhật chương học thành công.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                model.Subjects = await _subjectService.GetAllAsync();
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "TeacherUp")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _chapterService.DeleteAsync(id);
                TempData["Success"] = "Xóa chương học thành công.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }
            return RedirectToAction("Index");
        }
    }
}
