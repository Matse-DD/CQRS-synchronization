using Application.Contracts.Events.Enums;
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
        MongoUrl url = new(connectionString);
        MongoClient client = new(url);

        string databaseName = url.DatabaseName ?? throw new ArgumentException("Connection string does not contain database name");

        IMongoDatabase? database = client.GetDatabase(databaseName);

        _collection = database.GetCollection<BsonDocument>("events")!;
        _logger = logger;
    }

    public async Task StartListening(Action<string> callback, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Observer starting to listen for changes...");

        try
        {
            using IChangeStreamCursor<ChangeStreamDocument<BsonDocument>>? cursor = await _collection.WatchAsync(
                ConfigurePipeline(),
                ConfigureOptions(),
                cancellationToken
            );

            await CursorLifeCycleAsync(callback, cursor, cancellationToken);
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

    private static ChangeStreamOptions ConfigureOptions()
    {
        ChangeStreamOptions options = new ChangeStreamOptions();
        options.FullDocument = ChangeStreamFullDocumentOption.UpdateLookup;

        return options;
    }

    private static PipelineDefinition<ChangeStreamDocument<BsonDocument>, ChangeStreamDocument<BsonDocument>> ConfigurePipeline()
    {
        return new EmptyPipelineDefinition<ChangeStreamDocument<BsonDocument>>()
            .Match("{operationType: { $in: ['insert'] }}");
    }

    private async Task CursorLifeCycleAsync(Action<string> callback, IChangeStreamCursor<ChangeStreamDocument<BsonDocument>> cursor, CancellationToken cancellationToken)
    {
        while (await cursor.MoveNextAsync(cancellationToken))
        {
            foreach (ChangeStreamDocument<BsonDocument>? change in cursor.Current)
            {
                BsonDocument changedValue = change.FullDocument;

                _logger.LogInformation("Change detected (Operation: {OperationType}). Processing...", change.OperationType);
                await ProcessChange(callback, changedValue);
            }
        }
    }

    private async Task ProcessChange(Action<string> callback, BsonDocument changedValue)
    {  
        if (ShouldBeProcessed(changedValue))
        {
            callback(changedValue.SanitizeOccurredAt().ToJson());
        }
    }

    private static bool ShouldBeProcessed(BsonDocument incoming)
    {
        if (incoming.TryGetElement("status", out var element))
        {
            return element.Value.AsString == Status.PENDING.ToString();
        }

        return false;
    }
}
