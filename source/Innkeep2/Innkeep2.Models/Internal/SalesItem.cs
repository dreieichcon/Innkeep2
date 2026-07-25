using Innkeep2.Models.Pretix;

namespace Innkeep2.Models.Internal;

public sealed record SalesItem
{
	public required int Id { get; set; }

	public required int VariationId { get; set; }

	public required string Name { get; set; }

	public required decimal Price { get; set; }

	public required decimal TaxRate { get; set; }

	public static SalesItem[] FromPretix(PretixSalesItem item)
	{
		if (item.Variations.Count == 0)
		{
			return
			[
				new SalesItem
				{
					Id = item.Id,
					VariationId = 0,
					Name = item.Name.German,
					Price = item.DefaultPrice,
					TaxRate = item.TaxRate
				}
			];
		}

		return item.Variations.Select(x => new SalesItem
				{
					Id = item.Id,
					VariationId = x.Id,
					Name = x.Name.German,
					Price = x.Price,
					TaxRate = item.TaxRate
				}
			)
			.ToArray();
	}
}