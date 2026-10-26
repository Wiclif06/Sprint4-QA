using System.ComponentModel.DataAnnotations;
namespace PetJourneyTutorApi.Models;

public class Species
{
    [Key]

    public int IdEspecie { get; set; }

    [Required]
    [MaxLength(80)]

    public string NmEspecie { get; set; } = string.Empty;
}