using Bingo.Domain.Entities;

namespace Bingo.Application.Abstractions;

/// <summary>Cổng tới mô hình dự đoán (ML.NET) — trả về tổng dự đoán cho kỳ kế tiếp.</summary>
public interface IPredictionModel
{
    /// <summary>Dự đoán tổng (3-18) cho kỳ kế tiếp dựa trên các kỳ gần đây (mới nhất ở đầu).</summary>
    int PredictSum(IReadOnlyList<Draw> recentDraws);
}
