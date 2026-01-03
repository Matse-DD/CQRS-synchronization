using Application.Contracts.Events.EventOptions;
using System.Linq.Expressions;

namespace Application.WebApi.Contracts.Filters;

public static class CqrsEventExpressions
{
    public static Expression<Func<Event, bool>> CqrsEventsBasedOnStatusAndBeforeTime(string? status, DateTime? beforeTime)
    {
        if (status == null && beforeTime == null) return cqrsEvent => true;
        if (status == null) return cqrsEvent => cqrsEvent.OccuredAt <= beforeTime;
        if (beforeTime == null) return cqrsEvent => cqrsEvent.Status.Equals(status);

        return cqrsEvent => cqrsEvent.Status.Equals(status) && cqrsEvent.OccuredAt <= beforeTime; 
    }
}
