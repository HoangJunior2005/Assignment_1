namespace LearningDocumentSystem.Common.Constants
{
    public static class AppConstants
    {
        // File Upload
        public static readonly string[] AllowedFileTypes = { "pdf", "docx", "pptx" };
        public const long MaxFileSizeBytes = 50L * 1024 * 1024; // 50 MB
        public const string UploadFolder = "uploads";

        // Chunking - theo spec: 500-1000 ký tự mỗi chunk
        public const int ChunkSize = 800;
        public const int ChunkOverlap = 100;
        public const int MinChunkLength = 50;

        // Embedding (giả lập OpenAI ada-002)
        public const int EmbeddingDimension = 1536;

        // Pagination
        public const int DefaultPageSize = 10;

        // Session Keys
        public const string SessionUserId = "UserId";
        public const string SessionUsername = "Username";
        public const string SessionFullName = "FullName";
        public const string SessionRoles = "UserRoles";

        // Roles
        public const string RoleAdmin = "Admin";
        public const string RoleTeacher = "Teacher";
        public const string RoleStudent = "Student";

        // Index Status strings (theo DB schema)
        public const string StatusPending = "Pending";
        public const string StatusProcessing = "Processing";
        public const string StatusIndexed = "Indexed";
        public const string StatusFailed = "Failed";

        // Cookie Auth
        public const string AuthCookieName = "LDS.Auth";
        public const string LoginPath = "/Account/Login";
        public const string AccessDeniedPath = "/Account/AccessDenied";
    }
}
