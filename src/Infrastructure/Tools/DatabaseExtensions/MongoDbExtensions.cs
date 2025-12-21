using MongoDB.Bson;

namespace Infrastructure.Tools.DatabaseExtensions
{
    public static class MongoDbExtensions
    {
        public static BsonDocument SanitizeOccurredAt(this BsonDocument document)
        {
            if (document.Contains("occuredAt") && document["occuredAt"].IsBsonDateTime)
            {
                string dateTime = document["occuredAt"].ToUniversalTime().ToString("o");
                document["occuredAt"] = new BsonString(dateTime);
            }

            return document;
        }
    }
}
