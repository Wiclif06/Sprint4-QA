using System.ComponentModel.DataAnnotations;
namespace PetJourneyTutorApi.Models;

public class Pet
{
    [Key]

    public int IdPet { get; set; }

    [Required(ErrorMessage = "O nome do pet é obrigatório.")]
    [MaxLength(100)]

    public string NmPet { get; set; } = string.Empty;

    public DateTime? DtNascimento { get; set; }

    [Required(ErrorMessage = "O sexo do pet é obrigatório.")]
    [MaxLength(1)]

    public string DsSexo { get; set; } = string.Empty;

    [Required(ErrorMessage = "O tutor do pet é obrigatório.")]

    public int IdTutor { get; set; }

    [Required(ErrorMessage = "A espécie do pet é obrigatória.")]

    public int IdEspecie { get; set; }

    public int? IdRaca { get; set; }

    public int? IdClinica { get; set; }
}