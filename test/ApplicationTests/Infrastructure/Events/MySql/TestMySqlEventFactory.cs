using Application.Contracts.Events.EventOptions;
using Infrastructure.Events.Mappings.MySQL;
using Infrastructure.Persistence;

namespace ApplicationTests.Infrastructure.Events.MySql;

public class TestMySqlEventFactory
{
    private MySqlEventFactory _eventFactory;

    [SetUp]
    public void Setup()
    {
        _eventFactory = new MySqlEventFactory();
    }

    [Test]
    public void DetermineEvent_Should_Throw_JsonException_On_Invalid_Json()
    {
        // Arrange
        string invalidJson = "{ invalid_json: ";

        // Assert
        Assert.Throws<System.Text.Json.JsonException>(() => _eventFactory.DetermineEvent(invalidJson));
    }

    [Test]
    public void DetermineEvent_Should_Throw_ArgumentOutOfRangeException_On_Unknown_EventType()
    {
        // Arrange
        string unknownTypeEvent = @"
        {
          ""id"": ""84c9d1a3-b0e7-4f6c-9a2f-1e5b8d2c6f0a"",
          ""occurredAt"": ""2025-11-29T17:15:00Z"",
          ""aggregateName"": ""Product"",
          ""status"": ""PENDING"",
          ""eventType"": ""CREATE_TABLE"", 
          ""payload"": {}
        }";

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => _eventFactory.DetermineEvent(unknownTypeEvent));
    }

    [Test]
    public void MySqlEventFactory_Gives_MySqlInsertEvent_Back_When_Given_A_Event_Of_EventType_Insert()
    {
        // Arrange
        string insertEventMessage = @"
        {
          ""id"": ""84c9d1a3-b0e7-4f6c-9a2f-1e5b8d2c6f0a"",
          ""occurredAt"": ""2025-11-29T17:15:00Z"",
          ""aggregateName"": ""Product"",
          ""status"": ""PENDING"",
          ""eventType"": ""INSERT"",
          ""payload"": {
            ""product_id"": ""'038e2f47-c1a0-4b3d-98e1-5f2d0c1b4e9f'"",
            ""name"": ""'Wireless Mechanical Keyboard'"",
            ""sku"": ""'KB-WM-001'"",
            ""price"": 129.99,
            ""stock_level"": 50,
            ""is_active"": true
          }
        }";

        // Act
        Event determinedEvent = _eventFactory.DetermineEvent(insertEventMessage);
        CommandInfo mySqlCommandInfo = determinedEvent.GetCommandInfo();

        // Assert
        Assert.That(determinedEvent, Is.TypeOf(typeof(MySqlInsertEvent)));

        string expectedMySqlCommand =
            "INSERT INTO Product (product_id, name, sku, price, stock_level, is_active)\n" +
            @"VALUES (@product_id, @name, @sku, @price, @stock_level, is_active)";

        Assert.That(mySqlCommandInfo.PureCommand, Is.EqualTo(expectedMySqlCommand));
    }

    [Test]
    public void MySqlEventFactory_Gives_MySqlDeleteEvent_Back_When_Given_A_Event_Of_EventType_Delete()
    {
        // Arrange
        string deleteEventMessage = @"
        {
          ""id"": ""84c9d1a3-b0e7-4f6c-9a2f-1e5b8d2c6f0a"",
          ""occurredAt"": ""2025-11-29T17:15:00Z"",
          ""aggregateName"": ""Product"",
          ""status"": ""PENDING"",
          ""eventType"": ""DELETE"",
          ""payload"": {
            ""condition"": {
                ""amount_sold"": "">5"",
                ""price"": "">10""
            }
          }
        }";

        // Act
        Event determinedEvent = _eventFactory.DetermineEvent(deleteEventMessage);
        string mySqlCommand = determinedEvent.GetCommandInfo().PureCommand;

        // Assert
        Assert.That(determinedEvent, Is.TypeOf(typeof(MySqlDeleteEvent)));
        const string expectedMySqlCommand = "DELETE FROM Product WHERE amount_sold>5 AND price>10";
        Assert.That(mySqlCommand, Is.EqualTo(expectedMySqlCommand));
    }

    [Test]
    public void MySqlEventFactory_Gives_MySqlDeleteEvent_Back_When_Given_A_Event_Of_EventType_Delete_WithStringAndNumberIn()
    {
        // Arrange
        string deleteEventMessage = @"
        {
          ""id"": ""84c9d1a3-b0e7-4f6c-9a2f-1e5b8d2c6f0a"",
          ""occurredAt"": ""2025-11-29T17:15:00Z"",
          ""aggregateName"": ""Product"",
          ""status"": ""PENDING"",
          ""eventType"": ""DELETE"",
          ""payload"": {
            ""condition"": {
                ""name"": ""'gene in a bottle'"",
                ""price"": "">10""
            }
          }
        }";

        // Act
        Event determinedEvent = _eventFactory.DetermineEvent(deleteEventMessage);
        string mySqlCommand = determinedEvent.GetCommandInfo().PureCommand;

        // Assert
        Assert.That(determinedEvent, Is.TypeOf(typeof(MySqlDeleteEvent)));
        const string expectedMySqlCommand = "DELETE FROM Product WHERE name = @name AND price > @price";
        Assert.That(mySqlCommand, Is.EqualTo(expectedMySqlCommand));
    }

    [Test]
    public void MySqlEventFactory_Gives_MySqlDeleteEvent_Back_When_Given_A_Event_Of_EventType_Delete_With_DoubleSingleQuoteInName()
    {
        // Arrange
        string deleteEventMessage = @"
        {
          ""id"": ""84c9d1a3-b0e7-4f6c-9a2f-1e5b8d2c6f0a"",
          ""occurredAt"": ""2025-11-29T17:15:00Z"",
          ""aggregateName"": ""Product"",
          ""status"": ""PENDING"",
          ""eventType"": ""DELETE"",
          ""payload"": {
            ""condition"": {
                ""name"": ""'gene's in a bottle'"",
                ""price"": "">10""
            }
          }
        }";

        // Act
        Event determinedEvent = _eventFactory.DetermineEvent(deleteEventMessage);
        CommandInfo mySqlCommandInfo = determinedEvent.GetCommandInfo();

        // Assert
        Assert.That(determinedEvent, Is.TypeOf(typeof(MySqlDeleteEvent)));
        const string expectedMySqlCommand = "DELETE FROM Product WHERE name = @name AND price>@price";
        Assert.That(mySqlCommandInfo.PureCommand, Is.EqualTo(expectedMySqlCommand));

        Assert.That(mySqlCommandInfo.Parameters["@name"],Is.EqualTo("gene''s in a bottle"));
        //Assert.That(mySqlCommandInfo.Parameters["@price"], Is.EqualTo(10));
    }

    [Test]
    public void MySqlEventFactory_Gives_MySqlUpdateEvent_Back_When_Given_A_Event_Of_EventType_Update()
    {
        // Arrange
        string updateEventMessage = @"
        {
            ""id"": ""84c9d1a3-b0e7-4f6c-9a2f-1e5b8d2c6f0a"",
            ""occurredAt"": ""2025-11-29T17:15:00Z"",
            ""aggregateName"": ""Product"",
            ""status"": ""PENDING"",
            ""eventType"": ""UPDATE"",
            ""payload"": {
                ""condition"": {
                    ""amount_sold"": "">5"",
                    ""price"": "">10""
                },
                ""change"": {
                    ""price"": ""price * 1.10"",
                    ""amount_sold"":""amount_sold + 1""
                }
            }
        }";

        // Act
        Event determinedEvent = _eventFactory.DetermineEvent(updateEventMessage);
        string mySqlCommand = determinedEvent.GetCommandInfo().PureCommand;

        // Assert
        Assert.That(determinedEvent, Is.TypeOf(typeof(MySqlUpdateEvent)));

        string expectedMySqlCommand =
            "UPDATE Product\n" +
            "SET price = price * 1.10, amount_sold = amount_sold + 1\n" +
            "WHERE amount_sold>5 AND price>10";

        Assert.That(mySqlCommand, Is.EqualTo(expectedMySqlCommand));
    }
    [Test]
    public void MySqlEventFactory_Gives_MySqlUpdateEvent_Back_When_Given_A_Event_Of_EventType_Update_With_A_Bool()
    {
        // Arrange
        string updateEventMessage = @"
        {
            ""id"": ""84c9d1a3-b0e7-4f6c-9a2f-1e5b8d2c6f0a"",
            ""occurredAt"": ""2025-11-29T17:15:00Z"",
            ""aggregateName"": ""Product"",
            ""status"": ""PENDING"",
            ""eventType"": ""UPDATE"",
            ""payload"": {
                ""condition"": {
                    ""is_active"": ""false"",
                    ""price"": "">10""
                },
                ""change"": {
                    ""price"": ""price * 1.10"",
                    ""is_active"":""true""
                }
            }
        }";

        // Act
        Event determinedEvent = _eventFactory.DetermineEvent(updateEventMessage);
        string mySqlCommand = determinedEvent.GetCommandInfo().PureCommand;

        // Assert
        Assert.That(determinedEvent, Is.TypeOf(typeof(MySqlUpdateEvent)));

        string expectedMySqlCommand =
            "UPDATE Product\n" +
            "SET price = price * 1.10, is_active = true\n" +
            "WHERE is_active = false AND price>10";

        Assert.That(mySqlCommand, Is.EqualTo(expectedMySqlCommand));
    }
}