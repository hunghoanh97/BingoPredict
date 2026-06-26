namespace Bingo.Application.Simulation;

public interface IStrategyRegistry
{
    IBettingStrategy? Get(string key);
    IReadOnlyCollection<IBettingStrategy> All { get; }
}

public sealed class StrategyRegistry : IStrategyRegistry
{
    private readonly Dictionary<string, IBettingStrategy> _byKey;

    public StrategyRegistry(IEnumerable<IBettingStrategy> strategies)
    {
        _byKey = strategies.ToDictionary(s => s.Key, StringComparer.OrdinalIgnoreCase);
    }

    public IBettingStrategy? Get(string key) =>
        _byKey.TryGetValue(key, out var s) ? s : null;

    public IReadOnlyCollection<IBettingStrategy> All => _byKey.Values;
}
