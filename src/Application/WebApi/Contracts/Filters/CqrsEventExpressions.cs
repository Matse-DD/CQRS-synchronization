using Application.Contracts.Events.EventOptions;
using System.Linq.Expressions;

namespace Application.WebApi.Contracts.Filters;

public static class CqrsEventExpressions
{
    public static Expression<Func<Event, bool>> CqrsEventsBasedOnStatusAndBeforeTime(string status, DateTime beforeTime)
    {
        return cqrsEvent => cqrsEvent.Status.Equals(status) && cqrsEvent.OccuredAt <= beforeTime; 
    }
}
