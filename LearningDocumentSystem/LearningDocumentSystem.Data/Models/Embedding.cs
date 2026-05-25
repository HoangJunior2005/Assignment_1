using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LearningDocumentSystem.Entities.Models
{
    public class Embedding
    {
        [Key]
        [Column("EmbeddingID")]
        public int EmbeddingID { get; set; }

        [Required]
        [ForeignKey(nameof(Chunk))]
        public int ChunkID { get; set; }

        [Required]
        public string VectorData { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public virtual DocumentChunk Chunk { get; set; } = null!;
    }
}
