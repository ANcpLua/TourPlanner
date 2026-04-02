using DAL.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Tests.DAL.Infrastructure;

public abstract class SqliteRepositoryTestBase
{
    private SqliteConnection _connection = null!;

    protected TourPlannerContext DbContext { get; private set; } = null!;

    [SetUp]
    public void BaseSetUp()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        DbContext = CreateContext();
        DbContext.Database.EnsureCreated();
        OnSetUp();
    }

    [TearDown]
    public void BaseTearDown()
    {
        DbContext.Dispose();
        _connection.Dispose();
    }

    protected virtual void OnSetUp()
    {
    }

    protected TourPlannerContext CreateContext()
    {
        return new TourPlannerContext(
            new DbContextOptionsBuilder<TourPlannerContext>()
                .UseSqlite(_connection)
                .Options);
    }
}
