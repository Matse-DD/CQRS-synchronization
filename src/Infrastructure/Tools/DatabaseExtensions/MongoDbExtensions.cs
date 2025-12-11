using MongoDB.Bson;

namespace Infrastructure.Tools.DatabaseExtensions
{
    public static class MongoDbExtensions
    {
        public static BsonDocument SanitizeOccurredAt(this BsonDocument document)
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
