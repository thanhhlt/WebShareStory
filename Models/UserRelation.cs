using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace App.Models;

public enum RelationshipStatus
{
    None,
    Pending,
    Friend,
    Block
}
public class UserRelationModel
{
    [ForeignKey(nameof(User))]
    public required string UserId { get; set; }

    [ForeignKey(nameof(OtherUser))]
    public required string OtherUserId { get; set; } = "";
    
    [Required]
    public RelationshipStatus Status { get; set; }

    public DateTime DateCreated { get; set; } = DateTime.UtcNow;

    public DateTime DateUpdated { get; set; } = DateTime.UtcNow;

    public virtual AppUser? User { get; set; }
    public virtual AppUser? OtherUser { get; set; }
}