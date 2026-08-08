using Innkeep2.Database.Repository;
using Microsoft.EntityFrameworkCore;

namespace Innkeep2.Database.Tests.Repository;

public sealed class TestRepository(IDbContextFactory<TestDbContext> factory)
	: AbstractRepository<TestEntity, TestDbContext>(factory);