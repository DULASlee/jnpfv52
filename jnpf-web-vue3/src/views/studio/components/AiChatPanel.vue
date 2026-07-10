<template>
  <div class="ai-chat-panel">
    <!-- ====== 顶栏：一行极简 ====== -->
    <div class="top-bar">
      <div class="top-bar-left">
        <a-select v-model:value="selectedProvider" size="small" class="model-select" @change="handleProviderChange">
          <a-select-option v-for="p in providers" :key="p.providerCode" :value="p.providerCode">
            {{ p.name }}
          </a-select-option>
        </a-select>
      </div>
      <div class="top-bar-center">
        <span class="stage-text"> 阶段 {{ currentStage }}/{{ stages.length }}: {{ stages[currentStage - 1]?.name }} </span>
        <a-popover trigger="click" placement="bottomRight">
          <template #content>
            <div class="stage-detail-popover">
              <div v-for="s in stages" :key="s.stage" class="stage-item" :class="{ active: s.stage === currentStage, completed: s.stage < currentStage }">
                <span class="stage-dot"></span>
                <span class="stage-name">{{ s.stage }}. {{ s.name }}</span>
                <a-tag v-if="s.stage < currentStage" color="green" size="small">完成</a-tag>
                <a-tag v-if="s.stage === currentStage" color="blue" size="small">进行中</a-tag>
                <a-tag v-if="s.stage > currentStage" color="default" size="small">待执行</a-tag>
              </div>
            </div>
          </template>
          <a-button size="small" type="link">阶段详情 ▾</a-button>
        </a-popover>
      </div>
      <div class="top-bar-right">
        <a-button v-if="pipelineId" size="small" :loading="lifecycleLoading" @click="handleFreezePipeline">冻结</a-button>
        <a-button v-if="pipelineId" size="small" :loading="lifecycleLoading" @click="handleResumePipeline">恢复</a-button>
        <a-button v-if="pipelineId" size="small" :loading="lifecycleLoading" @click="handleForkPipeline">Fork</a-button>
        <a-button v-if="showObservatoryToggle" size="small" :type="observatoryPanelCollapsed ? 'default' : 'primary'" ghost @click="onToggleObservatory">
          <template #icon><NodeIndexOutlined /></template>
          观测台
        </a-button>
        <a-button size="small" @click="handleNewChat">
          <template #icon><PlusOutlined /></template>
          新对话
        </a-button>
      </div>
    </div>

    <!-- 任务意图（新建流水线前可选；创建后只读展示） -->
    <div class="work-mode-bar">
      <template v-if="!pipelineId">
        <a-radio-group v-model:value="workMode" size="small" button-style="solid">
          <a-radio-button value="greenfield">首次全量开发</a-radio-button>
          <a-radio-button value="bugfix">Debug 修复</a-radio-button>
          <a-radio-button value="enhancement">二次开发</a-radio-button>
        </a-radio-group>
        <template v-if="workMode !== 'greenfield'">
          <a-select
            v-model:value="sourcePipelineId"
            size="small"
            class="work-mode-select"
            placeholder="选择已生成系统"
            :loading="systemsLoading"
            show-search
            option-filter-prop="label"
            :options="generatedSystemOptions"
            @change="onSourcePipelineChange" />
          <a-select
            v-if="workMode === 'bugfix'"
            v-model:value="targetPageRoute"
            size="small"
            class="work-mode-select"
            placeholder="选择要修改的页面"
            :loading="routesLoading"
            :disabled="!sourcePipelineId"
            show-search
            option-filter-prop="label"
            :options="pageRouteOptions"
            @change="onTargetPageChange" />
        </template>
      </template>
      <template v-else>
        <a-tag :color="workModeTagColor">{{ workModeLabel }}</a-tag>
        <span v-if="sourcePipelineId" class="work-mode-meta">源系统 #{{ sourcePipelineId }}</span>
        <span v-if="targetPageLabel" class="work-mode-meta">页面：{{ targetPageLabel }}</span>
      </template>
    </div>

    <!-- ====== 中间：对话流（核心区域，占满全部空间） ====== -->
    <div class="chat-stream" ref="chatStreamRef" data-testid="chat-stream" @scroll="handleScroll">
      <!-- 欢迎界面 -->
      <div v-if="pipelineLoading" class="welcome-card loading-card">
        <a-spin tip="正在加载任务对话…" />
      </div>
      <div v-else-if="messages.length === 0 && !pipelineId" class="welcome-card">
        <div class="welcome-icon">🤖</div>
        <h2>AI 架构顾问</h2>
        <p>你好！请先提交你的原始业务需求，我会先做需求门控校验，再进入后续分析与生成。</p>
        <p class="hint">为了更快通过门控，建议一次写清楚角色、业务事件、数据实体。</p>
        <p class="hint gate-hint">提交后先经 <strong>SA 门控</strong>：需求须能解析为合格<strong>业务事件</strong>（含角色与数据实体）方可进入流水线。</p>
        <div class="gate-format">
          <div class="gate-format-title">建议输入格式</div>
          <ul>
            <li><strong>角色</strong>：谁在操作（如采购员、仓管、财务）</li>
            <li><strong>业务事件</strong>：发生了什么（如采购入库、销售出库、盘点调整）</li>
            <li><strong>数据实体</strong>：要管理什么数据（如商品、供应商、库存、单据）</li>
          </ul>
          <div class="gate-format-example"> 示例：采购员提交采购单后，仓管执行入库，系统维护商品、供应商、采购单、库存台账，并支持盘点差异处理。 </div>
        </div>
      </div>

      <!-- 消息列表 -->
      <template v-for="msg in messages" :key="msg.id">
        <!-- 用户消息：全宽卡片，左侧 🧑 图标 -->
        <div v-if="msg.role === 'user'" class="msg-card user-card">
          <div class="card-avatar user-card-avatar">🧑</div>
          <div class="card-body" v-html="renderMarkdown(msg.content)"></div>
        </div>

        <!-- AI 消息：全宽卡片，左侧 🤖 图标 -->
        <div v-else-if="msg.role === 'assistant'" class="msg-card ai-card">
          <div class="card-avatar ai-card-avatar">🤖</div>
          <div class="card-body">
            <div v-if="msg.thinking || (isLastAssistantMsg(msg) && loading)" class="thinking-block">
              <div class="thinking-header" @click="msg.thinkingCollapsed = !msg.thinkingCollapsed">
                <span>💭 推理与工作流{{ msg.thinkingCollapsed ? '（可折叠）' : '' }}</span>
                <span>{{ msg.thinkingCollapsed ? '▸' : '▾' }}</span>
              </div>
              <div v-if="!msg.thinkingCollapsed">
                <ChatWorkflowProgress v-if="isLastAssistantMsg(msg)" />
                <div v-if="isLastAssistantMsg(msg) && loading && !msg.thinking" class="thinking-anim">
                  <span class="thinking-label">{{ thinkingLabel }}</span>
                  <span class="dots"><i></i><i></i><i></i></span>
                </div>
                <div v-if="msg.thinking" class="thinking-content">{{ msg.thinking }}</div>
              </div>
            </div>

            <!-- AI 正文 -->
            <div class="card-text" v-html="renderMarkdown(msg.content)"></div>

            <!-- 策略选项卡片（可点击） -->
            <div v-if="msg.strategies && msg.strategies.length > 0" class="strategy-cards">
              <div v-for="(s, idx) in msg.strategies" :key="idx" class="strategy-card" @click="handleSelectStrategy(idx, s)">
                <span class="strategy-icon">{{ idx === 0 ? '🟢' : '🔵' }}</span>
                <div class="strategy-info">
                  <div class="strategy-title">{{ s.title }}</div>
                  <div class="strategy-desc">{{ s.description }}</div>
                </div>
                <a-button size="small" type="primary" ghost>选择此方案</a-button>
              </div>
            </div>

            <!-- ADR-005 交互式澄清问答卡片 -->
            <ClarificationCard
              v-if="msg.clarification"
              :set="msg.clarification"
              :pipeline-id="pipelineId"
              @answered="onClarificationAnswered(msg, $event)"
              @skip-all="onClarificationSkipAll(msg)" />

            <!-- 流内操作按钮（门控反馈 / 流水线错误） -->
            <div v-if="msg.actions && msg.actions.length > 0" class="stream-action-bar">
              <a-button v-for="(act, idx) in msg.actions" :key="idx" size="small" :type="act.type || 'default'" @click="handleChatAction(act)">
                {{ act.label }}
              </a-button>
            </div>

            <!-- 文档卡片（预览 + 下载） -->
            <div v-if="msg.document" class="doc-card">
              <span class="doc-emoji">📄</span>
              <div class="doc-info">
                <div class="doc-name">{{ msg.document.name }}</div>
              </div>
              <div class="doc-actions">
                <a-button size="small" type="link" @click="previewDoc(msg.document)">预览全文</a-button>
                <a-button size="small" type="link" @click="downloadDoc(msg.document, 'pdf')">下载 PDF</a-button>
                <a-button size="small" type="link" @click="downloadDoc(msg.document, 'word')">下载 Word</a-button>
              </div>
            </div>

            <!-- IR 预览 -->
            <IrPreviewCard v-if="msg.ir" :ir-data="msg.ir" />

            <!-- 阶段确认卡片（在 AI 回复末尾） -->
            <div v-if="msg.stageConfirmable && !msg.stageConfirmed" class="stage-confirm-card">
              <div class="confirm-badge">⬆️ 阶段 {{ currentStage }}: {{ stages[currentStage - 1]?.name }} ✅ 完成</div>
              <div class="confirm-actions">
                <a-button size="small" @click="handleRollback(currentStage - 1)" :disabled="currentStage <= 1">↩️ 回退修改</a-button>
                <a-button type="primary" size="small" @click="handleConfirmStage(msg)">确认并推进 ▶️</a-button>
              </div>
            </div>

            <!-- 已确认标记 -->
            <div v-if="msg.stageConfirmed" class="confirmed-badge">✅ 已确认，进入 {{ stages[currentStage - 1]?.name }}</div>
          </div>
        </div>

        <!-- 系统消息 -->
        <div v-else-if="msg.role === 'system'" class="system-msg">
          <div class="system-line"></div>
          <span class="system-text">{{ msg.content }}</span>
          <div class="system-line"></div>
        </div>
      </template>

      <IrSkeletonConfirmCard
        v-if="showSkeletonConfirm"
        :visible="showSkeletonConfirm"
        :payload="skeletonPayload"
        :confirm-loading="skeletonConfirmLoading"
        @confirm="handleConfirmSkeleton" />

      <IrRequirementSpecConfirmCard
        v-if="showRequirementSpecConfirm"
        :visible="showRequirementSpecConfirm"
        :pipeline-id="pipelineId"
        :document-title="requirementSpecTitle"
        :relative-path="requirementSpecDeliverable?.relativePath ?? requirementSpecDeliverable?.RelativePath"
        :confirm-loading="requirementSpecConfirmLoading"
        @confirm="handleConfirmRequirementSpec"
        @download="handleDownloadRequirementSpec" />
    </div>

    <!-- 滚动按钮 -->
    <div class="scroll-btns" v-show="showScrollButtons">
      <a-button v-show="showScrollUp" shape="circle" size="small" @click="scrollToTop">
        <template #icon><UpOutlined /></template>
      </a-button>
      <a-button v-show="showScrollDown" shape="circle" size="small" type="primary" @click="scrollToBottom">
        <template #icon><DownOutlined /></template>
      </a-button>
    </div>

    <!-- ====== 底部输入栏（固定） ====== -->
    <div class="input-bar">
      <a-upload :before-upload="handleUpload" :show-upload-list="false" multiple>
        <a-button class="attach-btn" type="text" data-testid="submit-requirement-attach-btn">
          <template #icon><PlusOutlined /></template>
        </a-button>
      </a-upload>
      <div class="input-wrap">
        <a-textarea
          ref="textareaRef"
          v-model:value="inputText"
          :placeholder="inputPlaceholder"
          :auto-size="{ minRows: 1, maxRows: 5 }"
          @press-enter="handleEnter"
          data-testid="submit-requirement-textarea" />
        <div v-if="attachments.length" class="att-list">
          <a-tag v-for="(f, i) in attachments" :key="i" closable @close="attachments.splice(i, 1)">📎 {{ f.name }}</a-tag>
        </div>
      </div>
      <a-button
        class="send-btn"
        :type="loading ? 'default' : 'primary'"
        :danger="loading"
        shape="circle"
        size="large"
        data-testid="submit-requirement-send-btn"
        @click="loading ? handleStop() : handleSend()">
        <template #icon>
          <SendOutlined v-if="!loading" />
          <PauseOutlined v-else />
        </template>
      </a-button>
    </div>
  </div>
