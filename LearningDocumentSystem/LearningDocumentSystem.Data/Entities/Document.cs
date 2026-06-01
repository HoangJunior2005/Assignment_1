using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LearningDocumentSystem.Entities.Models
{
    public class Document
    {
        [Key]
        [Column("DocumentID")]
        public int DocumentID { get; set; }

        [Required]
        [ForeignKey(nameof(Chapter))]
        public int ChapterID { get; set; }

        [Required]
        [MaxLength(255)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(10)]
        public string FileType { get; set; } = string.Empty;

        [Required]
        [MaxLength(2000)]
        public string StoragePath { get; set; } = string.Empty;

        public long FileSizeInBytes { get; set; }

        [MaxLength(20)]
        public string IndexStatus { get; set; } = "Pending";

        [Required]
        [ForeignKey(nameof(UploadedByUser))]
        public int UploadedBy { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        public DateTime? IndexedAt { get; set; }

        [MaxLength(64)]
        public string? FileHash { get; set; }

        // Navigation properties
        public virtual Chapter Chapter { get; set; } = null!;
        public virtual User UploadedByUser { get; set; } = null!;
        public virtual ICollection<DocumentChunk> Chunks { get; set; } = new List<DocumentChunk>();
    }
}
