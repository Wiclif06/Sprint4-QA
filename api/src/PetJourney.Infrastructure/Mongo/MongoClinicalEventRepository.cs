using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using PetJourneyTutorApi.Errors;
using PetJourneyTutorApi.Queries;
namespace PetJourneyTutorApi.Mongo;
public sealed class MongoClinicalEventRepository : IClinicalEventRepository
{
    private readonly Lazy<IMongoDatabase> _database;
    private readonly SemaphoreSlim _indexLock = new(1, 1);
    private volatile bool _indexesReady;
    static MongoClinicalEventRepository()
    {
        BsonClassMap.RegisterClassMap<ClinicalEvent>(map =>
        {
            map.AutoMap();
            map.MapIdMember(x => x.Id).SetSerializer(new StringSerializer(BsonType.ObjectId)).SetIdGenerator(StringObjectIdGenerator.Instance);
        });
    }
    public MongoClinicalEventRepository(IConfiguration config)
    {
        // No parsing, index creation, or network I/O during dependency injection.
        _database = new Lazy<IMongoDatabase>(() =>
        {
            var connection = config["Mongo:ConnectionString"];
            if (string.IsNullOrWhiteSpace(connection)) throw new DependencyUnavailableException("Serviço de eventos não configurado.");
            var settings = MongoClientSettings.FromConnectionString(connection);
            var seconds = Math.Clamp(config.GetValue<int?>("Mongo:TimeoutSeconds") ?? 3, 1, 30);
            settings.ServerSelectionTimeout = TimeSpan.FromSeconds(seconds);
            settings.ConnectTimeout = TimeSpan.FromSeconds(seconds);
            settings.SocketTimeout = TimeSpan.FromSeconds(seconds);
            return new MongoClient(settings).GetDatabase(config["Mongo:Database"] ?? "petjourney");
        });
    }
    private IMongoCollection<ClinicalEvent> Collection => _database.Value.GetCollection<ClinicalEvent>("clinical_events");
    private static async Task<T> ExecuteAsync<T>(Func<Task<T>> operation)
    {
        try { return await operation(); }
        catch (Exception ex) when (ex is MongoException or TimeoutException or FormatException or ArgumentException)
        { throw new DependencyUnavailableException("Serviço de eventos temporariamente indisponível.", ex); }
    }
    public async Task InitializeAsync(CancellationToken ct)
    {
        if (_indexesReady) return;
        await _indexLock.WaitAsync(ct);
        try
        {
            if (_indexesReady) return;
            await ExecuteAsync(async () =>
            {
                await Collection.Indexes.CreateOneAsync(new CreateIndexModel<ClinicalEvent>(
                    Builders<ClinicalEvent>.IndexKeys.Ascending(e => e.PetId).Descending(e => e.CreatedAtUtc).Ascending(e => e.Id),
                    new CreateIndexOptions { Name = "pet_created_id" }), cancellationToken: ct);
                return true;
            });
            _indexesReady = true;
        }
        finally { _indexLock.Release(); }
    }
    public Task<bool> PingAsync(CancellationToken ct) => ExecuteAsync(async () =>
    {
        await _database.Value.RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1), cancellationToken: ct);
        await InitializeAsync(ct);
        return true;
    });
    public async Task<PageResult<ClinicalEvent>> SearchAsync(int petId, ClinicalEventQuery options, CancellationToken ct)
    {
        Paging.Validate(options);
        await InitializeAsync(ct);
        return await ExecuteAsync(async () =>
        {
            var filter = Builders<ClinicalEvent>.Filter.Eq(e => e.PetId, petId);
            if (!string.IsNullOrWhiteSpace(options.Type)) filter &= Builders<ClinicalEvent>.Filter.Eq(e => e.Type, options.Type);
            var count = await Collection.CountDocumentsAsync(filter, cancellationToken: ct);
            var offset = ((long)options.Page - 1) * options.PageSize;
            if (count > int.MaxValue) throw new DependencyUnavailableException("Limite de consulta excedido.");
            List<ClinicalEvent> items = [];
            if (offset < count)
            {
                var field = options.SortBy switch { "id" => "_id", "type" => nameof(ClinicalEvent.Type), _ => nameof(ClinicalEvent.CreatedAtUtc) };
                var sort = options.Descending ? Builders<ClinicalEvent>.Sort.Descending(field) : Builders<ClinicalEvent>.Sort.Ascending(field);
                if (field != "_id") sort = sort.Ascending("_id");
                items = await Collection.Find(filter).Sort(sort).Skip((int)offset).Limit(options.PageSize).ToListAsync(ct);
            }
            return new PageResult<ClinicalEvent>(items, (int)count, options.Page, options.PageSize);
        });
    }
    public async Task<ClinicalEvent?> GetAsync(int petId, string id, CancellationToken ct)
    {
        if (!ObjectId.TryParse(id, out _)) return null;
        return await ExecuteAsync(async () => await Collection.Find(e => e.PetId == petId && e.Id == id).FirstOrDefaultAsync(ct));
    }
    public async Task<ClinicalEvent> AddAsync(ClinicalEvent item, CancellationToken ct)
    {
        await InitializeAsync(ct);
        return await ExecuteAsync(async () => { await Collection.InsertOneAsync(item, cancellationToken: ct); return item; });
    }
}
