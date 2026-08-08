using System.ComponentModel.DataAnnotations;
using Innkeep2.Database.Model;
using JetBrains.Annotations;

namespace Innkeep2.Cloud.Database.Models;

[UsedImplicitly]
public class InnkeepCloudSettings : AbstractDbItem
{
	[MaxLength(64)]
	public string? PretixOrganizerSlug { get; set; }
	
	[MaxLength(64)]
	public string? PretixEventSlug { get; set; }
	
	public bool UseTestMode { get; set; }
}