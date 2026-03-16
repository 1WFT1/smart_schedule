namespace TopAcademyAPI.Journal.Exceptions;

public class InvalidTokenException(
    Exception innerException,
    string message = "Неправильный токен.")
    : Exception(message, innerException);