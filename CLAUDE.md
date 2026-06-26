# CLAUDE.md

Tài liệu hướng dẫn cho Claude Code khi làm việc với repository này.

## Tổng quan

**Bingo18 Simulation** — hệ thống mô phỏng & backtest đa chiến lược cho game xổ số nhanh
**Bingo18** (Vietlott). Lấy kết quả quay thật từ API ngoài, cho **~18 user bot — mỗi user một
chiến lược cược riêng** — đặt cược theo từng kỳ, theo dõi vốn/lời lỗ theo ngày, và **tối ưu để
tối đa hóa lợi nhuận**.

Backend .NET 8 theo **Clean Architecture**; dữ liệu PostgreSQL; frontend là project ReactJS
riêng (`Bingo.Dashboard`).

### Luật chơi Bingo18 (đã nghiên cứu — Vietlott)
- Mỗi 6 phút quay 3 số, mỗi số 1-6 (có thể trùng) → `winningResult` = "236". Tổng 3-18.
- Khoảng: **Nhỏ 3-9, Hòa 10-11, Lớn 12-18**. Đơn vị cược 10.000đ; trúng = stake × hệ số.
- Bảng hệ số ("Cộng tổng", Tài/Xỉu/Hòa, Cơ bản, Triple) seed trong bảng `prize_rules`
  (xem `Bingo.Infrastructure/Persistence/DataSeeder.cs`).

### ⚠️ Sự thật quan trọng về lợi nhuận
Game quay ngẫu nhiên công bằng → **EV mọi kiểu cược ≈ −40..−44%** (nhà cái ăn). Backtest trên
toàn bộ dữ liệu thật xác nhận **không chiến lược nào có ROI dương dài hạn** (tốt nhất ~ −33%).
→ Giá trị hệ thống là **framework so sánh/tối ưu**: tìm chiến lược ít lỗ nhất, và phát hiện
các **ngày** có lợi nhuận dương (do variance/jackpot). Không hứa hẹn EV dương. Mục tiêu tối ưu
mặc định = **lợi nhuận/ROI** (không phải tần suất thắng).

## Cấu trúc giải pháp (`Bingo.sln`) — Clean Architecture

Phụ thuộc hướng vào trong: `ApiService → Infrastructure → Application → Domain`.

- **`Bingo.Domain`** — lõi, không phụ thuộc gì. Entities (`Draw`, `SimUser`, `Strategy`,
  `DailyAccount`, `Ticket`, `PrizeRule`, `UserStat`, `UserStrategyState`), enums (`BetKind`,
  `SizeResult`), luật chơi (`BingoRules`, `PayoutCalculator`), hằng số (`GameConstants`).
- **`Bingo.Application`** — interface (`IUnitOfWork`, `I*Repository`, `IBingoDataClient`,
  `IPredictionModel`, `IClock`), DTOs, **engine chiến lược** (`Simulation/`), **18 chiến lược**
  (`Simulation/Strategies/`), use-case services (`Services/`: `SimulationService`,
  `LeaderboardService`, `DrawIngestionService`, `TunerService`). `BacktestRunner` chạy backtest
  thuần (không ghi DB) cho tuner.
- **`Bingo.Infrastructure`** — EF Core `BingoDbContext` + cấu hình (`Persistence/`), repository
  impl + `UnitOfWork`, `DataSeeder`, EF `Migrations/`, client API (`External/BingoDataClient`),
  ML (`Ml/MlnetPredictionModel` + `PredictNumberModel` + `.mlnet`), Quartz `Jobs/SimulationJob`,
  `DependencyInjection.AddInfrastructure` + `MigrateAndSeedAsync`.
- **`Bingo.ApiService`** — presentation/composition root: minimal API `/api/sim/*`, DI, CORS,
  migrate+seed lúc khởi động (`Program.cs`).
- **`Bingo.AppHost`** — Aspire orchestrator: cấp **PostgreSQL (Docker) + database `BingoDb` +
  pgAdmin**, tham chiếu apiservice (tự inject connection string `BingoDb`).
- **`Bingo.ServiceDefaults`** — telemetry/health/service discovery dùng chung.
- **`Bingo.Dashboard`** — frontend ReactJS (Vite 4 + TS + recharts), project riêng, **CLAUDE.md
  riêng**. Tiêu thụ REST API.

