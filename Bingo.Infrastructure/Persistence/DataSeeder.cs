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
        new StrategySeed("ewma_adaptive", "EWMA thích nghi", "Trọng số mũ theo tổng, tự cập nhật mỗi kỳ.", true, "{\"alpha\":0.05}"),
        new StrategySeed("sparse_tai", "Cược Tài cách kỳ", "Chỉ cược Lớn mỗi N kỳ (mặc định 3), bỏ qua các kỳ còn lại.", false, "{\"everyN\":3}"),
        new StrategySeed("streak_break", "Bẻ cầu chọn lọc", "Chỉ cược khi có chuỗi 3 kỳ cùng khoảng rồi cược ngược; còn lại bỏ qua.", false, "{\"streak\":3}"),
        new StrategySeed("martingale_mid", "Martingale 10/11", "Cược tổng 10&11, gấp đôi tiền cược mỗi lần thua, reset khi thắng (bắt đầu 10.000).", true, null),
        new StrategySeed("smart_mix", "Mix quản lý vốn", "Cược cửa EV tốt nhất (Hòa) + chọn lọc + chốt lời/dừng lỗ theo ngày để giảm lỗ.", false, "{\"target\":\"Hoa\",\"takeProfit\":200000,\"stopLoss\":300000}"),
        new StrategySeed("fibonacci", "Fibonacci", "Thua tiến 1 bước Fibonacci, thắng lùi 2 bước (cược Tài). Reset mỗi ngày.", true, null),
        new StrategySeed("dalembert", "D'Alembert", "Thua +1 đơn vị, thắng −1 đơn vị (cược Tài, biến động thấp).", true, null),
        new StrategySeed("system_1326", "Hệ 1-3-2-6", "Tiến trình thuận khi thắng 1-3-2-6 (cược Tài).", true, null),
        new StrategySeed("labouchere", "Labouchère", "Cancellation: cược đầu+cuối chuỗi, thắng xóa 2 đầu, thua nối thêm (cược Tài).", true, null),
        new StrategySeed("ensemble_vote", "Mix bỏ phiếu", "Tổng hợp 4 tín hiệu, chỉ cược khi đồng thuận ≥3/4, còn lại bỏ qua.", false, "{\"window\":100,\"threshold\":3}"),
        new StrategySeed("kelly_frac", "Kelly phân số", "Cược 2% số dư vào cửa Hòa; thua thì cược tự co lại.", false, "{\"fraction\":0.02,\"target\":\"Hoa\"}")
    };

    private static readonly Dictionary<int, decimal> SumMultipliers = new()
    {
        [3] = 120m, [4] = 40m, [5] = 20m, [6] = 12m, [7] = 8m, [8] = 5.5m, [9] = 4.7m, [10] = 4.4m,
        [11] = 4.4m, [12] = 4.7m, [13] = 5.5m, [14] = 8m, [15] = 12m, [16] = 20m, [17] = 40m, [18] = 120m
    };

    public sealed record VariantUserSeed(string Name, string StrategyKey, string ConfigJson);

    // Các biến thể cấu hình để theo dõi song song trên leaderboard (cùng chiến lược, config khác nhau).
    public static readonly IReadOnlyList<VariantUserSeed> VariantUsers = new[]
    {
        new VariantUserSeed("Var - Mart Tài (thắng nghỉ)", "martingale_size", "{\"stopOnWin\":true}"),
        new VariantUserSeed("Var - Mart Tài (trần 1.28tr)", "martingale_size", "{\"maxStake\":1280000}"),
        new VariantUserSeed("Var - Mart 11 (thắng nghỉ)", "martingale_mid", "{\"single\":true,\"stopOnWin\":true}"),
        new VariantUserSeed("Var - Mart 11 (trần 320k)", "martingale_mid", "{\"single\":true,\"maxStake\":320000}"),
        new VariantUserSeed("Var - Mart 10&11 (cách 3 kỳ)", "martingale_mid", "{\"everyN\":3}"),
        new VariantUserSeed("Var - Mart 11 (cách 2, thắng nghỉ)", "martingale_mid", "{\"single\":true,\"everyN\":2,\"stopOnWin\":true}"),
        new VariantUserSeed("Var - Sparse Tài (everyN 20)", "sparse_tai", "{\"everyN\":20}"),
        new VariantUserSeed("Var - Bẻ cầu (streak 6)", "streak_break", "{\"streak\":6}"),
        new VariantUserSeed("Mix - Hòa + TP/SL", "smart_mix", "{\"target\":\"Hoa\",\"takeProfit\":200000,\"stopLoss\":300000}"),
        new VariantUserSeed("Mix - Hòa cách 3 + TP/SL", "smart_mix", "{\"target\":\"Hoa\",\"everyN\":3,\"takeProfit\":150000,\"stopLoss\":200000}"),
        new VariantUserSeed("Mix - 10&11 + TP/SL", "smart_mix", "{\"target\":\"mid\",\"takeProfit\":300000,\"stopLoss\":400000}"),
        new VariantUserSeed("Mix - Hòa TP nhỏ/SL chặt", "smart_mix", "{\"target\":\"Hoa\",\"takeProfit\":100000,\"stopLoss\":150000}")
    };

    public static async Task SeedAsync(BingoDbContext ctx, CancellationToken ct = default)
    {
        // Idempotent: thêm các chiến lược còn thiếu (cho phép bổ sung chiến lược mới mà không cần reset).
        var existingKeys = await ctx.Strategies.Select(s => s.Key).ToListAsync(ct);
        var missing = Strategies.Where(s => !existingKeys.Contains(s.Key)).ToList();
        if (missing.Count > 0)
        {
            ctx.Strategies.AddRange(missing.Select(s => new Strategy
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

        // Idempotent: tạo user cho mỗi chiến lược chưa có user (mỗi user một chiến lược riêng).
        var usedKeys = await ctx.SimUsers.Select(u => u.StrategyKey).ToListAsync(ct);
        var newUserStrategies = Strategies.Where(s => !usedKeys.Contains(s.Key)).ToList();
        if (newUserStrategies.Count > 0)
        {
            var now = DateTime.UtcNow;
            var startIndex = usedKeys.Count;
            ctx.SimUsers.AddRange(newUserStrategies.Select((s, i) => new SimUser
            {
                Name = $"Bot {startIndex + i + 1:00} - {s.Name}",
                StrategyKey = s.Key,
                ConfigJson = null,
                Enabled = true,
                CreatedAt = now
            }));
            await ctx.SaveChangesAsync(ct);
        }

        // Idempotent: thêm các user biến thể (theo Name) để theo dõi song song.
        var existingNames = await ctx.SimUsers.Select(u => u.Name).ToListAsync(ct);
        var newVariants = VariantUsers.Where(v => !existingNames.Contains(v.Name)).ToList();
        if (newVariants.Count > 0)
        {
            var now2 = DateTime.UtcNow;
            ctx.SimUsers.AddRange(newVariants.Select(v => new SimUser
            {
                Name = v.Name,
                StrategyKey = v.StrategyKey,
                ConfigJson = v.ConfigJson,
                Enabled = true,
                CreatedAt = now2
            }));
            await ctx.SaveChangesAsync(ct);
        }
    }
}
