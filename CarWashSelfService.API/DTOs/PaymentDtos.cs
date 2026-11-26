using CarWashSelfService.API.Entities;

namespace CarWashSelfService.API.DTOs;

public record PaymentDto(
    int Id,
    int UserId,
    string Username,
    int SessionId,
    decimal Amount,
    PaymentStatus Status,
    PaymentMethod Method,
    string? ExternalTransactionId,
    DateTime? PaidAt,
    DateTime CreatedAt);

// Amount опционален: если null — возьмём TotalAmount сеанса
public record PaymentCreateDto(int SessionId, PaymentMethod Method,
    decimal? Amount = null, string? ExternalTransactionId = null);