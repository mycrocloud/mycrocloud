using Microsoft.EntityFrameworkCore;
using WebApp.Infrastructure;
using WebApp.Migrations;

namespace WebApp.MigrationTest;

public class UnitTest
{
    readonly AppDbContext _dbContext;
    public UnitTest()
    {
        var factory = new AppDbContextFactory();
        _dbContext = factory.CreateDbContext([]);
    }

    [Fact(DisplayName = "Should Not Has Pending Model Changes")]
    public void Should_Not_Has_Pending_Model_Changes()
    {
        var result = _dbContext.Database.HasPendingModelChanges();
        Assert.False(result);
    }

    [Fact(DisplayName = "Should Migrate Database")]
    public void Should_Migrate_Database()
    {
        _dbContext.Database.Migrate();
    }
}