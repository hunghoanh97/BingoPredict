interface LoadingProps {
  label?: string;
}

export function Loading({ label = 'Đang tải...' }: LoadingProps) {
  return (
    <div className="state state-loading">
      <span className="spinner" aria-hidden />
      <span>{label}</span>
    </div>
  );
}

interface ErrorStateProps {
  message: string;
  offline?: boolean;
  onRetry?: () => void;
}

export function ErrorState({ message, offline, onRetry }: ErrorStateProps) {
  return (
    <div className={`state state-error${offline ? ' state-offline' : ''}`}>
      <strong>{offline ? 'Không kết nối được API' : 'Đã xảy ra lỗi'}</strong>
      <span className="state-detail">{message}</span>
      {onRetry && (
        <button className="btn" onClick={onRetry}>
          Thử lại
        </button>
      )}
    </div>
  );
}

export function EmptyState({ label = 'Không có dữ liệu' }: { label?: string }) {
  return <div className="state state-empty">{label}</div>;
}
