using Bingo.Domain.Entities;
using Bingo.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Bingo.Infrastructure.Persistence;

/// <summary>Nạp dữ liệu khởi tạo: danh mục chiến lược, bảng trả thưởng, và các user (mỗi user 1 chiến lược riêng).</summary>
public static class DataSeeder
{
    public sealed record StrategySeed(string Key, string Name, string Description, bool IsAdaptive, string? Params);

    // 18 chiến lược, mỗi cái một cách chơi khác nhau hướng tới tăng balance.
    public static readonly IReadOnlyList<StrategySeed> Strategies = new[]
    {
        new StrategySeed("always_tai", "Luôn Tài", "Mỗi kỳ cược Lớn (12-18) 10.000đ.", false, null),
        new StrategySeed("always_xiu", "Luôn Xỉu", "Mỗi kỳ cược Nhỏ (3-9) 10.000đ.", false, null),
        new StrategySeed("always_hoa", "Luôn Hòa", "Mỗi kỳ cược Hòa (10-11), trả thưởng cao hơn Tài/Xỉu.", false, null),
        new StrategySeed("most_likely_sum", "Tổng dễ ra (10 & 11)", "Cược 2 tổng xác suất cao nhất là 10 và 11.", false, null),
        new StrategySeed("hot_sum", "Tổng nóng", "Cược tổng xuất hiện nhiều nhất trong cửa sổ gần đây.", false, "{\"window\":200}"),
        new StrategySeed("cold_sum", "Tổng nguội (đến hẹn)", "Cược tổng lâu chưa xuất hiện nhất.", false, "{\"window\":500}"),
        new StrategySeed("frequency_weighted", "Theo tần suất", "Rải vé vào các tổng tần suất cao nhất.", false, "{\"topK\":5,\"window\":300}"),
        new StrategySeed("ev_max", "Săn EV (jackpot)", "Cược 2 tổng có kỳ vọng cao nhất (3 và 18).", false, null),
        new StrategySeed("hedge_jackpot", "Hedge jackpot", "Cược Lớn + tổng 3 + tổng 18 mỗi kỳ.", false, null),
        new StrategySeed("streak_follow", "Theo cầu", "Cược cùng khoảng (Tài/Xỉu/Hòa) với kỳ trước.", false, null),
        new StrategySeed("streak_reverse", "Bẻ cầu", "Cược ngược khoảng của kỳ trước.", false, null),
        new StrategySeed("number_hunter", "Săn số nóng", "Cách chơi Cơ bản: cược digit nóng nhất.", false, "{\"window\":200}"),
        new StrategySeed("ml_predict", "Dự đoán ML", "Dùng mô hình ML.NET dự đoán tổng kế tiếp.", false, null),
        new StrategySeed("random", "Ngẫu nhiên", "Mốc so sánh: cược ngẫu nhiên.", false, null),
        new StrategySeed("martingale_size", "Martingale (gấp đôi khi thua)", "Cược Tài, thua thì gấp đôi mức cược, thắng reset.", true, "{\"side\":\"Lon\"}"),
        new StrategySeed("paroli_size", "Paroli (gấp đôi khi thắng)", "Cược Tài, thắng thì gấp đôi mức cược, thua reset.", true, "{\"side\":\"Lon\"}"),
        new StrategySeed("markov_sum", "Markov", "Học ma trận chuyển tiếp tổng→tổng, cược tổng khả dĩ nhất.", true, null),
        new StrategySeed("ewma_adaptive", "EWMA thích nghi", "Trọng số mũ theo tổng, tự cập nhật mỗi kỳ.", true, "{\"alpha\":0.05}")
    };

    private static readonly Dictionary<int, decimal> SumMultipliers = new()
    {
        [3] = 120m, [4] = 40m, [5] = 20m, [6] = 12m, [7] = 8m, [8] = 5.5m, [9] = 4.7m, [10] = 4.4m,
        [11] = 4.4m, [12] = 4.7m, [13] = 5.5m, [14] = 8m, [15] = 12m, [16] = 20m, [17] = 40m, [18] = 120m
    };

    public static async Task SeedAsync(BingoDbContext ctx, CancellationToken ct = default)
    {
        if (!await ctx.Strategies.AnyAsync(ct))
        {
            ctx.Strategies.AddRange(Strategies.Select(s => new Strategy
            {
                Key = s.Key, Name = s.Name, Description = s.Description,
                IsAdaptive = s.IsAdaptive, DefaultParamsJson = s.Params, Enabled = true
            }));
            await ctx.SaveChangesAsync(ct);
        }

        if (!await ctx.PrizeRules.AnyAsync(ct))
        {
            var rules = new List<PrizeRule>();
            foreach (var (sum, mult) in SumMultipliers)
                rules.Add(new PrizeRule { BetKind = BetKind.Sum, BetValue = sum.ToString(), Multiplier = mult, Description = $"Cộng tổng = {sum}" });

            rules.Add(new PrizeRule { BetKind = BetKind.Size, BetValue = nameof(SizeResult.Nho), Multiplier = 1.5m, Description = "Nhỏ (3-9)" });
            rules.Add(new PrizeRule { BetKind = BetKind.Size, BetValue = nameof(SizeResult.Lon), Multiplier = 1.5m, Description = "Lớn (12-18)" });
            rules.Add(new PrizeRule { BetKind = BetKind.Size, BetValue = nameof(SizeResult.Hoa), Multiplier = 2.25m, Description = "Hòa (10-11)" });

            rules.Add(new PrizeRule { BetKind = BetKind.NumberCount, BetValue = "1", Multiplier = 1.2m, Description = "Digit xuất hiện 1 lần" });
            rules.Add(new PrizeRule { BetKind = BetKind.NumberCount, BetValue = "2", Multiplier = 2m, Description = "Digit xuất hiện 2 lần" });
            rules.Add(new PrizeRule { BetKind = BetKind.NumberCount, BetValue = "3", Multiplier = 3m, Description = "Digit xuất hiện 3 lần" });

            rules.Add(new PrizeRule { BetKind = BetKind.Triple, BetValue = "specific", Multiplier = 120m, Description = "Ba số trùng cụ thể" });
            rules.Add(new PrizeRule { BetKind = BetKind.Triple, BetValue = "any", Multiplier = 20m, Description = "Ba số trùng bất kỳ" });

            ctx.PrizeRules.AddRange(rules);
            await ctx.SaveChangesAsync(ct);
        }

        if (!await ctx.SimUsers.AnyAsync(ct))
        {
            var now = DateTime.UtcNow;
            ctx.SimUsers.AddRange(Strategies.Select((s, i) => new SimUser
            {
                Name = $"Bot {i + 1:00} - {s.Name}",
                StrategyKey = s.Key,
                ConfigJson = null,
                Enabled = true,
                CreatedAt = now
            }));
            await ctx.SaveChangesAsync(ct);
        }
    }
}
