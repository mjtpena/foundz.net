using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Foundz.Net.Data.Entities;

/// <summary>
/// Database entity for tool executions.
/// </summary>
[Table("ToolExecutions")]
public class ToolExecutionEntity
{
    [Key]
    public Guid Id { get; set; }
    
    [Required]
    public Guid MessageId { get; set; }
    
    [Required]
    [MaxLength(128)]
    public required string ToolName { get; set; }
    
    [Required]
    [Column(TypeName = "TEXT")]
    public required string Arguments { get; set; }
    
    [Column(TypeName = "TEXT")]
    public string? Result { get; set; }
    
    public bool Success { get; set; }
    public int ExecutionTimeMs { get; set; }
    public DateTime Timestamp { get; set; }
    
    // Navigation property
    public MessageEntity Message { get; set; } = null!;
}
