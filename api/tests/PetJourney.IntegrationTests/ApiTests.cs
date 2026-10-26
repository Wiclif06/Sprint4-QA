using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using PetJourneyTutorApi.Data;
namespace PetJourney.IntegrationTests;
[Collection("API")]
public class ApiTests(ApiFixture fixture) : IAsyncLifetime
{
    public Task InitializeAsync()=>fixture.ResetAsync();
    public Task DisposeAsync()=>Task.CompletedTask;
    [Theory]
    [InlineData("/api/pets")][InlineData("/api/tutores")][InlineData("/auth/me")][InlineData("/health")]
    public async Task Get_SemToken_Retorna401(string path)
    {
        // Arrange
        using var client=fixture.CreateClient();
        // Act
        var response=await client.GetAsync(path);
        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized,response.StatusCode);
    }
    [Fact]
    public async Task GetPets_Paginado_RetornaLinkSelf()
    {
        using var client = await fixture.LoginAsync();
        var response = await client.GetAsync("/api/pets?page=1&pageSize=1");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.TryGetValues("Link", out var values));
        Assert.Contains("rel=\"self\"", string.Join("", values!));
    }
    [Fact]
    public async Task GetEventos_OutroTutor_Retorna404SemConsultarMongo()
    {
        using var client = await fixture.LoginAsync();
        var response = await client.GetAsync("/api/pets/2/eventos");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
    [Fact]
    public async Task PostEventos_TutorSemPapelClinico_Retorna403()
    {
        using var client = await fixture.LoginAsync();
        var response = await client.PostAsJsonAsync("/api/pets/1/eventos", new { type = "Consulta", description = "Revisão clínica" });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
    [Fact]
    public async Task Login_SenhaIncorreta_Retorna401()
    {
        // Arrange
        using var client=fixture.CreateClient();
        // Act
        var response=await client.PostAsJsonAsync("/auth/login",new { username="carlos@petjourney.com",password="errada" });
        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized,response.StatusCode);
    }
    [Theory]
    [InlineData("carlos@petjourney.com",1)][InlineData("ana@petjourney.com",2)]
    [InlineData("admin.petfeliz@petjourney.com",1)][InlineData("admin.saudeanimal@petjourney.com",2)]
    [InlineData("dr.joao@petjourney.com",1)]
    public async Task GetPets_PerfilAutenticado_ListaSomenteEscopo(string username,int expectedPet)
    {
        // Arrange
        using var client=await fixture.LoginAsync(username);
        // Act
        var pets=await client.GetFromJsonAsync<JsonElement[]>("/api/pets");
        // Assert
        Assert.Single(pets!); Assert.Equal(expectedPet,pets![0].GetProperty("idPet").GetInt32());
    }
    [Theory]
    [InlineData("/api/pets/2")][InlineData("/api/pets/2/lembretes")][InlineData("/api/pets/2/timeline?mes=9&ano=2026")]
    [InlineData("/api/tutores/2")][InlineData("/api/clinicas/2")][InlineData("/api/lembretes/2")]
    public async Task Get_RecursoDeOutroTutor_Retorna404(string path)
    {
        // Arrange
        using var client=await fixture.LoginAsync();
        // Act
        var response=await client.GetAsync(path);
        // Assert
        Assert.Equal(HttpStatusCode.NotFound,response.StatusCode);
    }
    [Theory]
    [InlineData("/api/tutores")][InlineData("/api/clinicas")][InlineData("/api/lembretes")]
    public async Task Get_ClinicaAutenticada_IsolaListagens(string path)
    {
        // Arrange
        using var client=await fixture.LoginAsync("admin.petfeliz@petjourney.com");
        // Act
        var items=await client.GetFromJsonAsync<JsonElement[]>(path);
        // Assert
        Assert.Single(items!); Assert.DoesNotContain("Mia",JsonSerializer.Serialize(items)); Assert.DoesNotContain("Ana",JsonSerializer.Serialize(items));
    }
    [Fact]
    public async Task PostPet_OutroTutor_RejeitaSemPersistir()
    {
        // Arrange
        using var client=await fixture.LoginAsync();
        // Act
        var response=await client.PostAsJsonAsync("/api/pets",new { nmPet="Inválido",dsSexo="M",idTutor=2,idEspecie=1 });
        // Assert
        Assert.Equal(HttpStatusCode.BadRequest,response.StatusCode);
        Assert.Single((await client.GetFromJsonAsync<JsonElement[]>("/api/pets"))!);
    }
    [Fact]
    public async Task PostPet_Veterinario_Retorna403()
    {
        // Arrange
        using var client=await fixture.LoginAsync("dr.joao@petjourney.com");
        // Act
        var response=await client.PostAsJsonAsync("/api/pets",new { nmPet="Rex",dsSexo="M",idTutor=1,idEspecie=1 });
        // Assert
        Assert.Equal(HttpStatusCode.Forbidden,response.StatusCode);
    }
    [Fact]
    public async Task PetELembrete_FluxoCrud_PersisteEProtegeExclusao()
    {
        // Arrange
        using var client=await fixture.LoginAsync();
        // Act
        var created=await client.PostAsJsonAsync("/api/pets",new { nmPet="Thor",dsSexo="M",idTutor=1,idEspecie=1,idClinica=1 });
        var pet=await created.Content.ReadFromJsonAsync<JsonElement>(); var id=pet.GetProperty("idPet").GetInt32();
        var date=DateTime.UtcNow.Date.AddDays(2);
        var reminder=await client.PostAsJsonAsync("/api/lembretes",new { idPet=id,dsTipo="VACINA",dsDescricao="Vacina Thor",dtLembrete=date,dsStatus="PENDENTE" });
        var item=await reminder.Content.ReadFromJsonAsync<JsonElement>();var reminderId=item.GetProperty("idLembrete").GetInt32();
        var blocked=await client.DeleteAsync($"/api/pets/{id}");
        var updated=await client.PutAsJsonAsync($"/api/lembretes/{reminderId}",new { idLembrete=reminderId,idPet=id,dsTipo="VACINA",dsDescricao="Vacina atualizada",dtLembrete=date,dsStatus="CONCLUIDO" });
        var timeline=await client.GetFromJsonAsync<JsonElement[]>($"/api/pets/{id}/timeline?mes={date.Month}&ano={date.Year}");
        var removeReminder=await client.DeleteAsync($"/api/lembretes/{reminderId}");
        var removePet=await client.DeleteAsync($"/api/pets/{id}");
        // Assert
        Assert.Equal(HttpStatusCode.Created,created.StatusCode);Assert.NotNull(created.Headers.Location);
        Assert.Equal(HttpStatusCode.Created,reminder.StatusCode);Assert.Equal(HttpStatusCode.BadRequest,blocked.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent,updated.StatusCode);Assert.Single(timeline!);
        Assert.Equal("CONCLUIDO",timeline![0].GetProperty("status").GetString());
        Assert.Equal(HttpStatusCode.NoContent,removeReminder.StatusCode);Assert.Equal(HttpStatusCode.NoContent,removePet.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,(await client.GetAsync($"/api/pets/{id}")).StatusCode);
    }
    [Fact]
    public async Task PostTutor_AdminClinica_VinculaAutomaticamente()
    {
        // Arrange
        using var client=await fixture.LoginAsync("admin.petfeliz@petjourney.com");
        // Act
        var response=await client.PostAsJsonAsync("/api/tutores",new { nmTutor="Marcos",dsEmail="marcos@teste.local",nrTelefone="11999990000" });
        var tutor=await response.Content.ReadFromJsonAsync<JsonElement>();
        var clinics=await client.GetFromJsonAsync<JsonElement[]>($"/api/tutores/{tutor.GetProperty("idTutor").GetInt32()}/clinicas");
        // Assert
        Assert.Equal(HttpStatusCode.Created,response.StatusCode);Assert.Single(clinics!);Assert.Equal(1,clinics![0].GetProperty("idClinica").GetInt32());
    }
    [Theory]
    [InlineData("/health/live")][InlineData("/health/ready")]
    public async Task Health_DependenciasDisponiveis_Retorna200(string path)
    {
        // Arrange
        using var client=fixture.CreateClient();
        // Act
        var response=await client.GetAsync(path);
        // Assert
        Assert.Equal(HttpStatusCode.OK,response.StatusCode);
    }
    [Fact]
    public async Task Health_ServicoExternoFora_Retorna503SemDerrubarLiveness()
    {
        // Arrange
        using var client=fixture.CreateClient();fixture.External.Status=HttpStatusCode.ServiceUnavailable;
        // Act
        var ready=await client.GetAsync("/health/ready");var live=await client.GetAsync("/health/live");
        // Assert
        Assert.Equal(HttpStatusCode.ServiceUnavailable,ready.StatusCode);Assert.Equal(HttpStatusCode.OK,live.StatusCode);
    }
    [Fact]
    public async Task Get_CorrelationIdInformado_DevolveMesmoIdentificador()
    {
        // Arrange
        using var client=await fixture.LoginAsync();client.DefaultRequestHeaders.Add("X-Correlation-ID","qa-sprint3-001");
        // Act
        var response=await client.GetAsync("/api/pets");
        // Assert
        Assert.Equal("qa-sprint3-001",Assert.Single(response.Headers.GetValues("X-Correlation-ID")));
    }
    [Fact]
    public async Task PostPet_CorpoInvalido_Retorna400()
    {
        // Arrange
        using var client=await fixture.LoginAsync();
        // Act
        var response=await client.PostAsJsonAsync("/api/pets",new { });
        // Assert
        Assert.Equal(HttpStatusCode.BadRequest,response.StatusCode);
    }
    [Fact]
    public async Task GetMe_TutorAutenticado_DevolveIdentidade()
    {
        // Arrange
        using var client=await fixture.LoginAsync();
        // Act
        var me=await client.GetFromJsonAsync<JsonElement>("/auth/me");
        // Assert
        Assert.Equal("TUTOR",me.GetProperty("role").GetString());Assert.Equal(1,me.GetProperty("tutorId").GetInt32());
    }

    [Fact]
    public async Task Get_TokenAdulterado_Retorna401()
    {
        // Arrange
        using var client=await fixture.LoginAsync();
        var token=client.DefaultRequestHeaders.Authorization!.Parameter!;
        client.DefaultRequestHeaders.Authorization=new("Bearer",token[..^10]+"AAAAAAAAAA");
        // Act
        var response=await client.GetAsync("/api/pets");
        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized,response.StatusCode);
    }
    [Theory]
    [InlineData("PUT")][InlineData("DELETE")]
    public async Task Pet_OutraClinica_NaoAlteraRegistro(string method)
    {
        // Arrange
        using var client=await fixture.LoginAsync("admin.petfeliz@petjourney.com");
        using var request=new HttpRequestMessage(new HttpMethod(method),"/api/pets/2");
        if(method=="PUT")request.Content=JsonContent.Create(new { idPet=2,nmPet="Alterado",dsSexo="F",idTutor=2,idEspecie=2,idClinica=2 });
        // Act
        var response=await client.SendAsync(request);
        using var other=await fixture.LoginAsync("ana@petjourney.com");
        var pet=await other.GetFromJsonAsync<JsonElement>("/api/pets/2");
        // Assert
        Assert.Equal(HttpStatusCode.NotFound,response.StatusCode);Assert.Equal("Mia",pet.GetProperty("nmPet").GetString());
    }
    [Fact]
    public async Task PutPet_TrocaDeTutor_RejeitaTransferencia()
    {
        // Arrange
        using var client=await fixture.LoginAsync();
        // Act
        var response=await client.PutAsJsonAsync("/api/pets/1",new { idPet=1,nmPet="Rex",dsSexo="M",idTutor=2,idEspecie=1,idClinica=1 });
        // Assert
        Assert.Equal(HttpStatusCode.BadRequest,response.StatusCode);
        Assert.Equal(1,(await client.GetFromJsonAsync<JsonElement>("/api/pets/1")).GetProperty("idTutor").GetInt32());
    }
    [Fact]
    public async Task PutReminder_DataPassada_RejeitaSemModificar()
    {
        // Arrange
        using var client=await fixture.LoginAsync();
        // Act
        var response=await client.PutAsJsonAsync("/api/lembretes/1",new { idLembrete=1,idPet=1,dsTipo="VACINA",dsDescricao="Alterado",dtLembrete=DateTime.UtcNow.AddDays(-2),dsStatus="PENDENTE" });
        // Assert
        Assert.Equal(HttpStatusCode.BadRequest,response.StatusCode);
        Assert.Equal("Vacinação Rex",(await client.GetFromJsonAsync<JsonElement>("/api/lembretes/1")).GetProperty("dsDescricao").GetString());
    }
    [Fact]
    public async Task PostTutor_EmailOutraClinica_Retorna409SemExporDados()
    {
        // Arrange
        using var client=await fixture.LoginAsync("admin.petfeliz@petjourney.com");
        // Act
        var response=await client.PostAsJsonAsync("/api/tutores",new { nmTutor="Duplicado",dsEmail="ana@petjourney.com",nrTelefone="11999990000" });
        // Assert
        Assert.Equal(HttpStatusCode.Conflict,response.StatusCode);
        var content=await response.Content.ReadAsStringAsync();Assert.DoesNotContain("SQLite",content);Assert.DoesNotContain("Ana",content);
        Assert.Single((await client.GetFromJsonAsync<JsonElement[]>("/api/tutores"))!);
    }
    [Fact]
    public async Task Get_FalhaDePersistencia_Retorna500Correlacionado()
    {
        // Arrange
        using var client=await fixture.LoginAsync();
        using(var scope=fixture.Services.CreateScope()) {
            var db=scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.ExecuteSqlRawAsync(db.Database,"DROP TABLE TBLEMBRETE");
        }
        client.DefaultRequestHeaders.Add("X-Correlation-ID","qa-falha-banco");
        // Act
        var response=await client.GetAsync("/api/lembretes");
        var body=await response.Content.ReadFromJsonAsync<JsonElement>();
        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError,response.StatusCode);
        Assert.Equal("qa-falha-banco",body.GetProperty("correlationId").GetString());
        Assert.DoesNotContain("SQLite",body.ToString());Assert.DoesNotContain("stack",body.ToString());
    }
    [Fact]
    public async Task Health_ExcecaoExterna_Retorna503()
    {
        // Arrange
        using var client=fixture.CreateClient();fixture.External.Fail=true;
        // Act
        var response=await client.GetAsync("/health/ready");
        // Assert
        Assert.Equal(HttpStatusCode.ServiceUnavailable,response.StatusCode);
    }
    [Fact]
    public async Task Health_Diagnostico_AdminVeDependenciasETutorRecebe403()
    {
        // Arrange
        using var admin=await fixture.LoginAsync("admin.sistema@petjourney.com");using var tutor=await fixture.LoginAsync();
        // Act
        var health=await admin.GetFromJsonAsync<JsonElement>("/health");var denied=await tutor.GetAsync("/health");
        // Assert
        Assert.Equal("Healthy",health.GetProperty("status").GetString());Assert.Equal(2,health.GetProperty("checks").GetArrayLength());
        Assert.Equal(HttpStatusCode.Forbidden,denied.StatusCode);
    }
    [Fact]
    public async Task GetPets_TelemetriaAtiva_EmiteSpanDeServicoEMetricas()
    {
        // Arrange
        using var client=await fixture.LoginAsync();
        var spans=new System.Collections.Concurrent.ConcurrentBag<string>();var measurements=new System.Collections.Concurrent.ConcurrentBag<string>();
        using var listener=new System.Diagnostics.ActivityListener { ShouldListenTo=s=>s.Name=="PetJourneyTutorApi", Sample=(ref System.Diagnostics.ActivityCreationOptions<System.Diagnostics.ActivityContext> o)=>System.Diagnostics.ActivitySamplingResult.AllDataAndRecorded, ActivityStopped=a=>spans.Add(a.OperationName) };
        System.Diagnostics.ActivitySource.AddActivityListener(listener);
        using var meter=new System.Diagnostics.Metrics.MeterListener { InstrumentPublished=(instrument,l)=>{if(instrument.Meter.Name=="PetJourneyTutorApi")l.EnableMeasurementEvents(instrument);} };
        meter.SetMeasurementEventCallback<long>((instrument,value,tags,state)=>measurements.Add(instrument.Name));
        meter.SetMeasurementEventCallback<double>((instrument,value,tags,state)=>measurements.Add(instrument.Name));meter.Start();
        // Act
        var response=await client.GetAsync("/api/pets");
        // Assert
        Assert.Equal(HttpStatusCode.OK,response.StatusCode);Assert.Contains("PetService.GetAllAsync",spans);
        Assert.Contains("petjourney.requests",measurements);Assert.Contains("petjourney.request.duration",measurements);
    }

    [Theory]
    [InlineData("/api/especies",200)][InlineData("/api/especies/1",200)][InlineData("/api/especies/99",404)]
    [InlineData("/api/especies/1/racas",200)][InlineData("/api/especies/99/racas",404)]
    [InlineData("/api/racas",200)][InlineData("/api/racas/1",200)][InlineData("/api/racas/99",404)]
    [InlineData("/api/tutores/1",200)][InlineData("/api/tutores/1/pets",200)][InlineData("/api/tutores/2/pets",404)]
    [InlineData("/api/pets/1/lembretes",200)][InlineData("/api/clinicas/1",200)]
    [InlineData("/api/pets/1/timeline?mes=13&ano=2026",400)]
    public async Task Get_CatalogosEConsultasLegadas_RespeitaContrato(string path,int expected)
    {
        // Arrange
        using var client=await fixture.LoginAsync();
        // Act
        var response=await client.GetAsync(path);
        // Assert
        Assert.Equal(expected,(int)response.StatusCode);
    }
    [Fact]
    public async Task PutPet_DadosValidos_AtualizaSemMudarTitular()
    {
        // Arrange
        using var client=await fixture.LoginAsync();
        // Act
        var response=await client.PutAsJsonAsync("/api/pets/1",new { idPet=1,nmPet="Rex atualizado",dsSexo="M",idTutor=1,idEspecie=1,idRaca=1,idClinica=1 });
        var pet=await client.GetFromJsonAsync<JsonElement>("/api/pets/1");
        // Assert
        Assert.Equal(HttpStatusCode.NoContent,response.StatusCode);Assert.Equal("Rex atualizado",pet.GetProperty("nmPet").GetString());Assert.Equal(1,pet.GetProperty("idTutor").GetInt32());
    }
    [Fact]
    public async Task Tutor_FluxoAdministrativo_PreservaVinculosERegras()
    {
        // Arrange
        using var admin=await fixture.LoginAsync("admin.sistema@petjourney.com");
        // Act
        var blocked=await admin.DeleteAsync("/api/tutores/1");
        var create=await admin.PostAsJsonAsync("/api/tutores",new { nmTutor="Bruno",dsEmail="bruno@teste.local",nrTelefone="11999990000" });
        var id=(await create.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("idTutor").GetInt32();
        var update=await admin.PutAsJsonAsync($"/api/tutores/{id}",new { idTutor=id,nmTutor="Bruno Costa",dsEmail="bruno@teste.local",nrTelefone="11988880000" });
        var link=await admin.PutAsync($"/api/tutores/{id}/clinica/1",null);
        var clinics=await admin.GetFromJsonAsync<JsonElement[]>($"/api/tutores/{id}/clinicas");
        var unlink=await admin.DeleteAsync($"/api/tutores/{id}/clinica");
        var remove=await admin.DeleteAsync($"/api/tutores/{id}");
        // Assert
        Assert.Equal(HttpStatusCode.BadRequest,blocked.StatusCode);Assert.Equal(HttpStatusCode.Created,create.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent,update.StatusCode);Assert.Equal(HttpStatusCode.NoContent,link.StatusCode);Assert.Single(clinics!);
        Assert.Equal(HttpStatusCode.NoContent,unlink.StatusCode);Assert.Equal(HttpStatusCode.NoContent,remove.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,(await admin.GetAsync($"/api/tutores/{id}")).StatusCode);
    }
    [Theory]
    [InlineData(99,null,"M")][InlineData(2,1,"M")][InlineData(1,null,"X")]
    public async Task PostPet_CatalogoOuSexoInvalido_Retorna400(int species,int? breed,string sex)
    {
        // Arrange
        using var client=await fixture.LoginAsync();
        // Act
        var response=await client.PostAsJsonAsync("/api/pets",new { nmPet="Inválido",dsSexo=sex,idTutor=1,idEspecie=species,idRaca=breed });
        // Assert
        Assert.Equal(HttpStatusCode.BadRequest,response.StatusCode);Assert.Single((await client.GetFromJsonAsync<JsonElement[]>("/api/pets"))!);
    }
}
