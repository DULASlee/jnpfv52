/**
 * Run a prefetch after the browser is idle so list first-paint is not competed with chunk transform.
 */
export function scheduleIdlePrefetch(task: () => void | Promise<void>, timeout = 1500): void {
  const run = () => {
    try {
      void Promise.resolve(task()).catch(() => {
        /* prefetch must not surface errors */
      });
    } catch {
      /* ignore */
    }
  };

  if (typeof window !== 'undefined' && 'requestIdleCallback' in window) {
    (window as Window & { requestIdleCallback: (cb: () => void, opts?: { timeout: number }) => number }).requestIdleCallback(run, {
      timeout,
    });
    return;
  }

  setTimeout(run, 200);
}
