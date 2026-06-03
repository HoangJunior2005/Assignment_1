using System.ComponentModel.DataAnnotations;
using LearningDocumentSystem.Business.DTOs;

namespace LearningDocumentSystem.Web.ViewModels
{
    // ============================================================
    // AUTH
    // ============================================================
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập")]
        [Display(Name = "Tên đăng nhập")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Ghi nhớ đăng nhập")]
        public bool RememberMe { get; set; }
    }

    public class RegisterStudentViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập MSSV")]
        [Display(Name = "MSSV")]
        [MaxLength(50)]
        public string StudentCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        [Display(Name = "Họ và tên")]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        [MinLength(6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự")]
        [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d).{6,}$",
            ErrorMessage = "Mật khẩu quá yếu. Mật khẩu cần ít nhất 6 ký tự, gồm chữ và số.")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu")]
        [DataType(DataType.Password)]
        [Display(Name = "Xác nhận mật khẩu")]
        [Compare(nameof(Password), ErrorMessage = "Mật khẩu xác nhận không khớp")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    // ============================================================
    // DOCUMENT
    // ============================================================
    public class DocumentListViewModel
    {
        public IEnumerable<DocumentDto> Documents { get; set; } = [];
        public IEnumerable<SubjectDto> Subjects { get; set; } = [];
        public IEnumerable<ChapterDto> Chapters { get; set; } = [];
        public string? Keyword { get; set; }
        public int? SelectedSubjectId { get; set; }
        public int? SelectedChapterId { get; set; }
        public string? SelectedStatus { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }
        public int PageSize { get; set; } = 10;
    }

    public class DocumentUploadViewModel
    {
        [Required(ErrorMessage = "Tiêu đề không được để trống")]
        [MaxLength(255)]
        [Display(Name = "Tiêu đề tài liệu")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn chương")]
        [Display(Name = "Chương")]
        public int ChapterId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn file")]
        [Display(Name = "File tài liệu")]
        public IFormFile? File { get; set; }

        // Dropdown data
        public IEnumerable<SubjectDto> Subjects { get; set; } = [];
        public IEnumerable<ChapterDto> Chapters { get; set; } = [];
        public int? SelectedSubjectId { get; set; }
    }

    // ============================================================
    // SUBJECT
    // ============================================================
    public class SubjectListViewModel
    {
        public IEnumerable<SubjectDto> Subjects { get; set; } = [];
    }

    public class SubjectFormViewModel
    {
        public int SubjectID { get; set; }

        [Required(ErrorMessage = "Tên môn học không được để trống")]
        [MaxLength(255)]
        [Display(Name = "Tên môn học")]
        public string SubjectName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mã học phần không được để trống")]
        [MaxLength(50)]
        [RegularExpression(@"^[A-Z0-9]+$", ErrorMessage = "Mã học phần chỉ gồm chữ hoa và số (VD: INF205)")]
        [Display(Name = "Mã học phần")]
        public string SubjectCode { get; set; } = string.Empty;
    }

    // ============================================================
    // CHAPTER
    // ============================================================
    public class ChapterListViewModel
    {
        public IEnumerable<ChapterDto> Chapters { get; set; } = [];
        public IEnumerable<SubjectDto> Subjects { get; set; } = [];
        public int? SelectedSubjectId { get; set; }
    }

    public class ChapterFormViewModel
    {
        public int ChapterID { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn môn học")]
        [Display(Name = "Môn học")]
        public int SubjectID { get; set; }

        [Required(ErrorMessage = "Số chương không được để trống")]
        [Range(1, 100)]
        [Display(Name = "Số chương")]
        public int ChapterNumber { get; set; }

        [Required(ErrorMessage = "Tên chương không được để trống")]
        [MaxLength(255)]
        [Display(Name = "Tên chương")]
        public string ChapterName { get; set; } = string.Empty;

        public IEnumerable<SubjectDto> Subjects { get; set; } = [];
    }

    public class ErrorViewModel
    {
        public string? RequestId { get; set; }
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}
