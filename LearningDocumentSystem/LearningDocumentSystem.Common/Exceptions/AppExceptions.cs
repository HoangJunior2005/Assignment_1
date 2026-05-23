namespace LearningDocumentSystem.Common.Exceptions
{
    public class NotFoundException : Exception
    {
        public NotFoundException(string message) : base(message) { }
        public NotFoundException(string entityName, object key)
            : base($"Không tìm thấy {entityName} với ID = {key}") { }
    }

    public class BusinessException : Exception
    {
        public BusinessException(string message) : base(message) { }
    }

    public class ForbiddenException : Exception
    {
        public ForbiddenException(string message = "Bạn không có quyền thực hiện thao tác này.")
            : base(message) { }
    }

    public class InvalidFileException : Exception
    {
        public InvalidFileException(string message) : base(message) { }
    }
}
