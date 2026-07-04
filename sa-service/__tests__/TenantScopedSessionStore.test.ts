import { TenantScopedSessionStore } from '../src/storage/TenantScopedSessionStore';

describe('TenantScopedSessionStore', () => {
  it('isolates keys by tenant and project', () => {
    const store = new TenantScopedSessionStore();
    const keyA = store.buildKey('1', '100', 'BE-001', 'DomainModel');
    const keyB = store.buildKey('2', '100', 'BE-001', 'DomainModel');

    store.set(keyA, { tenantId: '1', projectId: '100', startedAt: Date.now() });
    store.set(keyB, { tenantId: '2', projectId: '100', startedAt: Date.now() });

    expect(store.get(keyA)?.tenantId).toBe('1');
    expect(store.get(keyB)?.tenantId).toBe('2');
    expect(store.listKeys()).toEqual(expect.arrayContaining([keyA, keyB]));
  });

  it('deleteByProject removes only matching tenant/project', () => {
    const store = new TenantScopedSessionStore();
    const keyA1 = store.buildKey('1', '100', 'BE-001', 'DomainModel');
    const keyA2 = store.buildKey('1', '200', 'BE-001', 'DomainModel');
    const keyB1 = store.buildKey('2', '100', 'BE-001', 'DomainModel');

    store.set(keyA1, { tenantId: '1', projectId: '100', startedAt: Date.now() });
    store.set(keyA2, { tenantId: '1', projectId: '200', startedAt: Date.now() });
    store.set(keyB1, { tenantId: '2', projectId: '100', startedAt: Date.now() });

    store.deleteByProject('1', '100');

    expect(store.get(keyA1)).toBeUndefined();
    expect(store.get(keyA2)).toBeDefined();
    expect(store.get(keyB1)).toBeDefined();
  });

  it('purgeExpired removes expired sessions', () => {
    jest.useFakeTimers();
    const store = new TenantScopedSessionStore();
    const key = store.buildKey('1', '100', 'BE-001', 'DomainModel');
    store.set(key, { tenantId: '1', projectId: '100', startedAt: Date.now() }, 1000);

    jest.advanceTimersByTime(1500);
    expect(store.purgeExpired()).toBe(1);
    expect(store.get(key)).toBeUndefined();
    jest.useRealTimers();
  });
});
