using System.ComponentModel.DataAnnotations;
using Application.Contracts.Persistence;
using ApplicationTests.Shared;
using ApplicationTests.Shared.Events.Mappings;
using ApplicationTests.Shared.Persistence;
using Infrastructure.Projectors;
using Infrastructure.Replay;
using Microsoft.Extensions.Logging.Abstractions;

namespace ApplicationTests.Replay;

public class ReplayTest
{
    [Test]
    public void Test_Replay_Should_Get_Priority_On_Change_Stream()
    {
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
        MockSchemaBuilder mockSchemaBuilder = new MockSchemaBuilder();

        Projector projector = new Projector(commandRepository, queryRepository, eventFactory, NullLogger<Projector>.Instance, mockSchemaBuilder);

        Replayer replayer = new Replayer(commandRepository, queryRepository, projector, NullLogger<Replayer>.Instance);

        MockObserver observer = new MockObserver(seedingObserver);

        //Act
        projector.Lock();
        observer.StartListening(projector.AddEvent, CancellationToken.None);
        replayer.Replay();

        //ASSERT
        SleepTillReady(queryRepository, 30);

        for(int i = 0; i < 30; i++)
        {
            Console.WriteLine($"{i} {queryRepository.History.ElementAt(i)}");
        }


        Assert.That(queryRepository.History.ElementAt(15), Is.EqualTo($"delete {seedingOutbox.ElementAt(0).eventId}"));

        Guid expectedFirstEventIdObserver = eventFactory.DetermineEvent(seedingObserver.ElementAt(0)).EventId;
        Assert.That(queryRepository.History.ElementAt(0), Is.EqualTo($"delete {expectedFirstEventIdObserver}"));
    }

    private static void SleepTillReady(MockQueryRepository queryRepository, int amountOfEvents)
    {
        int count = 0;
        while (count < 500 && queryRepository.History.Count < amountOfEvents)
        {
            Thread.Sleep(10);
            count++;
        }

    }
}