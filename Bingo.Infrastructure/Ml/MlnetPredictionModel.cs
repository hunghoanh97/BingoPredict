using System.Globalization;
using Bingo.Application.Abstractions;
using Bingo.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Bingo.Infrastructure.Ml;

/// <summary>
/// Bọc mô hình ML.NET <see cref="PredictNumberModel"/> để dự đoán tổng kỳ kế tiếp.
/// Nếu model không nạp được, trả về 10 (tổng có xác suất cao nhất) làm fallback an toàn.
/// </summary>
public sealed class MlnetPredictionModel : IPredictionModel
{
    private readonly ILogger<MlnetPredictionModel> _logger;
    private bool _modelBroken;

    public MlnetPredictionModel(ILogger<MlnetPredictionModel> logger) => _logger = logger;

    public int PredictSum(IReadOnlyList<Draw> recentDraws)
    {
        if (recentDraws.Count == 0) return 10;
        if (_modelBroken) return Fallback(recentDraws);

        var last = recentDraws[0];
        try
        {
            var input = new PredictNumberModel.ModelInput
            {
                Draw_Time = last.DrawAt.ToString("o", CultureInfo.InvariantCulture),
                Winning_Result = float.Parse(last.WinningResult, CultureInfo.InvariantCulture),
                Sum = last.Sum
            };
            var output = PredictNumberModel.Predict(input);
            return Math.Clamp((int)Math.Round(output.Score), 3, 18);
        }
        catch (Exception ex)
        {
            _modelBroken = true; // tránh log lặp; chuyển sang fallback thống kê
            _logger.LogWarning(ex, "Không dùng được model ML.NET, chuyển sang dự đoán mean-reversion");
            return Fallback(recentDraws);
        }
    }

    /// <summary>Fallback khi không có ML: dự đoán mean-reversion (đối xứng quanh 10.5) — khác biệt với
    /// chiến lược most_likely_sum, để mỗi user không trùng cách chơi.</summary>
    private static int Fallback(IReadOnlyList<Draw> recentDraws) =>
        Math.Clamp(21 - recentDraws[0].Sum, 3, 18);
}
