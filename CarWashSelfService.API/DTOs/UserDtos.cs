namespace CarWashSelfService.API.DTOs;

// В ответ НЕ отдаём PasswordHash — только безопасные поля
public record UserDto(int Id, string Username, string Email,
    string? PhoneNumber, decimal Balance, DateTime CreatedAt);

public record UserCreateDto(string Username, string Email,
    string? PhoneNumber, string Password);

public record UserUpdateDto(string Username, string Email, string? PhoneNumber);