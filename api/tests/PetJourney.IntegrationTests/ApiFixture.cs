using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PetJourneyTutorApi.Data;
using PetJourneyTutorApi.Models;
using PetJourneyTutorApi.Security;
namespace PetJourney.IntegrationTests;
public sealed class ClinicalHealthHandler : HttpMessageHandler
{
    public HttpStatusCode Status { get; set; } = HttpStatusCode.OK;
    public bool Fail { get; set; }
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
        Fail ? Task.FromException<HttpResponseMessage>(new HttpRequestException("Serviço de teste indisponível")) : Task.FromResult(new HttpResponseMessage(Status));
}
public class ApiFixture : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    public ClinicalHealthHandler External { get; } = new();
    public ApiFixture() { _connection.Open(); }
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, c) => {
            var values = new Dictionary<string,string?> { ["RateLimit:LoginPermitLimit"]="1000", ["Jwt:SigningKey"]="integration-tests-key-with-at-least-32-bytes", ["Jwt:Issuer"]="PetJourney", ["Jwt:Audience"]="PetJourney", ["Database:Provider"]="Sqlite", ["ExternalServices:ClinicalApiHealthUrl"]="http://clinical.test/health" };
            var accounts = new[] {
                new Account { Username="admin.sistema@petjourney.com", Role="ADMIN_SISTEMA" },
                new Account { Username="admin.petfeliz@petjourney.com", Role="ADMIN_CLINICA", ClinicId=1 },
                new Account { Username="admin.saudeanimal@petjourney.com", Role="ADMIN_CLINICA", ClinicId=2 },
                new Account { Username="dr.joao@petjourney.com", Role="VETERINARIO", ClinicId=1, VeterinarianId=1 },
                new Account { Username="carlos@petjourney.com", Role="TUTOR", TutorId=1 },
                new Account { Username="ana@petjourney.com", Role="TUTOR", TutorId=2 }
            };
            for(var i=0;i<accounts.Length;i++) {
                var a=accounts[i]; var prefix=$"Accounts:{i}:";
                values[prefix+"Username"]=a.Username;values[prefix+"Role"]=a.Role;
                values[prefix+"PasswordHash"]=new PasswordHasher<Account>().HashPassword(a,"123456");
                values[prefix+"ClinicId"]=a.ClinicId?.ToString();values[prefix+"TutorId"]=a.TutorId?.ToString(); values[prefix+"VeterinarianId"]=a.VeterinarianId?.ToString();
            }
            c.AddInMemoryCollection(values);
        });
        builder.ConfigureServices(s => {
            s.RemoveAll<DbContextOptions<AppDbContext>>();
            s.AddDbContext<AppDbContext>(o => o.UseSqlite(_connection));
            s.AddHttpClient("clinical-health").ConfigurePrimaryHttpMessageHandler(() => External);
        });
    }
    public async Task ResetAsync()
    {
        using var scope=Services.CreateScope(); var db=scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureDeletedAsync(); await db.Database.EnsureCreatedAsync();
        db.Species.AddRange(new Species { IdEspecie=1,NmEspecie="Cachorro" },new Species { IdEspecie=2,NmEspecie="Gato" });
        db.Breeds.Add(new Breed { IdRaca=1,IdEspecie=1,NmRaca="Golden Retriever" });
        db.Clinics.AddRange(new Clinic { IdClinica=1,NmClinica="Pet Feliz" },new Clinic { IdClinica=2,NmClinica="Saúde Animal" });
        db.Tutors.AddRange(new Tutor { IdTutor=1,NmTutor="Carlos",DsEmail="carlos@petjourney.com",NrTelefone="11911112222" },new Tutor { IdTutor=2,NmTutor="Ana",DsEmail="ana@petjourney.com",NrTelefone="11933334444" });
        db.TutorClinics.AddRange(new TutorClinic { TutorId=1,ClinicId=1 },new TutorClinic { TutorId=2,ClinicId=2 });
        db.Pets.AddRange(new Pet { IdPet=1,NmPet="Rex",DsSexo="M",IdTutor=1,IdClinica=1,IdEspecie=1 },new Pet { IdPet=2,NmPet="Mia",DsSexo="F",IdTutor=2,IdClinica=2,IdEspecie=2 });
        db.Reminders.AddRange(new Reminder { IdLembrete=1,IdPet=1,DsTipo="VACINA",DsDescricao="Vacinação Rex",DtLembrete=DateTime.UtcNow.Date.AddDays(3) },new Reminder { IdLembrete=2,IdPet=2,DsTipo="RETORNO",DsDescricao="Retorno Mia",DtLembrete=DateTime.UtcNow.Date.AddDays(4) });
        await db.SaveChangesAsync(); External.Status=HttpStatusCode.OK; External.Fail=false;
    }
    public async Task<HttpClient> LoginAsync(string username="carlos@petjourney.com")
    {
        var client=CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect=false });
        var response=await client.PostAsJsonAsync("/auth/login",new { username,password="123456" });
        response.EnsureSuccessStatusCode(); var body=await response.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization=new("Bearer",body.GetProperty("token").GetString()); return client;
    }
    protected override void Dispose(bool disposing) { base.Dispose(disposing); if(disposing) _connection.Dispose(); }
}
[CollectionDefinition("API")]
public class ApiCollection : ICollectionFixture<ApiFixture> { }

