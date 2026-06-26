import { api } from '../api/client';
import { useApi } from '../hooks/useApi';
import { Loading, ErrorState, EmptyState } from '../components/States';

export function Strategies() {
  const { data, loading, error, offline, reload } = useApi(() => api.getStrategies(), []);

  return (
    <section>
      <div className="page-head">
        <h1>Danh mục chiến lược</h1>
      </div>

      {loading && <Loading />}
      {!loading && error && <ErrorState message={error} offline={offline} onRetry={reload} />}
      {!loading && !error && (!data || data.length === 0) && <EmptyState />}

      {!loading && !error && data && data.length > 0 && (
        <div className="card-grid">
          {data.map((s) => (
            <div key={s.key} className="strategy-card">
              <div className="strategy-card-head">
                <h3>{s.name}</h3>
                <div className="strategy-tags">
                  {s.isAdaptive && <span className="badge badge-info">Thích nghi</span>}
                  {s.enabled ? (
                    <span className="badge badge-ok">Bật</span>
                  ) : (
                    <span className="badge">Tắt</span>
                  )}
                </div>
              </div>
              <code className="strategy-key">{s.key}</code>
              <p>{s.description}</p>
            </div>
          ))}
        </div>
      )}
    </section>
  );
}
