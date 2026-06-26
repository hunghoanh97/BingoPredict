import { useState } from 'react';
import { api, ApiOfflineError } from '../api/client';

interface ActionDef {
  key: string;
  label: string;
  description: string;
  run: () => Promise<{ message: string }>;
}

const actions: ActionDef[] = [
  {
    key: 'ingest',
    label: 'Nạp dữ liệu (Ingest)',
    description: 'Kéo các kỳ quay mới về hệ thống.',
    run: () => api.ingest(),
  },
  {
    key: 'run-tick',
    label: 'Chạy 1 nhịp (Run tick)',
    description: 'Cho các user đặt vé / quyết toán theo kỳ quay mới nhất.',
    run: () => api.runTick(),
  },
  {
    key: 'replay',
    label: 'Chạy lại (Replay)',
    description: 'Backtest toàn bộ mô phỏng trên dữ liệu đã nạp để điền kết quả các ngày.',
    run: () => api.replay(2000),
  },
  {
    key: 'optimize',
    label: 'Tối ưu lợi nhuận (Optimize)',
    description: 'Tinh chỉnh tham số chiến lược theo ROI và ghi cấu hình tốt nhất.',
    run: () => api.optimize('roi', 2000),
  },
  {
    key: 'discover',
    label: 'Săn lợi nhuận (Discover)',
    description: 'Backtest mọi chiến lược, xếp hạng theo ROI để tìm cách chơi sinh lời nhất.',
    run: async () => {
      const d = await api.discover('roi', 4000);
      const top = d.slice(0, 3).map((x) => `${x.key} ROI ${x.roi}%`).join('  |  ');
      return { message: d.length ? `Top lợi nhuận: ${top}` : 'Chưa có dữ liệu.' };
    },
  },
  {
    key: 'reset',
    label: 'Reset',
    description: 'Đặt lại toàn bộ trạng thái mô phỏng.',
    run: () => api.reset(),
  },
];

type ResultKind = 'ok' | 'error';
interface Result {
  kind: ResultKind;
  text: string;
}

export function Admin() {
  const [results, setResults] = useState<Record<string, Result>>({});
  const [busy, setBusy] = useState<string | null>(null);

  async function handle(action: ActionDef) {
    setBusy(action.key);
    try {
      const res = await action.run();
      setResults((r) => ({
        ...r,
        [action.key]: { kind: 'ok', text: res?.message ?? 'Thành công' },
      }));
    } catch (err) {
      const text =
        err instanceof ApiOfflineError
          ? 'Không kết nối được API'
          : err instanceof Error
          ? err.message
          : 'Lỗi không xác định';
      setResults((r) => ({ ...r, [action.key]: { kind: 'error', text } }));
    } finally {
      setBusy(null);
    }
  }

  return (
    <section>
      <div className="page-head">
        <h1>Quản trị mô phỏng</h1>
      </div>
      <p className="muted">Các thao tác gọi trực tiếp tới backend và hiển thị thông điệp trả về.</p>

      <div className="card-grid">
        {actions.map((a) => {
          const res = results[a.key];
          return (
            <div key={a.key} className="admin-card">
              <h3>{a.label}</h3>
              <p>{a.description}</p>
              <button
                className="btn"
                disabled={busy !== null}
                onClick={() => handle(a)}
              >
                {busy === a.key ? 'Đang chạy...' : 'Thực thi'}
              </button>
              {res && (
                <div className={`admin-result ${res.kind === 'ok' ? 'ok' : 'error'}`}>
                  {res.text}
                </div>
              )}
            </div>
          );
        })}
      </div>
    </section>
  );
}
