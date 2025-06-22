using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System;

public class Post
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(255)]
    [Column(TypeName = "character varying(255)")]
    public string Title { get; set; } = string.Empty;

    [Required]
    [Column(TypeName = "text")]
    public string Body { get; set; } = string.Empty;

    [Required]
    public string UserId { get; set; } = string.Empty;

    [ForeignKey("UserId")]
    public ApplicationUser? User { get; set; }

    public virtual ICollection<Comment>? Comments { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}


