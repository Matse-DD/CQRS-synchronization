using Infrastructure.Persistence.QueryRepository;
using Microsoft.Extensions.Logging.Abstractions;
using MySql.Data.MySqlClient;

namespace IntegrationTests.Persistence;

public class TestMySqlConcurrency
{
    private const string ConnectionStringQueryRepoMySql = "Server=localhost;Port=13306;Database=cqrs_read;User=root;Password=;";
    private const string ConnectionStringToStartRepoMySql = "Server=localhost;Port=13306;User=root;Password=;";

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        await using MySqlConnection connection = new MySqlConnection(ConnectionStringToStartRepoMySql);
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
        await using MySqlConnection connection = new MySqlConnection(ConnectionStringQueryRepoMySql);
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
        MySqlQueryRepository repo1 = new(ConnectionStringQueryRepoMySql, NullLogger<MySqlQueryRepository>.Instance);
        MySqlQueryRepository repo2 = new(ConnectionStringQueryRepoMySql, NullLogger<MySqlQueryRepository>.Instance);

        Guid testEventId = Guid.NewGuid();

        // Act
        await repo1.Execute("INSERT INTO TestTableConcurrency (value_col) VALUES ('test')", testEventId);

        // Assert
        Guid lastEventIdFromRepo2 = await repo2.GetLastSuccessfulEventId();
        Assert.That(lastEventIdFromRepo2, Is.EqualTo(testEventId));
    }

}