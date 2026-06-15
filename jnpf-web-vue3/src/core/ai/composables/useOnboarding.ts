/**
 * 新手引导 composable（D-8: 2026-06-20）。
 *
 * 使用 Ant Design Vue a-tour 组件，3 步引导业务专家。
 * localStorage 存储完成状态，已完成则不再弹出。
 */
import { ref } from 'vue';

const STORAGE_KEY = 'studio-onboarding-completed';

export interface TourStep {
  target: string | (() => HTMLElement | null);
  title: string;
  description: string;
  placement?: 'top' | 'bottom' | 'left' | 'right';
}

export function useOnboarding() {
  const isActive = ref(false);
  const currentStep = ref(0);

  const steps: TourStep[] = [
    {
      target: '.quick-app-entry .ant-input',
      title: '第一步：描述您的需求',
      description: '用自然语言描述您想要的系统。例如："我需要一个客户管理系统，能记录客户信息和跟进状态"',
      placement: 'bottom',
    },
    {
      target: '.pipeline-manager .pm-context-bar',
      title: '第二步：AI 自动完成全流程',
      description: 'AI 将自动完成需求分析→架构设计→详细设计→代码生成→部署预览，您只需在关键节点确认即可',
      placement: 'top',
    },
    {
      target: '.pm-right, .preview-panel',
      title: '第三步：预览您的应用',
      description: '生成完成后可在此预览应用，也可下载代码部署到正式环境',
      placement: 'left',
    },
  ];

  function start() {
    if (hasCompleted()) return;
    currentStep.value = 0;
    isActive.value = true;
  }

  function next() {
    if (currentStep.value < steps.length - 1) {
      currentStep.value++;
    } else {
      complete();
    }
  }

  function prev() {
    if (currentStep.value > 0) currentStep.value--;
  }

  function skip() {
    complete();
  }

  function complete() {
    isActive.value = false;
    localStorage.setItem(STORAGE_KEY, 'true');
  }

  function hasCompleted(): boolean {
    return localStorage.getItem(STORAGE_KEY) === 'true';
  }

  function reset() {
    localStorage.removeItem(STORAGE_KEY);
  }

  return { isActive, currentStep, steps, start, next, prev, skip, complete, hasCompleted, reset };
}
