using System.ComponentModel.DataAnnotations;
namespace PetJourneyTutorApi.Models;

public class Clinic
{
    [Key]

    public int IdClinica { get; set; }

    [Required]
    [MaxLength(200)]

    public string NmClinica { get; set; } = string.Empty;

    [MaxLength(300)]

    public string? DsEndereco { get; set; }

    [MaxLength(20)]

    public string? NrTelefone { get; set; }

    [MaxLength(150)]

    public string? DsEmail { get; set; }

    [MaxLength(20)]

    public string? DsStatus { get; set; }
}
