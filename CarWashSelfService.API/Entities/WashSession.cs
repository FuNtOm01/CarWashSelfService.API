using System.ComponentModel.DataAnnotations;

namespace CarWashSelfService.API.Entities
{
    public enum SessionStatus
    {
        Pending,     // создан, но ещё не начался / ждёт оплату
        Active,      // идёт мойка
        Completed,
        Cancelled
    }

    public class WashSession
    {
        [Key]
        public int Id { get; set; }

        // Обязательные связи: сеанс всегда привязан к клиенту, посту и тарифу.
        public int UserId { get; set; }
        public User? User { get; set; }

        public int WashPostId { get; set; }
        public WashPost? WashPost { get; set; }

        public int TariffId { get; set; }
        public Tariff? Tariff { get; set; }

        public DateTime StartTime { get; set; } = DateTime.UtcNow;
        public DateTime? EndTime { get; set; }        // null, пока сеанс не завершён
        public SessionStatus Status { get; set; } = SessionStatus.Pending;

        public decimal TotalAmount { get; set; }      // итоговая стоимость сеанса

        // У одного сеанса может быть несколько платежей (предоплата + доплата),
        // поэтому коллекция, а не одно свойство.
        public ICollection<PaymentTransaction> PaymentTransactions { get; set; } = new List<PaymentTransaction>();
    }
}
