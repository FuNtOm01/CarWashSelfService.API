using System.ComponentModel.DataAnnotations;

namespace CarWashSelfService.API.Entities
{
    public enum UserRole
    {
        Client = 0,
        Admin = 1
    }

    public class User
    {
        [Key]
        public int Id { get; set; }

        public string Username { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? PhoneNumber { get; set; }

        // Хэш пароля, а не сам пароль: у вас в Program.cs есть UseAuthorization,
        // значит планируется вход — хранить нужно именно хэш.
        public string PasswordHash { get; set; } = null!;

        // Баланс внутреннего кошелька для самообслуживания.
        public decimal Balance { get; set; }

        // Роль для авторизации: по умолчанию все новые — клиенты
        public UserRole Role { get; set; } = UserRole.Client;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Навигации «один-ко-многим» (обратная сторона связей из Session/Payment).
        public ICollection<WashSession> WashSessions { get; set; } = new List<WashSession>();
        public ICollection<PaymentTransaction> PaymentTransactions { get; set; } = new List<PaymentTransaction>();
    }
}
