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
    }
}