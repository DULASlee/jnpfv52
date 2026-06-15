<template>
  <div class="quick-app-entry">
    <div class="entry-header">
      <h2>快速创建应用</h2>
      <p class="entry-subtitle">用自然语言描述您想要的系统，AI 将自动完成需求分析到部署的全流程</p>
    </div>

    <div class="entry-content">
      <a-card :bordered="false" class="chat-card">
        <div class="chat-placeholder">
          <div class="placeholder-icon">
            <ThunderboltOutlined :style="{ fontSize: '48px', color: '#1890ff' }" />
          </div>
          <h3>AI 应用生成器</h3>
          <p>在此输入您的需求，例如：<br />"我需要一个客户管理系统，能记录客户信息、跟进状态和合同管理"</p>

          <a-textarea
            v-model:value="requirement"
            :auto-size="{ minRows: 4, maxRows: 8 }"
            placeholder="请描述您想要的系统…"
            :disabled="submitting"
            @press-enter="handleSubmit" />

          <a-button type="primary" size="large" :loading="submitting" :disabled="!requirement.trim()" class="submit-btn" @click="handleSubmit">
            <template #icon><RocketOutlined /></template>
            开始生成
          </a-button>
        </div>
      </a-card>
    </div>
  </div>
</template>

<script setup lang="ts">
  import { ref } from 'vue';
  import { useRouter } from 'vue-router';
  import { message } from 'ant-design-vue';
  import { ThunderboltOutlined, RocketOutlined } from '@ant-design/icons-vue';
  import { createPipeline } from '/@/api/founder/pipeline';

  defineOptions({ name: 'QuickAppEntry' });

  const router = useRouter();
  const requirement = ref('');
  const submitting = ref(false);

  const handleSubmit = async () => {
    const trimmed = requirement.value.trim();
    if (!trimmed) return;

    submitting.value = true;
    try {
      const res = await createPipeline({ name: trimmed.slice(0, 50), userRequirement: trimmed });
      const pipelineId = res.data?.id;
      if (pipelineId) {
        message.success('项目创建成功，正在跳转…');
        // 302 重定向到 ProjectDashboard
        router.push(`/studio/expert/my-projects?id=${pipelineId}`);
      } else {
        message.error('项目创建失败，请重试');
      }
    } catch {
      message.error('创建失败，请稍后重试');
    } finally {
      submitting.value = false;
    }
  };
</script>

<style lang="less" scoped>
  .quick-app-entry {
    max-width: 800px;
    margin: 0 auto;
    padding: 48px 24px;

    .entry-header {
      text-align: center;
      margin-bottom: 32px;

      h2 {
        font-size: 24px;
        font-weight: 600;
        color: #262626;
        margin-bottom: 8px;
      }

      .entry-subtitle {
        color: #8c8c8c;
        font-size: 14px;
      }
    }

    .entry-content {
      .chat-card {
        border-radius: 8px;

        .chat-placeholder {
          text-align: center;
          padding: 24px 0;

          .placeholder-icon {
            margin-bottom: 16px;
          }

          h3 {
            font-size: 18px;
            margin-bottom: 12px;
          }

          p {
            color: #595959;
            margin-bottom: 24px;
            line-height: 1.6;
          }

          :deep(.ant-input) {
            text-align: left;
            margin-bottom: 20px;
          }

          .submit-btn {
            min-width: 160px;
          }
        }
      }
    }
  }
</style>
