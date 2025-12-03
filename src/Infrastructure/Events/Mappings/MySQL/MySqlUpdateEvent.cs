using Application.Contracts.Events;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Infrastructure.Events.Mappings.MySQL
{
    public class MySqlUpdateEvent(IntermediateEvent intermediateEvent) : UpdateEvent(intermediateEvent)
    {
        public override string GetCommand()
        {
            Console.WriteLine("impletent the update creator");
            return "update command";
        }
    }
}
