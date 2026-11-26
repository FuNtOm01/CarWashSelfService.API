namespace CarWashSelfService.API.DTOs;

public record OverviewDto(int Users, int WashPosts, int FreePosts,
    int ActiveSessions, int CompletedSessions, int SucceededPayments, decimal Revenue);

public record RevenueDayDto(DateTime Date, decimal Revenue);