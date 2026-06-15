<template>
  <div class="architect-review">
    <a-page-header title="AI 架构评审" sub-title="提交 IR → 多维度审查 → 生成修复补丁" @back="() => $router.back()" />

    <div class="ar-body">
      <!-- 左栏：IR 输入 + 审查维度选择 -->
      <div class="ar-left">
        <a-card title="提交审查" :bordered="false" size="small">
          <a-form layout="vertical" size="small">
            <a-form-item label="IR JSON">
              <a-textarea v-model:value="irJson" :auto-size="{ minRows: 8, maxRows: 20 }" placeholder="粘贴 IR JSON 或从项目中导入…" />
            </a-form-item>

            <a-form-item label="审查维度">
              <a-checkbox-group v-model:value="dimensions" :options="dimensionOptions" />
            </a-form-item>

            <a-button type="primary" :loading="reviewing" :disabled="!canReview" block @click="handleReview">
              <template #icon><AuditOutlined /></template>
              开始审查
            </a-button>
          </a-form>
        </a-card>
      </div>

      <!-- 中栏：可视化 -->
      <div class="ar-center">
        <a-card title="可视化" :bordered="false" size="small">
          <template #extra>
            <a-radio-group v-model:value="viewMode" size="small" :disabled="!hasData">
              <a-radio-button value="tree">组件树</a-radio-button>
              <a-radio-button value="er">ER 图</a-radio-button>
            </a-radio-group>
          </template>

          <div v-if="!hasData" class="center-empty">
            <a-empty description="提交审查后展示" :image="Empty.PRESENTED_IMAGE_SIMPLE" />
          </div>

          <!-- ECharts 树图容器 -->
          <div v-show="viewMode === 'tree' && hasData" ref="treeChartRef" class="chart-container"></div>
          <div v-show="viewMode === 'er' && hasData" ref="erChartRef" class="chart-container"></div>

          <div class="chart-stats" v-if="hasData">
            <a-tag>模块: {{ parsedModules.length }}</a-tag>
            <a-tag>表: {{ parsedTables.length }}</a-tag>
          </div>
        </a-card>
      </div>

      <!-- 右栏：审查报告 -->
      <div class="ar-right">
        <a-card title="审查报告" :bordered="false" size="small">
          <template #extra>
            <a-tag v-if="report" :color="report.pass ? 'success' : 'error'">
              {{ report.pass ? '通过' : '需修改' }}
            </a-tag>
          </template>

          <div v-if="!report" class="right-empty">
            <a-empty description="等待审查结果" :image="Empty.PRESENTED_IMAGE_SIMPLE" />
          </div>

          <div v-else class="report-content">
            <div class="report-summary">
              <a-statistic title="评分" :value="report.score" suffix="/ 100" />
              <a-statistic title="问题数" :value="report.issues.length" />
            </div>

            <a-divider />

            <div class="issue-list">
              <a-alert
                v-for="(issue, i) in report.issues"
                :key="i"
                :type="issue.severity === 'critical' ? 'error' : issue.severity === 'warning' ? 'warning' : 'info'"
                :message="issue.title"
                :description="issue.description"
                show-icon
                class="issue-item" />
            </div>

            <a-divider />

            <div class="report-actions">
              <a-button type="primary" :disabled="!report.pass" block @click="handleAccept">
                <template #icon><CheckOutlined /></template>
                采纳建议 → 自动生成 IREditPatch
              </a-button>
            </div>
          </div>
        </a-card>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
  import { ref, computed, watch, nextTick } from 'vue';
  import { Empty, message } from 'ant-design-vue';
  import { AuditOutlined, CheckOutlined } from '@ant-design/icons-vue';
  import * as echarts from 'echarts';
  import { testChat } from '/@/api/founder/pipeline';

  defineOptions({ name: 'ArchitectReview' });

  const irJson = ref('');
  const dimensions = ref<string[]>(['architecture', 'database', 'naming']);
  const reviewing = ref(false);
  const viewMode = ref<'tree' | 'er'>('tree');
  const treeChartRef = ref<HTMLDivElement>();
  const erChartRef = ref<HTMLDivElement>();

  const dimensionOptions = [
    { label: '架构合理性', value: 'architecture' },
    { label: '数据库设计', value: 'database' },
    { label: '命名规范', value: 'naming' },
    { label: '安全合规', value: 'security' },
  ];

  const report = ref<ReviewReport | null>(null);

  const hasData = computed(() => report.value !== null);
  const canReview = computed(() => irJson.value.trim() && dimensions.value.length > 0);

  const parsedModules = computed(() => {
    try {
      const ir = JSON.parse(irJson.value);
      return ir.architecture?.modules || ir.modules || [];
    } catch {
      return [];
    }
  });

  const parsedTables = computed(() => {
    try {
      const ir = JSON.parse(irJson.value);
      return ir.architecture?.databaseDesign?.tables || ir.databaseDesign?.tables || [];
    } catch {
      return [];
    }
  });

  interface ReviewReport {
    pass: boolean;
    score: number;
    issues: Array<{ severity: 'critical' | 'warning' | 'info'; title: string; description: string }>;
    editPatches: Array<{ path: string; op: string; value: unknown }>;
  }

  function buildReviewPrompt(): string {
    const dims = dimensions.value.join('、');
    return `作为 JNPF 平台架构审查专家，请审查以下 IR 定义。审查维度：${dims}。

审查规则：
1. 架构合理性：模块划分是否清晰、依赖关系是否合理
2. 数据库设计：表结构是否规范、是否包含审计字段(TenantId/CreatorTime)
3. 命名规范：表名是否 UPPER_SNAKE、字段是否 F_ 前缀
4. 安全合规：是否有越权风险、敏感字段是否加密

请输出 JSON 格式：
{
  "pass": true/false,
  "score": 0-100,
  "issues": [{ "severity": "critical|warning|info", "title": "", "description": "" }],
  "editPatches": [{ "path": "$.path.to.field", "op": "replace|add|remove", "value": null }]
}

IR 数据：
${irJson.value}`;
  }

  const handleReview = async () => {
    reviewing.value = true;
    report.value = null;
    try {
      const res = await testChat({ prompt: buildReviewPrompt() });
      const text = (res.data?.content || res.data?.message || '{}') as string;
      const jsonMatch = text.match(/\{[\s\S]*\}/);
      report.value = jsonMatch ? JSON.parse(jsonMatch[0]) : parseFallback(text);
      await nextTick();
      renderCharts();
    } catch {
      message.error('审查失败，请重试');
    } finally {
      reviewing.value = false;
    }
  };

  function parseFallback(text: string): ReviewReport {
    return { pass: false, score: 0, issues: [{ severity: 'warning', title: '解析失败', description: text.slice(0, 200) }], editPatches: [] };
  }

  const handleAccept = () => {
    if (!report.value?.pass) return;
    message.success('已生成 IREditPatch，请在 IR 设计器中查看');
  };

  // ─── ECharts 渲染 ───
  function renderCharts() {
    if (hasData.value) {
      if (viewMode.value === 'tree') renderTreeChart();
      else renderErChart();
    }
  }

  function renderTreeChart() {
    const el = treeChartRef.value;
    if (!el) return;
    const chart = echarts.init(el);
    const modules = parsedModules.value;
    chart.setOption({
      tooltip: { trigger: 'item' },
      series: [
        {
          type: 'tree',
          data: [
            {
              name: 'IR Root',
              children: modules.map((m: { name: string; dependencies?: string[] }) => ({
                name: m.name,
                children: (m.dependencies || []).map((d: string) => ({ name: d })),
              })),
            },
          ],
          top: '5%',
          left: '10%',
          bottom: '5%',
          right: '20%',
          symbolSize: 10,
          label: { position: 'left', verticalAlign: 'middle', align: 'right', fontSize: 11 },
          leaves: { label: { position: 'right', verticalAlign: 'middle', align: 'left' } },
          initialTreeDepth: 3,
          expandAndCollapse: true,
        },
      ],
    });
    chart.resize();
  }

  function renderErChart() {
    const el = erChartRef.value;
    if (!el) return;
    const chart = echarts.init(el);
    const tables = parsedTables.value;
    const nodes = tables.flatMap((t: { name: string; columns?: Array<{ name: string }> }, i: number) => [
      { name: t.name, x: 200, y: i * 120 + 60, symbolSize: 40, category: 0 },
      ...(t.columns || []).slice(0, 6).map((c: { name: string }, j: number) => ({
        name: `${t.name}.${c.name}`,
        x: 500,
        y: i * 120 + j * 16 + 10,
        symbolSize: 6,
        category: 1,
      })),
    ]);

    chart.setOption({
      tooltip: { trigger: 'item' },
      legend: { data: ['表', '字段'] },
      series: [
        {
          type: 'graph',
          layout: 'force',
          data: nodes,
          categories: [{ name: '表' }, { name: '字段' }],
          roam: true,
          label: { show: true, fontSize: 10 },
          force: { repulsion: 200, edgeLength: 100 },
        },
      ],
    });
    chart.resize();
  }

  watch(viewMode, () => nextTick(renderCharts));
