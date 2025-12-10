
using Application.Contracts.Observer;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Infrastructure.Observer;

public class MongoDbObserver : IObserver
{
    private readonly MongoClient _client;
    private readonly IMongoDatabase _database;
    private readonly IMongoCollection<BsonDocument> _collection;
    private IChangeStreamCursor<ChangeStreamDocument<BsonDocument>> _cursor;

    public MongoDbObserver(string connectionString)
    {
        _client = new MongoClient(connectionString);
        _database = _client.GetDatabase("users");
        _collection = _database.GetCollection<BsonDocument>("events")!;
    }

    public async void StartListening(Action<string> callback)
    {
        PipelineDefinition<ChangeStreamDocument<BsonDocument>, ChangeStreamDocument<BsonDocument>> pipeline = new EmptyPipelineDefinition<ChangeStreamDocument<BsonDocument>>()
               .Match("{operationType: { $in: ['insert'] }}");

        ChangeStreamOptions options = new ChangeStreamOptions
        {
            FullDocument = ChangeStreamFullDocumentOption.UpdateLookup
        };

        _cursor = _collection.Watch(pipeline, options);

        foreach (ChangeStreamDocument<BsonDocument>? change in _cursor.ToEnumerable())
        {
            callback(change.FullDocument.ToJson());
        }
    }

    public async void StopListening()
    {
        _cursor.Dispose();
    }
}
