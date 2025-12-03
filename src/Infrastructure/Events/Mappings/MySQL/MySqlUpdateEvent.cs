using Application.Contracts.Events;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Infrastructure.Events.Mappings.MySQL
{
    public class MySqlUpdateEvent : Event
    {
        public MySqlUpdateEvent(string incomingEvent): base(incomingEvent)
        {
            Dictionary<string, object> propertiesEvent = JsonSerializer.Deserialize<Dictionary<string, object>>(incomingEvent);
            Dictionary<string, object> insertPayload = JsonSerializer.Deserialize<Dictionary<string, object>>(propertiesEvent["payload"].ToString());
            PayLoad = new InsertPayload(insertPayload);
        }
        public override string GetCommand()
        {
            Dictionary<string, object> change = JsonSerializer.Deserialize<Dictionary<string, object>>(PayLoad.GetValuePairs()["change"]);

            throw new NotImplementedException();
        }
    }
}
