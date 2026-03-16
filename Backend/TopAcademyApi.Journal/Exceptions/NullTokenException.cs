namespace TopAcademyAPI.Journal.Exceptions;

public class NullTokenException(
    string message = "Токен не указан.")
    : Exception(message);
    
    