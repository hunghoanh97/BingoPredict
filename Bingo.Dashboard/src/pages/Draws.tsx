import { api } from '../api/client';
import { useApi } from '../hooks/useApi';
import { Loading, ErrorState, EmptyState } from '../components/States';
import { formatDateTime, formatNumber } from '../utils/format';
import type { DrawSize } from '../api/types';

const SIZE_LABEL: Record<DrawSize, string> = {
  Nho: 'Nhỏ (3-9)',
  Hoa: 'Hòa (10-11)',
  Lon: 'Lớn (12-18)',
};

export function Draws() {
  // Auto refresh every 30s.
  const { data, loading, error, offline, reload } = useApi(
    () => api.getLatestDraws(20),
    [],
    30_000
  );

  return (
    <section>
      <div className="page-head">
        <h1>Kỳ quay gần nhất</h1>
        <div className="page-head-meta">
          <span className="muted">Tự làm mới mỗi 30 giây</span>
          <button className="btn btn-ghost" onClick={reload}>
            Làm mới
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
                <th>Thời gian quay</th>
                <th className="center">Kết quả</th>
                <th className="num">Tổng</th>
                <th>Cỡ</th>
                <th className="center">Bộ ba</th>
              </tr>
            </thead>
            <tbody>
              {data.map((d) => (
                <tr key={d.id}>
                  <td>{formatDateTime(d.drawAt)}</td>
                  <td className="center">
                    <span className="result-balls">
                      <span className="ball">{d.d1}</span>
                      <span className="ball">{d.d2}</span>
                      <span className="ball">{d.d3}</span>
                    </span>
                  </td>
                  <td className="num">{formatNumber(d.sum)}</td>
                  <td>
                    <span className={`tag size-${d.size}`}>{SIZE_LABEL[d.size] ?? d.size}</span>
                  </td>
                  <td className="center">
                    {d.isTriple ? <span className="badge badge-danger">Bộ ba</span> : '—'}
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
