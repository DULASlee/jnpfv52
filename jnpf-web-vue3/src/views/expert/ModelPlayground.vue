<template>
  <div class="model-playground">
    <a-page-header title="模型测试场" sub-title="对比不同模型的输出质量" @back="() => $router.back()" />

    <div class="pg-body">
      <!-- 左侧输入面板 -->
      <div class="pg-input-panel">
        <a-card title="输入配置" :bordered="false" size="small">
          <a-form :model="form" layout="vertical" size="small">
            <a-form-item label="System Prompt">
              <a-textarea v-model:value="form.systemPrompt" :auto-size="{ minRows: 3, maxRows: 8 }" placeholder="你是一个专业的低代码平台架构师…" />
            </a-form-item>
            <a-form-item label="User Prompt">
              <a-textarea v-model:value="form.userPrompt" :auto-size="{ minRows: 3, maxRows: 6 }" placeholder="请设计一个客户管理系统的数据库表结构…" />
            </a-form-item>

            <a-row :gutter="12">
              <a-col :span="12">
                <a-form-item label="模型 A">
                  <a-select v-model:value="form.modelA" :options="providerOptions" placeholder="选择模型" />
                </a-form-item>
              </a-col>
              <a-col :span="12">
                <a-form-item label="模型 B">
                  <a-select v-model:value="form.modelB" :options="providerOptions" placeholder="选择模型（可选）" allow-clear />
                </a-form-item>
              </a-col>
            </a-row>

            <a-row :gutter="12">
              <a-col :span="8">
                <a-form-item label="Temperature">
                  <a-slider v-model:value="form.temperature" :min="0" :max="2" :step="0.1" />
                </a-form-item>
              </a-col>
              <a-col :span="8">
                <a-form-item label="Max Tokens">
                  <a-input-number v-model:value="form.maxTokens" :min="100" :max="16000" :step="100" style="width: 100%" />
                </a-form-item>
              </a-col>
              <a-col :span="8">
                <a-form-item label="渲染模式">
                  <a-radio-group v-model:value="renderMode" size="small">
                    <a-radio-button value="markdown">Markdown</a-radio-button>
                    <a-radio-button value="json">JSON</a-radio-button>
                    <a-radio-button value="ir">IR</a-radio-button>
                  </a-radio-group>
                </a-form-item>
              </a-col>
            </a-row>

            <a-button type="primary" :loading="running" :disabled="!canRun" block @click="handleRun">
              <template #icon><ThunderboltOutlined /></template>
              同时测试两个模型
            </a-button>
          </a-form>
        </a-card>
      </div>

      <!-- 右侧输出面板：A/B 对比 -->
      <div class="pg-output-panel">
        <div class="output-columns">
          <!-- 模型 A 输出 -->
          <div class="output-col">
            <div class="col-header">
              <a-tag color="blue">{{ form.modelA || '模型 A' }}</a-tag>
              <span class="latency" v-if="latencyA !== null">{{ latencyA }}ms</span>
              <a-spin v-if="running" size="small" />
            </div>
            <div class="col-body" ref="outputAContainer">
              <div v-if="!outputA && !running" class="empty-hint">
                <a-empty description="运行测试后查看输出" :image="Empty.PRESENTED_IMAGE_SIMPLE" />
              </div>
              <div v-else-if="renderMode === 'markdown'" class="md-output">
                <pre>{{ outputA }}</pre>
              </div>
              <div v-else-if="renderMode === 'json'" class="json-output">
                <pre>{{ formattedJsonA }}</pre>
              </div>
              <div v-else class="ir-output">
                <pre>{{ formattedIrA }}</pre>
              </div>
            </div>
          </div>

          <!-- 模型 B 输出（可选） -->
          <div class="output-col" v-if="form.modelB">
            <div class="col-header">
              <a-tag color="green">{{ form.modelB }}</a-tag>
              <span class="latency" v-if="latencyB !== null">{{ latencyB }}ms</span>
              <a-spin v-if="running" size="small" />
            </div>
            <div class="col-body">
              <div v-if="!outputB && !running" class="empty-hint">
                <a-empty description="运行测试后查看输出" :image="Empty.PRESENTED_IMAGE_SIMPLE" />
              </div>
              <div v-else-if="renderMode === 'markdown'" class="md-output">
                <pre>{{ outputB }}</pre>
              </div>
              <div v-else-if="renderMode === 'json'" class="json-output">
                <pre>{{ formattedJsonB }}</pre>
              </div>
              <div v-else class="ir-output">
                <pre>{{ formattedIrB }}</pre>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
  import { ref, reactive, computed, nextTick } from 'vue';
  import { Empty } from 'ant-design-vue';
  import { ThunderboltOutlined } from '@ant-design/icons-vue';
  import { testChat } from '/@/api/founder/pipeline';

  defineOptions({ name: 'ModelPlayground' });

  const form = reactive({
    systemPrompt: '',
    userPrompt: '',
    modelA: 'mimo',
    modelB: undefined as string | undefined,
    temperature: 0.7,
    maxTokens: 4096,
  });

  const providerOptions = [
    { label: 'MiMo (默认)', value: 'mimo' },
    { label: 'DeepSeek', value: 'deepseek' },
    { label: 'OpenAI', value: 'openai' },
    { label: '通义千问', value: 'tongyi' },
  ];

  const renderMode = ref<'markdown' | 'json' | 'ir'>('markdown');
  const running = ref(false);
  const outputA = ref('');
  const outputB = ref('');
  const latencyA = ref<number | null>(null);
  const latencyB = ref<number | null>(null);

  const canRun = computed(() => form.userPrompt.trim() && form.modelA);

  const formattedJsonA = computed(() => formatJson(outputA.value));
  const formattedJsonB = computed(() => formatJson(outputB.value));
  const formattedIrA = computed(() => formatIr(outputA.value));
  const formattedIrB = computed(() => formatIr(outputB.value));

  function formatJson(text: string): string {
    try {
      return JSON.stringify(JSON.parse(text), null, 2);
    } catch {
      return text;
    }
  }

  function formatIr(text: string): string {
    return formatJson(text);
  }

  const handleRun = async () => {
    running.value = true;
    outputA.value = '';
    outputB.value = '';
    latencyA.value = null;
    latencyB.value = null;

    const prompt = form.systemPrompt ? `${form.systemPrompt}\n\n${form.userPrompt}` : form.userPrompt;

    // 并行请求模型 A 和模型 B
    const promises: Promise<void>[] = [];

    const callA = async () => {
      const t0 = performance.now();
      try {
        const res = await testChat({ prompt, providerCode: form.modelA });
        outputA.value = res.data?.content || res.data?.message || JSON.stringify(res.data);
      } catch {
        outputA.value = '[请求失败]';
      }
      latencyA.value = Math.round(performance.now() - t0);
    };

    promises.push(callA());

    if (form.modelB) {
      const callB = async () => {
        const t0 = performance.now();
        try {
          const res = await testChat({ prompt, providerCode: form.modelB });
          outputB.value = res.data?.content || res.data?.message || JSON.stringify(res.data);
        } catch {
          outputB.value = '[请求失败]';
        }
        latencyB.value = Math.round(performance.now() - t0);
      };
      promises.push(callB());
    }

    await Promise.allSettled(promises);
    running.value = false;
    await nextTick();
  };
