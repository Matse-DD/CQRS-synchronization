using ApplicationTests.Shared.Events.Mappings;
using ApplicationTests.Shared.Persistence;
using Infrastructure.Projectors;

namespace ApplicationTests.Infrastructure.Projectors;

public class TestProjector
{
    [Test]
    public async Task Test_Incoming_Events_Are_Being_Processed()
    {
        // Arrange
        const int expectedEventCount = 15;
        MockCommandRepository mockCommandRepo = new MockCommandRepository([]);
        SynchronizedQueryRepository syncQueryRepo = new SynchronizedQueryRepository(expectedEventCount);
        MockEventFactory mockEventFactory = new MockEventFactory();
        Projector projector = new Projector(mockCommandRepo, syncQueryRepo, mockEventFactory);
        ICollection<string> deleteEvents = new List<string>();

        for (int i = 0; i < expectedEventCount; i++)
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

        ICollection<string> expectedCommands = deleteEvents.Select(eventItem => mockEventFactory.DetermineEvent(eventItem).GetCommand()).ToList();

        // Act
        foreach (string eventItem in deleteEvents)
        {
            projector.AddEvent(eventItem);
        }

        // Assert
        try 
        {
            await syncQueryRepo.WaitForCompletionAsync().WaitAsync(TimeSpan.FromSeconds(5));
        }
        catch (TimeoutException)
        {
            Assert.Fail("Test gefaald door trage event projectie (timeout!!)");
        }
        
        Assert.That(syncQueryRepo.History, Has.Count.EqualTo(expectedCommands.Count));

        for (int i = 0; i < expectedCommands.Count; i++)
        {
            Assert.That(syncQueryRepo.History.ElementAt(i), Is.EqualTo(expectedCommands.ElementAt(i)));
        }
    }
}