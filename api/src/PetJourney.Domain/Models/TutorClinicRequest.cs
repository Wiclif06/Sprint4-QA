using System.ComponentModel.DataAnnotations;

namespace PetJourneyTutorApi.Models;

public class TutorClinicRequest
{
    [Required]
    public int IdClinica { get; set; }
}
