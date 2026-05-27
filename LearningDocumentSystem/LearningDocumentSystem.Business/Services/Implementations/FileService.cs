using LearningDocumentSystem.Business.Services.Interfaces;
using LearningDocumentSystem.Common.Constants;
using LearningDocumentSystem.Common.Exceptions;
using LearningDocumentSystem.Common.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LearningDocumentSystem.Business.Services.Implementations
{
    public class FileService : IFileService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<FileService> _logger;

        public FileService(IConfiguration config, ILogger<FileService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task<string> SaveFileAsync(IFormFile file, string uploadFolder)
        {
            if (file == null || file.Length == 0)
                throw new InvalidFileException("File không hợp lệ.");

            var fileType = FileHelper.GetFileType(file.FileName);
            if (!GetAllowedFileTypes().Contains(fileType))
                throw new InvalidFileException(AppMessages.MsgInvalidFileType);

            if (file.Length > GetMaxFileSizeBytes())
                throw new InvalidFileException(AppMessages.MsgFileSizeExceeded);

            var uniqueFileName = FileHelper.GenerateStoragePath(file.FileName);
            var fullPath = Path.Combine(uploadFolder, uniqueFileName);

            Directory.CreateDirectory(uploadFolder);

            await using var stream = new FileStream(fullPath, FileMode.Create);
            await file.CopyToAsync(stream);

            _logger.LogInformation("File saved: {FileName}", uniqueFileName);
            return uniqueFileName;
        }

        public void DeleteFile(string storagePath, string uploadFolder)
        {
            var fullPath = Path.Combine(uploadFolder, storagePath);
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                _logger.LogInformation("File deleted: {Path}", storagePath);
            }
        }

        private string[] GetAllowedFileTypes()
        {
            var configuredTypes = _config
                .GetSection("AppSettings:AllowedFileTypes")
                .GetChildren()
                .Select(x => x.Value?.Trim().TrimStart('.').ToLowerInvariant())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Cast<string>()
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
    }
}
