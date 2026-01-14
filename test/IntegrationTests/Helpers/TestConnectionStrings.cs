namespace IntegrationTests.Helpers;

public static class TestConnectionStrings
{
    public const string MySqlSetup = "Server=localhost;Port=13306;User=root;Password=;";
    
    public const string MySqlQuery = "Server=localhost;Port=13306;Database=cqrs_read;User=root;Password=;";
    
    public const string MongoDbCommand = "mongodb://localhost:27017/users?connect=direct&replicaSet=rs0";
}
