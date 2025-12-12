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
        _logger.LogInformation("Observer starting to listen for changes...");

        PipelineDefinition<ChangeStreamDocument<BsonDocument>, ChangeStreamDocument<BsonDocument>>? pipeline = new EmptyPipelineDefinition<ChangeStreamDocument<BsonDocument>>()
            .Match("{operationType: { $in: ['insert'] }}");

        ChangeStreamOptions options = new ChangeStreamOptions();
        options.FullDocument = ChangeStreamFullDocumentOption.UpdateLookup;

        try
        {
            using IChangeStreamCursor<ChangeStreamDocument<BsonDocument>>? cursor = await _collection.WatchAsync(pipeline, options, cancellationToken);

            while (await cursor.MoveNextAsync(cancellationToken))
            {
                foreach (ChangeStreamDocument<BsonDocument>? change in cursor.Current)
                {
                    _logger.LogInformation("Change detected (Operation: {OperationType}). Processing...", change.OperationType);
                    callback(change.FullDocument.SanitizeOccurredAt().ToJson());
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Observer stopped.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Observer occurred while listening for changes.");
            throw;
        }
    }
}