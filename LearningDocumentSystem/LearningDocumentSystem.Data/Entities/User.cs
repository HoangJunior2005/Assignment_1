using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LearningDocumentSystem.Entities.Models
{
    public class User
    {
        [Key]
        [Column("UserID")]
        public int UserID { get; set; }

        [Required]
        [MaxLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        [Column("SchoolID")]
        public int? SchoolID { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual School? School { get; set; }
        public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
        public virtual ICollection<Document> Documents { get; set; } = new List<Document>();
        public virtual StudentRegistry? StudentRegistry { get; set; }
    }
}
