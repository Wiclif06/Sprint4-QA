using PetJourneyTutorApi.Security;
using Microsoft.EntityFrameworkCore;
using PetJourneyTutorApi.Models;

namespace PetJourneyTutorApi.Data;

public class AppDbContext : DbContext
{
    private readonly ICurrentUser _user;
    public AppDbContext(DbContextOptions<AppDbContext> options, ICurrentUser user) : base(options)
    {
        _user = user;
    }

    public DbSet<Tutor> Tutors => Set<Tutor>();
    public DbSet<Pet> Pets => Set<Pet>();
    public DbSet<Reminder> Reminders => Set<Reminder>();
    public DbSet<Clinic> Clinics => Set<Clinic>();
    public DbSet<Species> Species => Set<Species>();
    public DbSet<Breed> Breeds => Set<Breed>();

    public DbSet<TutorClinic> TutorClinics => Set<TutorClinic>();

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        using var activity = PetJourneyTutorApi.Observability.Telemetry.Activities.StartActivity("persistence.save");
        return await base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Breed>().ToTable("TBRACA");
        modelBuilder.Entity<Breed>().Property(x => x.IdRaca).HasColumnName("IDRACA");
        modelBuilder.Entity<Breed>().Property(x => x.NmRaca).HasColumnName("NMRACA");
        modelBuilder.Entity<Breed>().Property(x => x.IdEspecie).HasColumnName("IDESPECIE");
        modelBuilder.Entity<Clinic>().ToTable("TBCLINICA");
        modelBuilder.Entity<Clinic>().Property(x => x.NmClinica).HasColumnName("NMCLINICA");
        modelBuilder.Entity<Clinic>().Property(x => x.DsEndereco).HasColumnName("DSENDERECO");
        modelBuilder.Entity<Clinic>().Property(x => x.NrTelefone).HasColumnName("NRTELEFONE");
        modelBuilder.Entity<Clinic>().Property(x => x.DsEmail).HasColumnName("DSEMAIL");
        modelBuilder.Entity<Clinic>().Property(x => x.DsStatus).HasColumnName("DSSTATUS");
        modelBuilder.Entity<Pet>().ToTable("TBPET");
        modelBuilder.Entity<Pet>().Property(x => x.NmPet).HasColumnName("NMPET");
        modelBuilder.Entity<Pet>().Property(x => x.DtNascimento).HasColumnName("DTNASCIMENTO");
        modelBuilder.Entity<Pet>().Property(x => x.DsSexo).HasColumnName("DSSEXO");
        modelBuilder.Entity<Pet>().Property(x => x.IdTutor).HasColumnName("IDTUTOR");
        modelBuilder.Entity<Pet>().Property(x => x.IdEspecie).HasColumnName("IDESPECIE");
        modelBuilder.Entity<Pet>().Property(x => x.IdRaca).HasColumnName("IDRACA");
        modelBuilder.Entity<Pet>().Property(x => x.IdClinica).HasColumnName("IDCLINICA");
        modelBuilder.Entity<Reminder>().ToTable("TBLEMBRETE");
        modelBuilder.Entity<Reminder>().Property(x => x.IdPet).HasColumnName("IDPET");
        modelBuilder.Entity<Reminder>().Property(x => x.DsTipo).HasColumnName("DSTIPO");
        modelBuilder.Entity<Reminder>().Property(x => x.DsDescricao).HasColumnName("DSDESCRICAO");
        modelBuilder.Entity<Reminder>().Property(x => x.DtLembrete).HasColumnName("DTLEMBRETE");
        modelBuilder.Entity<Reminder>().Property(x => x.DtNotificado).HasColumnName("DTNOTIFICADO");
        modelBuilder.Entity<Reminder>().Property(x => x.DsStatus).HasColumnName("DSSTATUS");
        modelBuilder.Entity<Species>().ToTable("TBESPECIE");
        modelBuilder.Entity<Species>().Property(x => x.IdEspecie).HasColumnName("IDESPECIE");
        modelBuilder.Entity<Species>().Property(x => x.NmEspecie).HasColumnName("NMESPECIE");
        modelBuilder.Entity<Tutor>().ToTable("TBTUTOR");
        modelBuilder.Entity<Tutor>().Property(x => x.NmTutor).HasColumnName("NMTUTOR");
        modelBuilder.Entity<Tutor>().Property(x => x.DsEmail).HasColumnName("DSEMAIL");
        modelBuilder.Entity<Tutor>().Property(x => x.NrTelefone).HasColumnName("NRTELEFONE");
        modelBuilder.Entity<Tutor>().Property(x => x.DsPlano).HasColumnName("DSPLANO");
        modelBuilder.Entity<Tutor>().Property(x => x.DtCadastro).HasColumnName("DTCADASTRO");
        modelBuilder.Entity<TutorClinic>().ToTable("PJTUTORCLINICA");
        modelBuilder.Entity<TutorClinic>().Property(x => x.TutorId).HasColumnName("IDTUTOR");
        modelBuilder.Entity<TutorClinic>().Property(x => x.ClinicId).HasColumnName("IDCLINICA");
        modelBuilder.Entity<Clinic>().Property(x => x.IdClinica).HasColumnName("IDCLINICA").ValueGeneratedOnAdd();
        modelBuilder.Entity<Pet>().Property(x => x.IdPet).HasColumnName("IDPET").ValueGeneratedOnAdd();
        modelBuilder.Entity<Reminder>().Property(x => x.IdLembrete).HasColumnName("IDLEMBRETE").ValueGeneratedOnAdd();
        modelBuilder.Entity<Tutor>().Property(x => x.IdTutor).HasColumnName("IDTUTOR").ValueGeneratedOnAdd();
        modelBuilder.Entity<TutorClinic>().HasKey(x => new { x.TutorId, x.ClinicId });
        modelBuilder.Entity<TutorClinic>().HasOne<Tutor>().WithMany().HasForeignKey(x => x.TutorId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<TutorClinic>().HasOne<Clinic>().WithMany().HasForeignKey(x => x.ClinicId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Pet>().HasOne<Tutor>().WithMany().HasForeignKey(x => x.IdTutor).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Pet>().HasOne<Species>().WithMany().HasForeignKey(x => x.IdEspecie).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Pet>().HasOne<Breed>().WithMany().HasForeignKey(x => x.IdRaca).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Pet>().HasOne<Clinic>().WithMany().HasForeignKey(x => x.IdClinica).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Reminder>().HasOne<Pet>().WithMany().HasForeignKey(x => x.IdPet).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Breed>().HasOne<Species>().WithMany().HasForeignKey(x => x.IdEspecie).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Pet>().HasQueryFilter(p => _user.IsSystemAdmin ||
            (_user.Role == "TUTOR" && p.IdTutor == _user.TutorId) ||
            ((_user.Role == "ADMIN_CLINICA" || _user.Role == "VETERINARIO") && p.IdClinica == _user.ClinicId));
        modelBuilder.Entity<Tutor>().HasQueryFilter(t => _user.IsSystemAdmin ||
            (_user.Role == "TUTOR" && t.IdTutor == _user.TutorId) ||
            ((_user.Role == "ADMIN_CLINICA" || _user.Role == "VETERINARIO") && TutorClinics.Any(l => l.TutorId == t.IdTutor && l.ClinicId == _user.ClinicId)));
        modelBuilder.Entity<Reminder>().HasQueryFilter(r => Pets.Any(p => p.IdPet == r.IdPet));
        modelBuilder.Entity<Clinic>().HasQueryFilter(c => _user.IsSystemAdmin ||
            ((_user.Role == "ADMIN_CLINICA" || _user.Role == "VETERINARIO") && c.IdClinica == _user.ClinicId) ||
            (_user.Role == "TUTOR" && TutorClinics.Any(l => l.TutorId == _user.TutorId && l.ClinicId == c.IdClinica)));
        modelBuilder.Entity<Tutor>(entity =>
        {
            entity.HasKey(t => t.IdTutor);
            entity.HasIndex(t => t.DsEmail).IsUnique();
            entity.Property(t => t.DtCadastro).HasDefaultValueSql(Database.IsSqlite() ? "CURRENT_TIMESTAMP" : "SYSDATE");
            entity.Property(t => t.DsPlano).HasDefaultValue("FREE");
        });

        modelBuilder.Entity<Pet>(entity =>
        {
            entity.HasKey(p => p.IdPet);
            entity.Property(p => p.IdEspecie).IsRequired();
        });

        modelBuilder.Entity<Reminder>(entity =>
        {
            entity.HasKey(r => r.IdLembrete);
            entity.Property(r => r.DsStatus).HasDefaultValue("PENDENTE");
        });

        modelBuilder.Entity<Clinic>(entity =>
        {
            entity.HasKey(c => c.IdClinica);
        });

        modelBuilder.Entity<Species>(entity =>
        {
            entity.HasKey(e => e.IdEspecie);
        });

        modelBuilder.Entity<Breed>(entity =>
        {
            entity.HasKey(r => r.IdRaca);
        });
    }
}
