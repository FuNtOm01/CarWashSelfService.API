using System.ComponentModel.DataAnnotations;

namespace CarWashSelfService.API.Entities
{
    public enum PaymentStatus
    {
        Pending,
        Succeeded,
        Failed,
        Refunded
    }

    public enum PaymentMethod
    {
        Cash,
        Card,
        AppBalance     // списание с User.Balance
    }

    public class PaymentTransaction
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }
        public User? User { get; set; }

        public int WashSessionId { get; set; }
        public WashSession? WashSession { get; set; }

        public decimal Amount { get; set; }
        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
        public PaymentMethod Method { get; set; }

        // Внешний идентификатор транзакции от платёжного шлюза (для идемпотентности
        // и сверки вебхуков). Nullable — пока платежа нет, его нет и снаружи.
        public string? ExternalTransactionId { get; set; }

        public DateTime? PaidAt { get; set; }         // null, пока не оплачен
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
