using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Foundz.Net.Data.Entities;

/// <summary>
/// Database entity for messages.
/// </summary>
[Table("Messages")]
public class MessageEntity
{
    [Key]
    public Guid Id { get; set; }
    
    [Required]
    public Guid SessionId { get; set; }
    
    [Required]
    [MaxLength(50)]
    public required string Role { get; set; }
    
    [Required]
    [Column(TypeName = "TEXT")]
    public required string Content { get; set; }
    
    public int? TokenCount { get; set; }
    public DateTime Timestamp { get; set; }
    public Guid? ParentMessageId { get; set; }
    
    [Column(TypeName = "TEXT")]
    public string? Metadata { get; set; }
    
    // Navigation properties
    public SessionEntity Session { get; set; } = null!;
    public ICollection<ToolExecutionEntity> ToolExecutions { get; set; } = [];
}
