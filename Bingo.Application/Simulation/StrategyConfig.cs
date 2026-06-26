using System.Text.Json;

namespace Bingo.Application.Simulation;

/// <summary>
/// Tham số chiến lược đọc từ JSON (gộp DefaultParamsJson của Strategy + ConfigJson của user).
/// </summary>
public sealed class StrategyConfig
{
    private readonly Dictionary<string, JsonElement> _values;

    private StrategyConfig(Dictionary<string, JsonElement> values) => _values = values;

    public static readonly StrategyConfig Empty = new(new());

    public static StrategyConfig Parse(string? defaultJson, string? overrideJson)
    {
        var dict = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
        Merge(dict, defaultJson);
        Merge(dict, overrideJson);
        return new StrategyConfig(dict);
    }

    private static void Merge(Dictionary<string, JsonElement> dict, string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return;
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Object) return;
            foreach (var prop in doc.RootElement.EnumerateObject())
                dict[prop.Name] = prop.Value.Clone();
        }
        catch (JsonException) { /* config lỗi -> bỏ qua, dùng default */ }
    }

    public int GetInt(string key, int def) =>
        _values.TryGetValue(key, out var v) && v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out var i) ? i : def;

    public double GetDouble(string key, double def) =>
        _values.TryGetValue(key, out var v) && v.ValueKind == JsonValueKind.Number && v.TryGetDouble(out var d) ? d : def;

    public string GetString(string key, string def) =>
        _values.TryGetValue(key, out var v) && v.ValueKind == JsonValueKind.String ? (v.GetString() ?? def) : def;

    public bool GetBool(string key, bool def) =>
        _values.TryGetValue(key, out var v) && (v.ValueKind == JsonValueKind.True || v.ValueKind == JsonValueKind.False) ? v.GetBoolean() : def;
}
