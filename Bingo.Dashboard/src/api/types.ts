// API contract types for the Bingo18 simulation backend (.NET).
// All shapes mirror the REST/JSON contract documented in CLAUDE.md.

export interface UserStat {
  totalTickets: number;
  totalWins: number;
  winRate: number; // 0-100
  totalStaked: number;
  totalPayout: number;
  netProfit: number;
  roi: number; // percent
  daysPlayed: number;
  daysBusted: number;
}

export interface LeaderboardEntry {
  simUserId: number;
  name: string;
  strategyKey: string;
  strategyName: string;
  totalTickets: number;
  totalWins: number;
  winRate: number; // 0-100
  totalStaked: number;
  totalPayout: number;
  netProfit: number;
  roi: number; // percent
  daysPlayed: number;
  daysBusted: number;
  currentBalanceToday: number | null;
  isBustedToday: boolean;
}

export interface SimUserDto {
  id: number;
  name: string;
  strategyKey: string;
  strategyName: string;
  enabled: boolean;
  stat: UserStat;
}

export interface DailyAccountDto {
  gameDate: string; // YYYY-MM-DD
  startingBalance: number;
  currentBalance: number;
  ticketsBought: number;
  totalStaked: number;
  totalPayout: number;
  wins: number;
  losses: number;
  netProfit: number;
  roi: number;
  isBusted: boolean;
}

export interface UserDetailDto {
  id: number;
  name: string;
  strategyKey: string;
  strategyName: string;
  description: string;
  config: Record<string, unknown>;
  stat: UserStat;
  daily: DailyAccountDto[];
}

export interface DailyPerUserDto extends DailyAccountDto {
  simUserId: number;
  userName: string;
  strategyKey: string;
}

export interface DailySummaryDto {
  date: string;
  perUser: DailyPerUserDto[];
  totals: {
    totalStaked: number;
    totalPayout: number;
    netProfit: number;
    bustedCount: number;
  };
}

export type DrawSize = 'Nho' | 'Hoa' | 'Lon';

export interface DrawDto {
  id: number;
  drawAt: string; // ISO
  winningResult: string; // "236"
  d1: number;
  d2: number;
  d3: number;
  sum: number;
  size: DrawSize;
  isTriple: boolean;
}

export type BetKind = 'Sum' | 'Size' | 'NumberCount' | 'Triple';

export interface TicketDto {
  id: number;
  simUserId: number;
  userName: string;
  targetDrawAt: string;
  drawId: number | null;
  betKind: BetKind;
  betValue: string;
  stake: number;
  multiplier: number;
  isSettled: boolean;
  isWin: boolean;
  payout: number;
  profit: number;
}

export interface StrategyDto {
  key: string;
  name: string;
  description: string;
  isAdaptive: boolean;
  enabled: boolean;
}

export interface MessageDto {
  message: string;
}

export type LeaderboardMetric = 'winrate' | 'roi';
