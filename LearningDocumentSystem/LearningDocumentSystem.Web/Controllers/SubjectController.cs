using LearningDocumentSystem.Business.DTOs;
using LearningDocumentSystem.Business.Services.Interfaces;
using LearningDocumentSystem.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LearningDocumentSystem.Web.Controllers
{
    [Authorize]
    public class SubjectController : Controller
    {
        private readonly ISubjectService _subjectService;
        private readonly ILogger<SubjectController> _logger;

        public SubjectController(ISubjectService subjectService, ILogger<SubjectController> logger)
        {
            _subjectService = subjectService;
            _logger         = logger;
        }

        public async Task<IActionResult> Index()
        {
            var subjects = await _subjectService.GetAllAsync();
            return View(new SubjectListViewModel { Subjects = subjects });
        }

        [HttpGet]
        [Authorize(Policy = "TeacherUp")]
        public IActionResult Create() => View(new SubjectFormViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "TeacherUp")]
        public async Task<IActionResult> Create(SubjectFormViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            try
            {
                await _subjectService.CreateAsync(new CreateSubjectDto
                {
                    SubjectName = model.SubjectName,
                    SubjectCode = model.SubjectCode
                });
                TempData["Success"] = "Tạo môn học thành công.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(model);
            }
        }

        [HttpGet]
        [Authorize(Policy = "TeacherUp")]
        public async Task<IActionResult> Edit(int id)
        {
            var s = await _subjectService.GetByIdAsync(id);
            if (s == null) return NotFound();
            return View(new SubjectFormViewModel
            {
                SubjectID   = s.SubjectID,
                SubjectName = s.SubjectName,
                SubjectCode = s.SubjectCode
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "TeacherUp")]
        public async Task<IActionResult> Edit(SubjectFormViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            try
            {
                await _subjectService.UpdateAsync(new UpdateSubjectDto
                {
                    SubjectID   = model.SubjectID,
                    SubjectName = model.SubjectName,
                    SubjectCode = model.SubjectCode
                });
                TempData["Success"] = "Cập nhật môn học thành công.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
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
                await _subjectService.DeleteAsync(id);
                TempData["Success"] = "Xóa môn học thành công.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }
            return RedirectToAction("Index");
        }
    }
}
