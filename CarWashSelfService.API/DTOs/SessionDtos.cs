using CarWashSelfService.API.Entities;

namespace CarWashSelfService.API.DTOs;

// Ответ с человекочитаемыми именами, а не только id-шниками
public record SessionDto(
    int Id,
    int UserId,
    string Username,
    int WashPostId,
    string WashPostName,
    int TariffId,
    string TariffName,
    DateTime StartTime,
    DateTime? EndTime,
    SessionStatus Status,
    decimal TotalAmount);

// Тело POST: клиент говорит «кто», «где» и «по какому тарифу»
public record SessionCreateDto(int UserId, int WashPostId, int TariffId);