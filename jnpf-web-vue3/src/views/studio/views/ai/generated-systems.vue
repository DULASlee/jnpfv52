<template>
  <div class="generated-systems">
    <div class="page-header">
      <h2>已生成系统</h2>
      <div class="filters">
        <a-input-search v-model:value="keyword" placeholder="搜索系统名称..." style="width: 240px" @search="loadList" />
        <a-select v-model:value="statusFilter" style="width: 140px" @change="loadList">
          <a-select-option value="">全部状态</a-select-option>
          <a-select-option value="requirement">需求分析</a-select-option>
          <a-select-option value="architecture">架构设计</a-select-option>
          <a-select-option value="design">总体设计</a-select-option>
          <a-select-option value="development">自动开发</a-select-option>
          <a-select-option value="delivery">交付验证</a-select-option>
          <a-select-option value="completed">已完成</a-select-option>
        </a-select>
      </div>
    </div>

    <a-spin :spinning="loading">
      <div v-if="list.length === 0" class="empty">暂无已生成系统（创建流水线后会自动出现在此）</div>
      <div v-else class="card-grid">
        <div v-for="item in filteredList" :key="item.id" class="system-card" :class="{ unread: (item.updateCount ?? 0) > 0 && !item.isRead }">
          <div class="card-header">
            <strong>{{ item.projectName }}</strong>
            <span v-if="(item.updateCount ?? 0) > 0 && !item.isRead" class="badge">
              {{ (item.updateCount ?? 0) > 99 ? '99+' : item.updateCount }}
            </span>
          </div>
          <div class="stage-row">
            <span class="stage-tag" :class="stageClass(item)">
              {{ stageLabel(item) }}
            </span>
            <span class="time">{{ formatTime(item.createTime) }}</span>
          </div>
          <p v-if="item.description" class="desc">{{ item.description.slice(0, 80) }}</p>
          <div class="actions">
            <a-button size="small" type="primary" @click="continueChat(item.id)"> 继续对话 </a-button>
            <a-button v-if="item.sandboxUrl" size="small" @click="openUrl(item.sandboxUrl)"> 沙箱试用 </a-button>
            <a-button v-if="item.sourceZipUrl" size="small" @click="openUrl(item.sourceZipUrl)"> 下载源码 </a-button>
          </div>
        </div>
      </div>
      <div class="pagination" v-if="total > pageSize">
        <a-pagination v-model:current="page" :total="total" :page-size="pageSize" @change="loadList" />
      </div>
    </a-spin>
  </div>
</template>

