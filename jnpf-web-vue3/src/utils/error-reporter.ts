import { useErrorLogStoreWithOut } from '/@/store/modules/errorLog';
import { ErrorTypeEnum } from '/@/enums/exceptionEnum';

class ErrorReporter {
  private queue: any[] = [];
  private flushing = false;

  report(error: { message: string; stack?: string; traceId?: string; source: 'uncaught' | 'unhandledrejection' | 'http' | 'vue' }) {
    const entry = {
      ...error,
      timestamp: new Date().toISOString(),
      url: window.location.href,
      userAgent: navigator.userAgent,
    };

    const errorLogStore = useErrorLogStoreWithOut();
    errorLogStore.addErrorLogInfo({
      type: error.source as unknown as ErrorTypeEnum,
      name: error.source,
      message: error.message,
      stack: error.stack || '',
      url: entry.url,
      detail: error.traceId || '',
    } as any);

    this.queue.push(entry);
    this.flush();
  }

  private async flush() {
    if (this.flushing || this.queue.length === 0) return;
    this.flushing = true;
    const batch = this.queue.splice(0, 10);
    try {
      await fetch('/api/log/frontend-error', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(batch),
        keepalive: true,
      });
    } catch {
      // 静默失败
    } finally {
      this.flushing = false;
    }
  }
}

export const errorReporter = new ErrorReporter();
