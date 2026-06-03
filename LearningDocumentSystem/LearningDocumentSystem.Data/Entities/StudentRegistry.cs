using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LearningDocumentSystem.Entities.Models
{
    public class StudentRegistry
    {
        [Key]
        [Column("RegistryID")]
        public int RegistryID { get; set; }

        [Required]
        [MaxLength(50)]
        public string StudentCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Column("SchoolID")]
        public int SchoolID { get; set; }

        public bool IsActivated { get; set; }

        public DateTime? ActivatedAt { get; set; }

        [Column("UserID")]
        public int? UserID { get; set; }

        public virtual School School { get; set; } = null!;
        public virtual User? User { get; set; }
    }
}
