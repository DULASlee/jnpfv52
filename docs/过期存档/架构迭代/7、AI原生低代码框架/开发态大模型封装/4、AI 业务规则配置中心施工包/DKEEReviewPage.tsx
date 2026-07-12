// 知识图谱 Pattern 审查页
import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { dkeeApi } from '../api/saApi';
import { KGPattern } from '../types/sa';

export function DKEEReviewPage() {
  const [industry, setIndustry] = useState('manufacturing');
  const { data: patterns, isLoading } = useQuery({
    queryKey: ['kg-patterns', industry],
    queryFn: () => dkeeApi.listPatterns(industry),
  });

  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <h2 className="text-2xl font-bold text-gray-900">知识图谱 Pattern</h2>
        <div className="flex items-center gap-2">
          <span className="text-sm text-gray-600">行业:</span>
          <select
            value={industry}
            onChange={(e) => setIndustry(e.target.value)}
            className="px-3 py-1.5 border border-gray-300 rounded"
          >
            <option value="manufacturing">制造业 (manufacturing)</option>
            <option value="ecommerce">电商 (ecommerce)</option>
            <option value="optical">眼镜店 (optical)</option>
            <option value="general">通用 (general)</option>
          </select>
        </div>
      </div>

      {isLoading ? (
        <div className="text-center py-8 text-gray-500">加载中...</div>
      ) : (
        <div className="space-y-3">
          {patterns?.map(p => (
            <div key={p.id} className="bg-white rounded-lg border border-gray-200 p-4">
              <div className="flex items-start justify-between mb-2">
                <div>
                  <div className="flex items-center gap-2">
                    <span className={`px-2 py-0.5 rounded text-xs font-medium ${
                      p.pattern_type === 'field_naming' ? 'bg-blue-100 text-blue-700' :
                      p.pattern_type === 'decision_rule' ? 'bg-purple-100 text-purple-700' :
                      'bg-green-100 text-green-700'
                    }`}>
                      {p.pattern_type}
                    </span>
                    <span className="text-sm text-gray-500">Pattern #{p.id}</span>
                    <span className="text-xs text-gray-400">来源: {p.source}</span>
                  </div>
                </div>
                <div className="text-right">
                  <div className={`text-2xl font-bold ${
                    p.score >= 0.8 ? 'text-green-600' : p.score >= 0.6 ? 'text-yellow-600' : 'text-red-600'
                  }`}>
                    {p.score.toFixed(2)}
                  </div>
                  <div className="text-xs text-gray-500">评分</div>
                </div>
              </div>

              {/* 评分条 */}
              <div className="w-full bg-gray-200 rounded-full h-1.5 mb-3">
                <div
                  className={`h-1.5 rounded-full ${
                    p.score >= 0.8 ? 'bg-green-500' : p.score >= 0.6 ? 'bg-yellow-500' : 'bg-red-500'
                  }`}
                  style={{ width: `${p.score * 100}%` }}
                />
              </div>

              {/* Pattern 内容预览 */}
              <pre className="bg-gray-50 rounded p-3 text-xs overflow-x-auto">
                {JSON.stringify(p.pattern_content, null, 2).slice(0, 400)}
                {JSON.stringify(p.pattern_content, null, 2).length > 400 && '...'}
              </pre>

              <div className="mt-2 text-xs text-gray-500">
                使用次数: {p.usage_count} · 状态: {p.is_active ? '✅ 激活' : '❌ 停用'}
              </div>
            </div>
          ))}
          {patterns?.length === 0 && (
            <div className="text-center py-8 text-gray-400">暂无 Pattern,请先完成几个项目分析</div>
          )}
        </div>
      )}
    </div>
  );
}