## Mô hình mô phỏng (cốt lõi)

- Mỗi `SimUser` gắn 1 `Strategy` (key) → engine gọi `IBettingStrategy.DecideBets(ctx)`.
- **Vốn:** mỗi user mỗi ngày reset **1.000.000đ** (`DailyAccount`, theo giờ VN +07). Hết tiền
  (không đủ 10.000đ) → `IsBusted`, dừng ngày đó.
- **Mức cược biến đổi:** không giới hạn cứng 5 vé — một dòng cược có thể 10k/20k/40k... (ví dụ
  Martingale gấp đôi tiền cược). Chặn an toàn `MaxBetsPerDraw=20` dòng/kỳ.
- **Settle:** so vé với `Draw` qua `PayoutCalculator`; cập nhật `DailyAccount`; chiến lược
  adaptive cập nhật state (`UserStrategyState`). `UserStat` dẫn xuất từ `DailyAccount`.
- Chiến lược adaptive (`IsAdaptive`): `martingale_size`, `paroli_size`, `markov_sum`,
  `ewma_adaptive` — tự cập nhật mỗi kỳ qua `OnSettled`.

## Lệnh thường dùng

```bash
# Chạy đầy đủ qua Aspire (tự spin Postgres qua Docker) — KHUYẾN NGHỊ
dotnet run --project Bingo.AppHost      # mở Aspire dashboard

# Build
dotnet build Bingo.sln

# EF migrations (DbContext ở Infrastructure; có IDesignTimeDbContextFactory)
dotnet ef migrations add <Name> -p Bingo.Infrastructure -s Bingo.ApiService

# Frontend
cd Bingo.Dashboard && npm install && npm run dev   # http://localhost:5551
```

Chạy ApiService riêng (không qua Aspire) cần connection string:
`ConnectionStrings__BingoDb="Host=...;Port=...;Database=BingoDb;Username=...;Password=..."`.
Tắt job live khi test: `Simulation__LiveEnabled=false`.

## API endpoints (`/api/sim`)
- Đọc: `GET leaderboard?metric=roi|winrate`, `GET users`, `GET users/{id}`, `GET daily?date=`,
  `GET draws/latest?count=`, `GET tickets?userId=&date=`, `GET strategies`,
  `GET discover?metric=&max=` (săn lợi nhuận: xếp hạng mọi chiến lược theo ROI).
- Hành động: `POST ingest` (nạp draws thật), `POST replay?max=` (backtest điền dữ liệu),
  `POST run-tick` (1 nhịp live), `POST optimize?metric=&max=` (ghi config tốt nhất vào user),
  `POST reset` (xóa dữ liệu cá cược, giữ draws/users/strategies).

**Khởi tạo dữ liệu nhanh:** chạy AppHost → `POST /api/sim/ingest` → `POST /api/sim/replay?max=4000`
→ xem `GET /api/sim/leaderboard?metric=roi`.

## Live job
`SimulationJob` (Quartz, `Simulation:LiveEnabled` mặc định bật) chạy mỗi 6 phút: ingest + settle
kỳ vừa quay + đặt cược kỳ kế tiếp.

## Stack
.NET 8 · .NET Aspire 9 · ASP.NET Core minimal API · EF Core 8 + Npgsql (PostgreSQL) ·
Quartz.NET · ML.NET (1 chiến lược, có fallback) · OpenTelemetry · React 18 + Vite 4 + recharts.

## Lưu ý khi chỉnh sửa
- Thêm chiến lược: tạo class trong `Bingo.Application/Simulation/Strategies/`, đăng ký ở
  `Bingo.Application/DependencyInjection.cs`, seed metadata + user trong `DataSeeder.cs`.
  Giữ **mỗi user một chiến lược riêng** (tránh trùng cách chơi).
- Hệ số trả thưởng ở `prize_rules` (seed trong `DataSeeder`), không hardcode trong chiến lược.
- `PredictNumberModel.mlnet` copy ra output (`Models/`) — đừng xóa; ML.NET là tùy chọn, có fallback.
- Mọi thay đổi DB qua `IUnitOfWork`; truy vấn đọc dùng `AsNoTracking`.
- API nguồn dữ liệu: `Bingo.Infrastructure/External/BingoDataClient.cs` (`bingo18.top`).
- Code & commit dùng tiếng Việt.

