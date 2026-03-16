namespace TopAcademyAPI.Journal.Exceptions;

public class InvalidLoginOrPasswordException(
    Exception innerException,
    string message = "Неправильный логин или пароль.")
    : Exception(message, innerException);