using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

public class Comment
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(255)]
    [Column(TypeName = "character varying(255)")]
    public string Body { get; set; }

    [Required]
    public string UserId { get; set; }

    [ForeignKey("UserId")]
    public ApplicationUser? User { get; set; }

    [Required]
    public int PostId { get; set; }

    [ForeignKey("PostId")]
    public Post? Post { get; set; }

    public string Status { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}