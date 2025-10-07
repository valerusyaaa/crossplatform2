namespace crossplatform2.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;

        // Добавляем метод проверки пароля 
        public bool CheckPassword(string password)
        {
            return Password == password;
        }
    }
}