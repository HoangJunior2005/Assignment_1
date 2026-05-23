namespace LearningDocumentSystem.Common.Helpers
{
    public static class FileHelper
    {
        private static readonly string[] AllowedExtensions = { ".pdf", ".docx", ".pptx" };

        public static bool IsAllowedExtension(string fileName)
        {
            var ext = Path.GetExtension(fileName)?.ToLowerInvariant();
            return !string.IsNullOrEmpty(ext) && AllowedExtensions.Contains(ext);
        }

        public static string GetFileType(string fileName)
            => Path.GetExtension(fileName).TrimStart('.').ToLowerInvariant();

        public static string GenerateStoragePath(string fileName)
        {
            var ext = Path.GetExtension(fileName).ToLowerInvariant();
            var safe = SanitizeName(Path.GetFileNameWithoutExtension(fileName));
            var ts = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var uid = Guid.NewGuid().ToString("N")[..8];
            return $"{ts}_{uid}_{safe}{ext}";
        }

        private static string SanitizeName(string name)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var s = string.Concat(name.Select(c => invalid.Contains(c) ? '_' : c));
            return s.Length > 40 ? s[..40] : s;
        }

        public static string FormatSize(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
            return $"{bytes / (1024.0 * 1024):F1} MB";
        }

        public static string GetFileIconClass(string fileType) => fileType switch
        {
            "pdf"  => "bi-file-earmark-pdf-fill text-danger",
            "docx" => "bi-file-earmark-word-fill text-primary",
            "pptx" => "bi-file-earmark-ppt-fill text-warning",
            _      => "bi-file-earmark text-secondary"
        };

        public static string GetFileTypeBadgeClass(string fileType) => fileType switch
        {
            "pdf"  => "bg-danger",
            "docx" => "bg-primary",
            "pptx" => "bg-warning text-dark",
            _      => "bg-secondary"
        };
    }
}
