import type {
  LeaderboardEntry,
  LeaderboardMetric,
  SimUserDto,
  UserDetailDto,
  DailySummaryDto,
  DrawDto,
  TicketDto,
  StrategyDto,
  MessageDto,
} from './types';

export const API_BASE_URL =
  import.meta.env.VITE_API_BASE_URL?.replace(/\/$/, '') || 'http://localhost:5550';

/** Thrown when the backend cannot be reached (network/offline). */
export class ApiOfflineError extends Error {
  constructor(message = 'Không kết nối được API') {
    super(message);
    this.name = 'ApiOfflineError';
  }
}

/** Thrown when the backend responds with a non-2xx status. */
export class ApiHttpError extends Error {
  status: number;
  constructor(status: number, message: string) {
    super(message);
    this.name = 'ApiHttpError';
    this.status = status;
  }
}

async function request<T>(
  path: string,
  options?: RequestInit & { query?: Record<string, string | number | undefined> }
): Promise<T> {
  const { query, ...init } = options ?? {};
  let url = `${API_BASE_URL}${path}`;
  if (query) {
    const params = new URLSearchParams();
    for (const [k, v] of Object.entries(query)) {
      if (v !== undefined && v !== null && v !== '') params.set(k, String(v));
    }
    const qs = params.toString();
    if (qs) url += `?${qs}`;
  }

  let res: Response;
  try {
    res = await fetch(url, {
      headers: { Accept: 'application/json', ...(init.headers ?? {}) },
      ...init,
    });
  } catch {
    // Network failure / backend offline / CORS block.
    throw new ApiOfflineError();
  }

  if (!res.ok) {
    let detail = res.statusText;
    try {
      const text = await res.text();
      if (text) detail = text;
    } catch {
      /* ignore */
    }
    throw new ApiHttpError(res.status, `HTTP ${res.status}: ${detail}`);
  }

  // POST endpoints with empty body still resolve fine.
  const ct = res.headers.get('content-type') ?? '';
  if (!ct.includes('application/json')) {
    return undefined as unknown as T;
  }
  return (await res.json()) as T;
}

export const api = {
  getLeaderboard(metric: LeaderboardMetric) {
    return request<LeaderboardEntry[]>('/api/sim/leaderboard', { query: { metric } });
  },
  getUsers() {
    return request<SimUserDto[]>('/api/sim/users');
  },
  getUser(id: number | string) {
    return request<UserDetailDto>(`/api/sim/users/${id}`);
  },
  getDaily(date: string) {
    return request<DailySummaryDto>('/api/sim/daily', { query: { date } });
  },
  getLatestDraws(count = 20) {
    return request<DrawDto[]>('/api/sim/draws/latest', { query: { count } });
  },
  getTickets(params: { userId?: number | string; date?: string }) {
    return request<TicketDto[]>('/api/sim/tickets', {
      query: { userId: params.userId, date: params.date },
    });
  },
  getStrategies() {
    return request<StrategyDto[]>('/api/sim/strategies');
  },
  // Admin actions
  ingest() {
    return request<MessageDto>('/api/sim/ingest', { method: 'POST' });
  },
  runTick() {
    return request<MessageDto>('/api/sim/run-tick', { method: 'POST' });
  },
  replay(max = 2000) {
    return request<MessageDto>('/api/sim/replay', { method: 'POST', query: { max } });
  },
  optimize(metric: LeaderboardMetric = 'roi', max = 2000) {
    return request<MessageDto>('/api/sim/optimize', { method: 'POST', query: { metric, max } });
  },
  reset() {
    return request<MessageDto>('/api/sim/reset', { method: 'POST' });
  },
  // Săn lợi nhuận: xếp hạng mọi chiến lược theo ROI trên dữ liệu thật.
  discover(metric: LeaderboardMetric = 'roi', max = 2000) {
    return request<
      Array<{ key: string; name: string; roi: number; netProfit: number; winRate: number }>
    >('/api/sim/discover', { query: { metric, max } });
  },
};
