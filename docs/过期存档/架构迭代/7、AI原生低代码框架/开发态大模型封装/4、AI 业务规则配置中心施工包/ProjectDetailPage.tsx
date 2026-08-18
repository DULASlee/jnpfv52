// 项目详情 - 跳转到各 SA 步骤
import { useParams, Link, useNavigate } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { projectApi, decisionTableApi, dictApi, stateMachineApi } from '../api/saApi';
import { ValidationBadge } from '../components/ValidationBadge';

export function ProjectDetailPage() {
  const { projectId } = useParams<{ projectId: string }>();
  const pid = Number(projectId);
  const navigate = useNavigate();

  const { data: project } = useQuery({
    queryKey: ['project', pid],
    queryFn: () => projectApi.get(pid),
    enabled: !!pid,
  });

  const { data: dict } = useQuery({
    queryKey: ['dict', pid],
    queryFn: () => dictApi.get(pid),
    enabled: !!pid,
  });

  const { data: decisionTables } = useQuery({
    queryKey: ['decision-tables', pid],
    queryFn: () => decisionTableApi.list(pid),
    enabled: !!pid,
  });

  const { data: stateMachines } = useQuery({
    queryKey: ['state-machines', pid],
    queryFn: () => stateMachineApi.get(pid),
    enabled: !!pid,
  });

  if (!project) return <div>加载中...</div>;

  const steps = [
    { key: 'scope', label: 'Step 1: 边界与事件', path: `/projects/${pid}/scope` },
    { key: 'dfd', label: 'Step 2: DFD 分层', path: `/projects/${pid}/dfd` },
    { key: 'bpm', label: 'Step 3: 业务流程图', path: `/projects/${pid}/bpm` },
    { key: 'dict', label: 'Step 4: 数据字典 ★', path: `/projects/${pid}/dictionary` },
    { key: 'pspec', label: 'Step 5: PSPEC', path: `/projects/${pid}/pspec` },
    { key: 'decisionTable', label: 'Step 6: 判定表 ★★', path: `/projects/${pid}/decision-tables` },
    { key: 'er', label: 'Step 7: ER 图', path: `/projects/${pid}/er` },
    { key: 'std', label: 'Step 8: 状态机', path: `/projects/${pid}/state-machines` },
    { key: 'ui', label: 'Step 9: UI 原型', path: `/projects/${pid}/ui` },
  ];

  return (
    <div>
      {/* 项目头 */}
      <div className="bg-white rounded-lg border border-gray-200 p-6 mb-6">
        <div className="flex items-start justify-between">
          <div className="flex-1">
            <div className="text-xs text-gray-500">项目 #{pid} · {project.tenantId}</div>
            <h2 className="text-xl font-bold text-gray-900 mt-1">{project.requirementText}</h2>
          </div>
          <button
            onClick={async () => {
              const { dkeeApi } = await import('../api/saApi');
              await dkeeApi.triggerExtraction(pid);
              alert('DKEE 提炼已触发');
            }}
            className="px-3 py-1.5 text-sm border border-purple-300 text-purple-700 rounded hover:bg-purple-50"
          >
            触发 DKEE 提炼
          </button>
        </div>
      </div>

      {/* 9 步导航 */}
      <h3 className="text-lg font-semibold text-gray-900 mb-3">9 步 SA 流水线</h3>
      <div className="grid grid-cols-1 md:grid-cols-3 gap-3 mb-6">
        {steps.map(step => {
          const status = project.validationStats[step.key as keyof typeof project.validationStats];
          return (
            <Link
              key={step.key}
              to={step.path}
              className="flex items-center justify-between bg-white rounded-lg border border-gray-200 p-4 hover:border-blue-400 hover:shadow-sm"
            >
              <span className="text-sm font-medium text-gray-900">{step.label}</span>
              {status && <ValidationBadge status={status} />}
            </Link>
          );
        })}
      </div>

      {/* 关键数据预览 */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-4">
        {/* 字典摘要 */}
        <div className="bg-white rounded-lg border border-gray-200 p-4">
          <h4 className="text-sm font-semibold text-gray-900 mb-2">
            数据字典
            {dict?.human_confirmed && <span className="ml-2 text-xs text-green-600">✓ 已确认</span>}
          </h4>
          <div className="text-2xl font-bold text-gray-900">{dict?.elements.length || 0}</div>
          <div className="text-xs text-gray-500 mt-1">字段总数</div>
          {dict?.is_pattern_source && (
            <div className="mt-2 text-xs text-purple-600">★ 已标记为 Pattern 来源</div>
          )}
        </div>

        {/* 判定表 */}
        <div className="bg-white rounded-lg border border-gray-200 p-4">
          <h4 className="text-sm font-semibold text-gray-900 mb-2">判定表</h4>
          <div className="text-2xl font-bold text-gray-900">{decisionTables?.length || 0}</div>
          <div className="text-xs text-gray-500 mt-1">张</div>
        </div>

        {/* 状态机 */}
        <div className="bg-white rounded-lg border border-gray-200 p-4">
          <h4 className="text-sm font-semibold text-gray-900 mb-2">状态机</h4>
          <div className="text-2xl font-bold text-gray-900">{stateMachines?.length || 0}</div>
          <div className="text-xs text-gray-500 mt-1">个实体</div>
        </div>
      </div>
    </div>
  );
}
