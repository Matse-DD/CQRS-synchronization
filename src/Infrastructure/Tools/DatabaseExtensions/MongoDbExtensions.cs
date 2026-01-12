using MongoDB.Bson;

namespace Infrastructure.Tools.DatabaseExtensions
{
    public static class MongoDbExtensions
    {
        public static BsonDocument SanitizeOccurredAt(this BsonDocument document)
        {
            if (document.Contains("occurredAt") && document["occurredAt"].IsBsonDateTime)
            {
                string dateTime = document["occurredAt"].ToUniversalTime().ToString("o");
                document["occurredAt"] = new BsonString(dateTime);
            }

            return document;
        }
    }
}