</script>

<style lang="less" scoped>
  .model-playground {
    height: 100%;
    display: flex;
    flex-direction: column;

    .pg-body {
      flex: 1;
      display: flex;
      gap: 16px;
      padding: 0 24px 24px;
      overflow: hidden;

      .pg-input-panel {
        width: 380px;
        flex-shrink: 0;
        overflow-y: auto;
      }

      .pg-output-panel {
        flex: 1;
        overflow: hidden;

        .output-columns {
          display: flex;
          gap: 16px;
          height: 100%;

          .output-col {
            flex: 1;
            display: flex;
            flex-direction: column;
            min-width: 0;

            .col-header {
              display: flex;
              align-items: center;
              gap: 8px;
              padding: 8px 12px;
              background: #fafafa;
              border-radius: 6px 6px 0 0;
              border: 1px solid #f0f0f0;
              border-bottom: none;

              .latency {
                color: #8c8c8c;
                font-size: 12px;
                margin-left: auto;
              }
            }

            .col-body {
              flex: 1;
              padding: 12px;
              border: 1px solid #f0f0f0;
              border-radius: 0 0 6px 6px;
              overflow-y: auto;
              background: #fff;

              .empty-hint {
                display: flex;
                align-items: center;
                justify-content: center;
                height: 100%;
              }

              .md-output {
                line-height: 1.7;
                white-space: pre-wrap;
                word-break: break-word;
              }

              .json-output pre,
              .ir-output pre {
                margin: 0;
                font-size: 13px;
                line-height: 1.5;
                white-space: pre-wrap;
                word-break: break-word;
              }
            }
          }
        }
      }
    }
  }
</style>