</template>

<script setup lang="ts">
  import { ref, computed, onMounted, onUnmounted, nextTick, watch, inject } from 'vue';
  import { IR_OBSERVATORY_KEY } from '../composables/useIrObservatory';
  import { PIPELINE_MATERIALS_KEY } from '../composables/usePipelineMaterials';
  import { PM_SKILL_KEY } from '../composables/usePmSkill';
  import { ANALYST_SKILL_KEY } from '../composables/useAnalystSkill';
  import { DESIGN_SKILL_KEY } from '../composables/useDesignSkills';
  import { DEVELOPER_SKILL_KEY } from '../composables/useDeveloperSkill';
  import type { SseAnalysisCompletedPayload, SseFragmentUpdatedPayload, SseIrEventPayload, SseSkillProgressPayload } from '../types/ir';
  import { PlusOutlined, SendOutlined, PauseOutlined, UpOutlined, DownOutlined, NodeIndexOutlined } from '@ant-design/icons-vue';
  import { message as antMessage, Modal } from 'ant-design-vue';
  import { defHttp } from '/@/utils/http/axios';
  import { createPipeline, getGeneratedProjectList, getPageRoutes, quickBugfix, quickEnhancement, triggerSaGate, freezePipeline, resumePipeline, forkPipeline } from '../api/studio/pipeline';
  import { runArchitectSkill, runSystemDesignClarificationSkill } from '../api/studio/designSkills';
  import { runRequirementAnalysis } from '../api/studio/skills';
  import IrPreviewCard from './chat/IrPreviewCard.vue';
  import ChatWorkflowProgress from './chat/ChatWorkflowProgress.vue';
  import IrSkeletonConfirmCard from './ir/IrSkeletonConfirmCard.vue';
  import IrRequirementSpecConfirmCard from './ir/IrRequirementSpecConfirmCard.vue';
  import ClarificationCard from './clarification/ClarificationCard.vue';
  import type { ChatStreamAction } from '../types/gate';
  import {
    buildAttachmentsReadyMarkdown,
    buildGateErrorMarkdown,
    buildGateFailedMarkdown,
    buildGatePassedMarkdown,
    gateErrorActions,
    gateFailedActions,
    normalizeSemanticFitness,
    parseGatePayload,
    streamTextToMessage,
  } from '../composables/gateStreamFormatter';
  import type { AttachmentsReadyPayload, GateErrorPayload, GateFailedPayload, GatePassedPayload } from '../types/gate';
  import { buildFetchSseUrl } from '/@/utils/http/sseUrl';
  import { getAuthHeader, getTenantId } from '/@/utils/auth';
  import { marked } from 'marked';
  import hljs from 'highlight.js';
  import 'highlight.js/styles/github.css';

  // ====== Markdown 渲染配置 ======
  marked.setOptions({
    breaks: true,
    gfm: true,
  } as any);

  const renderer = new marked.Renderer();

  renderer.table = function ({ header, body }: any) {
    return `<div class="md-table-wrap"><table><thead>${header}</thead><tbody>${body}</tbody></table></div>`;
  };

  renderer.code = function ({ text, lang }: any) {
    const language = lang && hljs.getLanguage(lang) ? lang : 'plaintext';
    const highlighted = hljs.highlight(text, { language }).value;
    return `<div class="md-code-block"><div class="md-code-header"><span>${
      lang || 'code'
    }</span><button class="md-copy-btn" onclick="navigator.clipboard.writeText(this.closest('.md-code-block').querySelector('code').textContent)">复制</button></div><pre><code class="hljs language-${language}">${highlighted}</code></pre></div>`;
  };

  renderer.link = function ({ href, title, text }: any) {
    const t = title ? `title="${title}"` : '';
    return `<a href="${href}" target="_blank" rel="noopener" ${t}>${text}</a>`;
  };

  renderer.blockquote = function ({ text }: any) {
    return `<blockquote class="md-blockquote">${text}</blockquote>`;
  };

  marked.use({ renderer });

  function renderMarkdown(text: string): string {
    if (!text) return '';
    try {
      return marked.parse(text) as string;
    } catch {
      return text.replace(/\n/g, '<br>');
    }
  }

  function parseSseJsonPayload(raw: unknown): Record<string, any> | null {
    if (!raw) return null;
    if (typeof raw === 'string') {
      try {
        return JSON.parse(raw);
      } catch {
        return null;
      }
    }
    return raw as Record<string, any>;
  }

  // ====== Props / Emits ======
  const props = withDefaults(defineProps<{ pipelineId?: number; initialMessage?: string; showObservatoryToggle?: boolean }>(), {
    pipelineId: 0,
    initialMessage: '',
    showObservatoryToggle: true,
  });
  const emit = defineEmits(['pipeline-complete', 'new-chat', 'pipeline-id-change']);

  const irObservatory = inject(IR_OBSERVATORY_KEY, null);
  const pipelineMaterials = inject(PIPELINE_MATERIALS_KEY, null);
  const pmSkill = inject(PM_SKILL_KEY, null);
  const analystSkill = inject(ANALYST_SKILL_KEY, null);
  const designSkill = inject(DESIGN_SKILL_KEY, null);
  const developerSkill = inject(DEVELOPER_SKILL_KEY, null);

  const showSkeletonConfirm = computed(() => pmSkill?.needsConfirmation.value ?? false);
  const skeletonPayload = computed(() => pmSkill?.skeletonSnapshot.value?.payload);
  const skeletonConfirmLoading = computed(() => pmSkill?.confirmLoading.value ?? false);

  const showRequirementSpecConfirm = computed(() => analystSkill?.needsRequirementSpecConfirmation.value ?? false);
  const requirementSpecConfirmLoading = computed(() => analystSkill?.confirmLoading.value ?? false);
  const requirementSpecDeliverable = computed(() => {
    const items = pipelineMaterials?.deliverables.value ?? [];
    return items.find(d => (d.fileName ?? d.FileName) === '02-requirement-spec.md' || (d.relativePath ?? d.RelativePath)?.includes('02-requirement-spec.md'));
  });
  const requirementSpecTitle = computed(() => {
    const name = requirementSpecDeliverable.value?.fileName ?? requirementSpecDeliverable.value?.FileName;
    if (name && name !== '02-requirement-spec.md') return name.replace(/\.md$/i, '');
    return undefined;
  });

  async function handleConfirmSkeleton(autoRunAnalyst: boolean) {
    if (!pmSkill) return;
    try {
      await pmSkill.confirmAndProceed(autoRunAnalyst);
      antMessage.success(autoRunAnalyst ? '骨架已确认，三轮需求分析已启动' : '骨架已确认');
      await irObservatory?.refreshAll();
      await refreshPipelineMaterials();
    } catch (e: any) {
      antMessage.error(e?.response?.data?.msg ?? e?.message ?? '确认失败');
    }
  }

  async function handleConfirmRequirementSpec(autoRunDesign: boolean) {
    if (!analystSkill) return;
    try {
      await analystSkill.confirmAndProceed(autoRunDesign);
      antMessage.success(autoRunDesign ? '需求说明书已确认，架构设计已启动' : '需求说明书已确认');
      await irObservatory?.refreshAll();
      await refreshPipelineMaterials();
    } catch (e: any) {
      antMessage.error(e?.response?.data?.msg ?? e?.message ?? '确认失败');
    }
  }

  function handleDownloadRequirementSpec() {
    const d = requirementSpecDeliverable.value;
    if (!d) {
      antMessage.warning('交付物尚未生成，请稍候刷新');
      return;
    }
    void pipelineMaterials?.downloadDeliverable(d);
  }

  // ====== 状态 ======
  const currentStage = ref(1);
  const messages = ref<any[]>([]);
  const blobUrls: string[] = []; // 追踪 Blob URL，组件卸载时释放
  const inputText = ref('');
  const loading = ref(false);
  const attachments = ref<File[]>([]);
  const selectedProvider = ref('deepseek');
  const providers = ref<any[]>([]);
  const selectedStrategy = ref(-1);
  const abortController = ref<AbortController | null>(null);
  const pipelineId = ref(props.pipelineId || 0);
  /** 阶段一 SA 门控是否已通过（未通过时每次发送走 sa-gate，不走 LLM execute） */
  const gatePassed = ref(false);
  const gateProcessing = ref(false);
  const lifecycleLoading = ref(false);

  const observatoryPanelCollapsed = computed(() => irObservatory?.panelCollapsed.value ?? true);

  function refreshPipelineMaterials() {
    return pipelineMaterials?.refresh() ?? Promise.resolve();
  }

  function isLastAssistantMsg(msg: { id: number; role: string }) {
    if (msg.role !== 'assistant') return false;
    for (let i = messages.value.length - 1; i >= 0; i--) {
      const m = messages.value[i];
      if (m.role === 'assistant') return m.id === msg.id;
    }
    return false;
  }

  function formatSkillProgressLine(progress: SseSkillProgressPayload): string {
    const step = progress.saStepName ? ` · ${progress.saStepName}` : '';
    const icon =
      progress.phase === 'completed' || progress.phase === 'stable' ? '✅' : progress.phase === 'failed' || progress.phase === 'aborted' ? '❌' : '▸';
    return `${icon} **${progress.skillId}** ${progress.percent}%${step} — ${progress.message || progress.phase}`;
  }

  function appendWorkflowThinking(msg: any, line: string) {
    if (!line || msg.thinking?.includes(line)) return;
    msg.thinking = (msg.thinking || '') + `\n${line}\n`;
    msg.thinkingCollapsed = false;
  }

  function onToggleObservatory() {
    irObservatory?.togglePanel();
  }

  type WorkMode = 'greenfield' | 'bugfix' | 'enhancement';
  const workMode = ref<WorkMode>('greenfield');
  const sourcePipelineId = ref<number | undefined>();
  const targetPageRoute = ref<string | undefined>();
  const targetPageLabel = ref<string | undefined>();
  const generatedSystems = ref<any[]>([]);
  const pageRoutes = ref<Array<{ route: string; label: string }>>([]);
  const systemsLoading = ref(false);
  const routesLoading = ref(false);

  const WORK_MODE_LABELS: Record<WorkMode, string> = {
    greenfield: '首次全量开发',
    bugfix: 'Debug 修复',
    enhancement: '二次开发',
  };

  const workModeLabel = computed(() => WORK_MODE_LABELS[workMode.value] ?? workMode.value);
  const workModeTagColor = computed(
    () => (({ greenfield: 'blue', bugfix: 'orange', enhancement: 'purple' } as Record<string, string>)[workMode.value] ?? 'default'),
  );

  const generatedSystemOptions = computed(() =>
    generatedSystems.value.map(s => ({
      value: s.id,
      label: s.projectName ?? s.name ?? `#${s.id}`,
    })),
  );

  const pageRouteOptions = computed(() =>
    pageRoutes.value.map(r => ({
      value: r.route,
      label: r.label ? `${r.label} (${r.route})` : r.route,
    })),
  );

  watch(
    pipelineId,
    (id, prevId) => {
      if (prevId !== undefined && prevId !== id) {
        abortController.value?.abort();
        abortController.value = null;
        loading.value = false;
      }
      emit('pipeline-id-change', id);
    },
    { immediate: true },
  );

  const chatStreamRef = ref<HTMLElement>();
  const textareaRef = ref();
  const showScrollUp = ref(false);
  const showScrollDown = ref(false);
  const showScrollButtons = ref(false);
  const autoScroll = ref(true);

  const stages = ref([
    { stage: 1, name: '需求分析', code: 'requirement', status: 'active' },
    { stage: 2, name: '架构设计', code: 'architecture', status: 'pending' },
    { stage: 3, name: '总体设计', code: 'design', status: 'pending' },
    { stage: 4, name: '自动开发', code: 'development', status: 'pending' },
    { stage: 5, name: '交付验证', code: 'delivery', status: 'pending' },
  ]);

  const STAGE_CODE_INDEX: Record<string, number> = {
    requirement: 1,
    architecture: 2,
    design: 3,
    development: 4,
    delivery: 5,
  };

  function stageCodeToNum(code: string | number | undefined): number {
    if (typeof code === 'number') return code;
    if (!code) return 1;
    if (STAGE_CODE_INDEX[code]) return STAGE_CODE_INDEX[code];
    const n = parseInt(String(code), 10);
    return Number.isFinite(n) ? n : 1;
  }

  const thinkingLabels = ['正在分析您的需求...', '正在理解业务领域...', '正在梳理业务规则...', '正在生成领域模型...', '正在设计方案策略...', '正在组织输出...'];
  const gateThinkingLabels = ['正在解析需求材料...', '正在识别业务事件...', '正在评估角色与数据实体...', '正在计算语义完整度...', '正在生成结构化反馈...'];
  const thinkingIndex = ref(0);
  const thinkingLabel = computed(() => {
    const labels = gateProcessing.value ? gateThinkingLabels : thinkingLabels;
    return labels[thinkingIndex.value % labels.length];
  });
  let thinkingTimer: any = null;
  watch(loading, v => {
    if (v) {
      thinkingIndex.value = 0;
      thinkingTimer = setInterval(() => {
        thinkingIndex.value++;
      }, 2500);
    } else {
      clearInterval(thinkingTimer);
    }
  });

  const inputPlaceholder = computed(() => {
    if (loading.value) return 'AI 正在思考中...';
    switch (currentStage.value) {
      case 1:
        return '描述你的业务需求，或回答 AI 的追问...';
      case 2:
        return '对架构方案有疑问？或确认后推进...';
      case 3:
        return '对详细设计有修改意见？或确认后推进...';
      default:
        return '输入消息...';
    }
  });

  // ====== 生命周期 ======
  onMounted(async () => {
    await loadProviders();
    void loadGeneratedSystems();
    if (pipelineId.value > 0) {
      await loadPipelineState();
      await refreshPipelineMaterials();
      if (messages.value.length === 0 && props.initialMessage) await sendMessage(props.initialMessage);
    }
  });

  onUnmounted(() => {
    abortController.value?.abort();
    // 释放所有 Blob URL，防止内存泄漏
    for (const url of blobUrls) {
      URL.revokeObjectURL(url);
    }
    blobUrls.length = 0;
  });

  // ====== 方法 ======
  async function loadGeneratedSystems() {
    systemsLoading.value = true;
    try {
      const res: any = await getGeneratedProjectList(1, 50);
      const data = res?.data ?? res;
      generatedSystems.value = data?.items ?? [];
    } catch {
      generatedSystems.value = [];
    } finally {
      systemsLoading.value = false;
    }
  }

  async function loadPageRoutesForSource(pid: number) {
    routesLoading.value = true;
    pageRoutes.value = [];
    try {
      const res: any = await getPageRoutes(pid);
      const data = res?.data ?? res;
      pageRoutes.value = data?.items ?? [];
    } catch {
      pageRoutes.value = [];
    } finally {
      routesLoading.value = false;
    }
  }

  function onSourcePipelineChange(pid: number) {
    targetPageRoute.value = undefined;
    targetPageLabel.value = undefined;
    if (workMode.value === 'bugfix' && pid) {
      void loadPageRoutesForSource(pid);
    }
  }

  function onTargetPageChange(route: string) {
    const hit = pageRoutes.value.find(r => r.route === route);
    targetPageLabel.value = hit?.label ?? route;
  }

  function validateWorkModeBeforeSend(): string | null {
    if (workMode.value === 'greenfield') return null;
    if (!sourcePipelineId.value) return '请选择已生成系统';
    if (workMode.value === 'bugfix' && !targetPageRoute.value) return 'Debug 修复须选择要修改的页面';
    return null;
  }

  async function loadProviders() {
    try {
      const res = await defHttp.get({ url: '/api/studio/pipeline/execute/providers' });
      providers.value = (res?.items ?? []).filter((p: any) => p.enabled);
      if (providers.value.length > 0) selectedProvider.value = providers.value[0].providerCode;
    } catch {}
  }

  const pipelineLoading = ref(false);

  function mapLoadedMessage(m: any) {
    return {
      id: m.id ?? m.Id ?? `${Date.now()}-${Math.random()}`,
      role: m.role ?? m.Role ?? 'system',
      content: m.content ?? m.Content ?? '',
      thinking: m.thinking ?? m.Thinking ?? '',
      thinkingCollapsed: true,
      strategies: m.strategies ?? m.Strategies ?? [],
      document: m.document ?? m.Document ?? null,
      ir: m.ir ?? m.Ir ?? null,
      actions: m.actions ?? m.Actions ?? [],
      stageConfirmable: m.stageConfirmable ?? m.StageConfirmable ?? false,
      stageConfirmed: m.stageConfirmed ?? m.StageConfirmed ?? false,
      clarification: m.clarification ?? m.Clarification ?? null,
    };
  }

  function detectGatePassedFromLoadedMessages(rawMessages: any[]): boolean {
    for (const m of rawMessages) {
      const stage = m.stage ?? m.Stage;
      const role = m.role ?? m.Role;
      const content = m.content ?? m.Content ?? '';
      if (role === 'system' && stage === 'gate') {
        try {
          const parsed = JSON.parse(content);
          if (parsed.passed === true || parsed.Passed === true) return true;
          if (parsed.passed === false || parsed.Passed === false) return false;
        } catch {
          /* ignore */
        }
      }
    }
    const hasAssistant = rawMessages.some(m => (m.role ?? m.Role) === 'assistant' && (m.content ?? m.Content));
    const hasFailureMarker = rawMessages.some(m => (m.content ?? m.Content)?.includes?.('尚未达到进入流水线的标准'));
    return hasAssistant && !hasFailureMarker;
  }

  async function loadPipelineState() {
    if (!pipelineId.value) return;
    pipelineLoading.value = true;
    try {
      const res = await defHttp.get({ url: '/api/studio/pipeline/execute/' + pipelineId.value });
      const data = (res as any)?.data ?? res;
      currentStage.value = stageCodeToNum(data?.currentStage ?? data?.CurrentStage);
      const rawMessages = data?.messages ?? data?.Messages ?? [];
      const wm = (data?.workMode ?? data?.WorkMode ?? 'greenfield') as WorkMode;
      workMode.value = wm === 'bugfix' || wm === 'enhancement' ? wm : 'greenfield';
      sourcePipelineId.value = data?.sourcePipelineId ?? data?.SourcePipelineId ?? undefined;
      targetPageRoute.value = data?.targetPageRoute ?? data?.TargetPageRoute ?? undefined;
      targetPageLabel.value = data?.targetPageLabel ?? data?.TargetPageLabel ?? undefined;
      gatePassed.value = workMode.value !== 'greenfield' ? true : detectGatePassedFromLoadedMessages(rawMessages);
      messages.value = rawMessages.map(mapLoadedMessage);
      updateStageStatus();
      scrollToBottom();
      if (currentStage.value >= 5) {
        void pipelineMaterials?.triggerDeliveryArtifacts(false);
      }
      await refreshPipelineMaterials();
    } catch (e) {
      console.error('加载状态失败', e);
    } finally {
      pipelineLoading.value = false;
    }
  }

  function updateStageStatus() {
    stages.value.forEach(s => {
      if (s.stage < currentStage.value) s.status = 'completed';
      else if (s.stage === currentStage.value) s.status = 'active';
      else s.status = 'pending';
    });
  }

  // ====== SSE 流式消息 ======
  const scrollOnStream = () => {
    if (autoScroll.value) scrollToBottom();
  };

  async function processSseEvent(data: Record<string, any>, msg: any): Promise<void> {
    switch (data.type) {
      case 'attachments_processing': {
        const hint = parseGatePayload<{ message?: string }>(data.data);
        msg.thinking += `\n📎 ${hint?.message ?? '正在处理附件…'}\n`;
        scrollOnStream();
        break;
      }
      case 'attachments_ready': {
        const attPayload = parseGatePayload<AttachmentsReadyPayload>(data.data) ?? {};
        msg.thinking += '\n' + buildAttachmentsReadyMarkdown(attPayload);
        scrollOnStream();
        await refreshPipelineMaterials();
        irObservatory && (irObservatory.preferredObservatoryTab.value = 'deliverables');
        break;
      }
      case 'gate_started':
        gateProcessing.value = true;
        msg.thinking += '🔍 SA 门控：正在评估需求材料能否解析为合格业务事件…\n';
        scrollOnStream();
        break;
      case 'gate_passed': {
        gateProcessing.value = false;
        gatePassed.value = true;
        msg.thinkingCollapsed = true;
        msg.actions = [];
        const payload = parseGatePayload<GatePassedPayload>(data.data) ?? {};
        const sf = normalizeSemanticFitness(payload) ?? ({ passed: true, score: 0, level: 'sufficient', identified: [], missing: [] } as const);
        const passedMd = buildGatePassedMarkdown(payload, sf);
        await streamTextToMessage(msg, passedMd, { onChunk: scrollOnStream });
        await refreshPipelineMaterials();
        break;
      }
      case 'gate_failed': {
        gateProcessing.value = false;
        gatePassed.value = false;
        msg.thinkingCollapsed = true;
        const failPayload = parseGatePayload<GateFailedPayload>(data.data) ?? {};
        const failSf = normalizeSemanticFitness(failPayload) ?? ({ passed: false, score: 0, level: 'insufficient', identified: [], missing: [] } as const);
        msg.actions = gateFailedActions();
        const failMd = buildGateFailedMarkdown(failPayload, failSf);
        await streamTextToMessage(msg, failMd, { onChunk: scrollOnStream });
        break;
      }
      case 'gate_error': {
        gateProcessing.value = false;
        const errPayload = parseGatePayload<GateErrorPayload>(data.data) ?? {
          message: data.data || data.content,
        };
        msg.thinkingCollapsed = true;
        msg.actions = gateErrorActions();
        await streamTextToMessage(msg, buildGateErrorMarkdown(errPayload), { onChunk: scrollOnStream });
        break;
      }
      case 'pm_skill_started': {
        const hint = parseGatePayload<{ pipelineId?: number; source?: string }>(data.data);
        msg.thinking += `\n📋 PM Skill 已启动（${hint?.source ?? 'gate_pass'}），正在提取 IR-0 业务事件骨架…\n`;
        scrollOnStream();
        break;
      }
      case 'stage_transition':
        msg.thinking += `\n✅ 已进入阶段：${data.data || data.content || 'requirement'}\n`;
        scrollOnStream();
        break;
      case 'thinking':
      case 'info':
        msg.thinking += (data.data || data.content || '') + '\n';
        scrollOnStream();
        break;
      case 'token':
      case 'delta':
        msg.content += data.data || data.content || data.delta?.content || '';
        scrollOnStream();
        break;
      case 'clarification_requested': {
        // ADR-005：后端在需求分析阶段下发结构化选择题，暂停流式 LLM 等待用户作答
        const clarificationData = parseSseJsonPayload(data.data);
        msg.clarification = clarificationData || data.clarification || null;
        scrollOnStream();
        break;
      }
      case 'strategy':
        msg.strategies = data.data || data.strategies || [];
        break;
      case 'document':
        msg.document = data.data || data.document;
        break;
      case 'ir':
        msg.ir = data.data || data.ir;
        break;
      case 'ir_event': {
        const irPayload = parseSseJsonPayload(data.data) as SseIrEventPayload | null;
        if (irPayload) {
          irObservatory?.onIrEvent(irPayload);
          irObservatory?.onIr3PipelineEvent(irPayload);
          if (irPayload.eventType === 'SA_Step_Completed') {
            const preview = irPayload.payloadPreview as Record<string, unknown> | string | undefined;
            const step =
              (irPayload as { saStepName?: string }).saStepName ??
              (typeof preview === 'object' && preview ? String(preview.stepName ?? preview.saStepName ?? '') : '');
            if (step) appendWorkflowThinking(msg, `✅ SA 九步 · ${step}`);
            scrollOnStream();
          }
          if (irPayload.fragmentType?.startsWith('IR0')) {
            msg.ir = irPayload.payloadPreview;
          }
          if (irPayload.fragmentType?.startsWith('IR2') || irPayload.eventType?.includes('Design')) {
            void designSkill?.refreshDesignContext();
          }
          if (irPayload.eventType === 'ConstraintViolationReported') {
            designSkill?.applyConstraintEvent(irPayload.payloadPreview);
          }
        }
        break;
      }
      case 'fragment_updated': {
        const fragPayload = parseSseJsonPayload(data.data) as SseFragmentUpdatedPayload | null;
        if (fragPayload) irObservatory?.onFragmentUpdated(fragPayload);
        break;
      }
      case 'skill_progress': {
        const progress = parseSseJsonPayload(data.data) as SseSkillProgressPayload | null;
        if (progress) {
          analystSkill?.handleSkillProgress(progress);
          designSkill?.handleSkillProgress(progress);
          developerSkill?.handleSkillProgress(progress);
          appendWorkflowThinking(msg, formatSkillProgressLine(progress));
          scrollOnStream();
          if (progress.phase === 'failed' || progress.phase === 'aborted') {
            msg.content += `\n\n**⚠️ Skill 执行异常（${progress.skillId}）**\n\n${progress.message || '未知错误'}\n`;
            if (progress.code) msg.content += `\n错误码：\`${progress.code}\`\n`;
            scrollOnStream();
          }
        }
        break;
      }
      case 'analysis_completed': {
        const done = parseSseJsonPayload(data.data) as SseAnalysisCompletedPayload | null;
        analystSkill?.markAnalysisCompleted();
        void designSkill?.refreshDesignContext();
        void irObservatory?.refreshAll();
        void refreshPipelineMaterials();
        if (done) {
          msg.content += `\n\n---\n\n✅ **需求分析完成**：${done.eventSpecCount} 个 EventSpec 已进入 stable 状态。详细 IR 见右侧观测台。\n`;
          scrollOnStream();
        }
        break;
      }
      case 'preview_ready': {
        const preview = parseSseJsonPayload(data.data) as { previewUrl?: string; sandboxId?: string } | null;
        void refreshPipelineMaterials();
        void irObservatory?.refreshAll();
        if (preview?.previewUrl) {
          msg.content += `\n\n---\n\n🚀 **试用环境已就绪**：[打开试用链接](${preview.previewUrl})\n`;
          scrollOnStream();
        }
        break;
      }
      case 'stage_complete':
        msg.stageConfirmable = true;
        if (msg.content && !msg.document) {
          const stageName = stages.value[currentStage.value - 1]?.name || '分析结果';
          const blob = new Blob([msg.content], { type: 'text/markdown;charset=utf-8' });
          const url = URL.createObjectURL(blob);
          blobUrls.push(url);
          msg.document = {
            name: stageName + '_' + pipelineId.value,
            previewUrl: url,
            downloadPdfUrl: url,
            downloadWordUrl: url,
          };
        }
        break;
      case 'done':
        break;
      case 'error': {
        const errText = data.data || data.content || 'AI 响应异常';
        msg.content += `\n\n## ⚠️ 流水线异常\n\n${errText}\n`;
        msg.actions = gateErrorActions();
        scrollOnStream();
        break;
      }
    }
  }

  async function readSseStream(aiMsgId: number): Promise<void> {
    abortController.value = new AbortController();
    const sseUrl = buildFetchSseUrl('/api/studio/pipeline/execute/' + pipelineId.value + '/events');
    const sseHeaders: Record<string, string> = { Accept: 'text/event-stream' };
    const authHeader = getAuthHeader();
    if (authHeader) sseHeaders['Authorization'] = authHeader;

    const response = await fetch(sseUrl, {
      method: 'GET',
      headers: sseHeaders,
      signal: abortController.value.signal,
    });
    if (!response.ok) throw new Error('HTTP ' + response.status);

    const reader = response.body?.getReader();
    const decoder = new TextDecoder();
    let buffer = '';
    if (!reader) return;

    while (true) {
      const { done, value } = await reader.read();
      if (done) break;
      buffer += decoder.decode(value, { stream: true });
      const lines = buffer.split('\n');
      buffer = lines.pop() || '';
      for (const line of lines) {
        if (line.startsWith(':')) {
          irObservatory?.onSseHeartbeat?.();
          continue;
        }
        if (!line.startsWith('data: ') || line === 'data: [DONE]') continue;
        try {
          const data = JSON.parse(line.substring(6));
          const msg = messages.value.find(m => m.id === aiMsgId);
          if (!msg) continue;
          await processSseEvent(data, msg);
        } catch {
          /* skip malformed line */
        }
      }
    }
  }

  async function sendMessage(content: string, uploadedFiles?: Array<{ name: string; url: string }>) {
    if (!content.trim() && (!uploadedFiles || uploadedFiles.length === 0)) return;
    loading.value = true;
    autoScroll.value = true;
    const submittedText = content.trim();
    const initialThinking = submittedText
      ? `📝 已收到原始需求：${submittedText.slice(0, 180)}${submittedText.length > 180 ? '…' : ''}\n`
      : uploadedFiles?.length
      ? `📎 已收到 ${uploadedFiles.length} 个附件，正在解析需求材料…\n`
      : '';

    messages.value.push({
      id: Date.now(),
      role: 'user',
      content: submittedText || (uploadedFiles?.map(f => `📎 ${f.name}`).join('\n') ?? ''),
      time: new Date().toLocaleTimeString(),
    });
    scrollToBottom();

    if (!pipelineId.value) {
      const modeError = validateWorkModeBeforeSend();
      if (modeError) {
        loading.value = false;
        antMessage.warning(modeError);
        messages.value.pop();
        return;
      }
      try {
        const res = await createPipeline({
          requirement: content,
          workMode: workMode.value,
          sourcePipelineId: workMode.value !== 'greenfield' ? sourcePipelineId.value : undefined,
          targetPageRoute: workMode.value === 'bugfix' ? targetPageRoute.value : undefined,
          targetPageLabel: workMode.value === 'bugfix' ? targetPageLabel.value : undefined,
        });
        const data = res?.data || res;
        pipelineId.value = data?.pipelineId || data?.PipelineId || data?.id || data?.Id || pipelineId.value;
        if (!pipelineId.value) {
          loading.value = false;
          messages.value.push({
            id: Date.now() + 2,
            role: 'assistant',
            content: '## ⚠️ 流水线创建失败\n\n未获取到有效 Pipeline ID，请重试。',
            actions: gateErrorActions(),
          });
          scrollToBottom();
          return;
        }
      } catch (e: any) {
        loading.value = false;
        messages.value.push({
          id: Date.now() + 2,
          role: 'assistant',
          content: `## ⚠️ 创建失败\n\n${e?.message || '未知错误'}`,
          actions: gateErrorActions(),
        });
        scrollToBottom();
        return;
      }
    }

    const aiMsgId = Date.now() + 1;
    const needsSaGate = workMode.value === 'greenfield' && currentStage.value === 1 && !gatePassed.value;
    const isQuickBugfix = workMode.value === 'bugfix';
    const isQuickEnhancement = workMode.value === 'enhancement';

    messages.value.push({
      id: aiMsgId,
      role: 'assistant',
      content: '',
      thinking: initialThinking,
      thinkingCollapsed: false,
      strategies: [],
      document: null,
      ir: null,
      actions: [] as ChatStreamAction[],
      stageConfirmable: false,
      stageConfirmed: false,
      clarification: null,
    });

    try {
      if (isQuickBugfix) {
        gateProcessing.value = false;
        gatePassed.value = true;
        const aiMsg = messages.value.find(m => m.id === aiMsgId);
        if (aiMsg) aiMsg.content = '🔧 **Debug 修复已启动**\n\n正在定位根因并增量重算受影响 Skill（跳过 SA 门控全链）…';
        await quickBugfix(pipelineId.value, content);
        scrollToBottom();
      } else if (isQuickEnhancement) {
        gateProcessing.value = false;
        gatePassed.value = true;
        const aiMsg = messages.value.find(m => m.id === aiMsgId);
        if (aiMsg) aiMsg.content = '🔄 **二次开发已启动**\n\n已继承源系统 IR，调度增量 Skill（跳过 SA 门控）…';
        await quickEnhancement(pipelineId.value, content);
        scrollToBottom();
      } else if (needsSaGate) {
        gateProcessing.value = true;
        const aiMsg = messages.value.find(m => m.id === aiMsgId);
        if (uploadedFiles?.length && aiMsg) {
          aiMsg.thinking += '📎 附件将随 SA 门控一并登记并解析（inte_assistant_attachment）…\n';
          scrollOnStream();
        }
        await triggerSaGate(pipelineId.value, content, true, uploadedFiles);
      } else {
        gateProcessing.value = false;
        await defHttp.post({
          url: '/api/studio/pipeline/execute/' + pipelineId.value + '/execute',
          data: {
            message: content,
            stageName: stages.value[currentStage.value - 1]?.code || 'requirement',
            provider: selectedProvider.value,
            attachments: uploadedFiles || [],
          },
        });
      }
      await readSseStream(aiMsgId);
    } catch (e: any) {
      const msg = messages.value.find(m => m.id === aiMsgId);
      if (e.name === 'AbortError') {
        if (msg) msg.content += '\n\n⏹️ [已停止生成]';
      } else if (msg) {
        if (!msg.content) {
          msg.content = `## ⚠️ 连接失败\n\n${e.message || '发送失败，请重试'}`;
          msg.actions = gateErrorActions();
        }
      }
    } finally {
      loading.value = false;
      gateProcessing.value = false;
      abortController.value = null;
      if (autoScroll.value) scrollToBottom();
    }
  }

  function handleChatAction(act: ChatStreamAction) {
    if (act.action === 'fill_prompt' && act.payload) {
      inputText.value = act.payload;
      nextTick(() => textareaRef.value?.focus?.());
      scrollToBottom();
      return;
    }
    if (act.action === 'focus_input') {
      nextTick(() => textareaRef.value?.focus?.());
    }
  }

  function handleSend() {
    const content = inputText.value.trim();
    // 允许纯附件发送（无文字）—— 门控会根据附件内容分析
    if ((!content && attachments.value.length === 0) || loading.value) return;
    inputText.value = '';

    // ═══ 上传附件到 JNPF 文件服务 ═══
    const filesToUpload = [...attachments.value];
    attachments.value = [];
    uploadAttachmentsAndSend(content, filesToUpload);
  }

  async function uploadAttachmentsAndSend(content: string, files: File[]) {
    const uploadedFiles: Array<{ name: string; url: string }> = [];
    const failedNames: string[] = [];
    const CONCURRENCY = 3;
    const tenantId = getTenantId();

    if (files.length > 0) {
      for (let i = 0; i < files.length; i += CONCURRENCY) {
        const batch = files.slice(i, i + CONCURRENCY);
        const results = await Promise.allSettled(
          batch.map(async file => {
            const formData = new FormData();
            formData.append('file', file);
            // 勿手动设置 Content-Type — axios 需自动附带 multipart boundary
            const res = await defHttp.post({
              url: '/api/file/Uploader/annex',
              data: formData,
              headers: tenantId ? { 'X-Tenant-Id': tenantId } : undefined,
            });
            const fileModel = res?.data;
            const url = typeof fileModel?.url === 'string' ? fileModel.url : '';
            if (!url) throw new Error('上传响应缺少 url');
            return { name: file.name, url };
          }),
        );
        results.forEach((r, idx) => {
          if (r.status === 'fulfilled' && r.value?.url) {
            uploadedFiles.push(r.value);
          } else {
            failedNames.push(batch[idx]?.name ?? '未知文件');
            console.error('附件上传失败:', r.status === 'rejected' ? r.reason : '缺少 url');
          }
        });
      }
    }

    if (failedNames.length > 0) {
      messages.value.push({
        id: Date.now(),
        role: 'assistant',
        content: `## ⚠️ 附件上传失败\n\n以下文件未能上传到文件服务：\n${failedNames.map(n => `- ${n}`).join('\n')}\n\n请确认已登录且租户有效后重试。`,
        actions: gateErrorActions(),
      });
      scrollToBottom();
      if (!content && uploadedFiles.length === 0) return;
    }

    if (content || uploadedFiles.length > 0) {
      await sendMessage(content, uploadedFiles);
    }
  }

  function handleEnter(e: KeyboardEvent) {
    if (!e.shiftKey) {
      e.preventDefault();
      handleSend();
    }
  }

  function handleStop() {
    abortController.value?.abort();
  }

  function handleSelectStrategy(idx: number, s: any) {
    selectedStrategy.value = idx;
    inputText.value = '我选择：' + s.title + '。' + (s.description || '');
    nextTick(() => handleSend());
  }

  // ADR-005 / 27 号：用户完成澄清作答后，清空卡片并触发下一轮
  async function onClarificationAnswered(msg: any, payload: { setId: string; triggerNextRound: boolean; nextAction: string; stage: string }) {
    msg.clarification = null;
    if (!payload.triggerNextRound || payload.nextAction === 'none') return;

    // 创建新的 assistant 消息占位，承接下一轮 SSE 流
    const aiMsgId = Date.now();
    const thinkingText =
      payload.nextAction === 'rerun-architect'
        ? '🏗️ 已收到架构澄清作答，正在重新运行架构设计（ToT）…\n'
        : payload.nextAction === 'rerun-system-design-clarification'
        ? '📐 已收到总体设计澄清作答，正在运行约束引擎并锁定系统设计…\n'
        : payload.nextAction === 'continue-requirement-analysis'
        ? '📋 已收到需求分析澄清作答，正在继续三轮精化…\n'
        : '🔄 已收到澄清补充，正在重新评估需求成熟度…\n';
    messages.value.push({
      id: aiMsgId,
      role: 'assistant',
      content: '',
      thinking: thinkingText,
      thinkingCollapsed: false,
      strategies: [],
      document: null,
      ir: null,
      actions: [] as ChatStreamAction[],
      stageConfirmable: false,
      stageConfirmed: false,
      clarification: null,
    });
    scrollToBottom();

    try {
      if (payload.nextAction === 'rerun-architect') {
        // 架构阶段二：重跑 architect-skill（读已 stable 的澄清答案，跑 ToT）
        await runArchitectSkill(pipelineId.value, {});
      } else if (payload.nextAction === 'rerun-system-design-clarification') {
        // 总体设计阶段二：重跑 system-design-clarification-skill（读已 stable 的澄清答案，跑约束引擎 + 锁定）
        await runSystemDesignClarificationSkill(pipelineId.value, {});
      } else if (payload.nextAction === 'continue-requirement-analysis') {
        // 27 号三轮编排器：续跑 requirement-analysis/run
        await runRequirementAnalysis(pipelineId.value, {});
      } else {
        // 需求阶段：触发 sa-gate，后端读取最新对话历史（含澄清补充）重新做 maturity 评估
        await triggerSaGate(pipelineId.value, '继续分析', false, []);
      }
      await readSseStream(aiMsgId);
    } catch (e: any) {
      const m = messages.value.find(x => x.id === aiMsgId);
      if (m) m.content += `\n\n⚠️ 重新评估失败：${e?.message || e}`;
    }
  }

  // 逃生口：用户选择"全部跳过直接分析"，卡片自身已提交 skipAll，这里只清空 UI
  function onClarificationSkipAll(msg: any) {
    msg.clarification = null;
  }

  async function handleConfirmStage(msg: any) {
    try {
      const res: any = await defHttp.post({
        url: '/api/studio/pipeline/execute/stage/' + pipelineId.value + '/confirm',
        data: { approved: true },
      });
      const data = res?.data ?? res;
      const triggered: string[] = data?.triggeredSkillIds ?? data?.TriggeredSkillIds ?? [];
      msg.stageConfirmed = true;
      if (currentStage.value >= 5) {
        emit('pipeline-complete', { stage: 5 });
        antMessage.success('全部阶段已完成！');
        return;
      }
      currentStage.value++;
      updateStageStatus();
      if (triggered.length > 0) {
        messages.value.push({
          id: Date.now(),
          role: 'system',
          content: '🚀 已触发 Skill：' + triggered.join('、') + '（后台运行中，请稍候观测台更新）',
        });
      } else {
        messages.value.push({
          id: Date.now(),
          role: 'system',
          content: '✅ 已进入阶段 ' + currentStage.value + ': ' + stages.value[currentStage.value - 1]?.name,
        });
        sendMessage('请开始阶段 ' + currentStage.value + '：' + stages.value[currentStage.value - 1]?.name);
      }
      scrollToBottom();
      if (currentStage.value >= 5) {
        void pipelineMaterials?.triggerDeliveryArtifacts(true);
      }
    } catch (e: any) {
      messages.value.push({
        id: Date.now(),
        role: 'assistant',
        content: `## ⚠️ 阶段确认失败\n\n${e?.message || '请重试'}`,
        actions: gateErrorActions(),
      });
      scrollToBottom();
    }
  }

  async function handleRollback(target: number) {
    if (target < 1) return;
    currentStage.value = target;
    updateStageStatus();
    messages.value.push({ id: Date.now(), role: 'system', content: '↩️ 已回退到阶段 ' + target + ': ' + stages.value[target - 1]?.name });
    scrollToBottom();
  }

  function handleUpload(file: File) {
    attachments.value.push(file);
    return false;
  }

  function handleNewChat() {
    if (pipelineId.value > 0 && messages.value.length > 0) {
      Modal.confirm({
        title: '开启新任务？',
        content: '当前流水线已保存，可在左侧「我的任务」中随时切换回来。确定开启新对话？',
        okText: '开启新任务',
        cancelText: '取消',
        onOk: () => resetChat(),
      });
      return;
    }
    resetChat();
  }

  async function handleFreezePipeline() {
    if (!pipelineId.value || lifecycleLoading.value) return;
    lifecycleLoading.value = true;
    try {
      await freezePipeline(pipelineId.value, '用户冻结');
      antMessage.success('流水线已冻结，写操作已锁定；可点「恢复」继续');
    } catch (e: any) {
      antMessage.error(e?.message || '冻结失败');
    } finally {
      lifecycleLoading.value = false;
    }
  }

  async function handleResumePipeline() {
    if (!pipelineId.value || lifecycleLoading.value) return;
    lifecycleLoading.value = true;
    try {
      await resumePipeline(pipelineId.value);
      antMessage.success('流水线已恢复');
    } catch (e: any) {
      antMessage.error(e?.message || '恢复失败');
    } finally {
      lifecycleLoading.value = false;
    }
  }

  async function handleForkPipeline() {
    if (!pipelineId.value || lifecycleLoading.value) return;
    lifecycleLoading.value = true;
    try {
      const res = await forkPipeline(pipelineId.value, { workMode: 'enhancement' });
      const data = (res as any)?.data ?? res;
      const newId = data?.pipelineId ?? data?.PipelineId;
      antMessage.success(newId ? `已 Fork 为流水线 ${newId}` : 'Fork 成功');
      if (newId) {
        pipelineId.value = Number(newId);
        await loadPipelineState();
      }
    } catch (e: any) {
      antMessage.error(e?.message || 'Fork 失败');
    } finally {
      lifecycleLoading.value = false;
    }
  }

  function resetChat() {
    pipelineId.value = 0;
    workMode.value = 'greenfield';
    sourcePipelineId.value = undefined;
    targetPageRoute.value = undefined;
    targetPageLabel.value = undefined;
    pageRoutes.value = [];
    gatePassed.value = false;
    gateProcessing.value = false;
    messages.value = [];
    inputText.value = '';
    attachments.value = [];
    currentStage.value = 1;
    updateStageStatus();
    emit('new-chat');
  }

  async function switchPipeline(id: number) {
    if (!id) return;
    abortController.value?.abort();
    abortController.value = null;
    loading.value = false;
    gateProcessing.value = false;
    pipelineId.value = id;
    messages.value = [];
    await loadPipelineState();
  }

  function handleProviderChange(code: string) {
    selectedProvider.value = code;
  }

  function previewDoc(doc: any) {
    window.open(doc.previewUrl, '_blank');
  }
  function downloadDoc(doc: any, fmt: string) {
    const a = document.createElement('a');
    a.href = fmt === 'pdf' ? doc.downloadPdfUrl : doc.downloadWordUrl;
    a.download = doc.name + (fmt === 'pdf' ? '.pdf' : '.docx');
    a.click();
  }

  // ====== 滚动控制 ======
  function scrollToBottom() {
    nextTick(() => {
      if (chatStreamRef.value) chatStreamRef.value.scrollTop = chatStreamRef.value.scrollHeight;
    });
  }
  function scrollToTop() {
    chatStreamRef.value?.scrollTo({ top: 0, behavior: 'smooth' });
  }
  function handleScroll() {
    const el = chatStreamRef.value;
    if (!el) return;
    showScrollUp.value = el.scrollTop > 200;
    showScrollDown.value = el.scrollHeight - el.scrollTop - el.clientHeight > 200;
    showScrollButtons.value = showScrollUp.value || showScrollDown.value;
    autoScroll.value = el.scrollHeight - el.scrollTop - el.clientHeight < 100;
  }

  defineExpose({
    pipelineId,
    currentStage,
    stages,
    switchPipeline,
  });
