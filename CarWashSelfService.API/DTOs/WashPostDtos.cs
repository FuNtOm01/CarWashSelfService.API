using CarWashSelfService.API.Entities;

namespace CarWashSelfService.API.DTOs;

public record WashPostDto(int Id, string Name, string? Address, PostStatus Status);

public record WashPostCreateDto(string Name, string? Address);

public record WashPostUpdateDto(string Name, string? Address, PostStatus Status);