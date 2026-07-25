using System.ComponentModel.DataAnnotations.Schema;

namespace Innkeep2.Models.Pretix.Core;

public class MultiLanguageString : Dictionary<string, string>
{

	[NotMapped]
	public string German
	{
		get
		{
			var entry = this.FirstOrDefault(x => x.Key.StartsWith("de"));
			return entry.Value ?? this.First().Value;
		}
	}
	
	[NotMapped]
	public string English
	{
		get
		{
			var entry = this.FirstOrDefault(x => x.Key.StartsWith("en"));
			return entry.Value ?? this.First().Value;
		}
	}
}