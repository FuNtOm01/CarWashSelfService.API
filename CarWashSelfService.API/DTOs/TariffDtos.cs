namespace CarWashSelfService.API.DTOs;

// Ответ: то, что видит клиент
public record TariffDto(int Id, string Name, string? Description,
    decimal Price, int DurationMinutes, bool IsActive);

// Тело POST: Id нет — его выдаст база
public record TariffCreateDto(string Name, string? Description,
    decimal Price, int DurationMinutes, bool IsActive = true);

// Тело PUT: всё поле целиком, частичных обновлений сегодня не делаем
public record TariffUpdateDto(string Name, string? Description,
    decimal Price, int DurationMinutes, bool IsActive);