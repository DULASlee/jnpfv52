/**
 * Knowledge Graph API (Phase 6 Day 16-20).
 */
import { defHttp } from '/@/utils/http/axios';
import { getFounderToken } from './index';

function authHeaders(): Record<string, string> {
  const token = getFounderToken();
  return token ? { 'X-Founder-Token': token } : {};
}

export function getKnowledgeNodes(params?: { label?: string; domain?: string; currentPage?: number; pageSize?: number }) {
  return defHttp.get({
    url: '/api/InteAssistant/KnowledgePatch/nodes',
    params,
    headers: { ...authHeaders() },
  });
}

export function getKnowledgeNodeDetail(id: string) {
  return defHttp.get({
    url: `/api/InteAssistant/KnowledgePatch/nodes/${id}`,
    headers: { ...authHeaders() },
  });
}

export function getKnowledgeEdges(params?: { relationType?: string; currentPage?: number; pageSize?: number }) {
  return defHttp.get({
    url: '/api/InteAssistant/KnowledgePatch/edges',
    params,
    headers: { ...authHeaders() },
  });
}

export function getKnowledgeStats() {
  return defHttp.get({
    url: '/api/InteAssistant/KnowledgePatch/stats',
    headers: { ...authHeaders() },
  });
}
