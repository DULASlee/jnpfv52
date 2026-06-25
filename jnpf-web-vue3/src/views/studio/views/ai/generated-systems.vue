<template>
  <div class="generated-systems">
    <div class="page-header">
      <h2>已生成系统</h2>
      <div class="filters">
        <a-input-search v-model:value="keyword" placeholder="搜索系统名称..." style="width: 240px" @search="loadList" />
        <a-select v-model:value="statusFilter" style="width: 140px" @change="loadList">
          <a-select-option value="">全部状态</a-select-option>
          <a-select-option value="stage1">需求分析</a-select-option>
          <a-select-option value="stage2">架构设计</a-select-option>
          <a-select-option value="stage3">总体设计</a-select-option>
          <a-select-option value="stage4">自动开发</a-select-option>
          <a-select-option value="completed">已完成</a-select-option>
          <a-select-option value="failed">失败</a-select-option>
        </a-select>
      </div>
    </div>

    <a-spin :spinning="loading">
      <div v-if="list.length === 0" class="empty">暂无已生成系统</div>
      <div v-else class="card-grid">
        <div v-for="item in list" :key="item.id" class="system-card" :class="{ unread: item.updateCount > 0 && !item.isRead }">
          <div class="card-header">
            <strong>{{ item.projectName }}</strong>
            <span v-if="item.updateCount > 0 && !item.isRead" class="badge">
              {{ item.updateCount > 99 ? '99+' : item.updateCount }}
            </span>
          </div>
          <div class="stage-row">
            <span class="stage-tag" :class="stageClass(item.currentStage)">
              {{ stageLabel(item.currentStage) }}
            </span>
            <span class="time">{{ formatTime(item.createTime) }}</span>
          </div>
          <p v-if="item.description" class="desc">{{ item.description.slice(0, 80) }}</p>
          <div class="actions">
            <a-button size="small" type="primary" @click="continueChat(item.id)"> 继续对话 </a-button>
            <a-button v-if="item.sandboxUrl" size="small" @click="openSandbox(item.sandboxUrl)"> 沙箱试用 </a-button>
            <a-button v-if="item.sourceZipUrl" size="small" @click="downloadSource(item.sourceZipUrl)"> 下载源码 </a-button>
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
  import { ref, onMounted } from 'vue';
  import { useRouter } from 'vue-router';
  import { defHttp } from '/@/utils/http/axios';

  const router = useRouter();
  const list = ref<any[]>([]);
  const loading = ref(false);
  const total = ref(0);
  const page = ref(1);
  const pageSize = ref(12);
  const keyword = ref('');
  const statusFilter = ref('');

  async function loadList() {
    loading.value = true;
    try {
      const res: any = await defHttp.get({
        url: '/api/studio/ai/project/list',
        data: { page: page.value, pageSize: pageSize.value },
      });
      list.value = res?.data?.items || res?.data || [];
      total.value = res?.data?.total || 0;
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
    router.push(`/studio/expert/my-projects/${id}`);
  }

  function openSandbox(url: string) {
    window.open(url, '_blank');
  }

  function downloadSource(url: string) {
    window.open(url, '_blank');
  }

  function stageLabel(stage: number): string {
    const labels = ['', '需求分析', '架构设计', '总体设计', '自动开发', '已完成'];
    return labels[stage] || '进行中';
  }

  function stageClass(stage: number): string {
    if (stage >= 5) return 'completed';
    if (stage === 4) return 'dev';
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

    .page-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 20px;
      h2 {
        margin: 0;
        font-size: 20px;
      }
      .filters {
        display: flex;
        gap: 8px;
      }
    }

    .empty {
      text-align: center;
      color: #999;
      padding: 60px;
    }

    .card-grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(320px, 1fr));
      gap: 16px;

      .system-card {
        background: #fff;
        border-radius: 8px;
        padding: 16px;
        border: 1px solid #f0f0f0;
        transition: all 0.2s;
        position: relative;

        &:hover {
          box-shadow: 0 4px 12px rgba(0, 0, 0, 0.08);
        }
        &.unread {
          border-left: 3px solid #1890ff;
        }

        .card-header {
          display: flex;
          justify-content: space-between;
          align-items: center;
          .badge {
            background: #ff4d4f;
            color: #fff;
            border-radius: 10px;
            min-width: 20px;
            height: 20px;
            display: flex;
            align-items: center;
            justify-content: center;
            font-size: 11px;
            padding: 0 6px;
          }
        }

        .stage-row {
          display: flex;
          align-items: center;
          gap: 8px;
          margin: 8px 0;
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
            &.dev {
              background: #fff7e6;
              color: #d48806;
            }
          }
          .time {
            font-size: 11px;
            color: #bbb;
          }
        }

        .desc {
          font-size: 12px;
          color: #888;
          margin: 0;
        }

        .actions {
          margin-top: 12px;
          display: flex;
          gap: 6px;
        }
      }
    }

    .pagination {
      margin-top: 24px;
      text-align: center;
    }
  }
</style>
