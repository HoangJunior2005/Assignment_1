using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LearningDocumentSystem.Entities.Models
{
    public class DocumentChunk
    {
        [Key]
        [Column("ChunkID")]
        public int ChunkID { get; set; }

        [Required]
        [ForeignKey(nameof(Document))]
        public int DocumentID { get; set; }

        public int ChunkIndex { get; set; }

        public int? PageNumber { get; set; }

        [Required]
        public string ContentText { get; set; } = string.Empty;

        // Navigation properties
        public virtual Document Document { get; set; } = null!;
        public virtual Embedding? Embedding { get; set; }
    }
}
