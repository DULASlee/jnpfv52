// 路由配置
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { Layout } from './components/Layout';
import { DashboardPage } from './pages/DashboardPage';
import { ProjectDetailPage } from './pages/ProjectDetailPage';
import { DecisionTableEditPage } from './pages/DecisionTableEditPage';
import { DKEEReviewPage } from './pages/DKEEReviewPage';

const queryClient = new QueryClient({
  defaultOptions: {
    queries: { staleTime: 30_000, refetchOnWindowFocus: false },
  },
});

export default function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <Routes>
          <Route path="/" element={<Layout />}>
            <Route index element={<DashboardPage />} />
            <Route path="projects/:projectId" element={<ProjectDetailPage />} />
            <Route path="projects/:projectId/decision-tables" element={<DecisionTableListRedirect />} />
            <Route path="projects/:projectId/decision-tables/:tableId" element={<DecisionTableEditPage />} />
            <Route path="patterns" element={<DKEEReviewPage />} />
            <Route path="changes" element={<div className="text-gray-500">修改日志 - 待实现</div>} />
            <Route path="*" element={<Navigate to="/" replace />} />
          </Route>
        </Routes>
      </BrowserRouter>
    </QueryClientProvider>
  );
}

// 简易跳转:取第一张判定表
function DecisionTableListRedirect() {
  // 实际项目应该列出所有判定表,这里简化
  return <Navigate to="." replace />;
}
