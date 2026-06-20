// 判定表编辑页
import { useParams } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { decisionTableApi } from '../api/saApi';
import { DecisionTableEditor } from '../components/DecisionTableEditor';

export function DecisionTableEditPage() {
  const { projectId, tableId } = useParams<{ projectId: string; tableId: string }>();
  const pid = Number(projectId);
  const userId = localStorage.getItem('userId') || 'user_123';

  const { data: tables } = useQuery({
    queryKey: ['decision-tables', pid],
    queryFn: () => decisionTableApi.list(pid),
    enabled: !!pid,
  });

  const table = tables?.find(t => t.id === tableId);

  if (!table) {
    return <div className="text-center py-8 text-gray-500">未找到判定表 {tableId}</div>;
  }

  return (
    <div>
      <div className="mb-4 flex items-center justify-between">
        <h2 className="text-2xl font-bold text-gray-900">判定表编辑</h2>
        <div className="text-sm text-gray-500">项目 #{pid}</div>
      </div>
      <DecisionTableEditor projectId={pid} table={table} userId={userId} />
    </div>
  );
}
