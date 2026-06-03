using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LearningDocumentSystem.Entities.Models
{
    public class School
    {
        [Key]
        [Column("SchoolID")]
        public int SchoolID { get; set; }

        [Required]
        [MaxLength(200)]
        public string SchoolName { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string SchoolCode { get; set; } = string.Empty;

        public virtual ICollection<StudentRegistry> StudentRegistries { get; set; } = new List<StudentRegistry>();
        public virtual ICollection<User> Users { get; set; } = new List<User>();
    }
}
