using Infrastructure.Persistence;
using Infrastructure.Persistence.QueryRepository;
using IntegrationTests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using MySql.Data.MySqlClient;

namespace IntegrationTests.Persistence;

public class TestMySqlQueryRepositoryAdvanced
{
    private MySqlQueryRepository _repository;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        await using MySqlConnection connection = new MySqlConnection(TestConnectionStrings.MySqlSetup);
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
        _repository = new MySqlQueryRepository(TestConnectionStrings.MySqlQuery, NullLogger<MySqlQueryRepository>.Instance);

        await using MySqlConnection connection = new MySqlConnection(TestConnectionStrings.MySqlQuery);
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
        await _repository.Execute(new CommandInfo(command), eventId);

        // Assert
        await using MySqlConnection connection = new MySqlConnection(TestConnectionStrings.MySqlQuery);
        await connection.OpenAsync();
        await using MySqlCommand verifyCmd = new MySqlCommand("SELECT COUNT(*) FROM Products", connection);
        long count = (long)(await verifyCmd.ExecuteScalarAsync())!;
        Assert.That(count, Is.EqualTo(2));

        Guid storedId = await _repository.GetLastSuccessfulEventId();
        Assert.That(storedId, Is.EqualTo(eventId));
    }

    [Test]
    public async Task Execute_Should_Handle_Update_Statements()
    {
        // Arrange
        await using (MySqlConnection connection = new MySqlConnection(TestConnectionStrings.MySqlQuery))
        {
            await connection.OpenAsync();
            await using MySqlCommand insertCmd = new MySqlCommand(
                "INSERT INTO Products (name, price) VALUES ('Test', 50.00)", connection);
            await insertCmd.ExecuteNonQueryAsync();
        }

        Guid eventId = Guid.NewGuid();
        string command = "UPDATE Products SET price = 75.00 WHERE name = 'Test'";

        // Act
        await _repository.Execute(new CommandInfo(command), eventId);

        // Assert
        await using MySqlConnection verifyConn = new MySqlConnection(TestConnectionStrings.MySqlQuery);
        await verifyConn.OpenAsync();
        await using MySqlCommand verifyCmd = new MySqlCommand(
            "SELECT price FROM Products WHERE name = 'Test'", verifyConn);
        decimal price = (decimal)(await verifyCmd.ExecuteScalarAsync())!;
        Assert.That(price, Is.EqualTo(75.00m));
    }

    [Test]
    public async Task Execute_Should_Handle_Delete_Statements()
    {
        // Arrange
        await using (MySqlConnection connection = new MySqlConnection(TestConnectionStrings.MySqlQuery))
        {
            await connection.OpenAsync();
            await using MySqlCommand insertCmd = new MySqlCommand(
                "INSERT INTO Products (name, price) VALUES ('ToDelete', 100.00), ('ToKeep', 200.00)",
                connection);
            await insertCmd.ExecuteNonQueryAsync();
        }

        Guid eventId = Guid.NewGuid();
        string command = "DELETE FROM Products WHERE name = 'ToDelete'";

        // Act
        await _repository.Execute(new CommandInfo(command), eventId);

        // Assert
        await using MySqlConnection verifyConn = new MySqlConnection(TestConnectionStrings.MySqlQuery);
        await verifyConn.OpenAsync();
        await using MySqlCommand verifyCmd = new MySqlCommand("SELECT COUNT(*) FROM Products", verifyConn);
        long count = (long)(await verifyCmd.ExecuteScalarAsync())!;
        Assert.That(count, Is.EqualTo(1));

        await using MySqlCommand nameCmd = new MySqlCommand(
            "SELECT name FROM Products", verifyConn);
        string name = (string)(await nameCmd.ExecuteScalarAsync())!;
        Assert.That(name, Is.EqualTo("ToKeep"));
    }

    [Test]
    public async Task Execute_Should_Maintain_Transaction_Atomicity()
    {
        // Arrange
        await using (MySqlConnection connection = new MySqlConnection(TestConnectionStrings.MySqlQuery))
        {
            await connection.OpenAsync();
            await using MySqlCommand insertCmd = new MySqlCommand(
                "INSERT INTO Products (id, name, price) VALUES (1, 'Product1', 50.00)", connection);
            await insertCmd.ExecuteNonQueryAsync();
        }

        Guid eventId = Guid.NewGuid();
        // Should fail due to invalid SQL syntax
        string command = @"
            UPDATE Products SET price = 100.00 WHERE id = 1;
            INVALID SQL SYNTAX HERE;";

        // Act & Assert
        try
        {
            await _repository.Execute(new CommandInfo(command), eventId);
            Assert.Fail("Should have thrown exception due to invalid SQL");
        }
        catch (MySqlException)
        {
            // Expected to fail due to syntax error
        }

        // Verify the Products table was not updated (transaction rolled back)
        await using MySqlConnection verifyConn = new MySqlConnection(TestConnectionStrings.MySqlQuery);
        await verifyConn.OpenAsync();
        await using MySqlCommand verifyCmd = new MySqlCommand(
            "SELECT price FROM Products WHERE id = 1", verifyConn);
        decimal? price = (decimal?)(await verifyCmd.ExecuteScalarAsync());

        Assert.That(price, Is.EqualTo(50.00m));

        Guid storedId = await _repository.GetLastSuccessfulEventId();
        Assert.That(storedId, Is.EqualTo(Guid.Empty));
    }

    [Test]
    public async Task Execute_Should_Handle_Sequential_Events_Correctly()
    {
        // Arrange
        Guid eventId1 = Guid.NewGuid();
        Guid eventId2 = Guid.NewGuid();
        Guid eventId3 = Guid.NewGuid();

        // Act
        await _repository.Execute(new CommandInfo("INSERT INTO Products (name, price) VALUES ('Product1', 10.00)"), eventId1);
        await _repository.Execute(new CommandInfo("INSERT INTO Products (name, price) VALUES ('Product2', 20.00)"), eventId2);
        await _repository.Execute(new CommandInfo("UPDATE Products SET price = 15.00 WHERE name = 'Product1'"), eventId3);

        // Assert
        Guid lastEventId = await _repository.GetLastSuccessfulEventId();
        Assert.That(lastEventId, Is.EqualTo(eventId3));

        await using MySqlConnection connection = new MySqlConnection(TestConnectionStrings.MySqlQuery);
        await connection.OpenAsync();
        await using MySqlCommand verifyCmd = new MySqlCommand("SELECT COUNT(*) FROM Products", connection);
        long count = (long)(await verifyCmd.ExecuteScalarAsync())!;
        Assert.That(count, Is.EqualTo(2));
    }

    [Test]
    public async Task Execute_Should_Handle_Special_Characters_In_Data()
    {
        // Arrange
        Guid eventId = Guid.NewGuid();
        string command = "INSERT INTO Products (name, price) VALUES ('Product with \"quotes\" and ''apostrophes''', 99.99)";

        // Act
        await _repository.Execute(new CommandInfo(command), eventId);

        // Assert
        await using MySqlConnection connection = new MySqlConnection(TestConnectionStrings.MySqlQuery);
        await connection.OpenAsync();
        await using MySqlCommand verifyCmd = new MySqlCommand(
            "SELECT name FROM Products", connection);
        string name = (string)(await verifyCmd.ExecuteScalarAsync())!;
        Assert.That(name, Does.Contain("quotes"));
    }

    [Test]
    public async Task GetLastSuccessfulEventId_Should_Persist_Across_Repository_Instances()
    {
        // Arrange
        Guid eventId = Guid.NewGuid();
        await _repository.Execute(new CommandInfo("INSERT INTO Products (name, price) VALUES ('Test', 10.00)"), eventId);

        // Act
        MySqlQueryRepository newRepository = new(TestConnectionStrings.MySqlQuery, NullLogger<MySqlQueryRepository>.Instance);
        Guid retrievedId = await newRepository.GetLastSuccessfulEventId();

        // Assert
        Assert.That(retrievedId, Is.EqualTo(eventId));
    }

    [Test]
    public async Task Execute_Should_Handle_NULL_Values()
    {
        // Arrange
        Guid eventId = Guid.NewGuid();
        string command = "INSERT INTO Products (name, price) VALUES ('NullTest', NULL)";

        // Act
        await _repository.Execute(new CommandInfo(command), eventId);

        // Assert
        await using MySqlConnection connection = new MySqlConnection(TestConnectionStrings.MySqlQuery);
        await connection.OpenAsync();
        await using MySqlCommand verifyCmd = new MySqlCommand(
            "SELECT price FROM Products WHERE name = 'NullTest'", connection);
        object? price = await verifyCmd.ExecuteScalarAsync();
        Assert.That(price, Is.EqualTo(DBNull.Value));
    }
}
