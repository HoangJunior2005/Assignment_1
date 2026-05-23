using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using DocumentFormat.OpenXml.Wordprocessing;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using LearningDocumentSystem.Business.Services.Interfaces;
using LearningDocumentSystem.Common.Constants;
using Microsoft.Extensions.Logging;
using System.Text;

namespace LearningDocumentSystem.Business.Services.Implementations
{
    public class ChunkingService : IChunkingService
    {
        private readonly ILogger<ChunkingService> _logger;

        public ChunkingService(ILogger<ChunkingService> logger)
        {
            _logger = logger;
        }

        public async Task<List<(string Content, int PageNumber)>> ExtractChunksAsync(string filePath, string fileType)
        {
            _logger.LogInformation("Extracting chunks from {FileType} file: {Path}", fileType, filePath);

            var rawText = fileType.ToLowerInvariant() switch
            {
                "pdf"  => await ExtractPdfTextAsync(filePath),
                "docx" => await ExtractDocxTextAsync(filePath),
                "pptx" => await ExtractPptxTextAsync(filePath),
                _      => throw new NotSupportedException($"File type '{fileType}' không được hỗ trợ.")
            };

            return ChunkText(rawText);
        }

        // ============================================================
        // PDF - sử dụng iText7
        // ============================================================
        private Task<List<(string Text, int Page)>> ExtractPdfTextAsync(string filePath)
        {
            var result = new List<(string Text, int Page)>();
            try
            {
                using var reader = new PdfReader(filePath);
                using var pdf    = new PdfDocument(reader);
                for (int i = 1; i <= pdf.GetNumberOfPages(); i++)
                {
                    var text = PdfTextExtractor.GetTextFromPage(pdf.GetPage(i)).Trim();
                    if (!string.IsNullOrWhiteSpace(text))
                        result.Add((text, i));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting PDF: {Path}", filePath);
            }
            return Task.FromResult(result);
        }

        // ============================================================
        // DOCX - sử dụng DocumentFormat.OpenXml
        // ============================================================
        private Task<List<(string Text, int Page)>> ExtractDocxTextAsync(string filePath)
        {
            var result = new List<(string Text, int Page)>();
            try
            {
                using var doc    = WordprocessingDocument.Open(filePath, false);
                var body         = doc.MainDocumentPart?.Document?.Body;
                if (body == null) return Task.FromResult(result);

                var sb = new StringBuilder();
                foreach (var para in body.Elements<Paragraph>())
                {
                    var text = para.InnerText.Trim();
                    if (!string.IsNullOrWhiteSpace(text))
                        sb.AppendLine(text);
                }
                result.Add((sb.ToString(), 1));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting DOCX: {Path}", filePath);
            }
            return Task.FromResult(result);
        }

        // ============================================================
        // PPTX - sử dụng DocumentFormat.OpenXml
        // ============================================================
        private Task<List<(string Text, int Page)>> ExtractPptxTextAsync(string filePath)
        {
            var result = new List<(string Text, int Page)>();
            try
            {
                using var prs = PresentationDocument.Open(filePath, false);
                var slides    = prs.PresentationPart?.SlideParts?.ToList();
                if (slides == null) return Task.FromResult(result);

                for (int i = 0; i < slides.Count; i++)
                {
                    var sb = new StringBuilder();
                    var shapes = slides[i].Slide?.CommonSlideData?.ShapeTree
                        ?.Elements<DocumentFormat.OpenXml.Presentation.Shape>() ?? [];
                    foreach (var shape in shapes)
                    {
                        var txt = shape.TextBody?.InnerText?.Trim();
                        if (!string.IsNullOrWhiteSpace(txt))
                            sb.AppendLine(txt);
                    }
                    if (sb.Length > 0)
                        result.Add((sb.ToString(), i + 1));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting PPTX: {Path}", filePath);
            }
            return Task.FromResult(result);
        }

        // ============================================================
        // Chunking: tách text thành đoạn ~800 ký tự có overlap
        // ============================================================
        private List<(string Content, int PageNumber)> ChunkText(List<(string Text, int Page)> pages)
        {
            var chunks = new List<(string Content, int PageNumber)>();
            var buffer = new StringBuilder();
            int currentPage = 1;

            foreach (var (text, page) in pages)
            {
                currentPage = page;
                var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                foreach (var word in words)
                {
                    buffer.Append(word).Append(' ');

                    if (buffer.Length >= AppConstants.ChunkSize)
                    {
                        var content = buffer.ToString().Trim();
                        if (content.Length >= AppConstants.MinChunkLength)
                            chunks.Add((content, currentPage));

                        // Overlap: giữ lại ~100 ký tự cuối
                        var keep = content.Length > AppConstants.ChunkOverlap
                            ? content[^AppConstants.ChunkOverlap..]
                            : content;
                        buffer.Clear();
                        buffer.Append(keep).Append(' ');
                    }
                }
            }

            // Chunk cuối
            if (buffer.Length >= AppConstants.MinChunkLength)
                chunks.Add((buffer.ToString().Trim(), currentPage));

            // Nếu không extract được nội dung → tạo 1 chunk placeholder
            if (chunks.Count == 0)
                chunks.Add(("Tài liệu chưa có nội dung text hoặc định dạng không được hỗ trợ.", 1));

            return chunks;
        }
    }
}
