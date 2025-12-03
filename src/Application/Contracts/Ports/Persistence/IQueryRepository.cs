using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Contracts.Ports.Persistence
{
    public interface IQueryRepository
    {
        public Guid GetLastSuccessfulEventId();
        public void Execute(string command, Guid eventId);
    }
}
