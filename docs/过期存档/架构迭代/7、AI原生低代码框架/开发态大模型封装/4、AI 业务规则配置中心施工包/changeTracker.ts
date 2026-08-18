// 变更追踪器 - 记录人类修改,喂给 DKEE 学习
import { ChangeRecord } from '../types/sa';
import { changeApi } from '../api/saApi';

export class ChangeTracker {
  private userId: string;
  private projectId: number;
  private pendingChanges: ChangeRecord[] = [];

  constructor(projectId: number, userId: string) {
    this.projectId = projectId;
    this.userId = userId;
  }

  /**
   * 记录一个修改
   */
  record(
    table: ChangeRecord['table'],
    recordId: number,
    field: string,
    before: any,
    after: any,
    reason?: string
  ): void {
    this.pendingChanges.push({
      table,
      recordId,
      field,
      before,
      after,
      userId: this.userId,
      reason,
      timestamp: new Date().toISOString(),
    });
  }

  /**
   * 获取所有待提交修改
   */
  getPending(): ChangeRecord[] {
    return [...this.pendingChanges];
  }

  /**
   * 提交所有修改到后端(DKEE 会自动学习)
   */
  async commit(): Promise<void> {
    if (this.pendingChanges.length === 0) return;

    // 批量提交
    await Promise.all(
      this.pendingChanges.map(change => changeApi.record(change))
    );

    this.pendingChanges = [];
  }

  /**
   * 丢弃所有待提交修改
   */
  discard(): void {
    this.pendingChanges = [];
  }
}

// =====================================================
// Diff 工具 - 比较前后值,生成可读描述
// =====================================================
export function describeChange(field: string, before: any, after: any): string {
  if (before === undefined) return `添加字段 "${field}" = ${JSON.stringify(after)}`;
  if (after === undefined) return `删除字段 "${field}"`;
  if (typeof before === 'number' && typeof after === 'number') {
    return `修改 "${field}": ${before} → ${after}`;
  }
  return `修改 "${field}": ${JSON.stringify(before)} → ${JSON.stringify(after)}`;
}
