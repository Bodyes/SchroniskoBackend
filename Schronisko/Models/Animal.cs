using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;



    public class Animal
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(255)]
        [Column(TypeName = "character varying(255)")]
        public string Name { get; set; }

        [Required]
        [Column(TypeName = "text")]
        public string Description { get; set; }
        
        public DateTime Age { get; set; }

        [Required]
        public string UserId { get; set; }

        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool Adopted { get; set; } = false;
    }

