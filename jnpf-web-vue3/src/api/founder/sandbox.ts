/**
 * Sandbox Management API (Phase 6 Day 16-20).
 */
import { defHttp } from '/@/utils/http/axios';
import { getFounderToken } from './index';

function authHeaders(): Record<string, string> {
  const token = getFounderToken();
  return token ? { 'X-Founder-Token': token } : {};
}

export function getSandboxList() {
  return defHttp.get({ url: '/api/sandbox/list', headers: { ...authHeaders() } });
}

export function createSandbox(data: { tenantId: string; cpuLimit?: number; memoryLimit?: string; timeoutSeconds?: number }) {
  return defHttp.post({ url: '/api/sandbox/create', data, headers: { ...authHeaders() } });
}

export function getSandboxStatus(id: string) {
  return defHttp.get({ url: `/api/sandbox/${id}`, headers: { ...authHeaders() } });
}

export function destroySandbox(id: string) {
  return defHttp.delete({ url: `/api/sandbox/${id}`, headers: { ...authHeaders() } });
}

export function deployToSandbox(id: string, formData: FormData) {
  return defHttp.post({
    url: `/api/sandbox/${id}/deploy`,
    data: formData,
    headers: {
      ...authHeaders(),
      'Content-Type': 'multipart/form-data',
    },
  });
}
