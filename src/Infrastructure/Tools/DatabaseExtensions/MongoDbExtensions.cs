using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure.Tools.DatabaseExtensions
{
    public static class MongoDbExtensions
    {
        public static BsonDocument SanitizeOccuredAt(this BsonDocument document)
        {
            BsonDocument clone = new BsonDocument(document);

            if (clone.Contains("occurredAt") && clone["occurredAt"].IsBsonDateTime)
            {
                string dt = clone["occurredAt"].ToUniversalTime().ToString("o");
                clone["occurredAt"] = new BsonString(dt);
            }

            return clone;
        }
    }
}
