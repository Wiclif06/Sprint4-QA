namespace PetJourneyTutorApi.Errors;
public sealed class AccessDeniedException() : Exception("Acesso negado.");
public sealed class RequestValidationException(string message) : Exception(message);
public sealed class DataConflictException(string message, Exception? inner = null) : Exception(message, inner);
public sealed class DependencyUnavailableException(string message, Exception? inner = null) : Exception(message, inner);