<script setup lang="ts">
  import { computed, onMounted, ref } from 'vue';
  import { useRouter } from 'vue-router';
  import { defHttp } from '/@/utils/http/axios';
  import { getPipelineList } from '../../api/studio/pipeline';

  interface ProjectItem {
    id: number;
    projectName: string;
    description?: string;
    currentStage: number;
    pipelineStatus?: string;
    sandboxUrl?: string;
    sourceZipUrl?: string;
    createTime?: string;
    updateCount?: number;
    isRead?: boolean;
  }

  const router = useRouter();
  const list = ref<ProjectItem[]>([]);
  const loading = ref(false);
  const total = ref(0);
  const page = ref(1);
  const pageSize = ref(12);
  const keyword = ref('');
  const statusFilter = ref('');

  const STAGE_LABELS: Record<string, string> = {
    requirement: '需求分析',
    architecture: '架构设计',
    design: '总体设计',
    development: '自动开发',
    delivery: '交付验证',
    completed: '已完成',
  };

  const filteredList = computed(() => {
    let rows = list.value;
    if (keyword.value) {
      const kw = keyword.value.toLowerCase();
      rows = rows.filter(r => r.projectName?.toLowerCase().includes(kw));
    }
    if (statusFilter.value) {
      rows = rows.filter(r => (r.pipelineStatus || '') === statusFilter.value);
    }
    return rows;
  });

  async function loadList() {
    loading.value = true;
    try {
      const res: any = await defHttp.get({
        url: '/api/studio/ai/project/list',
        params: { page: page.value, pageSize: pageSize.value },
      });
      const data = res?.data ?? res;
      let items: ProjectItem[] = (data?.items || []).map((p: any) => ({
        id: p.id ?? p.F_Id,
        projectName: p.projectName ?? p.F_ProjectName ?? '未命名',
        description: p.description ?? p.F_Description,
        currentStage: p.currentStage ?? p.F_CurrentStage ?? 1,
        pipelineStatus: p.pipelineStatus ?? p.F_PipelineStatus,
        sandboxUrl: p.sandboxUrl ?? p.F_SandboxUrl,
        sourceZipUrl: p.sourceZipUrl ?? p.F_SourceZipUrl,
        createTime: p.createTime ?? p.F_CreatorTime,
        updateCount: p.updateCount ?? p.F_UpdateCount ?? 0,
        isRead: p.isRead ?? p.F_IsRead ?? true,
      }));

      if (items.length === 0) {
        const pipelines = await getPipelineList(0, 50);
        const pl = Array.isArray(pipelines) ? pipelines : (pipelines as any)?.data ?? [];
        items = pl.map((p: any) => ({
          id: p.id,
          projectName: p.name || `流水线 #${p.id}`,
          description: p.name,
          currentStage: 0,
          pipelineStatus: p.currentStage,
          createTime: p.updatedAt,
          updateCount: 0,
          isRead: true,
        }));
      }

      list.value = items;
      total.value = data?.total ?? items.length;
    } catch {
      list.value = [];
    } finally {
      loading.value = false;
    }
  }

  async function continueChat(id: number) {
    try {
      await defHttp.post({ url: `/api/studio/ai/project/${id}/mark-read` });
    } catch {
      /* ignore */
    }
    router.push({ path: '/studio/ai/submit-requirement', query: { pipelineId: String(id) } });
  }

  function openUrl(url: string) {
    window.open(url.startsWith('http') ? url : window.location.origin + url, '_blank');
  }

  function stageLabel(item: ProjectItem): string {
    if (item.pipelineStatus && STAGE_LABELS[item.pipelineStatus]) {
      return STAGE_LABELS[item.pipelineStatus];
    }
    const labels = ['', '需求分析', '架构设计', '总体设计', '自动开发', '交付验证'];
    return labels[item.currentStage] || '进行中';
  }

  function stageClass(item: ProjectItem): string {
    const code = item.pipelineStatus || '';
    if (code === 'delivery' || item.currentStage >= 5) return 'completed';
    if (code === 'development' || item.currentStage === 4) return 'dev';
    return '';
  }

  function formatTime(t?: string): string {
    if (!t) return '';
    return new Date(t).toLocaleDateString();
  }

  onMounted(loadList);
</script>

<style scoped lang="less">
  .generated-systems {
    max-width: 1100px;
    margin: 0 auto;
    padding: 24px;
  }

  .page-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 20px;

    h2 {
      margin: 0;
    }

    .filters {
      display: flex;
      gap: 12px;
    }
  }

  .empty {
    text-align: center;
    color: #999;
    padding: 48px;
  }

  .card-grid {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
    gap: 16px;
  }

  .system-card {
    background: #fff;
    border: 1px solid #f0f0f0;
    border-radius: 8px;
    padding: 16px;

    &.unread {
      border-color: #91d5ff;
    }

    .card-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 8px;
    }

    .badge {
      background: #ff4d4f;
      color: #fff;
      font-size: 11px;
      padding: 0 6px;
      border-radius: 10px;
    }

    .stage-row {
      display: flex;
      justify-content: space-between;
      font-size: 12px;
      margin-bottom: 8px;

      .stage-tag {
        padding: 2px 8px;
        border-radius: 4px;
        background: #f5f5f5;

        &.dev {
          background: #e6f7ff;
          color: #1890ff;
        }

        &.completed {
          background: #f6ffed;
          color: #52c41a;
        }
      }

      .time {
        color: #bbb;
      }
    }

    .desc {
      font-size: 12px;
      color: #666;
      margin-bottom: 12px;
    }

    .actions {
      display: flex;
      flex-wrap: wrap;
      gap: 8px;
    }
  }

  .pagination {
    margin-top: 24px;
    text-align: center;
  }
</style>
