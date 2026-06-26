const currencyFmt = new Intl.NumberFormat('vi-VN', {
  maximumFractionDigits: 0,
});

const numberFmt = new Intl.NumberFormat('vi-VN', {
  maximumFractionDigits: 0,
});

/** Format an amount as Vietnamese đồng, e.g. 1.234.567 đ */
export function formatVnd(value: number | null | undefined): string {
  if (value === null || value === undefined || Number.isNaN(value)) return '—';
  return `${currencyFmt.format(value)} đ`;
}

export function formatNumber(value: number | null | undefined): string {
  if (value === null || value === undefined || Number.isNaN(value)) return '—';
  return numberFmt.format(value);
}

/** Format a percentage value (already 0-100) e.g. 42,5% */
export function formatPercent(value: number | null | undefined, digits = 1): string {
  if (value === null || value === undefined || Number.isNaN(value)) return '—';
  return `${value.toLocaleString('vi-VN', {
    minimumFractionDigits: digits,
    maximumFractionDigits: digits,
  })}%`;
}

/** Format an ISO datetime to local Vietnamese date+time. */
export function formatDateTime(iso: string | null | undefined): string {
  if (!iso) return '—';
  const d = new Date(iso);
  if (Number.isNaN(d.getTime())) return iso;
  return d.toLocaleString('vi-VN', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit',
  });
}

/** YYYY-MM-DD for today (local). */
export function todayIso(): string {
  const d = new Date();
  const m = String(d.getMonth() + 1).padStart(2, '0');
  const day = String(d.getDate()).padStart(2, '0');
  return `${d.getFullYear()}-${m}-${day}`;
}

/** CSS class helper for profit/loss colouring. */
export function pnlClass(value: number | null | undefined): string {
  if (value === null || value === undefined || value === 0) return 'pnl-zero';
  return value > 0 ? 'pnl-pos' : 'pnl-neg';
}
