using Innkeep2.Database.Model;
using JetBrains.Annotations;

namespace Innkeep2.Database.Tests.Repository;

[TestClass]
public class AbstractInnkeepRepositoryTests
{
	private TestDbContextFactory _factory = null!;
	private TestRepository _sut = null!;

	[UsedImplicitly]
	public TestContext TestContext { get; set; }

	[TestInitialize]
	public void TestInitialize()
	{
		_factory = new TestDbContextFactory();
		_sut = new TestRepository(_factory);
	}

	[TestCleanup]
	public void TestCleanup() => _factory.Dispose();

	[TestMethod]
	public async Task CreateAsync_PersistsEntity()
	{
		var entity = new TestEntity
		{
			Name = "Alpha"
		};

		var result = await _sut.CreateAsync(entity, TestContext.CancellationToken);

		Assert.IsTrue(result.IsSuccess);
		Assert.AreNotEqual(0, result.Value!.Id);
	}

	[TestMethod]
	public async Task GetAsync_ReturnsFailure_WhenNotFound()
	{
		var result = await _sut.GetAsync(999, TestContext.CancellationToken);

		Assert.IsFalse(result.IsSuccess);
		Assert.AreEqual("Db.NotFound", result.Error!.Code);
	}

	[TestMethod]
	public async Task UpdateAsync_PersistsChanges()
	{
		var created = await _sut.CreateAsync(
			new TestEntity
			{
				Name = "Alpha"
			},
			TestContext.CancellationToken
		);

		created.Value!.Name = "Beta";

		var updated = await _sut.UpdateAsync(created.Value!, TestContext.CancellationToken);
		var fetched = await _sut.GetAsync(created.Value!.Id, TestContext.CancellationToken);

		Assert.IsTrue(updated.IsSuccess);
		Assert.AreEqual("Beta", fetched.Value!.Name);
	}

	[TestMethod]
	public async Task DeleteAsync_RemovesEntity()
	{
		var created = await _sut.CreateAsync(
			new TestEntity
			{
				Name = "Alpha"
			},
			TestContext.CancellationToken
		);

		await _sut.DeleteAsync(created.Value!, TestContext.CancellationToken);
		var fetched = await _sut.GetAsync(created.Value!.Id, TestContext.CancellationToken);

		Assert.IsFalse(fetched.IsSuccess);
	}

	[TestMethod]
	[DataRow(Operation.Create)]
	[DataRow(Operation.Update)]
	[DataRow(Operation.Delete)]
	public async Task CrudAsync_DispatchesToCorrectOperation(Operation operation)
	{
		var created = await _sut.CreateAsync(
			new TestEntity
			{
				Name = "Alpha"
			},
			TestContext.CancellationToken
		);

		if (operation == Operation.Create)
			created.Value!.Id = 0;
		
		created.Value!.Operation = operation;
		created.Value!.Name = "Beta";

		var result = await _sut.CrudAsync(created.Value!, TestContext.CancellationToken);

		Assert.IsTrue(result.IsSuccess);
	}

	[TestMethod]
	public async Task CreateAsync_ReturnsFailure_OnConstraintViolation()
	{
		var entity = new TestEntity
		{
			Name = null!
		};

		var result = await _sut.CreateAsync(entity, TestContext.CancellationToken);

		Assert.IsFalse(result.IsSuccess);
		Assert.AreEqual("Db.SaveFailed", result.Error!.Code);
	}
}