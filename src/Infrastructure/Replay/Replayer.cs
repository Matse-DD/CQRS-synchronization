using Application.Contracts.Persistence;
using Infrastructure.Projectors;

namespace Infrastructure.Replay;

public class Replayer(ICommandRepository commandRepository, IQueryRepository queryRepository, Projector projector)
{
    private readonly ICommandRepository _commandRepository = commandRepository;
    private readonly IQueryRepository _queryRepository = queryRepository;
    private readonly Projector projector = projector;
}