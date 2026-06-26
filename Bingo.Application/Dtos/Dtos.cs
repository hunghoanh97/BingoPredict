namespace Bingo.Application.Dtos;

public sealed record UserStatDto(
    int TotalTickets,
    int TotalWins,
    decimal WinRate,
    decimal TotalStaked,
    decimal TotalPayout,
    decimal NetProfit,
    decimal Roi,
    int DaysPlayed,
    int DaysBusted);

public sealed record LeaderboardEntryDto(
    int SimUserId,
    string Name,
    string StrategyKey,
    string StrategyName,
    int TotalTickets,
    int TotalWins,
    decimal WinRate,
    decimal TotalStaked,
    decimal TotalPayout,
    decimal NetProfit,
    decimal Roi,
    int DaysPlayed,
    int DaysBusted,
    decimal? CurrentBalanceToday,
    bool IsBustedToday);

public sealed record SimUserDto(
    int Id,
    string Name,
    string StrategyKey,
    string StrategyName,
    bool Enabled,
    UserStatDto Stat);

public sealed record DailyAccountDto(
    DateOnly GameDate,
    decimal StartingBalance,
    decimal CurrentBalance,
    int TicketsBought,
    decimal TotalStaked,
    decimal TotalPayout,
    int Wins,
    int Losses,
    decimal NetProfit,
    decimal Roi,
    bool IsBusted);

public sealed record DailyAccountWithUserDto(
    int SimUserId,
    string UserName,
    string StrategyKey,
    DateOnly GameDate,
    decimal StartingBalance,
    decimal CurrentBalance,
    int TicketsBought,
    decimal TotalStaked,
    decimal TotalPayout,
    int Wins,
    int Losses,
    decimal NetProfit,
    decimal Roi,
    bool IsBusted);

public sealed record UserDetailDto(
    int Id,
    string Name,
    string StrategyKey,
    string StrategyName,
    string Description,
    object? Config,
    UserStatDto Stat,
    IReadOnlyList<DailyAccountDto> Daily);

public sealed record DailyTotalsDto(
    decimal TotalStaked,
    decimal TotalPayout,
    decimal NetProfit,
    int BustedCount);

public sealed record DailySummaryDto(
    DateOnly Date,
    IReadOnlyList<DailyAccountWithUserDto> PerUser,
    DailyTotalsDto Totals);

public sealed record DrawDto(
    long Id,
    DateTime DrawAt,
    string WinningResult,
    int D1,
    int D2,
    int D3,
    int Sum,
    string Size,
    bool IsTriple);

public sealed record TicketDto(
    long Id,
    int SimUserId,
    string UserName,
    DateTime TargetDrawAt,
    long? DrawId,
    string BetKind,
    string BetValue,
    decimal Stake,
    decimal Multiplier,
    bool IsSettled,
    bool IsWin,
    decimal Payout,
    decimal Profit);

public sealed record StrategyDto(
    string Key,
    string Name,
    string Description,
    bool IsAdaptive,
    bool Enabled);

public sealed record OperationResultDto(string Message);

/// <summary>Kết quả backtest của một chiến lược với cấu hình tốt nhất (công cụ săn lợi nhuận).</summary>
public sealed record StrategyDiscoveryDto(
    string Key,
    string Name,
    string BestConfig,
    int Tickets,
    decimal WinRate,
    decimal TotalStaked,
    decimal TotalPayout,
    decimal NetProfit,
    decimal Roi);
