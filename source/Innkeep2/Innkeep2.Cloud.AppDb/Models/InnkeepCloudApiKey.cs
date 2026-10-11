using System.ComponentModel.DataAnnotations;
using Demolite.Db.Models;
using Innkeep2.Credentials.Interfaces;

namespace Innkeep2.Cloud.AppDb.Models;

public class InnkeepCloudApiKey : AbstractDbItem, IApiKey
{
    [MaxLength(255)]
    public required string Name { get; set; }
    
    [MaxLength(64)]
    public required string KeyHash { get; set; }
    
    public required DateTime CreatedAt { get; set; }
    
    public bool Revoked { get; set; }
    
}