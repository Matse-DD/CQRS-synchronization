using System.Text.Json;
using Application.Contracts.Events.Factory;
using Application.Contracts.Persistence;
using ApplicationTests.Shared.Events.Mappings;

namespace ApplicationTests.Contracts.Events;

public class TestEventEquality
{
    [Test]
    public void Event_Equals_Should_Return_True_When_Ids_Match()
    {
        // Arrange
        Guid id = Guid.NewGuid();
        IntermediateEvent iEvent = CreateIntermediate(id);
        MockInsertEvent event1 = new MockInsertEvent(iEvent);
        MockInsertEvent event2 = new MockInsertEvent(iEvent);

        // Assert
        Assert.That(event1, Is.EqualTo(event2));
        Assert.That(event1, Is.EqualTo(event2));
        Assert.That(event1.GetHashCode(), Is.EqualTo(event2.GetHashCode()));
    }

    [Test]
    public void Event_Equals_Should_Return_False_When_Ids_Differ()
    {
        // Arrange
        MockInsertEvent event1 = new MockInsertEvent(CreateIntermediate(Guid.NewGuid()));
        MockInsertEvent event2 = new MockInsertEvent(CreateIntermediate(Guid.NewGuid()));

        // Assert
        Assert.That(event1, Is.Not.EqualTo(event2));
        Assert.That(event1, Is.Not.EqualTo(event2));
    }

    [Test]
    public void Event_Equals_Should_Return_False_For_Null_Or_Different_Type()
    {
        // Arrange
        MockInsertEvent event1 = new MockInsertEvent(CreateIntermediate(Guid.NewGuid()));

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(event1, Is.Not.Null);
            Assert.That(event1, Is.Not.EqualTo(new object()));
        }
    }

    [Test]
    public void OutboxEvent_Record_Should_Support_Equality()
    {
        // Arrange
        string id = Guid.NewGuid().ToString();
        const string payload = "{}";

        OutboxEvent ev1 = new OutboxEvent(id, payload);
        OutboxEvent ev2 = new OutboxEvent(id, payload);
        OutboxEvent ev3 = new OutboxEvent("different", payload);

        // Assert
        Assert.That(ev1, Is.EqualTo(ev2));
        Assert.That(ev1, Is.Not.EqualTo(ev3));
        Assert.That(ev1.ToString(), Does.Contain(id));
    }

    private IntermediateEvent CreateIntermediate(Guid id)
    {
        string json = $@"{{
            ""id"": ""{id}"",
            ""occurredAt"": ""{DateTime.UtcNow:O}"",
            ""aggregateName"": ""Test"",
            ""status"": ""PENDING"",
            ""eventType"": ""INSERT"",
            ""payload"": {{ ""key"": ""value"" }}
        }}";

        return JsonSerializer.Deserialize<IntermediateEvent>(json)!;
    }
}