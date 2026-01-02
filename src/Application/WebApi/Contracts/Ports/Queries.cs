using Application.Contracts.Events.EventOptions;
using System.Linq.Expressions;

namespace Application.WebApi.Contracts.Ports
{
    public interface IGetEventsByFiltersQuery
    {
        Task<IReadOnlyList<Event>> Fetch(Expression<Func<Event, bool>> filter);
    }
}
