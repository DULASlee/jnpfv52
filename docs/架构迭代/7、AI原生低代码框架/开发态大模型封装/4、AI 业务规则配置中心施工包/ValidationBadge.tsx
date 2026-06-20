// 验证状态徽章
import { ValidationStatus } from '../types/sa';

export function ValidationBadge({ status }: { status: ValidationStatus }) {
  const config = {
    PASS: { color: 'bg-green-100 text-green-700 border-green-200', label: '通过' },
    FAIL: { color: 'bg-red-100 text-red-700 border-red-200', label: '失败' },
    PENDING: { color: 'bg-gray-100 text-gray-600 border-gray-200', label: '待校验' },
  }[status];

  return (
    <span className={`inline-flex items-center gap-1 px-2 py-0.5 rounded text-xs font-medium border ${config.color}`}>
      <span className={`w-1.5 h-1.5 rounded-full ${
        status === 'PASS' ? 'bg-green-500' : status === 'FAIL' ? 'bg-red-500' : 'bg-gray-400'
      }`} />
      {config.label}
    </span>
  );
}

// 来源徽章(AI 生成 vs 人类修改)
export function SourceBadge({ source }: { source: 'ai' | 'human' }) {
  if (source === 'ai') {
    return (
      <span className="inline-flex items-center px-1.5 py-0.5 rounded text-xs font-medium bg-green-100 text-green-700">
        AI
      </span>
    );
  }
  return (
    <span className="inline-flex items-center px-1.5 py-0.5 rounded text-xs font-medium bg-yellow-100 text-yellow-700">
        人改
      </span>
  );
}
