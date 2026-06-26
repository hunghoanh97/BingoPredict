# Bingo.Dashboard — Frontend ReactJS

## Dự án là gì
Đây là **frontend dashboard** cho hệ thống **mô phỏng cá cược Bingo18** (backend viết bằng .NET).
Dashboard hiển thị:
- Bảng xếp hạng các "user mô phỏng" theo nhiều chiến lược cược (win rate / ROI / lãi lỗ).
- Chi tiết từng user: thống kê tổng, biểu đồ ROI & win rate theo ngày, biểu đồ lãi/lỗ theo ngày, danh sách vé gần đây.
- Feed các kỳ quay (draws) gần nhất, tự làm mới mỗi 30 giây.
- Danh mục các chiến lược cược.
- Panel quản trị để gọi các tác vụ backend (ingest, run-tick, optimize, reset).

Đây là project **ĐỘC LẬP**, chạy song song với backend. Khi backend offline, mọi trang hiển thị
trạng thái loading rồi báo lỗi **"Không kết nối được API"**.

## Stack
- **Vite 4.5.x** (BẮT BUỘC — xem ràng buộc Node bên dưới)
- **React 18.2** + **React DOM 18.2**
- **TypeScript 5.2**
- **react-router-dom 6** (dùng `HashRouter`)
- **recharts 2.10** (biểu đồ Line / Bar)
- CSS thuần (`src/index.css`), giao diện tối (dark theme).

### Ràng buộc Node (QUAN TRỌNG)
Môi trường dùng **Node v16.20.2 / npm 8.19.4**. Vì vậy **BẮT BUỘC dùng Vite 4.5.x**
(Vite 5/6/7 yêu cầu Node 18/20+). Không thêm package nào yêu cầu Node >= 18.

## Cấu trúc thư mục
```
Bingo.Dashboard/
├─ index.html               # HTML entry (mount #root, nạp /src/main.tsx)
├─ package.json
├─ vite.config.ts           # plugin react, dev server port 5551
├─ tsconfig.json            # config TS cho src/
├─ tsconfig.node.json       # config TS cho vite.config.ts
├─ .env                     # VITE_API_BASE_URL (mặc định http://localhost:5550)
├─ .gitignore
├─ CLAUDE.md                # tài liệu này
└─ src/
   ├─ main.tsx              # bootstrap ReactDOM
   ├─ App.tsx               # khai báo router + routes
   ├─ index.css            # toàn bộ style
   ├─ vite-env.d.ts        # type cho import.meta.env
   ├─ api/
   │  ├─ types.ts          # toàn bộ type của hợp đồng API
   │  └─ client.ts         # fetch wrapper + đối tượng `api` + lỗi offline/http
   ├─ hooks/
   │  └─ useApi.ts         # hook fetch có loading/error/offline + polling
   ├─ components/
   │  ├─ Layout.tsx        # navbar + <Outlet/>
   │  └─ States.tsx        # Loading / ErrorState / EmptyState
   └─ pages/
      ├─ Leaderboard.tsx   # "/"  bảng xếp hạng, toggle metric winrate|roi
      ├─ UserDetail.tsx    # "/users/:id" chi tiết + charts + tickets
      ├─ Draws.tsx         # "/draws" feed kỳ quay, auto refresh 30s
      ├─ Strategies.tsx    # "/strategies" danh mục chiến lược
      └─ Admin.tsx         # "/admin" 4 nút gọi POST, hiện message
```

## Cách chạy
```bash
npm install        # cài dependencies
npm run dev        # chạy dev server (http://localhost:5551)
npm run build      # type-check (tsc) + build production vào dist/
npm run preview    # xem thử bản build
```

## Biến môi trường
| Biến                 | Mặc định                  | Ý nghĩa                         |
| -------------------- | ------------------------- | ------------------------------- |
| `VITE_API_BASE_URL`  | `http://localhost:5550`   | Base URL của backend .NET API   |

Đặt trong file `.env` (đã commit) hoặc `.env.local` (không commit, ưu tiên cao hơn).

