<template>
  <div class="jnpf-content-wrapper">
    <div class="jnpf-content-wrapper-center">
      <div class="jnpf-content-wrapper-content">
        <!-- 页面头部 -->
        <a-page-header title="AI Studio" sub-title="Baobab-Studio Phase 1 — 五阶段流水线" :ghost="false">
          <template #extra>
            <a-space>
              <a-tag color="processing">Sprint 0-B</a-tag>
              <a-button type="primary" size="small" @click="handleTestHealth">
                <template #icon>
                  <ThunderboltOutlined />
                </template>
                健康检查
              </a-button>
            </a-space>
          </template>
        </a-page-header>

        <!-- 流水线状态面板占位 -->
        <a-row :gutter="16" style="margin-top: 16px">
          <a-col :span="8">
            <a-card class="stat-card" :bordered="false">
              <template #title>
                <span>
                  <NodeIndexOutlined />
                  流水线状态
                </span>
              </template>
              <a-empty description="流水线面板将在 Phase 2 激活" :image="aEmpty.PRESENTED_IMAGE_SIMPLE" />
            </a-card>
          </a-col>

          <a-col :span="8">
            <a-card class="stat-card" :bordered="false">
              <template #title>
                <span>
                  <ApiOutlined />
                  Provider 连通性
                </span>
              </template>
              <a-skeleton active :loading="healthLoading" :paragraph="{ rows: 2 }">
                <a-descriptions v-if="healthResult" size="small" :column="1" bordered>
                  <a-descriptions-item label="状态">
                    <a-badge :status="healthResult.isHealthy ? 'success' : 'error'" :text="healthResult.isHealthy ? '正常' : '异常'" />
                  </a-descriptions-item>
                  <a-descriptions-item label="Provider">
                    {{ healthResult.provider }}
                  </a-descriptions-item>
                  <a-descriptions-item label="延迟"> {{ healthResult.latencyMs }}ms </a-descriptions-item>
                  <a-descriptions-item v-if="healthResult.error" label="错误">
                    <a-typography-text type="danger">
                      {{ healthResult.error }}
                    </a-typography-text>
                  </a-descriptions-item>
                </a-descriptions>
                <a-empty v-else description="点击上方「健康检查」按钮测试" :image="aEmpty.PRESENTED_IMAGE_SIMPLE" />
              </a-skeleton>
            </a-card>
          </a-col>

          <a-col :span="8">
            <a-card class="stat-card" :bordered="false">
              <template #title>
                <span>
                  <DatabaseOutlined />
                  知识图谱
                </span>
              </template>
              <a-empty description="知识图谱面板将在 Phase 2 激活" :image="aEmpty.PRESENTED_IMAGE_SIMPLE" />
            </a-card>
          </a-col>
        </a-row>

        <!-- Prompt 模板占位 -->
        <a-card class="stat-card" :bordered="false" style="margin-top: 16px">
          <template #title>
            <span>
              <FileTextOutlined />
              Prompt 模板库
            </span>
          </template>
          <a-empty description="Prompt 模板管理将在 Phase 2 接入" :image="aEmpty.PRESENTED_IMAGE_SIMPLE" />
        </a-card>
      </div>
    </div>
  </div>
</template>

<script lang="ts" setup>
  import { ref } from 'vue';
  import { ThunderboltOutlined, NodeIndexOutlined, ApiOutlined, DatabaseOutlined, FileTextOutlined } from '@ant-design/icons-vue';
  import { Empty as AEmpty } from 'ant-design-vue';
  import { useMessage } from '/@/hooks/web/useMessage';
  import type { ProviderHealth } from '/@/ai/gateway/types';

  const aEmpty = AEmpty;
  const { createMessage } = useMessage();

  /** 健康检查状态 */
  const healthLoading = ref(false);
  const healthResult = ref<ProviderHealth | null>(null);

  /** 测试 Provider 健康检查 */
  async function handleTestHealth() {
    healthLoading.value = true;
    try {
      // Phase 2: 替换为真实 LlmGatewayService API 调用
      // const { data } = await apiGet('/api/InteAssistant/LlmGateway/HealthCheck');

      // 模拟健康检查（地桩）
      await new Promise(resolve => setTimeout(resolve, 500));
      healthResult.value = {
        isHealthy: true,
        provider: 'stub',
        latencyMs: Math.floor(Math.random() * 100),
      };
      createMessage.success('健康检查通过');
    } catch {
      healthResult.value = {
        isHealthy: false,
        provider: 'stub',
        latencyMs: 0,
        error: '连接超时',
      };
      createMessage.error('健康检查失败');
    } finally {
      healthLoading.value = false;
    }
  }
</script>

<style lang="less" scoped>
  :deep(.ant-page-header) {
    background: #fff;
    border-radius: 8px;
    padding: 16px 24px;
  }

  .stat-card {
    min-height: 240px;
    border-radius: 8px;
    box-shadow: 0 1px 3px rgba(0, 0, 0, 0.06), 0 1px 2px rgba(0, 0, 0, 0.04);
    transition: box-shadow 0.2s ease;

    &:hover {
      box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
    }
  }
</style>
