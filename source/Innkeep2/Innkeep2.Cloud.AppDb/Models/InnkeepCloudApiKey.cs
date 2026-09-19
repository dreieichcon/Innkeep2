using System.ComponentModel.DataAnnotations;
using Innkeep2.Database.Model;

namespace Innkeep2.Cloud.AppDb.Models;

public class InnkeepCloudApiKey : AbstractDbItem
{
    [MaxLength(255)]
    public required string Name { get; set; }
    
    [MaxLength(64)]
    public required string KeyHash { get; set; }
    
    public required DateTime CreatedAt { get; set; }
    
    public bool Revoked { get; set; }
    
}