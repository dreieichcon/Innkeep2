using Innkeep2.Database.Model;

namespace Innkeep2.Database.Tests.Repository;

public sealed class TestEntity : AbstractDbItem
{
	public required string Name { get; set; }
}