

namespace Backend.API.Interfaces
{
    public interface IPasswordHasher
    {
        string Hash(string password);
        bool Verify(string password, string hash);
        string Decrypt(string hash); // Для расшифровки пароля журнала
    }

    public class BCryptPasswordHasher : IPasswordHasher
    {
        public string Hash(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        public bool Verify(string password, string hash)
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }

        public string Decrypt(string hash)
        {
            // BCrypt не умеет расшифровывать, поэтому нужно другое решение
            // Для простоты используем обратимое шифрование
            throw new NotImplementedException("Для обратимого шифрования используйте AES");
        }
    }
}