</script>

<style lang="less" scoped>
  .architect-review {
    height: 100%;
    display: flex;
    flex-direction: column;

    .ar-body {
      flex: 1;
      display: flex;
      gap: 16px;
      padding: 0 24px 24px;
      overflow: hidden;

      .ar-left {
        width: 320px;
        flex-shrink: 0;
        overflow-y: auto;
      }

      .ar-center {
        flex: 1;
        display: flex;
        flex-direction: column;
        min-width: 0;

        :deep(.ant-card-body) {
          flex: 1;
          display: flex;
          flex-direction: column;
        }

        .center-empty {
          flex: 1;
          display: flex;
          align-items: center;
          justify-content: center;
        }

        .chart-container {
          flex: 1;
          min-height: 350px;
        }

        .chart-stats {
          margin-top: 8px;
          display: flex;
          gap: 8px;
        }
      }

      .ar-right {
        width: 360px;
        flex-shrink: 0;
        overflow-y: auto;

        .right-empty {
          padding: 48px 0;
        }

        .report-content {
          .report-summary {
            display: flex;
            gap: 24px;

            :deep(.ant-statistic) {
              .ant-statistic-content-value {
                font-size: 24px;
              }
            }
          }

          .issue-list {
            .issue-item {
              margin-bottom: 8px;
            }
          }

          .report-actions {
            margin-top: 8px;
          }
        }
      }
    }
  }
</style>
