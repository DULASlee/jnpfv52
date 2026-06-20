// 项目看板
import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { projectApi } from '../api/saApi';
import { ValidationBadge } from '../components/ValidationBadge';

export function DashboardPage() {
  const { data: projects, isLoading } = useQuery({
    queryKey: ['projects'],
    queryFn: projectApi.list,
  });

  if (isLoading) return <div className="text-center py-8 text-gray-500">加载中...</div>;

  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <h2 className="text-2xl font-bold text-gray-900">项目看板</h2>
        <button
          onClick={() => {
            const requirementText = prompt('请输入客户需求:');
            if (requirementText) {
              const tenantId = 'tenant_001';
              const userId = localStorage.getItem('userId') || 'user_123';
              projectApi.create({ tenantId, requirementText, userId });
            }
          }}
          className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700"
        >
          + 新建需求分析
        </button>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
        {projects?.map(p => (
          <Link
            key={p.projectId}
            to={`/projects/${p.projectId}`}
            className="block bg-white rounded-lg border border-gray-200 p-4 hover:border-blue-400 hover:shadow-sm transition-all"
          >
            <div className="flex items-start justify-between mb-2">
              <div>
                <div className="text-xs text-gray-500">项目 #{p.projectId}</div>
                <h3 className="text-sm font-semibold text-gray-900 mt-1 line-clamp-2">
                  {p.requirementText.slice(0, 60)}...
                </h3>
              </div>
              <span className={`px-2 py-0.5 rounded text-xs ${
                p.status === 'completed' ? 'bg-green-100 text-green-700' :
                p.status === 'awaiting_review' ? 'bg-yellow-100 text-yellow-700' :
                'bg-blue-100 text-blue-700'
              }`}>
                {p.status === 'completed' ? '已完成' : p.status === 'awaiting_review' ? '待 review' : '分析中'}
              </span>
            </div>
            <div className="mt-3 flex flex-wrap gap-1">
              <ValidationBadge status={p.validationStats.scope} />
              <ValidationBadge status={p.validationStats.dfd} />
              <ValidationBadge status={p.validationStats.dict} />
              <ValidationBadge status={p.validationStats.decisionTable} />
            </div>
            <div className="mt-3 text-xs text-gray-400">
              更新于 {new Date(p.updatedAt).toLocaleString('zh-CN')}
            </div>
          </Link>
        ))}
      </div>
    </div>
  );
}
