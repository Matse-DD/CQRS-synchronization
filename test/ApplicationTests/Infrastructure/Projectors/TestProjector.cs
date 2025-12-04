using ApplicationTests.Shared.Events.Mappings;
using ApplicationTests.Shared.Persistence;
using Infrastructure.Projectors;

namespace ApplicationTests.Infrastructure.Projectors;

public class TestProjector
{
    [Test]
    public void Test_Incoming_Events_Are_Being_Processed()
    {
        // Arrange
        MockCommandRepository mockCommandRepo = new MockCommandRepository();
        MockQueryRepository mockQueryRepo = new MockQueryRepository();
        MockEventFactory mockEventFactory = new MockEventFactory();

        Projector projector = new Projector(mockCommandRepo, mockQueryRepo, mockEventFactory);

        ICollection<string> deleteEvents = new List<string>();

        for (int i = 0; i < 15; i++)
        {
            deleteEvents.Add(
               $@"
                    {{
                      ""event_id"": ""{Guid.NewGuid().ToString()}"",
                      ""occured_at"": ""2025-11-29T17:15:00Z"",
                      ""aggregate_name"": ""Product"",
                      ""status"": ""PENDING"",
                      ""event_type"": ""DELETE"",
                      ""payload"": {{
                        ""condition"": {{
                            ""amount_sold"": "">5"",
                            ""price"": "">10""
                            }}
                        }}
                    }}
                ");
        }

        ICollection<string> expectedCommands = new List<string>();

        foreach (string eventItem in deleteEvents)
        {
            expectedCommands.Add(mockEventFactory.DetermineEvent(eventItem).GetCommand());
        }

        // Act
        foreach (string eventItem in deleteEvents)
        {
            projector.AddEvent(eventItem);
        }

        // Assert
        int count = 0;
        while (count < 100 && mockQueryRepo.History.Count == 0)
        {
            Task.Delay(10);
            count++;
        }

        for (int i = 0; i < expectedCommands.Count; i++)
        {
            Assert.That(mockQueryRepo.History.ElementAt(i), Is.EqualTo(expectedCommands.ElementAt(i)));
        }
    }
}
