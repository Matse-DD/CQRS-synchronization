using Application.Contracts.Persistence;
using Infrastructure.Projectors;

namespace Infrastructure.Replay;

public class Replayer(ICommandRepository commandRepository, IQueryRepository queryRepository, Projector projector)
{
    private readonly ICommandRepository _commandRepository = commandRepository;
    private readonly IQueryRepository _queryRepository = queryRepository;
    private readonly Projector _projector = projector;

    public void Replay()
    {
        _projector.Lock();
        StartReplaying();
    }

    private async void StartReplaying()
    {
        try
        {
            IEnumerable<OutboxEvent> outboxEvents = await _commandRepository.GetAllEvents();
            await _queryRepository.Clear();

            _projector.AddEventsToFront(outboxEvents.Select(e => e.eventItem));
            _projector.Unlock();
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error in Replay Mechanism: {e.Message}");
        }
    }
}