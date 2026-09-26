using Innkeep2.Models.Internal;

namespace Innkeep2.Services.Client;

public sealed class ClientCartService
{
    private readonly List<SalesItem> _items = [];

    public IReadOnlyList<SalesItem> Items => _items;

    public decimal Total => _items.Sum(x => x.Price * (x.Quantity ?? 1));

    public event EventHandler? Changed;

    public void AddItem(SalesItem item, int quantity = 1)
    {
        var existing = _items.FirstOrDefault(x => x.Id == item.Id && x.VariationId == item.VariationId);

        if (existing is not null)
            existing.Quantity = (existing.Quantity ?? 1) + quantity;
        else
            _items.Add(item with { Quantity = quantity });

        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void RemoveItem(SalesItem item)
    {
        _items.RemoveAll(x => x.Id == item.Id && x.VariationId == item.VariationId);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Clear()
    {
        _items.Clear();
        Changed?.Invoke(this, EventArgs.Empty);
    }
}