## Hợp đồng API mà dashboard tiêu thụ
REST/JSON, base mặc định `http://localhost:5550`. Type khai trong `src/api/types.ts`.
Đơn vị tiền: **VND** (định dạng vi-VN, hiển thị kèm "đ"). `winRate`/`roi` là **phần trăm (0-100)**.

### GET /api/sim/leaderboard?metric=winrate|roi → LeaderboardEntry[]
```ts
LeaderboardEntry = {
  simUserId: number, name: string, strategyKey: string, strategyName: string,
  totalTickets: number, totalWins: number, winRate: number /*0-100*/,
  totalStaked: number, totalPayout: number, netProfit: number, roi: number /*percent*/,
  daysPlayed: number, daysBusted: number,
  currentBalanceToday: number | null, isBustedToday: boolean
}
```

### GET /api/sim/users → SimUserDto[]
```ts
SimUserDto = { id: number, name: string, strategyKey: string, strategyName: string,
  enabled: boolean, stat: UserStat }
UserStat = { totalTickets, totalWins, winRate, totalStaked, totalPayout,
  netProfit, roi, daysPlayed, daysBusted } // tất cả là number
```

### GET /api/sim/users/{id} → UserDetailDto
```ts
UserDetailDto = { id: number, name: string, strategyKey: string, strategyName: string,
  description: string, config: Record<string, unknown>, stat: UserStat, daily: DailyAccountDto[] }
DailyAccountDto = { gameDate: string /*YYYY-MM-DD*/, startingBalance: number, currentBalance: number,
  ticketsBought: number, totalStaked: number, totalPayout: number, wins: number, losses: number,
  netProfit: number, roi: number, isBusted: boolean }
```

### GET /api/sim/daily?date=YYYY-MM-DD → DailySummaryDto
```ts
DailySummaryDto = { date: string,
  perUser: Array<DailyAccountDto & { simUserId: number, userName: string, strategyKey: string }>,
  totals: { totalStaked: number, totalPayout: number, netProfit: number, bustedCount: number } }
```

### GET /api/sim/draws/latest?count=20 → DrawDto[]
```ts
DrawDto = { id: number, drawAt: string /*ISO*/, winningResult: string /*"236"*/,
  d1: number, d2: number, d3: number, sum: number,
  size: "Nho" | "Hoa" | "Lon", isTriple: boolean }
```

### GET /api/sim/tickets?userId=&date= → TicketDto[]
```ts
TicketDto = { id: number, simUserId: number, userName: string, targetDrawAt: string,
  drawId: number | null, betKind: "Sum" | "Size" | "NumberCount" | "Triple", betValue: string,
  stake: number, multiplier: number, isSettled: boolean, isWin: boolean,
  payout: number, profit: number }
```

### GET /api/sim/strategies → StrategyDto[]
```ts
StrategyDto = { key: string, name: string, description: string,
  isAdaptive: boolean, enabled: boolean }
```

### POST (mỗi cái trả về { message: string })
- `POST /api/sim/ingest`   — nạp kỳ quay mới
- `POST /api/sim/run-tick` — chạy 1 nhịp mô phỏng
- `POST /api/sim/optimize` — tối ưu tham số chiến lược
- `POST /api/sim/reset`    — reset trạng thái mô phỏng

## Ghi chú nghiệp vụ
- Cỡ (size) theo tổng 3 chữ số: **"Nho" = 3-9**, **"Hoa" = 10-11**, **"Lon" = 12-18**.
- `isTriple`: 3 chữ số giống nhau (bộ ba).
- "Cháy" (busted): số dư trong ngày về 0.
- Tô màu: lời (> 0) màu xanh, lỗ (< 0) màu đỏ.

## Quy ước khi sửa code
- Mọi gọi API đi qua đối tượng `api` trong `src/api/client.ts`. Đừng gọi `fetch` trực tiếp ở component.
- Mọi trang dữ liệu dùng hook `useApi` để có sẵn loading/error/offline (+ polling tùy chọn).
- Định dạng tiền/% qua helper trong `src/utils/format.ts` (`formatVnd`, `formatPercent`, ...).
- Giữ ràng buộc Vite 4 / Node 16. Không nâng major version các package lõi.
```
