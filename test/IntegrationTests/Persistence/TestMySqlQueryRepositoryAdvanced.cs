using Infrastructure.Persistence.QueryRepository;
using Microsoft.Extensions.Logging.Abstractions;
using MySql.Data.MySqlClient;

namespace IntegrationTests.Persistence;

public class TestMySqlQueryRepositoryAdvanced
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
            CREATE TABLE IF NOT EXISTS Products (id INT AUTO_INCREMENT PRIMARY KEY, name VARCHAR(100), price DECIMAL(10,2));
            CREATE TABLE IF NOT EXISTS Orders (id INT AUTO_INCREMENT PRIMARY KEY, product_id INT, quantity INT);
            TRUNCATE TABLE last_info;
            INSERT IGNORE INTO last_info (id, last_event_id) VALUES (1, NULL);";

        await using MySqlCommand cmd = new MySqlCommand(setupSql, connection);
        await cmd.ExecuteNonQueryAsync();
    }
    [SetUp]
    public async Task SetUp()
    {
        _repository = new MySqlQueryRepository(ConnectionStringQueryRepoMySql, NullLogger<MySqlQueryRepository>.Instance);

        await using MySqlConnection connection = new MySqlConnection(ConnectionStringQueryRepoMySql);
        await connection.OpenAsync();

        await using MySqlCommand cmd = new MySqlCommand(@"
            UPDATE last_info SET last_event_id = NULL WHERE id = 1; 
            TRUNCATE TABLE Products;
            TRUNCATE TABLE Orders;", connection);
        await cmd.ExecuteNonQueryAsync();
    }
    [Test]
    public async Task Execute_Should_Handle_Complex_Insert_Statements()
    {
        // Arrange
        Guid eventId = Guid.NewGuid();
        string command = @"INSERT INTO Products (name, price) 
                          VALUES ('Test Product', 99.99), 
                                 ('Another Product', 149.99)";

        // Act
        await _repository.Execute(command, eventId);

        // Assert
        await using MySqlConnection connection = new MySqlConnection(ConnectionStringQueryRepoMySql);
        await connection.OpenAsync();
        await using MySqlCommand verifyCmd = new MySqlCommand("SELECT COUNT(*) FROM Products", connection);
        long count = (long)(await verifyCmd.ExecuteScalarAsync())!;
        Assert.That(count, Is.EqualTo(2));

        Guid storedId = await _repository.GetLastSuccessfulEventId();
        Assert.That(storedId, Is.EqualTo(eventId));
    }
}