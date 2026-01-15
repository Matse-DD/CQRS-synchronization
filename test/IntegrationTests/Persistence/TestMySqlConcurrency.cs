using Application.Contracts.Persistence;
using Infrastructure.Persistence.QueryRepository;
using IntegrationTests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using MySql.Data.MySqlClient;

namespace IntegrationTests.Persistence;

public class TestMySqlConcurrency
{
    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        await using MySqlConnection connection = new MySqlConnection(TestConnectionStrings.MySqlSetup);
        await connection.OpenAsync();

        const string setupSql = @"
            CREATE DATABASE IF NOT EXISTS cqrs_read; 
            USE cqrs_read; 
            CREATE TABLE IF NOT EXISTS last_info (id INT AUTO_INCREMENT PRIMARY KEY, last_event_id CHAR(36));
            DROP TABLE IF EXISTS TestTableConcurrency;
            CREATE TABLE TestTableConcurrency (id INT AUTO_INCREMENT PRIMARY KEY, value_col VARCHAR(100));
            TRUNCATE TABLE last_info;
            INSERT IGNORE INTO last_info (id, last_event_id) VALUES (1, NULL);";

        await using MySqlCommand cmd = new MySqlCommand(setupSql, connection);
        await cmd.ExecuteNonQueryAsync();
    }

    [SetUp]
    public async Task SetUp()
    {
        await using MySqlConnection connection = new MySqlConnection(TestConnectionStrings.MySqlQuery);
        await connection.OpenAsync();

        await using MySqlCommand cmd = new MySqlCommand(@"
            UPDATE last_info SET last_event_id = NULL WHERE id = 1; 
            TRUNCATE TABLE TestTableConcurrency;", connection);
        await cmd.ExecuteNonQueryAsync();
    }

    [Test]
    public async Task Multiple_Repositories_Should_Share_Same_LastEventId()
    {
        // Arrange
        MySqlQueryRepository repo1 = new(TestConnectionStrings.MySqlQuery, NullLogger<MySqlQueryRepository>.Instance);
        MySqlQueryRepository repo2 = new(TestConnectionStrings.MySqlQuery, NullLogger<MySqlQueryRepository>.Instance);

        Guid testEventId = Guid.NewGuid();

        // Act
        await repo1.Execute(new CommandInfo("INSERT INTO TestTableConcurrency (value_col) VALUES ('test')"), testEventId);

        // Assert
        Guid lastEventIdFromRepo2 = await repo2.GetLastSuccessfulEventId();
        Assert.That(lastEventIdFromRepo2, Is.EqualTo(testEventId));
    }

    [Test]
    public async Task Concurrent_Executions_Should_Track_Latest_EventId()
    {
        // Arrange
        MySqlQueryRepository repository = new(TestConnectionStrings.MySqlQuery, NullLogger<MySqlQueryRepository>.Instance);

        List<Task> tasks = new();
        List<Guid> eventIds = new();

        // Act
        for (int i = 0; i < 5; i++)
        {
            Guid eventId = Guid.NewGuid();
            eventIds.Add(eventId);
            tasks.Add(repository.Execute(new CommandInfo($"INSERT INTO TestTableConcurrency (value_col) VALUES ('value{i}')"), eventId));
        }

        await Task.WhenAll(tasks);

        // Assert
        Guid finalEventId = await repository.GetLastSuccessfulEventId();
        Assert.That(eventIds, Contains.Item(finalEventId));

        await using MySqlConnection connection = new MySqlConnection(TestConnectionStrings.MySqlQuery);
        await connection.OpenAsync();
        await using MySqlCommand cmd = new MySqlCommand("SELECT COUNT(*) FROM TestTableConcurrency", connection);
        long count = (long)(await cmd.ExecuteScalarAsync())!;

        Assert.That(count, Is.EqualTo(5));
    }

    [Test]
    public async Task Execute_Should_Be_Atomic_With_EventId_Update()
    {
        // Arrange
        MySqlQueryRepository repository = new(TestConnectionStrings.MySqlQuery, NullLogger<MySqlQueryRepository>.Instance);
        Guid eventId = Guid.NewGuid();

        // Act
        try
        {
            await repository.Execute(new CommandInfo("INVALID SQL SYNTAX"), eventId);
            Assert.Fail("Should have thrown exception");
        }
        catch (MySqlException)
        {
            // Expected exception
        }

        // Assert
        Guid storedEventId = await repository.GetLastSuccessfulEventId();
        Assert.That(storedEventId, Is.EqualTo(Guid.Empty));
    }

    [Test]
    public async Task GetLastSuccessfulEventId_Should_Return_Empty_When_No_Events_Processed()
    {
        // Arrange
        MySqlQueryRepository repository = new(TestConnectionStrings.MySqlQuery, NullLogger<MySqlQueryRepository>.Instance);

        // Act
        Guid lastEventId = await repository.GetLastSuccessfulEventId();

        // Assert
        Assert.That(lastEventId, Is.EqualTo(Guid.Empty));
    }

    [Test]
    public async Task Execute_Should_Handle_Multiple_Statements()
    {
        // Arrange
        MySqlQueryRepository repository = new(TestConnectionStrings.MySqlQuery, NullLogger<MySqlQueryRepository>.Instance);
        Guid eventId = Guid.NewGuid();

        // Act
        await repository.Execute(new CommandInfo(@"
            INSERT INTO TestTableConcurrency (value_col) VALUES ('first');
            INSERT INTO TestTableConcurrency (value_col) VALUES ('second');
            INSERT INTO TestTableConcurrency (value_col) VALUES ('third');
        "), eventId);

        // Assert
        await using MySqlConnection connection = new MySqlConnection(TestConnectionStrings.MySqlQuery);
        await connection.OpenAsync();
        await using MySqlCommand cmd = new MySqlCommand("SELECT COUNT(*) FROM TestTableConcurrency", connection);
        long count = (long)(await cmd.ExecuteScalarAsync())!;

        Assert.That(count, Is.EqualTo(3));


        Guid storedEventId = await repository.GetLastSuccessfulEventId();
        Assert.That(storedEventId, Is.EqualTo(eventId));
    }
}
