using Application.Contracts.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Contracts.Ports.Persistence
{
    public interface ICommandQuery
    {
        public ICollection<Event> GetAllEventsFromOutbox();
        public void MarkEvent(Event, )
    }
}
