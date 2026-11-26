using System.ComponentModel.DataAnnotations;

namespace CarWashSelfService.API.Entities
{
    public enum PostStatus
    {
        Free,
        Occupied,
        Maintenance
    }

    public class WashPost
    {
        [Key]
        public int Id { get; set; }

        public string Name { get; set; } = null!;     // например «Пост №1»
        public string? Address { get; set; }
        public PostStatus Status { get; set; } = PostStatus.Free;

        // ВАЖНО: сюда НЕ добавляем CurrentSessionId. Иначе между WashPost и
        // WashSession возникнет вторая связь поверх уже существующей (WashSession.WashPostId),
        // и EF либо заругается, либо построит лишнюю FK-колонку. Занятость поста
        // определяется запросом по активному сеансу, а не полем в посте.
        public ICollection<WashSession> WashSessions { get; set; } = new List<WashSession>();
    }
}
