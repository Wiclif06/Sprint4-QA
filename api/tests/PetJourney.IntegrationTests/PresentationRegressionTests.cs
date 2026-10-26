using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace PetJourney.IntegrationTests;

[Collection("API")]
public sealed class PresentationRegressionTests(ApiFixture fixture) : IAsyncLifetime
{
    public Task InitializeAsync() => fixture.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Theory]
    [InlineData("carlos@petjourney.com", 2)]
    [InlineData("admin.petfeliz@petjourney.com", 2)]
    [InlineData("admin.sistema@petjourney.com", 999)]
    public async Task UpdateMissingOrHiddenTutor_Returns404AndPreservesOwnerData(string username, int id)
    {
        using var client = await fixture.LoginAsync(username);
        var response = await client.PutAsJsonAsync($"/api/tutores/{id}", new
        {
            idTutor = id, nmTutor = "Alteração indevida", dsEmail = "alterado@example.test",
            nrTelefone = "11999999999", dsPlano = "FREE"
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Tutor não encontrado.", error.GetProperty("title").GetString());
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var owner = await fixture.LoginAsync("ana@petjourney.com");
        var tutor = await owner.GetFromJsonAsync<JsonElement>("/api/tutores/2");
        Assert.Equal("Ana", tutor.GetProperty("nmTutor").GetString());
        Assert.Equal("ana@petjourney.com", tutor.GetProperty("dsEmail").GetString());
    }

    [Fact]
    public async Task DeleteMissingTutor_Returns404WithReadableMessage()
    {
        using var client = await fixture.LoginAsync("admin.sistema@petjourney.com");
        var response = await client.DeleteAsync("/api/tutores/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Tutor não encontrado.", error.GetProperty("title").GetString());
    }
}
