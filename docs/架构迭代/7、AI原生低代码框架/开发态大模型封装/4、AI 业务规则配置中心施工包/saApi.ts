// SA 资产 API
import client from './client';
import {
  SAProject, DataDictionary, DecisionTable, StateMachine, ChangeRecord, KGPattern
} from '../types/sa';

// =====================================================
// 项目
// =====================================================
export const projectApi = {
  list: () => client.get<SAProject[]>('/projects').then(r => r.data),
  get: (id: number) => client.get<SAProject>(`/projects/${id}`).then(r => r.data),
  create: (data: { tenantId: string; requirementText: string; userId: string }) =>
    client.post<SAProject>('/projects', data).then(r => r.data),
};

// =====================================================
// 数据字典
// =====================================================
export const dictApi = {
  get: (projectId: number) =>
    client.get<DataDictionary>(`/projects/${projectId}/dictionary`).then(r => r.data),
  update: (projectId: number, data: Partial<DataDictionary>) =>
    client.put<DataDictionary>(`/projects/${projectId}/dictionary`, data).then(r => r.data),
  markAsPatternSource: (projectId: number) =>
    client.post(`/projects/${projectId}/dictionary/mark-pattern-source`).then(r => r.data),
};

// =====================================================
// 判定表
// =====================================================
export const decisionTableApi = {
  list: (projectId: number) =>
    client.get<DecisionTable[]>(`/projects/${projectId}/decision-tables`).then(r => r.data),
  get: (projectId: number, tableId: string) =>
    client.get<DecisionTable>(`/projects/${projectId}/decision-tables/${tableId}`).then(r => r.data),
  update: (projectId: number, tableId: string, data: Partial<DecisionTable>) =>
    client.put<DecisionTable>(`/projects/${projectId}/decision-tables/${tableId}`, data).then(r => r.data),
};

// =====================================================
// 状态机
// =====================================================
export const stateMachineApi = {
  get: (projectId: number) =>
    client.get<StateMachine[]>(`/projects/${projectId}/state-machines`).then(r => r.data),
  update: (projectId: number, entity: string, data: Partial<StateMachine>) =>
    client.put<StateMachine>(`/projects/${projectId}/state-machines/${entity}`, data).then(r => r.data),
};

// =====================================================
// 修改记录(DKEE 学习入口)
// =====================================================
export const changeApi = {
  record: (record: ChangeRecord) =>
    client.post('/changes', record).then(r => r.data),
  list: (projectId: number, table?: string) =>
    client.get<ChangeRecord[]>('/changes', { params: { projectId, table } }).then(r => r.data),
};

// =====================================================
// DKEE Pattern
// =====================================================
export const dkeeApi = {
  listPatterns: (industry: string) =>
    client.get<KGPattern[]>(`/dkee/patterns?industry=${industry}`).then(r => r.data),
  getStats: (industry: string) =>
    client.get(`/dkee/stats?industry=${industry}`).then(r => r.data),
  triggerExtraction: (projectId: number) =>
    client.post(`/dkee/extract/${projectId}`).then(r => r.data),
};
