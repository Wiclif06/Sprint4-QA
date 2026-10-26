using System.ComponentModel.DataAnnotations;
namespace PetJourneyTutorApi.Models;

public class Tutor
{
    [Key]

    public int IdTutor { get; set; }

    [Required(ErrorMessage = "O nome do tutor é obrigatório.")]
    [MaxLength(200)]

    public string NmTutor { get; set; } = string.Empty;

    [Required(ErrorMessage = "O e-mail é obrigatório.")]
    [EmailAddress(ErrorMessage = "E-mail inválido.")]
    [MaxLength(150)]

    public string DsEmail { get; set; } = string.Empty;

    [Required(ErrorMessage = "O telefone é obrigatório.")]
    [MaxLength(20)]

    public string NrTelefone { get; set; } = string.Empty;

    [MaxLength(20)]

    public string? DsPlano { get; set; } = "FREE";

    public DateTime DtCadastro { get; set; } = DateTime.Now;
}
