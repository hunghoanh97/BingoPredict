import { useCallback, useEffect, useRef, useState } from 'react';
import { ApiOfflineError } from '../api/client';

export interface ApiState<T> {
  data: T | null;
  loading: boolean;
  error: string | null;
  /** True specifically when the backend is unreachable. */
  offline: boolean;
  reload: () => void;
}

function toMessage(err: unknown): { msg: string; offline: boolean } {
  if (err instanceof ApiOfflineError) {
    return { msg: 'Không kết nối được API', offline: true };
  }
  if (err instanceof Error) return { msg: err.message, offline: false };
  return { msg: 'Đã xảy ra lỗi không xác định', offline: false };
}

/**
 * Fetch data from an async fetcher with loading/error/offline states.
 * Optionally polls every `pollMs` milliseconds.
 *
 * `deps` controls when the fetcher is re-created/re-run (like useEffect deps).
 */
export function useApi<T>(
  fetcher: () => Promise<T>,
  deps: unknown[] = [],
  pollMs?: number
): ApiState<T> {
  const [data, setData] = useState<T | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [offline, setOffline] = useState(false);

  // eslint-disable-next-line react-hooks/exhaustive-deps
  const stableFetcher = useCallback(fetcher, deps);
  const mounted = useRef(true);

  const run = useCallback(
    async (showSpinner: boolean) => {
      if (showSpinner) setLoading(true);
      try {
        const result = await stableFetcher();
        if (!mounted.current) return;
        setData(result);
        setError(null);
        setOffline(false);
      } catch (err) {
        if (!mounted.current) return;
        const { msg, offline: off } = toMessage(err);
        setError(msg);
        setOffline(off);
      } finally {
        if (mounted.current) setLoading(false);
      }
    },
    [stableFetcher]
  );

  useEffect(() => {
    mounted.current = true;
    run(true);
    let timer: ReturnType<typeof setInterval> | undefined;
    if (pollMs && pollMs > 0) {
      timer = setInterval(() => run(false), pollMs);
    }
    return () => {
      mounted.current = false;
      if (timer) clearInterval(timer);
    };
  }, [run, pollMs]);

  return { data, loading, error, offline, reload: () => run(true) };
}
