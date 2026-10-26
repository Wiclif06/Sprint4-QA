using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PetJourneyTutorApi.Models;
using PetJourneyTutorApi.Security;
namespace PetJourneyTutorApi.Data;
public static class DemoData
{
    public static void Configure(WebApplicationBuilder builder)
    {
        if (!builder.Configuration.GetValue<bool>("Demo:Enabled")) return;
        if (!builder.Environment.IsDevelopment() || builder.Configuration["Database:Provider"] != "Sqlite")
            throw new InvalidOperationException("Demo exige Development e Database:Provider=Sqlite.");
        var values=new Dictionary<string,string?>();
        if (string.IsNullOrWhiteSpace(builder.Configuration["Jwt:SigningKey"])) values["Jwt:SigningKey"]=Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        var accounts=new[] {
            new Account { Username="admin.sistema@petjourney.com",Role="ADMIN_SISTEMA" },
            new Account { Username="admin.petfeliz@petjourney.com",Role="ADMIN_CLINICA",ClinicId=1 },
            new Account { Username="admin.saudeanimal@petjourney.com",Role="ADMIN_CLINICA",ClinicId=2 },
            new Account { Username="dr.joao@petjourney.com",Role="VETERINARIO",ClinicId=1,VeterinarianId=1 },
            new Account { Username="carlos@petjourney.com",Role="TUTOR",TutorId=1 },
            new Account { Username="ana@petjourney.com",Role="TUTOR",TutorId=2 }
        };
        for(var i=0;i<accounts.Length;i++) {
            var a=accounts[i];var prefix=$"Accounts:{i}:";
            values[prefix+"Username"]=a.Username;values[prefix+"Role"]=a.Role;
            values[prefix+"PasswordHash"]=new PasswordHasher<Account>().HashPassword(a,"123456");
            values[prefix+"ClinicId"]=a.ClinicId?.ToString();values[prefix+"TutorId"]=a.TutorId?.ToString();values[prefix+"VeterinarianId"]=a.VeterinarianId?.ToString();
        }
        builder.Configuration.AddInMemoryCollection(values);
    }
    public static async Task SeedAsync(WebApplication app)
    {
        if (!app.Configuration.GetValue<bool>("Demo:Enabled")) return;
        using var scope=app.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<AppDbContext>();
        // Only a newly created local database receives sample records.
        if (!await db.Database.EnsureCreatedAsync()) return;
        db.Species.AddRange(new Species { IdEspecie=1,NmEspecie="Cachorro" },new Species { IdEspecie=2,NmEspecie="Gato" });
        db.Breeds.Add(new Breed { IdRaca=1,IdEspecie=1,NmRaca="Golden Retriever" });
        db.Clinics.AddRange(new Clinic { IdClinica=1,NmClinica="Pet Feliz" },new Clinic { IdClinica=2,NmClinica="Saúde Animal" });
        db.Tutors.AddRange(new Tutor { IdTutor=1,NmTutor="Carlos",DsEmail="carlos@petjourney.com",NrTelefone="11911112222" },new Tutor { IdTutor=2,NmTutor="Ana",DsEmail="ana@petjourney.com",NrTelefone="11933334444" });
        db.TutorClinics.AddRange(new TutorClinic { TutorId=1,ClinicId=1 },new TutorClinic { TutorId=2,ClinicId=2 });
        db.Pets.AddRange(new Pet { IdPet=1,NmPet="Rex",DsSexo="M",IdTutor=1,IdClinica=1,IdEspecie=1,IdRaca=1 },new Pet { IdPet=2,NmPet="Mia",DsSexo="F",IdTutor=2,IdClinica=2,IdEspecie=2 });
        db.Reminders.AddRange(new Reminder { IdPet=1,DsTipo="VACINA",DsDescricao="Vacinação do Rex",DtLembrete=DateTime.UtcNow.Date.AddDays(2) },new Reminder { IdPet=2,DsTipo="RETORNO",DsDescricao="Retorno da Mia",DtLembrete=DateTime.UtcNow.Date.AddDays(3) });
        await db.SaveChangesAsync();
    }
}
