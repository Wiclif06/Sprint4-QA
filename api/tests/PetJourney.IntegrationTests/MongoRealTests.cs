using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Driver;
namespace PetJourney.IntegrationTests;
public sealed class MongoRealFactAttribute : FactAttribute
{
    public MongoRealFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("PETJOURNEY_TEST_MONGO")))
            Skip = "Configure PETJOURNEY_TEST_MONGO para executar contra um MongoDB real de testes.";
    }
}
[Collection("API")]
public sealed class MongoRealTests(ApiFixture fixture) : IAsyncLifetime
{
    private readonly string _database = "petjourney_tests_" + Guid.NewGuid().ToString("N");
    public Task InitializeAsync() => fixture.ResetAsync();
    public async Task DisposeAsync()
    {
        var connection = Environment.GetEnvironmentVariable("PETJOURNEY_TEST_MONGO");
        if (connection != null)
        {
            var settings = MongoClientSettings.FromConnectionString(connection);
            settings.ServerSelectionTimeout = TimeSpan.FromSeconds(3);
            await new MongoClient(settings).DropDatabaseAsync(_database);
        }
    }
    private WebApplicationFactory<Program> Factory(string? connection = null) => fixture.WithWebHostBuilder(b =>
        b.ConfigureAppConfiguration((_, c) => c.AddInMemoryCollection(new Dictionary<string,string?>
        {
            ["Mongo:ConnectionString"] = connection ?? Environment.GetEnvironmentVariable("PETJOURNEY_TEST_MONGO"),
            ["Mongo:Database"] = _database, ["Mongo:Required"] = "true", ["Mongo:TimeoutSeconds"] = "1"
        })));
    [MongoRealFact]
    public async Task Api_WriteReadRestartFilterAndIndex_WorkAgainstRealMongo()
    {
        string location;
        using (var first = Factory())
        using (var client = await Sprint4RegressionTests.LoginAsync(first, "dr.joao@petjourney.com"))
        {
            var created = await client.PostAsJsonAsync("/api/pets/1/eventos", new { type = "Consulta", description = "Persistência real da Sprint 4" });
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);
            location = created.Headers.Location!.ToString();
            var second = await client.PostAsJsonAsync("/api/pets/1/eventos", new { type = "Vacina", description = "Segunda inserção real" });
            Assert.Equal(HttpStatusCode.Created, second.StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/ready")).StatusCode);
        }
        // A new host/repository reads the same persisted document.
        using (var second = Factory())
        using (var client = await Sprint4RegressionTests.LoginAsync(second, "carlos@petjourney.com"))
        {
            var item = await client.GetFromJsonAsync<JsonElement>(location);
            Assert.Equal("dr.joao@petjourney.com", item.GetProperty("createdBy").GetString());
            Assert.Equal("Persistência real da Sprint 4", item.GetProperty("description").GetString());
            var filtered = await client.GetAsync("/api/pets/1/eventos?type=Consulta&pageSize=1&sortBy=type");
            Assert.Single((await filtered.Content.ReadFromJsonAsync<JsonElement[]>())!);
            Assert.Equal("1", Assert.Single(filtered.Headers.GetValues("X-Total-Count")));
            var paged = await client.GetAsync("/api/pets/1/eventos?pageSize=1&page=2");
            Assert.Single((await paged.Content.ReadFromJsonAsync<JsonElement[]>())!);
            Assert.Equal("2", Assert.Single(paged.Headers.GetValues("X-Total-Count")));
            var distant = await client.GetFromJsonAsync<JsonElement[]>("/api/pets/1/eventos?page=2147483647&pageSize=100");
            Assert.Empty(distant!);
        }
        var collection = new MongoClient(Environment.GetEnvironmentVariable("PETJOURNEY_TEST_MONGO")).GetDatabase(_database).GetCollection<BsonDocument>("clinical_events");
        using var indexes = await collection.Indexes.ListAsync();
        Assert.Contains(await indexes.ToListAsync(), index => index["name"] == "pet_created_id");
        Assert.Equal(2, await collection.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
    }
    [MongoRealFact]
    public async Task OtherTenant_CannotReadPersistedEvent()
    {
        using var factory = Factory();
        using var vet = await Sprint4RegressionTests.LoginAsync(factory, "dr.joao@petjourney.com");
        var response = await vet.PostAsJsonAsync("/api/pets/1/eventos", new { type = "Consulta", description = "Registro privado" });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var other = await Sprint4RegressionTests.LoginAsync(factory, "ana@petjourney.com");
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync(response.Headers.Location)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync("/api/pets/1/eventos")).StatusCode);
    }
    [Fact]
    public async Task UnavailableMongo_Readiness503Liveness200AndHiddenPet404()
    {
        // Closed loopback port: no connection to external infrastructure.
        using var factory = Factory("mongodb://127.0.0.1:1");
        using var client = await Sprint4RegressionTests.LoginAsync(factory, "carlos@petjourney.com");
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/pets/2/eventos")).StatusCode);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await client.GetAsync("/api/pets/1/eventos")).StatusCode);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await client.GetAsync("/health/ready")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/live")).StatusCode);
    }
}
