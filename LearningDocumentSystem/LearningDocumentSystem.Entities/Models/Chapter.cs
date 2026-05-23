using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LearningDocumentSystem.Entities.Models
{
    public class Chapter
    {
        [Key]
        [Column("ChapterID")]
        public int ChapterID { get; set; }

        [Required]
        [ForeignKey(nameof(Subject))]
        public int SubjectID { get; set; }

        [Required]
        public int ChapterNumber { get; set; }

        [Required]
        [MaxLength(255)]
        public string ChapterName { get; set; } = string.Empty;

        // Navigation properties
        public virtual Subject Subject { get; set; } = null!;
        public virtual ICollection<Document> Documents { get; set; } = new List<Document>();
    }
}
