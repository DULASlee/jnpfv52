<template>
  <div class="project-dashboard">
    <div class="dashboard-header">
      <h2>我的项目</h2>
      <a-button type="primary" @click="handleCreateNew">
        <template #icon><PlusOutlined /></template>
        快速创建
      </a-button>
    </div>

    <div class="dashboard-body">
      <div class="project-list-panel">
        <a-card :bordered="false" class="list-card" title="项目列表">
          <template #extra>
            <a-input-search v-model:value="searchKeyword" placeholder="搜索项目…" style="width: 200px" @search="handleSearch" />
          </template>

          <a-spin :spinning="loading">
            <div v-if="projects.length === 0" class="empty-state">
              <a-empty description="暂无项目">
                <a-button type="primary" @click="handleCreateNew">创建第一个项目</a-button>
              </a-empty>
            </div>

            <div v-else class="project-grid">
              <a-card v-for="project in projects" :key="project.id" class="project-card" hoverable @click="handleSelectProject(project.id)">
                <div class="card-content">
                  <div class="project-title">{{ project.name || '未命名项目' }}</div>
                  <a-tag :color="getStatusColor(project.stageStatus)">
                    {{ getStatusLabel(project.stageStatus) }}
                  </a-tag>
                  <div class="project-meta">
                    <span>{{ project.currentStage || '初始化' }}</span>
                    <span>{{ formatTime(project.lastModifyTime) }}</span>
                  </div>
                </div>
              </a-card>
            </div>
          </a-spin>
        </a-card>
      </div>

      <div class="project-detail-panel">
        <router-view />
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
  import { ref, onMounted } from 'vue';
  import { useRouter } from 'vue-router';
  import { PlusOutlined } from '@ant-design/icons-vue';
  import { getPipelineList } from '/@/api/founder/pipeline';

  defineOptions({ name: 'ProjectDashboard' });

  const router = useRouter();
  const loading = ref(false);
  const searchKeyword = ref('');
  const projects = ref<any[]>([]);

  const statusLabelMap: Record<string, string> = {
    running: '运行中',
    review: '待审核',
    stale: '已超时',
    abandoned: '已放弃',
    completed: '已完成',
    blocked: '已阻断',
  };

  const statusColorMap: Record<string, string> = {
    running: 'processing',
    review: 'warning',
    stale: 'default',
    abandoned: 'default',
    completed: 'success',
    blocked: 'error',
  };

  const getStatusLabel = (status: string) => statusLabelMap[status] || status || '未知';
  const getStatusColor = (status: string) => statusColorMap[status] || 'default';

  const formatTime = (time?: string) => {
    if (!time) return '';
    return new Date(time).toLocaleDateString('zh-CN');
  };

  const handleCreateNew = () => {
    router.push('/studio/expert/quick-app-entry');
  };

  const handleSelectProject = (id: number) => {
    router.push(`/studio/expert/my-projects?id=${id}`);
  };

  const handleSearch = async (keyword: string) => {
    searchKeyword.value = keyword;
    await fetchProjects();
  };

  const fetchProjects = async () => {
    loading.value = true;
    try {
      const res = await getPipelineList(0, 20);
      projects.value = res.data?.list || [];
    } catch {
      projects.value = [];
    } finally {
      loading.value = false;
    }
  };

  onMounted(() => {
    fetchProjects();
  });
</script>

<style lang="less" scoped>
  .project-dashboard {
    padding: 24px;

    .dashboard-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 24px;

      h2 {
        font-size: 20px;
        font-weight: 600;
        margin: 0;
      }
    }

    .dashboard-body {
      .list-card {
        margin-bottom: 24px;
      }

      .empty-state {
        padding: 48px 0;
      }

      .project-grid {
        display: grid;
        grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
        gap: 16px;

        .project-card {
          .card-content {
            .project-title {
              font-size: 15px;
              font-weight: 500;
              margin-bottom: 8px;
              overflow: hidden;
              text-overflow: ellipsis;
              white-space: nowrap;
            }

            .project-meta {
              display: flex;
              justify-content: space-between;
              margin-top: 12px;
              font-size: 12px;
              color: #8c8c8c;
            }
          }
        }
      }
    }
  }
</style>
