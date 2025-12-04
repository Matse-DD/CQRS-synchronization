using Application.Contracts.Events.EventOptions;
using Infrastructure.Events.Mappings.MySQL;

namespace ApplicationTests.Infrastructure.Events.MySql;

public class TestMySqlEventFactory
{
    [Test]
    public void MySqlEventFactory_Gives_MySqlInsertEvent_Back_When_Given_A_Event_Of_EventType_Insert()
    {
        // Arrange
        string insertEventMessage = @"
        {
          ""event_id"": ""84c9d1a3-b0e7-4f6c-9a2f-1e5b8d2c6f0a"",
          ""occured_at"": ""2025-11-29T17:15:00Z"",
          ""aggregate_name"": ""Product"",
          ""status"": ""PENDING"",
          ""event_type"": ""INSERT"",
          ""payload"": {
            ""product_id"": ""038e2f47-c1a0-4b3d-98e1-5f2d0c1b4e9f"",
            ""name"": ""Wireless Mechanical Keyboard"",
            ""sku"": ""KB-WM-001"",
            ""price"": 129.99,
            ""stock_level"": 50,
            ""is_active"": true
          }
        }";

        MySqlEventFactory eventFactory = new MySqlEventFactory();

        // Act
        Event determinedEvent = eventFactory.DetermineEvent(insertEventMessage);
        string mySqlCommand = determinedEvent.GetCommand();

        // Assert
        Assert.That(determinedEvent, Is.TypeOf(typeof(MySqlInsertEvent)));

        string expectedMySqlCommand =
            "INSERT INTO Product (product_id, name, sku, price, stock_level, is_active)\n" +
            @"VALUES (""038e2f47-c1a0-4b3d-98e1-5f2d0c1b4e9f"", ""Wireless Mechanical Keyboard"", ""KB-WM-001"", 129.99, 50, True)";

        Assert.That(mySqlCommand, Is.EqualTo(expectedMySqlCommand));
    }

    [Test]
    public void MySqlEventFactory_Gives_MySqlDeleteEvent_Back_When_Given_A_Event_Of_EventType_Delete()
    {
        // Arrange
        string deleteEventMessage = @"
        {
          ""event_id"": ""84c9d1a3-b0e7-4f6c-9a2f-1e5b8d2c6f0a"",
          ""occured_at"": ""2025-11-29T17:15:00Z"",
          ""aggregate_name"": ""Product"",
          ""status"": ""PENDING"",
          ""event_type"": ""DELETE"",
          ""payload"": {
            ""condition"": {
                ""amount_sold"": "">5"",
                ""price"": "">10""
            }
          }
        }";

        MySqlEventFactory eventFactory = new MySqlEventFactory();

        // Act
        Event determinedEvent = eventFactory.DetermineEvent(deleteEventMessage);
        string mySqlCommand = determinedEvent.GetCommand();

        // Assert
        Assert.That(determinedEvent, Is.TypeOf(typeof(MySqlDeleteEvent)));

        string expectedMySqlCommand =
            "DELETE FROM Product WHERE amount_sold>5 AND price>10";

        Assert.That(mySqlCommand, Is.EqualTo(expectedMySqlCommand));
    }

    [Test]
    public void MySqlEventFactory_Gives_MySqlUpdateEvent_Back_When_Given_A_Event_Of_EventType_Update()
    {
        // Arrange
        string updateEventMessage = @"
        {
            ""event_id"": ""84c9d1a3-b0e7-4f6c-9a2f-1e5b8d2c6f0a"",
            ""occured_at"": ""2025-11-29T17:15:00Z"",
            ""aggregate_name"": ""Product"",
            ""status"": ""PENDING"",
            ""event_type"": ""UPDATE"",
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

        MySqlEventFactory eventFactory = new MySqlEventFactory();

        // Act
        Event determinedEvent = eventFactory.DetermineEvent(updateEventMessage);
        string mySqlCommand = determinedEvent.GetCommand();

        // Assert
        Assert.That(determinedEvent, Is.TypeOf(typeof(MySqlUpdateEvent)));

        string expectedMySqlCommand =
            "UPDATE Product\n" +
            "SET price = price * 1.10, amount_sold = amount_sold + 1\n" +
            "WHERE amount_sold>5 AND price>10";

        Assert.That(mySqlCommand, Is.EqualTo(expectedMySqlCommand));
    }
}
