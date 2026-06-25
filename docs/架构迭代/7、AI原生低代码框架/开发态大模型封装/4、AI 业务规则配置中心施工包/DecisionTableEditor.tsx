// 判定表编辑器 - 人类专家批改 AI 生成业务规则的核心界面
import { useState, useMemo } from 'react';
import { DecisionTable } from '../types/sa';
import { ChangeTracker, describeChange } from '../utils/changeTracker';
import { decisionTableApi } from '../api/saApi';
import { useMutation, useQueryClient } from '@tanstack/react-query';

interface Props {
  projectId: number;
  table: DecisionTable;
  userId: string;
}

export function DecisionTableEditor({ projectId, table, userId }: Props) {
  const [draft, setDraft] = useState<DecisionTable>(table);
  const [modifiedCells, setModifiedCells] = useState<Set<string>>(new Set());
  const [showDiff, setShowDiff] = useState(false);
  const tracker = useMemo(() => new ChangeTracker(projectId, userId), [projectId, userId]);
  const queryClient = useQueryClient();

  // 修改单元格
  const updateCell = (rowIdx: number, condIdx: number, value: any) => {
    const newDraft = { ...draft };
    const newRules = [...newDraft.rules];
    newRules[rowIdx] = {
      ...newRules[rowIdx],
      conditionMask: [...newRules[rowIdx].conditionMask],
    };
    newRules[rowIdx].conditionMask[condIdx] = value;
    newDraft.rules = newRules;
    setDraft(newDraft);
    setModifiedCells(prev => new Set(prev).add(`${rowIdx}-${condIdx}`));
  };

  // 修改动作
  const updateAction = (rowIdx: number, actionIdx: number) => {
    const newDraft = { ...draft };
    const newRules = [...newDraft.rules];
    newRules[rowIdx] = { ...newRules[rowIdx], actionIndex: actionIdx };
    newDraft.rules = newRules;
    setDraft(newDraft);
    setModifiedCells(prev => new Set(prev).add(`action-${rowIdx}`));
  };

  // 修改条件阈值
  const updateConditionValue = (condIdx: number, value: any) => {
    const newDraft = { ...draft };
    const newConditions = [...newDraft.conditions];
    const old = newConditions[condIdx];
    newConditions[condIdx] = { ...old, value };
    newDraft.conditions = newConditions;
    setDraft(newDraft);
    setModifiedCells(prev => new Set(prev).add(`cond-${condIdx}`));
    tracker.record('sa_decision_table', table.id as any, `conditions[${condIdx}].value`, old.value, value);
  };

  // 保存
  const saveMutation = useMutation({
    mutationFn: async () => {
      await decisionTableApi.update(projectId, table.id, draft);
      await tracker.commit();
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['decision-table', projectId, table.id] });
      setModifiedCells(new Set());
      setShowDiff(false);
      alert('✓ 已保存,DKEE 已自动学习本次修改');
    },
  });

  return (
    <div className="bg-white rounded-lg border border-gray-200">
      {/* 顶部工具栏 */}
      <div className="px-4 py-3 border-b border-gray-200 flex items-center justify-between">
        <div>
          <h2 className="text-base font-semibold text-gray-900">
            判定表 {table.id}
          </h2>
          <p className="text-xs text-gray-500 mt-0.5">
            跨事件一致性:{table.cross_event_consistency ? '✅ 一致' : '⚠️ 冲突'} ·
            AI 信心度:{table.is_pattern_source ? '高' : '中'} ·
            规则数:{draft.rules.length}
          </p>
        </div>
        <div className="flex items-center gap-2">
          {modifiedCells.size > 0 && (
            <span className="text-xs text-yellow-700 bg-yellow-50 px-2 py-1 rounded">
              {modifiedCells.size} 处修改未保存
            </span>
          )}
          <button
            onClick={() => setShowDiff(!showDiff)}
            className="px-3 py-1.5 text-sm border border-gray-300 rounded hover:bg-gray-50"
          >
            {showDiff ? '隐藏' : '查看'}变更
          </button>
          <button
            onClick={() => saveMutation.mutate()}
            disabled={modifiedCells.size === 0 || saveMutation.isPending}
            className="px-3 py-1.5 text-sm bg-blue-600 text-white rounded hover:bg-blue-700 disabled:bg-gray-300"
          >
            {saveMutation.isPending ? '保存中...' : '保存并触发 DKEE 学习'}
          </button>
        </div>
      </div>

      {/* Diff 预览 */}
      {showDiff && modifiedCells.size > 0 && (
        <div className="px-4 py-3 bg-yellow-50 border-b border-yellow-200">
          <h3 className="text-sm font-semibold text-yellow-900 mb-2">本次变更:</h3>
          <ul className="text-xs text-yellow-800 space-y-1">
            {Array.from(modifiedCells).map(cellKey => {
              const [type, idx, sub] = cellKey.split('-');
              if (type === 'cond') {
                return (
                  <li key={cellKey}>
                    • 条件 [{draft.conditions[Number(idx)].name}] 阈值:{' '}
                    {JSON.stringify(table.conditions[Number(idx)].value)} → {JSON.stringify(draft.conditions[Number(idx)].value)}
                  </li>
                );
              }
              return <li key={cellKey}>• {cellKey} 已修改</li>;
            })}
          </ul>
        </div>
      )}

      {/* 判定表主体 */}
      <div className="overflow-x-auto">
        <table className="w-full text-sm">
          <thead>
            {/* 条件行 */}
            <tr className="bg-gray-50 border-b border-gray-200">
              <th className="px-3 py-2 text-left font-semibold text-gray-700 w-24 sticky left-0 bg-gray-50">规则</th>
              {draft.conditions.map((cond, condIdx) => (
                <th key={condIdx} className="px-3 py-2 text-left font-semibold text-gray-700 min-w-[180px]">
                  <div className="flex flex-col gap-1">
                    <span className="text-xs text-gray-500">条件 #{condIdx + 1}</span>
                    <span className="font-mono text-sm">{cond.name}</span>
                    <span className="text-xs text-gray-500">{cond.operator}</span>
                    <input
                      type="text"
                      value={JSON.stringify(cond.value)}
                      onChange={(e) => {
                        try {
                          updateConditionValue(condIdx, JSON.parse(e.target.value));
                        } catch {
                          // ignore invalid JSON
                        }
                      }}
                      className={`px-2 py-1 text-xs font-mono border rounded mt-1 ${
                        modifiedCells.has(`cond-${condIdx}`) ? 'cell-human' : 'cell-ai'
                      }`}
                    />
                  </div>
                </th>
              ))}
              <th className="px-3 py-2 text-left font-semibold text-gray-700 min-w-[200px]">动作</th>
            </tr>
          </thead>
          <tbody>
            {draft.rules.map((rule, rowIdx) => {
              const action = draft.actions[rule.actionIndex];
              return (
                <tr key={rowIdx} className="border-b border-gray-100 hover:bg-gray-50">
                  <td className="px-3 py-2 font-mono text-xs text-gray-500 sticky left-0 bg-white">
                    R{rowIdx + 1}
                  </td>
                  {draft.conditions.map((_, condIdx) => {
                    const cellKey = `${rowIdx}-${condIdx}`;
                    const isModified = modifiedCells.has(cellKey);
                    return (
                      <td key={condIdx} className="px-2 py-2">
                        <select
                          value={rule.conditionMask[condIdx] ? '✓' : '✗'}
                          onChange={(e) => updateCell(rowIdx, condIdx, e.target.value === '✓')}
                          className={`w-full px-2 py-1 rounded text-sm font-bold ${
                            isModified ? 'cell-human' : rule.conditionMask[condIdx] ? 'cell-ai' : ''
                          }`}
                        >
                          <option value="✓">✓ 真</option>
                          <option value="✗">✗ 假</option>
                        </select>
                      </td>
                    );
                  })}
                  <td className="px-2 py-2">
                    <select
                      value={rule.actionIndex}
                      onChange={(e) => updateAction(rowIdx, Number(e.target.value))}
                      className={`w-full px-2 py-1 rounded text-sm ${
                        modifiedCells.has(`action-${rowIdx}`) ? 'cell-human' : 'cell-ai'
                      }`}
                    >
                      {draft.actions.map((act, actIdx) => (
                        <option key={actIdx} value={actIdx}>
                          {act.name}
                        </option>
                      ))}
                    </select>
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>

      {/* 底部说明 */}
      <div className="px-4 py-3 bg-gray-50 border-t border-gray-200 text-xs text-gray-600 flex items-center gap-4">
        <span className="flex items-center gap-1">
          <span className="w-3 h-3 cell-ai rounded"></span> AI 生成
        </span>
        <span className="flex items-center gap-1">
          <span className="w-3 h-3 cell-human rounded"></span> 人类修改
        </span>
        <span className="ml-auto text-gray-500">
          提示:每次保存后,DKEE 会自动学习你的修改模式,下次 AI 会更准
        </span>
      </div>
    </div>
  );
}
