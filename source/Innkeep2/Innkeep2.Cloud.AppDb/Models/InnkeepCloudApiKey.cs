using System.ComponentModel.DataAnnotations;
using Innkeep2.Database.Model;
using Innkeep2.Models.Fiskaly.Tss;

namespace Innkeep2.Cloud.AppDb.Models;

public class InnkeepCloudApiKey : AbstractDbItem
{
    public required string Name { get; set; }
    
    [MaxLength(64)]
    public required string KeyHash { get; set; }
    
    public required DateTime CreatedAd { get; set; }
    
    public bool Revoked { get; set; }
    
}