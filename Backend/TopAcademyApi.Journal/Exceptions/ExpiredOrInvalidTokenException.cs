namespace TopAcademyAPI.Journal.Exceptions;

public class ExpiredOrInvalidTokenException(
    Exception innerException,
    string message = "Неправильный или истекший refresh-token для обновления.")
    : Exception(message, innerException);