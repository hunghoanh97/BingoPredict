using System.Text.Json;

namespace Bingo.Application.Simulation;

/// <summary>
/// Trạng thái thay đổi được của chiến lược adaptive, lưu/đọc dưới dạng JSON.
/// Mọi giá trị quy về mảng double (vô hướng = mảng độ dài 1) để đơn giản hóa.
/// </summary>
public sealed class StrategyState
{
    private readonly Dictionary<string, double[]> _data;
    public bool Dirty { get; private set; }

    private StrategyState(Dictionary<string, double[]> data) => _data = data;

    public static StrategyState FromJson(string? json)
    {
        var dict = new Dictionary<string, double[]>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(json))
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<Dictionary<string, double[]>>(json);
                if (parsed != null) dict = new(parsed, StringComparer.OrdinalIgnoreCase);
            }
            catch (JsonException) { /* state hỏng -> khởi tạo rỗng */ }
        }
        return new StrategyState(dict);
    }

    public string ToJson() => JsonSerializer.Serialize(_data);

    public double GetScalar(string key, double def) =>
        _data.TryGetValue(key, out var a) && a.Length > 0 ? a[0] : def;

    public void SetScalar(string key, double value)
    {
        _data[key] = new[] { value };
        Dirty = true;
    }

    public double[] GetArray(string key, int length, double fill)
    {
        if (_data.TryGetValue(key, out var a) && a.Length == length)
            return (double[])a.Clone();
        var arr = new double[length];
        Array.Fill(arr, fill);
        return arr;
    }

    /// <summary>Đọc mảng đã lưu với độ dài bất kỳ (cho chuỗi Labouchère), null nếu chưa có.</summary>
    public double[]? GetArrayOrNull(string key) =>
        _data.TryGetValue(key, out var a) ? (double[])a.Clone() : null;

    public void SetArray(string key, double[] value)
    {
        _data[key] = value;
        Dirty = true;
    }
}
