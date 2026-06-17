<template>
  <div class="submit-requirement">
    <div class="page-header">
      <h2>提交需求</h2>
      <a-button size="small" @click="showHistory = !showHistory">
        {{ showHistory ? '收起' : '历史记录' }}
      </a-button>
    </div>

    <div class="form-section">
      <a-textarea
        v-model:value="requirementText"
        :auto-size="{ minRows: 4, maxRows: 10 }"
        placeholder="请描述你的业务需求，例如：&#10;'我需要一个进销存管理系统，支持多仓库、多供应商，包含库存盘点、采购订单、销售订单等功能'" />
      <AttachmentUpload @update:files="onFiles" />
      <div v-if="industryContext" class="industry-hint">
        <span>🏭 当前行业上下文: {{ industryContext }}</span>
      </div>
      <div class="submit-row">
        <a-button type="primary" size="large" :loading="submitting" :disabled="!requirementText.trim()" @click="handleSubmit">
          提交需求，开始 AI 分析
        </a-button>
      </div>
    </div>

    <div v-if="showHistory" class="history-section">
      <h3>历史需求</h3>
      <div v-if="historyLoading" class="loading">加载中...</div>
      <div v-else-if="historyList.length === 0" class="empty">暂无历史需求</div>
      <div v-for="item in historyList" :key="item.id" class="history-card">
        <div class="card-main">
          <strong>{{ item.projectName }}</strong>
          <span class="stage-tag" :class="stageClass(item.pipelineStatus)">
            {{ stageLabel(item.currentStage) }}
          </span>
        </div>
        <div class="card-meta">{{ item.description || '暂无描述' }}</div>
        <div class="card-actions">
          <a-button size="small" type="link" @click="continuePipeline(item.id)"> 继续对话 </a-button>
          <a-button size="small" type="link" @click="viewDetail(item.id)">查看详情</a-button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
  import { ref, onMounted } from 'vue';
  import { useRouter } from 'vue-router';
  import { defHttp } from '/@/utils/http/axios';
  import AttachmentUpload from '../../components/chat/AttachmentUpload.vue';

  const router = useRouter();
  const requirementText = ref('');
  const attachedFiles = ref<File[]>([]);
  const submitting = ref(false);
  const showHistory = ref(false);
  const historyList = ref<any[]>([]);
  const historyLoading = ref(false);
  const industryContext = ref('');

  function onFiles(files: File[]) {
    attachedFiles.value = files;
  }

  async function handleSubmit() {
    if (!requirementText.value.trim()) return;
    submitting.value = true;
    try {
      const res: any = await defHttp.post({
        url: '/api/founder/ai/pipeline/create',
        data: {
          name: requirementText.value.slice(0, 50) || '新项目',
          description: requirementText.value,
        },
      });
      const pid = res?.data?.PipelineId || res?.data?.pipelineId || res?.PipelineId;
      if (pid) {
        router.push(`/studio/expert/my-projects/${pid}`);
      }
    } catch {
      // Fallback
      router.push('/studio/expert/my-projects/1');
    } finally {
      submitting.value = false;
    }
  }

  async function loadHistory() {
    historyLoading.value = true;
    try {
      const res: any = await defHttp.get({ url: '/api/studio/ai/project/list', data: { page: 1, pageSize: 10 } });
      historyList.value = res?.data?.items || res?.data || [];
    } catch {
      historyList.value = [];
    } finally {
      historyLoading.value = false;
    }
  }

  function continuePipeline(id: number) {
    router.push(`/studio/expert/my-projects/${id}`);
  }

  function viewDetail(_id: number) {
    router.push('/studio/ai/generated-systems');
  }

  function stageLabel(stage: number): string {
    const labels = ['', '需求分析', '架构设计', '总体设计', '自动开发', '交付'];
    return labels[stage] || `阶段${stage}`;
  }

  function stageClass(status: string) {
    return { completed: 'completed', failed: 'failed' }[status] || '';
  }

  onMounted(loadHistory);
</script>

<style scoped lang="less">
  .submit-requirement {
    max-width: 900px;
    margin: 0 auto;
    padding: 24px;

    .page-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 20px;
      h2 {
        margin: 0;
        font-size: 20px;
      }
    }

    .form-section {
      background: #fff;
      border-radius: 8px;
      padding: 24px;
      box-shadow: 0 1px 3px rgba(0, 0, 0, 0.06);

      .industry-hint {
        margin-top: 12px;
        padding: 8px 12px;
        background: #fff7e6;
        border-radius: 4px;
        font-size: 13px;
        color: #d48806;
      }

      .submit-row {
        margin-top: 20px;
        text-align: center;
      }
    }

    .history-section {
      margin-top: 32px;

      h3 {
        font-size: 16px;
        margin-bottom: 12px;
      }
      .loading,
      .empty {
        text-align: center;
        color: #999;
        padding: 20px;
      }

      .history-card {
        background: #fff;
        border-radius: 6px;
        padding: 12px 16px;
        margin-bottom: 8px;
        border: 1px solid #f0f0f0;
        transition: box-shadow 0.2s;

        &:hover {
          box-shadow: 0 2px 8px rgba(0, 0, 0, 0.08);
        }

        .card-main {
          display: flex;
          justify-content: space-between;
          align-items: center;
          .stage-tag {
            font-size: 11px;
            padding: 2px 8px;
            border-radius: 4px;
            background: #e6f7ff;
            color: #1890ff;
            &.completed {
              background: #f6ffed;
              color: #52c41a;
            }
            &.failed {
              background: #fff2f0;
              color: #ff4d4f;
            }
          }
        }
        .card-meta {
          font-size: 12px;
          color: #888;
          margin-top: 4px;
        }
        .card-actions {
          margin-top: 8px;
          display: flex;
          gap: 8px;
        }
      }
    }
  }
</style>
