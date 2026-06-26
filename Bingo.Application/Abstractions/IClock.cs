namespace Bingo.Application.Abstractions;

/// <summary>Đồng hồ trừu tượng để dễ kiểm thử và tái lập.</summary>
public interface IClock
{
    DateTime UtcNow { get; }
}
