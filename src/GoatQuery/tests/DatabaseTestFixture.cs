using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Testcontainers.PostgreSql;
using Xunit;

public class DatabaseTestFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _postgresContainer;
    private TestDbContext? _dbContext;

    public TestDbContext DbContext => _dbContext ?? throw new InvalidOperationException("Database not initialized");

    public async Task InitializeAsync()
    {
        // Create and start PostgreSQL container
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("testdb")
            .WithUsername("test")
            .WithPassword("test")
            .Build();

        await _postgresContainer.StartAsync();

        // Create DbContext with connection to container
        var connectionString = _postgresContainer.GetConnectionString();
        var optionsBuilder = new DbContextOptionsBuilder<TestDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        // Enable EF Core logging
        optionsBuilder.LogTo(
            Console.WriteLine,
            new[] { DbLoggerCategory.Database.Command.Name, DbLoggerCategory.Query.Name },
            LogLevel.Information,
            DbContextLoggerOptions.DefaultWithLocalTime | DbContextLoggerOptions.SingleLine
        );

        optionsBuilder.EnableSensitiveDataLogging();
        optionsBuilder.EnableDetailedErrors();

        _dbContext = new TestDbContext(optionsBuilder.Options);

        // Create database schema
        await _dbContext.Database.EnsureCreatedAsync();

        // Seed test data
        await SeedTestData();
    }

    private async Task SeedTestData()
    {
        var users = TestData.Users.Values.ToList();
        await _dbContext.Users.AddRangeAsync(users);
        await _dbContext.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        if (_dbContext != null)
        {
            await _dbContext.DisposeAsync();
        }

        if (_postgresContainer != null)
        {
            await _postgresContainer.StopAsync();
            await _postgresContainer.DisposeAsync();
        }
    }
}