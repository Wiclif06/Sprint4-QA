namespace PetJourneyTutorApi.Domain;
public static class CareRules
{
    public static string? ValidatePet(string name, string sex, DateTime? birthDate, DateTime today)
    {
        if (string.IsNullOrWhiteSpace(name)) return "O nome do pet é obrigatório.";
        if (sex is not ("M" or "F")) return "O sexo deve ser M ou F.";
        if (birthDate?.Date > today.Date) return "O nascimento não pode estar no futuro.";
        return null;
    }
    public static string? ValidateReminder(DateTime date, string status, DateTime today)
    {
        if (date.Date < today.Date) return "A data do lembrete não pode ser anterior à data atual.";
        if (status is not ("PENDENTE" or "CONCLUIDO" or "CANCELADO")) return "Status de lembrete inválido.";
        return null;
    }
    public static bool CanOwnPet(string role, int? tutorId, int? clinicId, int petTutorId, int? petClinicId) => role switch
    {
        "ADMIN_SISTEMA" => true,
        "TUTOR" => tutorId.HasValue && tutorId == petTutorId,
        "ADMIN_CLINICA" => clinicId.HasValue && clinicId == petClinicId,
        _ => false
    };
}
