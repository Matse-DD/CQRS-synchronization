using Application.Contracts.Persistence;
using ApplicationTests.Shared;
using ApplicationTests.Shared.Events.Mappings;
using ApplicationTests.Shared.Persistence;
using Infrastructure.Projectors;
using Infrastructure.Recover;

namespace ApplicationTests.Infrastructure.Recover;

public class TestRecovery
{
    [Test]
    public void Recover_Should_Get_Priority_On_Change_Stream()
    {
        // Arrange
        ICollection<OutboxEvent> seedingOutbox = [];

        for (int i = 0; i < 15; i++)
        {
            Guid guid = Guid.NewGuid();
            seedingOutbox.Add(
                new OutboxEvent(guid.ToString(),
                $@"
                    {{
                        ""id"": ""{guid}"",
                        ""occurredAt"": ""2025-11-29T17:15:00Z"",
                        ""aggregateName"": ""Product"",
                        ""status"": ""PENDING"",
                        ""eventType"": ""DELETE"",
                        ""payload"": {{
                        ""condition"": {{
                            ""amount_sold"": "">5"",
                            ""price"": "">10""
                            }}
                        }}
                    }}
                ")
            );
        }

        ICollection<string> seedingObserver = [];
        for (int i = 0; i < 15; i++)
        {
            seedingObserver.Add(
               $@"
                    {{
                      ""id"": ""{Guid.NewGuid()}"",
                      ""occurredAt"": ""2025-11-29T17:15:00Z"",
                      ""aggregateName"": ""Product"",
                      ""status"": ""PENDING"",
                      ""eventType"": ""DELETE"",
                      ""payload"": {{
                        ""condition"": {{
                            ""amount_sold"": "">5"",
                            ""price"": "">10""
                            }}
                        }}
                    }}
                "
            );
        }
        MockCommandRepository commandRepository = new MockCommandRepository(seedingOutbox);
        MockQueryRepository queryRepository = new MockQueryRepository();
        MockEventFactory eventFactory = new MockEventFactory();
        Projector projector = new Projector(commandRepository, queryRepository, eventFactory);

        Recovery recovery = new Recovery(commandRepository, queryRepository, projector);

        MockObserver observer = new MockObserver(seedingObserver);

        // Act
        projector.Lock();
        observer.StartListening(projector.AddEvent, CancellationToken.None);
        recovery.Recover();

        // Assert
        SleepTillReady(queryRepository);

        Assert.That(queryRepository.History.ElementAt(0), Is.EqualTo($"delete {seedingOutbox.ElementAt(0).eventId}"));

        Guid expectedFirstEventIdObserver = eventFactory.DetermineEvent(seedingObserver.ElementAt(0)).EventId;
        Assert.That(queryRepository.History.ElementAt(15), Is.EqualTo($"delete {expectedFirstEventIdObserver}"));
    }

    [Test]
    public void Recover_Should_Be_Able_To_Skip_LastSuccessEventId()
    {
        // Arrange
        ICollection<OutboxEvent> seedingOutbox = [];

        for (int i = 0; i < 5; i++)
        {
            Guid guid = Guid.NewGuid();
            seedingOutbox.Add(
                new OutboxEvent(guid.ToString(),
                $@"
                    {{
                        ""id"": ""{guid}"",
                        ""occurredAt"": ""2025-11-29T17:15:00Z"",
                        ""aggregateName"": ""Product"",
                        ""status"": ""PENDING"",
                        ""eventType"": ""DELETE"",
                        ""payload"": {{
                        ""condition"": {{
                            ""amount_sold"": "">5"",
                            ""price"": "">10""
                            }}
                        }}
                    }}
                ")
            );
        }

        MockCommandRepository commandRepository = new MockCommandRepository(seedingOutbox);
        MockQueryRepository queryRepository = new MockQueryRepository();
        queryRepository.LastSuccessfulEventId = new Guid(seedingOutbox.ElementAt(0).eventId);

        MockEventFactory eventFactory = new MockEventFactory();
        Projector projector = new Projector(commandRepository, queryRepository, eventFactory);

        Recovery recovery = new Recovery(commandRepository, queryRepository, projector);

        // Act
        recovery.Recover();

        // Assert
        SleepTillReady(queryRepository);

        Assert.That(queryRepository.History.ElementAt(0), Is.EqualTo($"delete {seedingOutbox.ElementAt(1).eventId}"));
    }

    private void SleepTillReady(MockQueryRepository mockQueryRepo)
    {
        int count = 0;
        while (count < 500 && mockQueryRepo.History.Count == 0)
        {
            Thread.Sleep(10);
            count++;
        }
    }
}