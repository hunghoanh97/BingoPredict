using Bingo.Application.Dtos;

namespace Bingo.Application.Services;

public interface ILeaderboardService
{
    Task<IReadOnlyList<LeaderboardEntryDto>> GetLeaderboardAsync(string metric);
    Task<IReadOnlyList<SimUserDto>> GetUsersAsync();
    Task<UserDetailDto?> GetUserDetailAsync(int id);
    Task<DailySummaryDto> GetDailyAsync(DateOnly date);
    Task<IReadOnlyList<DrawDto>> GetLatestDrawsAsync(int count);
    Task<IReadOnlyList<TicketDto>> GetTicketsAsync(int? userId, DateOnly? date);
    Task<IReadOnlyList<StrategyDto>> GetStrategiesAsync();
}
