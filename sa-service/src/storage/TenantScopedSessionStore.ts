export interface StepSession {
  tenantId: string;
  projectId: string;
  eventId?: string;
  stepName?: string;
  runId?: string;
  startedAt: number;
  completedAt?: number;
  data?: Record<string, unknown>;
}

/**
 * 租户隔离的 SA step 会话存储（P2.5-S02）。
 * Key: `${tenantId}:${projectId}:${eventId}:${stepName}`
 */
export class TenantScopedSessionStore {
  private sessions = new Map<string, StepSession>();
  private expirations = new Map<string, number>();

  buildKey(tenantId: string, projectId: string, eventId: string, stepName: string): string {
    return `${tenantId}:${projectId}:${eventId}:${stepName}`;
  }

  get(key: string): StepSession | undefined {
    this.purgeKeyIfExpired(key);
    return this.sessions.get(key);
  }

  set(key: string, session: StepSession, ttlMs = 30 * 60 * 1000): void {
    this.sessions.set(key, session);
    this.expirations.set(key, Date.now() + ttlMs);
  }

  markCompleted(key: string): void {
    const session = this.sessions.get(key);
    if (session) {
      session.completedAt = Date.now();
      this.sessions.set(key, session);
    }
  }

  deleteByProject(tenantId: string, projectId: string): void {
    const prefix = `${tenantId}:${projectId}:`;
    for (const key of [...this.sessions.keys()]) {
      if (key.startsWith(prefix)) {
        this.sessions.delete(key);
        this.expirations.delete(key);
      }
    }
  }

  purgeExpired(): number {
    const now = Date.now();
    let removed = 0;
    for (const [key, expiresAt] of this.expirations.entries()) {
      if (expiresAt <= now) {
        this.sessions.delete(key);
        this.expirations.delete(key);
        removed++;
      }
    }
    return removed;
  }

  listKeys(): string[] {
    this.purgeExpired();
    return [...this.sessions.keys()];
  }

  private purgeKeyIfExpired(key: string): void {
    const expiresAt = this.expirations.get(key);
    if (expiresAt != null && expiresAt <= now()) {
      this.sessions.delete(key);
      this.expirations.delete(key);
    }
  }
}

function now(): number {
  return Date.now();
}

export const tenantSessionStore = new TenantScopedSessionStore();
