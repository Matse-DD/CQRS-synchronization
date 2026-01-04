using Application.Contracts.Events.Enums;
using Application.Contracts.Events.EventOptions;
using System.ComponentModel;
using System.Linq.Expressions;

namespace Application.WebApi.Contracts.Filters;

public static class CqrsEventExpressions
{
    public static Expression<Func<Event, bool>> CqrsEventsBasedOnStatusAndBeforeTime(string? status, DateTime? beforeTime)
    {
        if (status == null && beforeTime == null) return cqrsEvent => true;
        if (status == null) return cqrsEvent => cqrsEvent.OccuredAt <= beforeTime;

        if (Enum.TryParse(status, out Status statusEnum))
        {

            if (beforeTime == null) return cqrsEvent => cqrsEvent.Status.Equals(statusEnum);

            return cqrsEvent => cqrsEvent.Status == statusEnum
                && cqrsEvent.OccuredAt <= beforeTime;
        }

        throw new InvalidEnumArgumentException();
    }
}
