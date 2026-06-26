import { useState } from 'react';
import { Link } from 'react-router-dom';
import { api } from '../api/client';
import type { LeaderboardMetric } from '../api/types';
import { useApi } from '../hooks/useApi';
import { Loading, ErrorState, EmptyState } from '../components/States';
import { formatVnd, formatNumber, formatPercent, pnlClass } from '../utils/format';

export function Leaderboard() {
  const [metric, setMetric] = useState<LeaderboardMetric>('winrate');
  const { data, loading, error, offline, reload } = useApi(
    () => api.getLeaderboard(metric),
    [metric]
  );

  return (
    <section>
      <div className="page-head">
        <h1>Bảng xếp hạng</h1>
        <div className="metric-toggle">
          <span>Sắp xếp theo:</span>
          <button
            className={`toggle-btn${metric === 'winrate' ? ' active' : ''}`}
            onClick={() => setMetric('winrate')}
          >
            Win rate
          </button>
          <button
            className={`toggle-btn${metric === 'roi' ? ' active' : ''}`}
            onClick={() => setMetric('roi')}
          >
            ROI
          </button>
        </div>
      </div>

      {loading && <Loading />}
      {!loading && error && <ErrorState message={error} offline={offline} onRetry={reload} />}
      {!loading && !error && (!data || data.length === 0) && <EmptyState />}

      {!loading && !error && data && data.length > 0 && (
        <div className="table-wrap">
          <table className="data-table">
            <thead>
              <tr>
                <th>#</th>
                <th>Tên</th>
                <th>Chiến lược</th>
                <th className="num">Win rate</th>
                <th className="num">ROI</th>
                <th className="num">Lãi/Lỗ ròng</th>
                <th className="num">Tổng vé</th>
                <th className="num">Thắng</th>
                <th className="num">Ngày chơi</th>
                <th className="center">Cháy hôm nay</th>
              </tr>
            </thead>
            <tbody>
              {data.map((e, i) => (
                <tr key={e.simUserId}>
                  <td>{i + 1}</td>
                  <td>
                    <Link to={`/users/${e.simUserId}`} className="link">
                      {e.name}
                    </Link>
                  </td>
                  <td>
                    <span className="tag">{e.strategyName || e.strategyKey}</span>
                  </td>
                  <td className="num">{formatPercent(e.winRate)}</td>
                  <td className={`num ${pnlClass(e.roi)}`}>{formatPercent(e.roi)}</td>
                  <td className={`num ${pnlClass(e.netProfit)}`}>{formatVnd(e.netProfit)}</td>
                  <td className="num">{formatNumber(e.totalTickets)}</td>
                  <td className="num">{formatNumber(e.totalWins)}</td>
                  <td className="num">{formatNumber(e.daysPlayed)}</td>
                  <td className="center">
                    {e.isBustedToday ? (
                      <span className="badge badge-danger">Cháy</span>
                    ) : (
                      <span className="badge badge-ok">OK</span>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </section>
  );
}
