using Application.Contracts.Observer;
using Infrastructure.Tools.DatabaseExtensions;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Infrastructure.Observer;

public class MongoDbObserver : IObserver
{
    private readonly IMongoCollection<BsonDocument> _collection;
    private readonly ILogger<MongoDbObserver> _logger;

    public MongoDbObserver(string connectionString, ILogger<MongoDbObserver> logger)
    {
        MongoClient client = new(connectionString);
        IMongoDatabase? database = client.GetDatabase("users");
        _collection = database.GetCollection<BsonDocument>("events")!;
        _logger = logger;
    }

    public async Task StartListening(Action<string> callback, CancellationToken cancellationToken)
    {
        PipelineDefinition<ChangeStreamDocument<BsonDocument>, ChangeStreamDocument<BsonDocument>>? pipeline = new EmptyPipelineDefinition<ChangeStreamDocument<BsonDocument>>()
            .Match("{operationType: { $in: ['insert'] }}");

        ChangeStreamOptions options = new ChangeStreamOptions();
        options.FullDocument = ChangeStreamFullDocumentOption.UpdateLookup;
        using IChangeStreamCursor<ChangeStreamDocument<BsonDocument>>? cursor = await _collection.WatchAsync(pipeline, options, cancellationToken);

        while (await cursor.MoveNextAsync(cancellationToken))
        {
            foreach (ChangeStreamDocument<BsonDocument>? change in cursor.Current)
            {
                callback(change.FullDocument.SanitizeOccurredAt().ToJson());
            }
        }
    }
}