using Infrastructure.Persistence.QueryRepository;
using MySql.Data.MySqlClient;

namespace IntegrationTests.Persistence;

public class TestMySqlQueryRepository
{
    private const string ConnectionStringQueryRepoMySql = "Server=localhost;Port=13306;Database=cqrs_read;User=root;Password=;";
    private MySqlQueryRepository _repository;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        const string connectionNoDb = "Server=localhost;Port=13306;User=root;Password=;";
        await using MySqlConnection connection = new MySqlConnection(connectionNoDb);
        await connection.OpenAsync();

        const string setupSql = @"
            CREATE DATABASE IF NOT EXISTS cqrs_read; 
            USE cqrs_read; 
            CREATE TABLE IF NOT EXISTS last_info (id INT AUTO_INCREMENT PRIMARY KEY, last_event_id CHAR(36));
            CREATE TABLE IF NOT EXISTS TestTable (id INT AUTO_INCREMENT PRIMARY KEY, name VARCHAR(50));
            TRUNCATE TABLE last_info;
            INSERT IGNORE INTO last_info (id, last_event_id) VALUES (1, NULL);";

        await using MySqlCommand cmd = new MySqlCommand(setupSql, connection);
        await cmd.ExecuteNonQueryAsync();
    }

    [SetUp]
    public async Task SetUp()
    {
        _repository = new MySqlQueryRepository(ConnectionStringQueryRepoMySql);
        
        await using MySqlConnection connection = new MySqlConnection(ConnectionStringQueryRepoMySql);
        await connection.OpenAsync();
        
        await using MySqlCommand cmd = new MySqlCommand("UPDATE last_info SET last_event_id = NULL WHERE id = 1; TRUNCATE TABLE TestTable;", connection);
        await cmd.ExecuteNonQueryAsync();
    }

    [Test]
    public async Task GetLastSuccessfulEventId_Should_Return_Empty_Guid_Initially()
    {
        // Act
        Guid result = await _repository.GetLastSuccessfulEventId();

        // Assert
        Assert.That(result, Is.EqualTo(Guid.Empty));
    }

    [Test]
    public async Task Execute_Should_Run_Command_And_Update_LastEventId()
    {
        // Arrange
        Guid eventId = Guid.NewGuid();
        string command = "INSERT INTO TestTable (name) VALUES ('IntegrationTest')";

        // Act
        await _repository.Execute(command, eventId);

        // Assert
        Guid storedId = await _repository.GetLastSuccessfulEventId();
        Assert.That(storedId, Is.EqualTo(eventId));

        await using MySqlConnection connection = new MySqlConnection(ConnectionStringQueryRepoMySql);
        await connection.OpenAsync();
        await using MySqlCommand verifyCmd = new MySqlCommand("SELECT count(*) FROM TestTable WHERE name = 'IntegrationTest'", connection);
        long count = (long) (await verifyCmd.ExecuteScalarAsync())!;
        Assert.That(count, Is.EqualTo(1));
    }

    [Test]
    public async Task Execute_Should_Rollback_Transaction_On_Failure()
    {
        // Arrange
        Guid eventId = Guid.NewGuid();
        const string invalidCommand = "INSERT INTO NonExistentTable (name) VALUES ('Fail')";

        // Act & Assert
        Assert.ThrowsAsync<MySqlException>(async () => await _repository.Execute(invalidCommand, eventId));
        Guid storedId = await _repository.GetLastSuccessfulEventId();
        Assert.That(storedId, Is.EqualTo(Guid.Empty));
    }
}