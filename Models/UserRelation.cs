using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace App.Models;

public enum RelationshipStatus
{
    None,
    Follow,
    Block
}
public class UserRelationModel
{
    [ForeignKey(nameof(User))]
    public string? UserId { get; set; }

    [ForeignKey(nameof(OtherUser))]
    public string? OtherUserId { get; set; }
    
    [Required]
    public RelationshipStatus Status { get; set; }

    public DateTimeOffset? DateCreated { get; set; }

    public virtual AppUser? User { get; set; }
    public virtual AppUser? OtherUser { get; set; }
}