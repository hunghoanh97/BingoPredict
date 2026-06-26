import { Link, useParams } from 'react-router-dom';
import {
  LineChart,
  Line,
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  Legend,
  ResponsiveContainer,
  Cell,
} from 'recharts';
import { api } from '../api/client';
import { useApi } from '../hooks/useApi';
import { Loading, ErrorState, EmptyState } from '../components/States';
import {
  formatVnd,
  formatNumber,
  formatPercent,
  formatDateTime,
  pnlClass,
} from '../utils/format';

export function UserDetail() {
  const { id } = useParams<{ id: string }>();

  const detail = useApi(() => api.getUser(id!), [id]);
  const tickets = useApi(() => api.getTickets({ userId: id }), [id]);

  if (detail.loading) return <Loading />;
  if (detail.error)
    return (
      <ErrorState message={detail.error} offline={detail.offline} onRetry={detail.reload} />
    );
  if (!detail.data) return <EmptyState />;

  const u = detail.data;
  const stat = u.stat;

  // recharts wants ascending date order.
  const daily = [...u.daily].sort((a, b) => a.gameDate.localeCompare(b.gameDate));

  return (
    <section>
      <div className="page-head">
        <h1>
          {u.name} <span className="tag">{u.strategyName || u.strategyKey}</span>
        </h1>
        <Link to="/" className="btn btn-ghost">
          ← Bảng xếp hạng
        </Link>
      </div>

      {u.description && <p className="muted">{u.description}</p>}

      <div className="stat-grid">
        <StatCard label="Win rate" value={formatPercent(stat.winRate)} />
        <StatCard label="ROI" value={formatPercent(stat.roi)} cls={pnlClass(stat.roi)} />
        <StatCard
          label="Lãi/Lỗ ròng"
          value={formatVnd(stat.netProfit)}
          cls={pnlClass(stat.netProfit)}
        />
        <StatCard label="Tổng vé" value={formatNumber(stat.totalTickets)} />
        <StatCard label="Số lần thắng" value={formatNumber(stat.totalWins)} />
        <StatCard label="Tổng cược" value={formatVnd(stat.totalStaked)} />
        <StatCard label="Tổng nhận" value={formatVnd(stat.totalPayout)} />
        <StatCard label="Ngày chơi" value={formatNumber(stat.daysPlayed)} />
        <StatCard label="Ngày cháy" value={formatNumber(stat.daysBusted)} />
      </div>

      {Object.keys(u.config ?? {}).length > 0 && (
        <details className="config-box">
          <summary>Cấu hình chiến lược</summary>
          <pre>{JSON.stringify(u.config, null, 2)}</pre>
        </details>
      )}

      <h2>ROI &amp; Win rate theo ngày</h2>
      {daily.length === 0 ? (
        <EmptyState label="Chưa có dữ liệu theo ngày" />
      ) : (
        <div className="chart-card">
          <ResponsiveContainer width="100%" height={300}>
            <LineChart data={daily} margin={{ top: 8, right: 24, left: 0, bottom: 0 }}>
              <CartesianGrid strokeDasharray="3 3" stroke="#2a3245" />
              <XAxis dataKey="gameDate" stroke="#8a93a6" fontSize={12} />
              <YAxis stroke="#8a93a6" fontSize={12} unit="%" />
              <Tooltip
                contentStyle={{ background: '#1b2030', border: '1px solid #2a3245' }}
                formatter={(v: number) => formatPercent(v)}
              />
              <Legend />
              <Line
                type="monotone"
                dataKey="roi"
                name="ROI"
                stroke="#4fd1c5"
                dot={false}
                strokeWidth={2}
              />
              <Line
                type="monotone"
                dataKey="winRate"
                name="Win rate"
                stroke="#f6ad55"
                dot={false}
                strokeWidth={2}
              />
            </LineChart>
          </ResponsiveContainer>
        </div>
      )}

      <h2>Lãi/Lỗ ròng theo ngày</h2>
      {daily.length === 0 ? (
        <EmptyState label="Chưa có dữ liệu theo ngày" />
      ) : (
        <div className="chart-card">
          <ResponsiveContainer width="100%" height={300}>
            <BarChart data={daily} margin={{ top: 8, right: 24, left: 0, bottom: 0 }}>
              <CartesianGrid strokeDasharray="3 3" stroke="#2a3245" />
              <XAxis dataKey="gameDate" stroke="#8a93a6" fontSize={12} />
              <YAxis stroke="#8a93a6" fontSize={12} />
              <Tooltip
                contentStyle={{ background: '#1b2030', border: '1px solid #2a3245' }}
                formatter={(v: number) => formatVnd(v)}
              />
              <Bar dataKey="netProfit" name="Lãi/Lỗ ròng">
                {daily.map((d, i) => (
                  <Cell key={i} fill={d.netProfit >= 0 ? '#48bb78' : '#f56565'} />
                ))}
              </Bar>
            </BarChart>
          </ResponsiveContainer>
        </div>
      )}

      <h2>Vé gần đây</h2>
      {tickets.loading && <Loading />}
      {!tickets.loading && tickets.error && (
        <ErrorState message={tickets.error} offline={tickets.offline} onRetry={tickets.reload} />
      )}
      {!tickets.loading && !tickets.error && (!tickets.data || tickets.data.length === 0) && (
        <EmptyState label="Chưa có vé" />
      )}
      {!tickets.loading && !tickets.error && tickets.data && tickets.data.length > 0 && (
        <div className="table-wrap">
          <table className="data-table">
            <thead>
              <tr>
                <th>Kỳ quay</th>
                <th>Loại cược</th>
                <th>Giá trị</th>
                <th className="num">Tiền cược</th>
                <th className="num">Hệ số</th>
                <th className="center">Kết quả</th>
                <th className="num">Tiền nhận</th>
                <th className="num">Lãi/Lỗ</th>
              </tr>
            </thead>
            <tbody>
              {tickets.data.map((t) => (
                <tr key={t.id}>
                  <td>{formatDateTime(t.targetDrawAt)}</td>
                  <td>
                    <span className="tag">{t.betKind}</span>
                  </td>
                  <td>{t.betValue}</td>
                  <td className="num">{formatVnd(t.stake)}</td>
                  <td className="num">{t.multiplier}x</td>
                  <td className="center">
                    {!t.isSettled ? (
                      <span className="badge">Chờ</span>
                    ) : t.isWin ? (
                      <span className="badge badge-ok">Thắng</span>
                    ) : (
                      <span className="badge badge-danger">Thua</span>
                    )}
                  </td>
                  <td className="num">{formatVnd(t.payout)}</td>
                  <td className={`num ${pnlClass(t.profit)}`}>{formatVnd(t.profit)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </section>
  );
}

function StatCard({ label, value, cls }: { label: string; value: string; cls?: string }) {
  return (
    <div className="stat-card">
      <div className="stat-label">{label}</div>
      <div className={`stat-value ${cls ?? ''}`}>{value}</div>
    </div>
  );
}
