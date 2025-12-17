using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Foundz.Net.Data.Entities;

/// <summary>
/// Database entity for sessions.
/// </summary>
[Table("Sessions")]
public class SessionEntity
{
    [Key]
    public Guid Id { get; set; }
    
    [Required]
    [MaxLength(256)]
    public required string UserId { get; set; }
    
    [Required]
    [MaxLength(512)]
    public required string ProjectPath { get; set; }
    
    [Required]
    [MaxLength(128)]
    public required string ModelName { get; set; }
    
    [Required]
    [MaxLength(50)]
    public required string Status { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    [Column(TypeName = "TEXT")]
    public string? Metadata { get; set; }
    
    // Navigation property
    public ICollection<MessageEntity> Messages { get; set; } = [];
}
