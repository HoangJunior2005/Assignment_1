namespace LearningDocumentSystem.Common.Constants
{
    public static class AppMessages
    {
        public const string MsgInvalidFileType  = "Chỉ chấp nhận file PDF, DOCX, PPTX.";
        public const string MsgFileSizeExceeded = "File vượt quá giới hạn 50MB.";
        public const string MsgUploadSuccess    = "Tài liệu đã được upload và xử lý thành công.";
        public const string MsgUploadFailed     = "Upload thất bại. Vui lòng thử lại.";
        public const string MsgDeleteSuccess    = "Xóa tài liệu thành công.";
        public const string MsgNotFound         = "Không tìm thấy tài nguyên.";
        public const string MsgLoginFailed      = "Tên đăng nhập hoặc mật khẩu không đúng.";
        public const string MsgAccessDenied     = "Bạn không có quyền thực hiện thao tác này.";

        public const string MsgStudentCodeNotFound   = "MSSV không thuộc trường";
        public const string MsgStudentAlreadyExists  = "Tài khoản đã tồn tại";
        public const string MsgStudentInfoInvalid    = "Thông tin sinh viên không hợp lệ";
        public const string MsgPasswordWeak          = "Mật khẩu quá yếu. Mật khẩu cần ít nhất 6 ký tự, gồm chữ và số.";
        public const string MsgPasswordMismatch      = "Mật khẩu xác nhận không khớp";
        public const string MsgRegisterSuccess       = "Đăng kí thành công";
    }
}