</script>

<style scoped lang="less">
  .work-mode-bar {
    display: flex;
    flex-wrap: wrap;
    align-items: center;
    gap: 8px 12px;
    padding: 8px 16px;
    background: #fff;
    border-bottom: 1px solid #e8e8e8;
    flex-shrink: 0;

    .work-mode-select {
      min-width: 200px;
      max-width: 280px;
    }

    .work-mode-meta {
      font-size: 12px;
      color: #8c8c8c;
    }
  }
  .ai-chat-panel {
    display: flex;
    flex-direction: column;
    height: 100%;
    width: 100%;
    overflow: hidden;
    background: #f5f5f5;
    font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'PingFang SC', sans-serif;
    position: relative;
  }

  /* ====== 顶栏 ====== */
  .top-bar {
    display: flex;
    align-items: center;
    justify-content: space-between;
    height: 48px;
    padding: 0 16px;
    border-bottom: 1px solid #e8e8e8;
    background: #fff;
    flex-shrink: 0;
    .top-bar-left,
    .top-bar-center,
    .top-bar-right {
      display: flex;
      align-items: center;
      gap: 8px;
    }
    .model-select {
      width: 180px;
    }
    .stage-text {
      font-size: 13px;
      color: #666;
      font-weight: 500;
    }
  }

  .stage-detail-popover {
    min-width: 200px;
    .stage-item {
      display: flex;
      align-items: center;
      gap: 8px;
      padding: 6px 0;
      .stage-dot {
        width: 8px;
        height: 8px;
        border-radius: 50%;
        background: #d9d9d9;
      }
      &.active .stage-dot {
        background: #1890ff;
      }
      &.completed .stage-dot {
        background: #52c41a;
      }
      .stage-name {
        flex: 1;
        font-size: 13px;
      }
    }
  }

  /* ====== 对话流 ====== */
  .chat-stream {
    flex: 1;
    overflow-y: auto;
    overflow-x: hidden;
    padding: 24px 0;
    scroll-behavior: smooth;
  }

  /* ====== 欢迎卡片 ====== */
  .welcome-card {
    max-width: 680px;
    margin: 0 auto 24px;
    background: #fff;
    border: 1px solid #e8e8e8;
    border-radius: 12px;
    padding: 32px;
    text-align: center;

    &.loading-card {
      padding: 48px 32px;
    }
    .welcome-icon {
      font-size: 48px;
      margin-bottom: 12px;
    }
    h2 {
      font-size: 20px;
      margin-bottom: 12px;
      font-weight: 600;
      color: #1a1a1a;
    }
    p {
      font-size: 14px;
      color: #333;
      line-height: 1.8;
      margin-bottom: 4px;
    }
    .hint {
      color: #1890ff;
      font-size: 13px;
      margin-bottom: 20px;
    }
    .gate-hint {
      color: #d48806;
      background: #fffbe6;
      padding: 8px 12px;
      border-radius: 6px;
      border: 1px solid #ffe58f;
      text-align: left;
      margin-bottom: 16px;
    }
    .gate-format {
      text-align: left;
      border: 1px solid #e8e8e8;
      border-radius: 8px;
      padding: 10px 12px;
      background: #fafafa;
      .gate-format-title {
        font-size: 13px;
        font-weight: 600;
        color: #333;
        margin-bottom: 6px;
      }
      ul {
        margin: 0;
        padding-left: 18px;
      }
      li {
        font-size: 12px;
        color: #666;
        margin-bottom: 4px;
      }
      .gate-format-example {
        margin-top: 6px;
        font-size: 12px;
        color: #1890ff;
      }
    }
  }

  /* ====== 消息卡片（全宽，设计稿原版） ====== */
  .msg-card {
    display: flex;
    gap: 12px;
    max-width: 680px;
    margin: 0 auto 16px;
    padding: 16px 20px;
    background: #fff;
    border: 1px solid #e8e8e8;
    border-radius: 12px;
  }

  .card-avatar {
    width: 32px;
    height: 32px;
    border-radius: 50%;
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 16px;
    flex-shrink: 0;
  }

  .card-body {
    flex: 1;
    min-width: 0;
    font-size: 14px;
    line-height: 1.7;
    color: #333;
    word-break: break-word;
  }

  .card-text {
    :deep(h1),
    :deep(h2),
    :deep(h3) {
      margin: 16px 0 8px;
      font-weight: 600;
    }
    :deep(h1) {
      font-size: 18px;
    }
    :deep(h2) {
      font-size: 16px;
    }
    :deep(h3) {
      font-size: 15px;
    }
    :deep(strong) {
      font-weight: 600;
    }
    :deep(li) {
      margin-left: 16px;
      margin-bottom: 4px;
    }
  }

  /* ====== Markdown 渲染样式 ====== */
  :deep(.md-table-wrap) {
    overflow-x: auto;
    margin: 12px 0;
    border: 1px solid #e8e8e8;
    border-radius: 8px;
  }
  :deep(table) {
    width: 100%;
    border-collapse: collapse;
    font-size: 13px;
    th,
    td {
      padding: 10px 14px;
      border-bottom: 1px solid #f0f0f0;
      text-align: left;
    }
    th {
      background: #fafafa;
      font-weight: 600;
      color: #1a1a1a;
      border-bottom: 2px solid #e8e8e8;
    }
    tr:hover td {
      background: #f9f9f9;
    }
    tr:last-child td {
      border-bottom: none;
    }
  }

  :deep(.md-code-block) {
    margin: 12px 0;
    border: 1px solid #e8e8e8;
    border-radius: 8px;
    overflow: hidden;
    background: #f6f8fa;
  }
  :deep(.md-code-header) {
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: 6px 12px;
    background: #f0f0f0;
    border-bottom: 1px solid #e8e8e8;
    font-size: 12px;
    color: #666;
  }
  :deep(.md-copy-btn) {
    background: none;
    border: 1px solid #d9d9d9;
    border-radius: 4px;
    padding: 2px 8px;
    font-size: 11px;
    cursor: pointer;
    color: #666;
    &:hover {
      border-color: #1890ff;
      color: #1890ff;
    }
  }
  :deep(.md-code-block pre) {
    margin: 0;
    padding: 12px 16px;
    overflow-x: auto;
  }
  :deep(.md-code-block code) {
    font-family: 'Menlo', 'Monaco', 'Consolas', monospace;
    font-size: 13px;
    line-height: 1.5;
  }

  :deep(code:not(.hljs)) {
    background: #f5f5f5;
    padding: 2px 6px;
    border-radius: 4px;
    font-size: 13px;
    font-family: 'Menlo', 'Monaco', 'Consolas', monospace;
  }

  :deep(.md-blockquote) {
    margin: 12px 0;
    padding: 8px 16px;
    border-left: 4px solid #1890ff;
    background: #f0f7ff;
    border-radius: 0 8px 8px 0;
    color: #555;
    p {
      margin: 0;
    }
  }

  :deep(ul),
  :deep(ol) {
    padding-left: 20px;
    margin: 8px 0;
    li {
      margin-bottom: 4px;
      line-height: 1.7;
    }
  }

  :deep(a) {
    color: #1890ff;
    text-decoration: none;
    &:hover {
      text-decoration: underline;
    }
  }

  :deep(hr) {
    border: none;
    border-top: 1px solid #e8e8e8;
    margin: 16px 0;
  }

  :deep(img) {
    max-width: 100%;
    border-radius: 8px;
    margin: 8px 0;
  }

  /* ====== 思考过程（折叠） ====== */
  .thinking-block {
    margin-bottom: 12px;
    border: 1px solid #e8e8e8;
    border-radius: 8px;
    background: #fafafa;
    overflow: hidden;
    .thinking-header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding: 8px 12px;
      cursor: pointer;
      font-size: 12px;
      color: #888;
      &:hover {
        background: #f0f0f0;
      }
    }
    .thinking-content {
      padding: 0 12px 8px;
      font-size: 12px;
      color: #999;
      line-height: 1.6;
      white-space: pre-wrap;
    }
  }

  /* ====== 思考中动画 ====== */
  .thinking-anim {
    display: flex;
    align-items: center;
    gap: 8px;
    .thinking-label {
      color: #1890ff;
      font-size: 13px;
    }
    .dots {
      display: flex;
      gap: 4px;
      i {
        width: 6px;
        height: 6px;
        border-radius: 50%;
        background: #1890ff;
        animation: bounce 1.4s infinite ease-in-out both;
        &:nth-child(1) {
          animation-delay: -0.32s;
        }
        &:nth-child(2) {
          animation-delay: -0.16s;
        }
      }
    }
  }
  @keyframes bounce {
    0%,
    80%,
    100% {
      transform: scale(0);
    }
    40% {
      transform: scale(1);
    }
  }

  /* ====== 流内操作按钮 ====== */
  .stream-action-bar {
    display: flex;
    flex-wrap: wrap;
    gap: 8px;
    margin-top: 12px;
    padding-top: 12px;
    border-top: 1px dashed #e8e8e8;
  }

  /* ====== 策略选项卡片 ====== */
  .strategy-cards {
    display: flex;
    flex-direction: column;
    gap: 8px;
    margin-top: 12px;
  }
  .strategy-card {
    display: flex;
    align-items: center;
    gap: 12px;
    padding: 12px;
    border: 1px solid #e8e8e8;
    border-radius: 8px;
    background: #fff;
    cursor: pointer;
    transition: all 0.2s;
    &:hover {
      border-color: #1890ff;
      background: #f0f7ff;
    }
    .strategy-icon {
      font-size: 18px;
      flex-shrink: 0;
    }
    .strategy-info {
      flex: 1;
    }
    .strategy-title {
      font-weight: 600;
      font-size: 14px;
      margin-bottom: 2px;
    }
    .strategy-desc {
      font-size: 13px;
      color: #666;
    }
  }

  /* ====== 文档卡片 ====== */
  .doc-card {
    display: flex;
    align-items: center;
    gap: 12px;
    margin-top: 12px;
    padding: 12px;
    border: 1px solid #e8e8e8;
    border-radius: 8px;
    background: #fff;
    .doc-emoji {
      font-size: 24px;
    }
    .doc-info {
      flex: 1;
      .doc-name {
        font-weight: 500;
        font-size: 14px;
      }
    }
    .doc-actions {
      display: flex;
      gap: 4px;
    }
  }

  /* ====== 阶段确认卡片 ====== */
  .stage-confirm-card {
    margin-top: 16px;
    padding: 12px 16px;
    border: 1px solid #b7eb8f;
    border-radius: 8px;
    background: #f6ffed;
    .confirm-badge {
      font-size: 14px;
      font-weight: 500;
      color: #52c41a;
      margin-bottom: 12px;
    }
    .confirm-actions {
      display: flex;
      justify-content: flex-end;
      gap: 8px;
    }
  }

  .confirmed-badge {
    margin-top: 12px;
    padding: 8px 12px;
    background: #f0f0f0;
    border-radius: 6px;
    font-size: 13px;
    color: #52c41a;
    text-align: center;
  }

  /* ====== 系统消息 ====== */
  .system-msg {
    display: flex;
    align-items: center;
    gap: 12px;
    padding: 12px 48px;
    max-width: 680px;
    margin: 0 auto;
    .system-line {
      flex: 1;
      height: 1px;
      background: #e8e8e8;
    }
    .system-text {
      font-size: 12px;
      color: #999;
      white-space: nowrap;
    }
  }

  /* ====== 滚动按钮 ====== */
  .scroll-btns {
    position: absolute;
    right: 24px;
    bottom: 80px;
    display: flex;
    flex-direction: column;
    gap: 8px;
    z-index: 10;
    :deep(.ant-btn) {
      box-shadow: 0 2px 8px rgba(0, 0, 0, 0.15);
    }
  }

  /* ====== 底部输入栏 ====== */
  .input-bar {
    display: flex;
    align-items: flex-end;
    gap: 8px;
    padding: 12px 24px;
    border-top: 1px solid #e8e8e8;
    background: #fff;
    flex-shrink: 0;
    .attach-btn {
      width: 36px;
      height: 36px;
      border: 1px dashed #d9d9d9;
      border-radius: 50%;
      display: flex;
      align-items: center;
      justify-content: center;
      flex-shrink: 0;
      &:hover {
        border-color: #1890ff;
        color: #1890ff;
      }
    }
    .input-wrap {
      flex: 1;
      :deep(textarea.ant-input) {
        border: none;
        box-shadow: none;
        resize: none;
        font-size: 14px;
        padding: 6px 0;
      }
      .att-list {
        display: flex;
        gap: 4px;
        flex-wrap: wrap;
        padding-top: 4px;
      }
    }
    .send-btn {
      width: 36px;
      height: 36px;
      flex-shrink: 0;
    }
  }
</style>
