using System.ComponentModel.DataAnnotations;
namespace PetJourneyTutorApi.Models;

public class Breed
{
    [Key]

    public int IdRaca { get; set; }

    [Required]
    [MaxLength(100)]

    public string NmRaca { get; set; } = string.Empty;

    [Required]

    public int IdEspecie { get; set; }
}