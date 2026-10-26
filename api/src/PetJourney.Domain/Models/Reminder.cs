using System.ComponentModel.DataAnnotations;
namespace PetJourneyTutorApi.Models;

public class Reminder
{
    [Key]

    public int IdLembrete { get; set; }

    [Required(ErrorMessage = "O pet do lembrete é obrigatório.")]

    public int IdPet { get; set; }

    [Required(ErrorMessage = "O tipo do lembrete é obrigatório.")]
    [MaxLength(100)]

    public string DsTipo { get; set; } = string.Empty;

    [Required(ErrorMessage = "A descrição do lembrete é obrigatória.")]
    [MaxLength(300)]

    public string DsDescricao { get; set; } = string.Empty;

    [Required(ErrorMessage = "A data do lembrete é obrigatória.")]

    public DateTime DtLembrete { get; set; }

    public DateTime? DtNotificado { get; set; }

    [Required(ErrorMessage = "O status do lembrete é obrigatório.")]
    [MaxLength(20)]

    public string DsStatus { get; set; } = "PENDENTE";
}
