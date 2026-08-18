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

            <AmendmentEchoCard
              v-if="msg.amendmentProposal"
              :understanding="msg.amendmentProposal.understanding"
              :applying="msg.amendmentApplying"
              :applied="msg.amendmentApplied"
              @apply="handleApplyRequirementAmendment(msg)" />

            <!-- 流内操作按钮（门控反馈 / 流水线错误） -->
            <div v-if="msg.actions && msg.actions.length > 0" class="stream-action-bar">
              <a-button v-for="(act, idx) in msg.actions" :key="idx" size="small" :type="act.type || 'default'" @click="handleChatAction(act)">
                {{ act.label }}
              </a-button>
            </div>

            <!-- 文档卡片（预览 + 下载）—— 支持会话恢复的多交付物 -->
            <div v-if="msg.deliverableLinks?.length" class="doc-card-list">
              <div v-for="d in msg.deliverableLinks" :key="d.relativePath" class="doc-card">
                <span class="doc-emoji">📄</span>
                <div class="doc-info">
                  <div class="doc-name">{{ d.name }}</div>
                </div>
                <div class="doc-actions">
                  <a-button size="small" type="link" @click="previewDoc({ name: d.name, relativePath: d.relativePath })">预览</a-button>
                  <a-button size="small" type="link" @click="downloadDoc({ name: d.name, relativePath: d.relativePath }, 'word')">下载</a-button>
                </div>
              </div>
            </div>
            <div v-else-if="msg.document" class="doc-card">
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
        v-if="newPipelineSpecConfirm"
        :visible="newPipelineSpecConfirm"
        :pipeline-id="pipelineId"
        :document-title="requirementSpecTitle ?? '需求说明书'"
        :relative-path="requirementSpecDeliverable?.relativePath ?? requirementSpecDeliverable?.RelativePath ?? '02-requirement-spec.md'"
        :confirm-loading="newPipelineConfirmLoading"
        :pm-score="requirementSpecPmScore"
        :pm-gaps="requirementSpecPmGaps"
        :pm-verdict="requirementSpecPmVerdict"
        @confirm="handleNewPipelineSpecConfirm"
        @force-confirm="handleNewPipelineSpecForceConfirm"
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
  import { ContentTypeEnum } from '/@/enums/httpEnum';
  import { createPipeline, getGeneratedProjectList, getPageRoutes, quickBugfix, quickEnhancement, triggerSaGate, freezePipeline, resumePipeline, forkPipeline } from '../api/studio/pipeline';
  import { runArchitectSkill, runDesignOrchestrator, runSystemDesignClarificationSkill } from '../api/studio/designSkills';
  import { applyRequirementAmendment, proposeRequirementAmendment, runRequirementAnalysis, type PmAmendProposeResult } from '../api/studio/skills';
  import ChatWorkflowProgress from './chat/ChatWorkflowProgress.vue';
  import IrSkeletonConfirmCard from './ir/IrSkeletonConfirmCard.vue';
  import IrRequirementSpecConfirmCard from './ir/IrRequirementSpecConfirmCard.vue';
  import AmendmentEchoCard from './ir/AmendmentEchoCard.vue';
  import ClarificationCard from './clarification/ClarificationCard.vue';
  import type { ChatStreamAction } from '../types/gate';
  import {
    buildAttachmentsReadyMarkdown,
    buildGateErrorMarkdown,
    buildGateFailedMarkdown,
    gateErrorActions,
    gateFailedActions,
    normalizeSemanticFitness,
    parseGatePayload,
    streamTextToMessage,
  } from '../composables/gateStreamFormatter';
  import { hydrateChatSession } from '../composables/hydrateChatSession';
  import type { AttachmentsReadyPayload, GateErrorPayload, GateFailedPayload, GatePassedPayload } from '../types/gate';
  import { buildFetchSseUrl } from '/@/utils/http/sseUrl';
  import { getAuthHeader, getTenantId } from '/@/utils/auth';
  import { getPipelineDeliverableText } from '../api/studio/pipeline';
  import { getRequirementSpecContent } from '../api/studio/skills';
  import {
    isRequirementSpecPath,
    pickRequirementSpecMarkdown,
    unwrapStudioApi,
    type RequirementSpecContentPayload,
  } from '../utils/requirementSpec';
  import { marked } from 'marked';
  import hljs from 'highlight.js';
  import 'highlight.js/styles/github.css';

  // ====== Markdown 渲染配置 ======
  marked.setOptions({
    breaks: true,
    gfm: true,
  } as any);

  const renderer = new marked.Renderer();

  // marked ≥15：table(token) 的 header/rows 是 token 对象，不是 HTML。
  // 旧写法 `${header}${body}` → `[object Object],…undefined`（用户可见）。
  renderer.table = function (token: any) {
    const base = (marked.Renderer.prototype as any).table.call(this, token);
    return `<div class="md-table-wrap">${base}</div>`;
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

  // 门控通过后骨架由 PM 编排器内部自动 Stabilize，用户不参与 IR-0 审阅
  const showSkeletonConfirm = computed(
    () => (pmSkill?.needsConfirmation.value ?? false) && !gatePassed.value,
  );
  const skeletonPayload = computed(() => pmSkill?.skeletonSnapshot.value?.payload);
  const skeletonConfirmLoading = computed(() => pmSkill?.confirmLoading.value ?? false);

  const showRequirementSpecConfirm = computed(() => false);
  // CR-20260713-03：新 4 步线性 PM 流程的需求说明书确认（唯一卡片）
  const newPipelineSpecConfirm = ref(false);
  const newPipelineConfirmLoading = ref(false);
  /** PM 需求分析流式 token 写入折叠区，不灌正文（CR-20260717-02） */
  const pmStreamActive = ref(false);
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
  const requirementSpecPmScore = computed(() => analystSkill?.pmReview.value?.score ?? null);
  const requirementSpecPmGaps = computed(() => analystSkill?.pmReview.value?.gaps ?? []);
  const requirementSpecPmVerdict = computed(() => analystSkill?.pmReview.value?.verdict ?? '');

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

  async function handleConfirmRequirementSpec(autoRunDesign: boolean, forceConfirm = false) {
    if (!analystSkill) return;
    try {
      await analystSkill.confirmAndProceed(autoRunDesign, forceConfirm);
      antMessage.success(autoRunDesign ? '需求说明书已确认，架构设计已启动' : '需求说明书已确认');
      await irObservatory?.refreshAll();
      await refreshPipelineMaterials();
    } catch (e: any) {
      antMessage.error(e?.response?.data?.msg ?? e?.message ?? '确认失败');
    }
  }

  function handleForceConfirmRequirementSpec(autoRunDesign: boolean) {
    void handleConfirmRequirementSpec(autoRunDesign, true);
  }

  // CR-20260713-03：新流程步骤④确认/修改处理
  // CR-20260714-01：补全参数传递 — 确认/反馈都走 runRequirementAnalysis 带 specFeedback
  const pendingSpecFeedback = ref(false); // 用户点了"我要修改"后置 true，下次发送当反馈

  async function handleNewPipelineSpecConfirm(autoRunDesign: boolean, forceConfirm = false) {
    newPipelineSpecConfirm.value = false;
    newPipelineConfirmLoading.value = true;
    loading.value = true;
    const aiMsgId = Date.now();
    messages.value.push({
      id: aiMsgId,
      role: 'assistant',
      content: '',
      thinking: forceConfirm
        ? '✅ 已强制确认需求说明书（低分留痕），PM 正在 Finalize…\n'
        : '✅ 需求说明书已确认，PM 正在进入终评与 Finalize…\n',
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
      const runData: Parameters<typeof runRequirementAnalysis>[1] = {};
      if (forceConfirm || autoRunDesign) {
        runData.forceConfirm = true;
        runData.forceReason = forceConfirm
          ? '用户强制确认-低分留痕'
          : '全链条赶进度-确认并进入架构设计';
      }
      await runRequirementAnalysisWithSse(aiMsgId, runData);
      advanceToArchitectureStage();
      if (autoRunDesign) {
        await startArchitectureDesign(aiMsgId);
        antMessage.success('需求说明书已确认，架构设计已启动');
      } else {
        antMessage.success('需求说明书已确认');
      }
    } catch (e: any) {
      antMessage.error(e?.response?.data?.msg ?? e?.message ?? '确认失败');
      newPipelineSpecConfirm.value = true;
    } finally {
      loading.value = false;
      newPipelineConfirmLoading.value = false;
    }
  }

  function handleNewPipelineSpecForceConfirm(autoRunDesign: boolean) {
    void handleNewPipelineSpecConfirm(autoRunDesign, true);
  }

  function advanceToArchitectureStage() {
    currentStage.value = 2;
    gatePassed.value = true;
    updateStageStatus();
    void irObservatory?.refreshAll();
    void refreshPipelineMaterials();
  }

  /** 步骤⑤ Finalize 完成后启动设计编排器（POST → 轮询/SSE 进度） */
  async function pollDesignProgress(msg: any, maxMs = 30 * 60 * 1000) {
    if (!designSkill) return;
    const start = Date.now();
    let lastLine = '';
    while (Date.now() - start < maxMs) {
      await designSkill.refreshDesignContext();
      const phases = designSkill.phases.value ?? [];
      const line = phases.map(p => `${designSkill!.skillLabel(p.skillId)}: ${p.phase}`).join(' · ');
      if (line && line !== lastLine && msg) {
        lastLine = line;
        msg.thinking += `\n📊 ${line}\n`;
        scrollOnStream();
      }
      if (designSkill.designComplete.value) {
        if (msg) {
          msg.thinking += '\n✅ 设计阶段完成（SystemDesignLocked）\n';
          msg.content =
            '## ✅ 架构与总体设计已完成\n\n可在 IR 观测台查看 Architecture / DDL / UI / SystemDesign 片段。';
          msg.thinkingCollapsed = true;
        }
        currentStage.value = Math.max(currentStage.value, 3);
        updateStageStatus();
        void irObservatory?.refreshAll();
        return;
      }
      if (designSkill.lastError.value) {
        if (msg) {
          msg.content = `## ⚠️ 设计 Skill 异常\n\n${designSkill.lastError.value}`;
          msg.thinkingCollapsed = true;
        }
        return;
      }
      const failedPhase = phases.find(p => p.phase === 'failed');
      if (failedPhase && msg) {
        msg.content = `## ⚠️ 设计 Skill 失败\n\n${designSkill.skillLabel(failedPhase.skillId)} 运行失败，请查看 IR 观测台或后端日志。`;
        msg.thinkingCollapsed = true;
        return;
      }
      await new Promise<void>(resolve => setTimeout(resolve, 3000));
    }
    if (msg) {
      msg.content = '## ⏱️ 设计仍在后台运行\n\n请展开 IR 观测台查看进度，或稍后刷新页面。';
      msg.thinkingCollapsed = true;
    }
  }

  async function startArchitectureDesign(aiMsgId: number) {
    const msg = messages.value.find(m => m.id === aiMsgId);
    if (msg) {
      msg.thinking += '\n🏗️ 正在启动架构设计编排器（architect / db-design / ui-design 并行）…\n';
      scrollOnStream();
    }
    loading.value = true;
    try {
      if (designSkill) {
        await designSkill.refreshDesignContext();
        if (designSkill.designComplete.value) {
          if (msg) {
            msg.thinking += '\n✅ 检测到设计阶段已完成（SystemDesignLocked）\n';
            msg.content = '## ✅ 架构与总体设计已完成\n\nIR-2 已锁定，可在 IR 观测台查看各片段。';
            msg.thinkingCollapsed = true;
          }
          currentStage.value = Math.max(currentStage.value, 3);
          updateStageStatus();
          return;
        }
        await designSkill.runDesign();
        const sseTask = readSseStream(aiMsgId).catch(() => {});
        await Promise.all([pollDesignProgress(msg), sseTask]);
      } else {
        await runDesignOrchestrator(pipelineId.value, { providerCode: selectedProvider.value });
        await readSseStream(aiMsgId).catch(() => {});
      }
      void designSkill?.refreshDesignContext();
    } catch (e: any) {
      const err = e?.response?.data?.msg ?? e?.message ?? '架构设计启动失败';
      if (msg) {
        msg.thinkingCollapsed = true;
        msg.content = `## ⚠️ 架构设计启动失败\n\n${err}`;
      }
      antMessage.error(err);
    } finally {
      loading.value = false;
    }
  }

  async function handleNewPipelineSpecFeedback() {
    // 用户点"我要修改" → 关闭确认卡片，激活主输入框让用户打字反馈
    newPipelineSpecConfirm.value = false;
    pendingSpecFeedback.value = true;
    antMessage.info('请在下方输入框描述你的修改意见');
    inputText.value = '';
  }

  async function handleApplyRequirementAmendment(msg: any) {
    const proposal = msg.amendmentProposal as PmAmendProposeResult | undefined;
    if (!proposal || !pipelineId.value || msg.amendmentApplying || msg.amendmentApplied) return;
    msg.amendmentApplying = true;
    try {
      await applyRequirementAmendment(pipelineId.value, {
        proposalId: proposal.proposalId,
        understanding: proposal.understanding,
        userMessage: msg.amendmentUserMessage,
        providerCode: selectedProvider.value,
      });
      msg.amendmentApplied = true;
      msg.content = '## 已更新需求分析说明书\n\n已应用你的补充要求，并重新生成 02 需求分析说明书与 PM 复审结果。你可以预览最新文档，或继续在输入框提出修改。';
      await irObservatory?.refreshAll();
      await refreshPipelineMaterials();
      antMessage.success('已更新 02 需求分析说明书');
      scrollToBottom();
    } catch (e: any) {
      antMessage.error(e?.response?.data?.msg ?? e?.message ?? '应用失败');
    } finally {
      msg.amendmentApplying = false;
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
    const pct = typeof progress.percent === 'number' ? progress.percent : null;
    const pctLabel = pct != null ? ` (${pct}%)` : '';
    if (progress.code) {
      return `❌ **${progress.skillId}**${pctLabel} — ${progress.message || progress.code}`;
    }
    if (progress.pmStep != null && progress.skillId === 'pm-skill') {
      const phaseIcon =
        progress.phase === 'completed'
          ? '✅'
          : progress.phase === 'awaiting_user'
            ? '⏸️'
            : progress.phase === 'handoff'
              ? '🔗'
              : progress.phase === 'failed' || progress.phase === 'aborted'
                ? '❌'
                : '▸';
      const next =
        progress.nextStep != null && progress.phase === 'handoff' ? ` → 步骤${progress.nextStep}` : '';
      const round = progress.clarRound ? ` · 第${progress.clarRound}轮` : '';
      return `${phaseIcon} **PM 步骤${progress.pmStep}**${round}${next}${pctLabel} — ${progress.message || progress.phase}`;
    }
    const step = progress.saStepName ? ` · ${progress.saStepName}` : '';
    const icon =
      progress.phase === 'completed' || progress.phase === 'stable' ? '✅' : progress.phase === 'failed' || progress.phase === 'aborted' ? '❌' : '▸';
    return `${icon} **${progress.skillId}**${pctLabel}${step} — ${progress.message || progress.phase}`;
  }

  function appendWorkflowThinking(msg: any, line: string) {
    if (!line) return;
    const normalized = line.replace(/\*\*/g, '').trim();
    if (msg.thinking?.includes(normalized)) return;
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
  /** 流式贴底 rAF 句柄（onUnmounted 必须 cancel） */
  let streamScrollRaf = 0;
  let streamScrollCalls = 0;
  let streamScrollLastLogAt = 0;

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
    if (streamScrollRaf) {
      cancelAnimationFrame(streamScrollRaf);
      streamScrollRaf = 0;
    }
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
      deliverableLinks: [] as Array<{ name: string; relativePath: string }>,
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

      // 先拉附件/交付物，再水合主聊天（避免重进后只剩残缺 JSON）
      await refreshPipelineMaterials();
      const hydrated = hydrateChatSession({
        rawMessages,
        attachments: pipelineMaterials?.attachments.value ?? [],
        deliverables: pipelineMaterials?.deliverables.value ?? [],
      });
      messages.value = hydrated.messages.length ? hydrated.messages : rawMessages.map(mapLoadedMessage);

      // #region agent log
      fetch('http://127.0.0.1:7354/ingest/a6dd8c09-a41a-4bdf-b8f4-ed467f774eaa', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', 'X-Debug-Session-Id': 'ead5d0' },
        body: JSON.stringify({
          sessionId: 'ead5d0',
          runId: 'session-hydrate',
          hypothesisId: 'H-restore',
          location: 'AiChatPanel.vue:loadPipelineState',
          message: 'chat-session-hydrated',
          timestamp: Date.now(),
          data: { pipelineId: pipelineId.value, ...hydrated.stats },
        }),
      }).catch(() => {});
      // #endregion

      updateStageStatus();
      scrollToBottom();
      if (currentStage.value >= 5) {
        void pipelineMaterials?.triggerDeliveryArtifacts(false);
      }
      // 确认框依赖 IR 快照
      void irObservatory?.refreshAll();
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
  // 流式输出时用 rAF 合并滚动，避免与 CSS smooth 叠加造成页面抖动
  const scrollOnStream = () => {
    if (!autoScroll.value) return;
    streamScrollCalls++;
    if (streamScrollRaf) return;
    streamScrollRaf = requestAnimationFrame(() => {
      streamScrollRaf = 0;
      scrollToBottomImmediate();
      // #region agent log
      const now = Date.now();
      if (now - streamScrollLastLogAt > 500) {
        streamScrollLastLogAt = now;
        const el = chatStreamRef.value;
        fetch('http://127.0.0.1:7354/ingest/a6dd8c09-a41a-4bdf-b8f4-ed467f774eaa', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json', 'X-Debug-Session-Id': 'ead5d0' },
          body: JSON.stringify({
            sessionId: 'ead5d0',
            runId: 'jitter-post-fix',
            hypothesisId: 'A',
            location: 'AiChatPanel.vue:scrollOnStream',
            message: 'stream-scroll-raf',
            timestamp: now,
            data: {
              pendingCalls: streamScrollCalls,
              scrollHeight: el?.scrollHeight ?? 0,
              scrollTop: el?.scrollTop ?? 0,
              clientHeight: el?.clientHeight ?? 0,
              scrollBehavior: el ? getComputedStyle(el).scrollBehavior : null,
            },
          }),
        }).catch(() => {});
        streamScrollCalls = 0;
      }
      // #endregion
    });
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
        // UX 收敛（2026-07-17）：门控分析结果是后台处理 + PM 消费，不输出几百行 markdown 到对话流。
        // 用户只需看到：PM 深度追问卡片 + 最后需求说明书确认。门控通过仅给一句折叠提示。
        const payload = parseGatePayload<GatePassedPayload>(data.data) ?? {};
        const sf = normalizeSemanticFitness(payload) ?? ({ passed: true, score: 0, level: 'sufficient', identified: [], missing: [] } as const);
        msg.thinking += `\n✅ 需求材料评估通过（${sf.score}/100），正在分析…\n`;
        scrollOnStream();
        await refreshPipelineMaterials();
        break;
      }
      case 'gate_failed': {
        gateProcessing.value = false;
        gatePassed.value = false;
        msg.thinkingCollapsed = true;
        const failPayload = parseGatePayload<GateFailedPayload>(data.data) ?? {};
        const failSf = normalizeSemanticFitness(failPayload) ?? ({ passed: false, score: 0, level: 'insufficient', identified: [], missing: [], nextStepGuidance: undefined } as const);
        msg.actions = gateFailedActions();
        const failMd = buildGateFailedMarkdown(failPayload, failSf);
        // #region agent log
        fetch('http://127.0.0.1:7354/ingest/a6dd8c09-a41a-4bdf-b8f4-ed467f774eaa',{method:'POST',headers:{'Content-Type':'application/json','X-Debug-Session-Id':'ead5d0'},body:JSON.stringify({sessionId:'ead5d0',runId:'post-fix',hypothesisId:'G1',location:'AiChatPanel.vue:gate_failed',message:'gate_failed markdown built',data:{reason:failPayload?.reason??null,hintType:typeof failPayload?.hint,missingCount:failSf.missing?.length??0,missingSample:failSf.missing?.slice?.(0,3)??null,guidanceType:typeof failSf.nextStepGuidance,guidancePreview:String(failSf.nextStepGuidance??'').slice(0,120),mdHasObjectObject:failMd.includes('[object Object]'),mdPreview:failMd.slice(0,500),exampleActionLabel:msg.actions?.[0]?.label??null,examplePayloadPreview:String(msg.actions?.[0]?.payload??'').slice(0,80)},timestamp:Date.now()})}).catch(()=>{});
        // #endregion
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
        pmStreamActive.value = true;
        const hint = parseGatePayload<{ pipelineId?: number; source?: string }>(data.data);
        msg.thinking += `\n📋 PM 需求分析已启动（${hint?.source ?? 'gate_pass'}），正在完善需求…\n`;
        scrollOnStream();
        break;
      }
      case 'pm_skill_failed': {
        pmStreamActive.value = false;
        gateProcessing.value = false;
        loading.value = false;
        msg.thinkingCollapsed = true;
        const pmFail = parseGatePayload<{ message?: string; errorCode?: string }>(data.data) ?? {};
        // #region agent log
        fetch('http://127.0.0.1:7354/ingest/a6dd8c09-a41a-4bdf-b8f4-ed467f774eaa',{method:'POST',headers:{'Content-Type':'application/json','X-Debug-Session-Id':'ead5d0'},body:JSON.stringify({sessionId:'ead5d0',runId:'post-fix',hypothesisId:'H5',location:'AiChatPanel.vue:pm_skill_failed',message:'pm skill failed event',data:{errorCode:pmFail.errorCode??null,msgPreview:String(pmFail.message??'').slice(0,200)},timestamp:Date.now()})}).catch(()=>{});
        // #endregion
        const pmErrMd = [
          '## ⚠️ 门控已通过，但 PM 需求分析未完成',
          '',
          pmFail.message || 'PM 流水线执行失败，请重试或稍后在流水线中重新触发。',
          '',
          pmFail.errorCode ? `错误代码：\`${pmFail.errorCode}\`` : '',
          '',
          '---',
          '',
          '需求评估（门控）本身已成功；可稍后在流水线中重新触发 PM Skill。',
        ]
          .filter(Boolean)
          .join('\n');
        await streamTextToMessage(msg, (msg.content ? msg.content + '\n\n' : '') + pmErrMd, { onChunk: scrollOnStream });
        break;
      }
      case 'stage_transition': {
        const stage = String(data.data || data.content || 'requirement');
        msg.thinking += `\n✅ 已进入阶段：${stage}\n`;
        if (stage === 'design' || stage === 'architecture') {
          advanceToArchitectureStage();
        }
        scrollOnStream();
        break;
      }
      case 'design_orchestrator_started':
        msg.thinking += `\n🏗️ 设计编排已启动…\n`;
        loading.value = true;
        scrollOnStream();
        break;
      case 'design_orchestrator_completed':
        pmStreamActive.value = false;
        loading.value = false;
        msg.thinking += `\n✅ 设计编排已完成\n`;
        msg.thinkingCollapsed = true;
        void designSkill?.refreshDesignContext();
        scrollOnStream();
        break;
      case 'design_orchestrator_failed': {
        pmStreamActive.value = false;
        loading.value = false;
        msg.thinkingCollapsed = true;
        const fail = parseGatePayload<{ message?: string; status?: string }>(data.data) ?? {};
        msg.content = `## ⚠️ 设计编排失败\n\n${fail.message || '设计 Skill 未能完成，请查看 IR 观测台或重试。'}`;
        scrollOnStream();
        break;
      }
      case 'thinking':
      case 'info':
        msg.thinking += (data.data || data.content || '') + '\n';
        scrollOnStream();
        break;
      case 'token':
      case 'delta':
        if (pmStreamActive.value) {
          msg.thinking += data.data || data.content || data.delta?.content || '';
        } else {
          msg.content += data.data || data.content || data.delta?.content || '';
        }
        scrollOnStream();
        break;
      case 'clarification_requested': {
        pmStreamActive.value = false;
        msg.thinkingCollapsed = true;
        loading.value = false;
        // ADR-005：后端在需求分析阶段下发结构化选择题，暂停流式 LLM 等待用户作答
        const clarificationData = parseSseJsonPayload(data.data);
        msg.clarification = clarificationData || data.clarification || null;
        if (msg.thinking && !msg.thinking.includes('等待您的作答')) {
          msg.thinking += '\n⏸️ 等待您的作答（请在下方澄清卡片中选择）\n';
        }
        scrollOnStream();
        break;
      }
      case 'spec_confirm_requested': {
        pmStreamActive.value = false;
        msg.thinkingCollapsed = true;
        // CR-20260713-03：新流程步骤④ — 需求说明书已生成，弹出预览/下载/确认卡片
        if (!msg.content?.trim()) {
          msg.content = '需求说明书已生成，请确认通过或提出修改意见。';
        }
        newPipelineSpecConfirm.value = true;
        gateProcessing.value = false;
        loading.value = false;
        void refreshPipelineMaterials();
        scrollOnStream();
        break;
      }
      case 'strategy':
        msg.strategies = data.data || data.strategies || [];
        break;
      case 'document':
        msg.document = data.data || data.document;
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
            if (step) appendWorkflowThinking(msg, `✅ SA 编译 · ${step}`);
            scrollOnStream();
          }
          // IR0 片段仅进观测台，不在用户聊天区展示 IR 预览
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
          if (progress.skillId === 'pm-skill' && progress.pmStep != null) {
            msg.thinkingCollapsed = progress.phase === 'awaiting_user';
            if (progress.phase === 'awaiting_user') {
              loading.value = false;
            } else if (progress.phase !== 'completed') {
              loading.value = true;
              pmStreamActive.value =
                progress.phase === 'started' || progress.phase === 'progress' || progress.phase === 'handoff';
            }
          }
          scrollOnStream();
          if (progress.code || progress.phase === 'failed' || progress.phase === 'aborted') {
            const skillLabel =
              progress.skillId === 'analyst-skill'
                ? '需求分析 Finalize'
                : progress.skillId === 'pm-skill'
                ? 'PM 需求分析'
                : progress.skillId ?? 'Skill';
            msg.content += `\n\n**⚠️ ${skillLabel} 异常**\n\n${progress.message || '未知错误'}\n`;
            if (progress.code) msg.content += `\n错误码：\`${progress.code}\`\n`;
            loading.value = false;
            pmStreamActive.value = false;
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

  /** PM 新流程：先 POST 再订阅 SSE（ReplaceChannel 在 POST 内执行，先连 SSE 会挂到已废弃通道） */
  async function runRequirementAnalysisWithSse(
    aiMsgId: number,
    data?: Parameters<typeof runRequirementAnalysis>[1],
  ) {
    pmStreamActive.value = true;
    loading.value = true;
    const msg = messages.value.find(m => m.id === aiMsgId);
    if (msg) {
      msg.thinkingCollapsed = false;
      if (!String(msg.thinking || '').includes('PM 需求分析')) {
        msg.thinking += '📋 PM 需求分析进行中…\n';
        scrollOnStream();
      }
    }
    const res: any = await runRequirementAnalysis(pipelineId.value, data ?? {});
    const body = res?.data ?? res;
    if (body?.status === 'already_running' && msg) {
      msg.thinking += '📋 检测到 PM 仍在后台运行，已重新连接进度流（请勿重复点继续）…\n';
      scrollOnStream();
    }
    await readSseStream(aiMsgId);
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

  /** 用户明确要求进入架构设计（勿当作 PM 需求补充） */
  function isArchitectureAdvanceIntent(text: string): boolean {
    const t = text.trim();
    return (
      /^(请)?(进入|开始|启动|进入到?)\s*架构(\s*设计)?(\s*阶段)?$/i.test(t) ||
      /^架构设计(\s*阶段)?$/i.test(t)
    );
  }

  async function sendMessage(content: string, uploadedFiles?: Array<{ name: string; url: string }>) {
    if (!content.trim() && (!uploadedFiles || uploadedFiles.length === 0)) return;
    const submittedText = content.trim();
    const isResumeKeyword = /^(继续|继续分析|ok|OK|好的)$/i.test(submittedText);
    const isArchAdvance = isArchitectureAdvanceIntent(submittedText);
    if (isResumeKeyword && loading.value) {
      antMessage.info('PM 分析仍在进行中，请展开下方「推理与工作流」查看进度，无需重复发送「继续」');
      return;
    }
    loading.value = true;
    autoScroll.value = true;
    const initialThinking = isResumeKeyword
      ? '📋 收到继续指令，正在连接 PM 分析进度…\n'
      : isArchAdvance
      ? '🏗️ 收到进入架构设计指令…\n'
      : currentStage.value === 2
      ? '🏗️ 收到指令，准备启动架构设计…\n'
      : submittedText
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

    if (newPipelineSpecConfirm.value && !pendingSpecFeedback.value && !isArchAdvance) {
      loading.value = false;
      antMessage.info('请使用下方确认卡片操作：确认进入下一阶段，或选择修改后在输入框描述意见');
      return;
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
      let sseHandled = false;
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
      } else if (
        isArchAdvance &&
        gatePassed.value &&
        workMode.value === 'greenfield'
      ) {
        // 「进入架构设计」等指令：Finalize → 切阶段 → 启动设计编排器（禁止误走 PM 续跑）
        gateProcessing.value = false;
        const aiMsg = messages.value.find(m => m.id === aiMsgId);
        if (aiMsg) {
          aiMsg.thinking += '🏗️ 正在 Finalize 需求并启动架构设计…\n';
          scrollOnStream();
        }
        newPipelineSpecConfirm.value = false;
        if (currentStage.value >= 2) {
          await startArchitectureDesign(aiMsgId);
          sseHandled = true;
        } else {
          await designSkill?.refreshDesignContext();
          if (!designSkill?.analysisFinalized.value) {
            await runRequirementAnalysisWithSse(aiMsgId, {
              forceConfirm: true,
              forceReason: '用户指令-进入架构设计',
            });
          }
          advanceToArchitectureStage();
          await startArchitectureDesign(aiMsgId);
          antMessage.success('已进入架构设计阶段');
          sseHandled = true;
        }
      } else if (gatePassed.value && currentStage.value === 1 && workMode.value === 'greenfield') {
        // CR-20260718：门控通过后「继续/补充」必须走 PM 编排器 + SSE，禁止误走旧 execute LLM 流
        gateProcessing.value = false;
        const aiMsg = messages.value.find(m => m.id === aiMsgId);
        if (aiMsg) {
          aiMsg.thinking += isResumeKeyword
            ? '📋 正在续连 PM 需求分析进度…\n'
            : '📋 PM 正在处理您的补充并续跑分析…\n';
          scrollOnStream();
        }
        await runRequirementAnalysisWithSse(
          aiMsgId,
          isResumeKeyword ? {} : { userMessage: submittedText },
        );
        sseHandled = true;
      } else if (currentStage.value === 2) {
        // 架构设计阶段：走 design 编排器，禁止误走 execute 通用 LLM / 门控
        gateProcessing.value = false;
        const aiMsg = messages.value.find(m => m.id === aiMsgId);
        if (aiMsg) {
          aiMsg.thinking += '🏗️ 正在启动架构设计编排器…\n';
          scrollOnStream();
        }
        await startArchitectureDesign(aiMsgId);
        sseHandled = true;
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
      if (!sseHandled) await readSseStream(aiMsgId);
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

    // CR-20260714-01 改动5：用户输入第一响应 — pendingSpecFeedback 时作为反馈提交
    if (pendingSpecFeedback.value && content) {
      pendingSpecFeedback.value = false;
      messages.value.push({
        id: Date.now(),
        role: 'user',
        content,
        time: new Date().toLocaleTimeString(),
      });
      scrollToBottom();
      loading.value = true;
      const aiMsgId = Date.now() + 1;
      messages.value.push({
        id: aiMsgId,
        role: 'assistant',
        content: '',
        thinking: '📝 已收到修改意见，PM 正在重新分析需求…\n',
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
      runRequirementAnalysisWithSse(aiMsgId, { specFeedback: content })
        .then(() => {
          antMessage.success('已提交修改意见，PM 正在重新分析…');
        })
        .catch((e: any) => {
          antMessage.error(e?.response?.data?.msg ?? e?.message ?? '提交失败，请重试');
          pendingSpecFeedback.value = true;
        })
        .finally(() => {
          loading.value = false;
        });
      return;
    }

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
            // 必须显式 FORM_DATA：defHttp 默认 Content-Type=application/json，否则后端 ChunkModel.file 为 null → NRE
            // #region agent log
            fetch('http://127.0.0.1:7354/ingest/a6dd8c09-a41a-4bdf-b8f4-ed467f774eaa',{method:'POST',headers:{'Content-Type':'application/json','X-Debug-Session-Id':'ead5d0'},body:JSON.stringify({sessionId:'ead5d0',runId:'post-fix',hypothesisId:'A',location:'AiChatPanel.vue:uploadAttachmentsAndSend',message:'annex upload start',data:{fileName:file.name,fileSize:file.size,tenantId:tenantId||null,contentType:ContentTypeEnum.FORM_DATA},timestamp:Date.now()})}).catch(()=>{});
            // #endregion
            try {
              const res = await defHttp.post({
                url: '/api/file/Uploader/annex',
                data: formData,
                headers: {
                  'Content-Type': ContentTypeEnum.FORM_DATA,
                  ...(tenantId ? { 'X-Tenant-Id': tenantId } : {}),
                },
              });
              const fileModel = res?.data;
              const url = typeof fileModel?.url === 'string' ? fileModel.url : '';
              // #region agent log
              fetch('http://127.0.0.1:7354/ingest/a6dd8c09-a41a-4bdf-b8f4-ed467f774eaa',{method:'POST',headers:{'Content-Type':'application/json','X-Debug-Session-Id':'ead5d0'},body:JSON.stringify({sessionId:'ead5d0',runId:'post-fix',hypothesisId:'B',location:'AiChatPanel.vue:uploadAttachmentsAndSend',message:'annex upload response',data:{fileName:file.name,resKeys:res&&typeof res==='object'?Object.keys(res):[],hasData:!!fileModel,url:url||null,fileModelKeys:fileModel&&typeof fileModel==='object'?Object.keys(fileModel):[]},timestamp:Date.now()})}).catch(()=>{});
              // #endregion
              if (!url) throw new Error('上传响应缺少 url');
              return { name: file.name, url };
            } catch (err: any) {
              // #region agent log
              fetch('http://127.0.0.1:7354/ingest/a6dd8c09-a41a-4bdf-b8f4-ed467f774eaa',{method:'POST',headers:{'Content-Type':'application/json','X-Debug-Session-Id':'ead5d0'},body:JSON.stringify({sessionId:'ead5d0',runId:'post-fix',hypothesisId:'A',location:'AiChatPanel.vue:uploadAttachmentsAndSend',message:'annex upload error',data:{fileName:file.name,errMsg:String(err?.message||err),errName:err?.name||null,status:err?.response?.status??null,bizCode:err?.response?.data?.code??null,bizMsg:typeof err?.response?.data?.msg==='string'?err.response.data.msg:String(err?.response?.data?.msg??err?.message??'')},timestamp:Date.now()})}).catch(()=>{});
              // #endregion
              throw err;
            }
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
    if (!payload.triggerNextRound || payload.nextAction === 'none') {
      antMessage.warning('作答已保存，但未能自动续跑 PM 分析；请发送「继续」恢复进度');
      return;
    }

    loading.value = true;
    // 创建新的 assistant 消息占位，承接下一轮 SSE 流
    const aiMsgId = Date.now();
    const thinkingText =
      payload.nextAction === 'rerun-architect'
        ? '🏗️ 已收到架构澄清作答，正在重新运行架构设计（ToT）…\n'
        : payload.nextAction === 'rerun-system-design-clarification'
        ? '📐 已收到总体设计澄清作答，正在运行约束引擎并锁定系统设计…\n'
        : payload.nextAction === 'continue-requirement-analysis'
        ? '📋 已收到需求澄清作答，正在合并答案 → 更新骨架 → 九步重编译 → PM 反向完善（约 1–3 分钟，请展开下方推理区）…\n'
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
        // 架构阶段二：先 POST 建 SSE 通道，再订阅（对齐 runRequirementAnalysisWithSse）
        await runArchitectSkill(pipelineId.value, {});
        await readSseStream(aiMsgId).catch(() => {});
        void designSkill?.refreshDesignContext();
      } else if (payload.nextAction === 'rerun-system-design-clarification') {
        await runSystemDesignClarificationSkill(pipelineId.value, {});
        await readSseStream(aiMsgId).catch(() => {});
        void designSkill?.refreshDesignContext();
      } else if (payload.nextAction === 'continue-requirement-analysis') {
        await runRequirementAnalysisWithSse(aiMsgId, {});
      } else {
        await triggerSaGate(pipelineId.value, '继续分析', false, []);
        await readSseStream(aiMsgId);
      }
    } catch (e: any) {
      const m = messages.value.find(x => x.id === aiMsgId);
      if (m) m.content += `\n\n⚠️ 重新评估失败：${e?.message || e}`;
    } finally {
      loading.value = false;
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

  async function previewDoc(doc: any) {
    if (doc?.relativePath && pipelineId.value) {
      try {
        const rel = String(doc.relativePath).replace(/\\/g, '/');
        let text: string;
        if (isRequirementSpecPath(rel)) {
          const payload = unwrapStudioApi<RequirementSpecContentPayload>(await getRequirementSpecContent(pipelineId.value));
          text = pickRequirementSpecMarkdown(payload);
          if (!text?.trim()) throw new Error('正式版需求说明书尚未生成');
        } else {
          text = await getPipelineDeliverableText(pipelineId.value, rel);
        }
        const blob = new Blob([text], { type: 'text/markdown;charset=utf-8' });
        const url = URL.createObjectURL(blob);
        blobUrls.push(url);
        window.open(url, '_blank');
        return;
      } catch (e: any) {
        antMessage.error((e?.response?.data?.msg ?? e?.message) || '预览失败');
        return;
      }
    }
    if (doc?.previewUrl) window.open(doc.previewUrl, '_blank');
  }
  function downloadDoc(doc: any, fmt: string) {
    if (doc?.relativePath && pipelineMaterials) {
      void pipelineMaterials.downloadDeliverable({
        fileName: doc.name,
        FileName: doc.name,
        relativePath: doc.relativePath,
        RelativePath: doc.relativePath,
      });
      return;
    }
    const a = document.createElement('a');
    a.href = fmt === 'pdf' ? doc.downloadPdfUrl : doc.downloadWordUrl;
    a.download = doc.name + (fmt === 'pdf' ? '.pdf' : '.docx');
    a.click();
  }

  // ====== 滚动控制 ======
  /** 瞬时贴底（流式/程序滚动）；勿配合 CSS scroll-behavior:smooth，否则高频赋值会抖动 */
  function scrollToBottomImmediate() {
    const el = chatStreamRef.value;
    if (!el) return;
    el.scrollTop = el.scrollHeight;
  }
  function scrollToBottom() {
    nextTick(() => scrollToBottomImmediate());
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
      color: rgba(0, 0, 0, 0.45);
    }
  }
  .ai-chat-panel {
    display: flex;
    flex-direction: column;
    height: 100%;
    width: 100%;
    overflow: hidden;
    /* 继承 JNPF/Ant Design 全局字体与字号（默认 14px），禁止自建系统字体栈 */
    font-family: inherit;
    font-size: 14px;
    color: rgba(0, 0, 0, 0.85);
    background: #f4f7f9;
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
      font-size: 14px;
      color: rgba(0, 0, 0, 0.65);
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
        font-size: 14px;
      }
    }
  }

  /* ====== 对话流 ====== */
  .chat-stream {
    flex: 1;
    overflow-y: auto;
    overflow-x: hidden;
    padding: 24px 0;
    /* 禁止 smooth：流式 token/打字机高频改 scrollTop 时，smooth 会互相打断导致整页抖动 */
    scroll-behavior: auto;
  }

  /* ====== 欢迎卡片 ====== */
  .welcome-card {
    max-width: 680px;
    margin: 0 auto 24px;
    background: #fff;
    border: 1px solid #f0f0f0;
    border-radius: 4px;
    padding: 32px;
    text-align: center;
    box-shadow: 0 1px 2px rgba(0, 0, 0, 0.03);

    &.loading-card {
      padding: 48px 32px;
    }
    .welcome-icon {
      font-size: 40px;
      margin-bottom: 12px;
    }
    h2 {
      font-size: 16px;
      margin-bottom: 12px;
      font-weight: 600;
      color: rgba(0, 0, 0, 0.85);
    }
    p {
      font-size: 14px;
      color: rgba(0, 0, 0, 0.65);
      line-height: 1.6;
      margin-bottom: 4px;
    }
    .hint {
      color: #1890ff;
      font-size: 14px;
      margin-bottom: 20px;
    }
    .gate-hint {
      color: #d48806;
      background: #fffbe6;
      padding: 8px 12px;
      border-radius: 4px;
      border: 1px solid #ffe58f;
      text-align: left;
      margin-bottom: 16px;
      font-size: 14px;
    }
    .gate-format {
      text-align: left;
      border: 1px solid #f0f0f0;
      border-radius: 4px;
      padding: 10px 12px;
      background: #fafafa;
      .gate-format-title {
        font-size: 14px;
        font-weight: 600;
        color: rgba(0, 0, 0, 0.85);
        margin-bottom: 6px;
      }
      ul {
        margin: 0;
        padding-left: 18px;
      }
      li {
        font-size: 14px;
        color: rgba(0, 0, 0, 0.65);
        margin-bottom: 4px;
      }
      .gate-format-example {
        margin-top: 6px;
        font-size: 12px;
        color: #1890ff;
      }
    }
  }

  /* ====== 消息卡片（对齐 Ant Card：小圆角、浅边框） ====== */
  .msg-card {
    display: flex;
    gap: 12px;
    max-width: 680px;
    margin: 0 auto 16px;
    padding: 16px 20px;
    background: #fff;
    border: 1px solid #f0f0f0;
    border-radius: 4px;
    box-shadow: 0 1px 2px rgba(0, 0, 0, 0.03);
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
    line-height: 1.5715;
    color: rgba(0, 0, 0, 0.85);
    word-break: break-word;
  }

  .card-text {
    :deep(h1),
    :deep(h2),
    :deep(h3) {
      margin: 16px 0 8px;
      font-weight: 600;
      color: rgba(0, 0, 0, 0.85);
    }
    :deep(h1) {
      font-size: 16px;
    }
    :deep(h2) {
      font-size: 16px;
    }
    :deep(h3) {
      font-size: 14px;
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
    font-size: 14px;
    th,
    td {
      padding: 8px 12px;
      border-bottom: 1px solid #f0f0f0;
      text-align: left;
    }
    th {
      background: #fafafa;
      font-weight: 600;
      color: rgba(0, 0, 0, 0.85);
      border-bottom: 1px solid #f0f0f0;
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
    font-size: 12px;
    cursor: pointer;
    color: rgba(0, 0, 0, 0.65);
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
    border: 1px solid #f0f0f0;
    border-radius: 4px;
    background: #fafafa;
    overflow: hidden;
    .thinking-header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding: 8px 12px;
      cursor: pointer;
      font-size: 14px;
      color: rgba(0, 0, 0, 0.65);
      &:hover {
        background: #f5f5f5;
      }
    }
    .thinking-content {
      padding: 0 12px 8px;
      font-size: 12px;
      color: rgba(0, 0, 0, 0.45);
      line-height: 1.5715;
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
      font-size: 14px;
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
    border: 1px solid #f0f0f0;
    border-radius: 4px;
    background: #fff;
    cursor: pointer;
    transition: all 0.2s;
    &:hover {
      border-color: #1890ff;
      background: #e6f7ff;
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
      color: rgba(0, 0, 0, 0.85);
    }
    .strategy-desc {
      font-size: 12px;
      color: rgba(0, 0, 0, 0.45);
    }
  }

  /* ====== 文档卡片 ====== */
  .doc-card-list {
    display: flex;
    flex-direction: column;
    gap: 8px;
    margin-top: 12px;
  }
  .doc-card {
    display: flex;
    align-items: center;
    gap: 12px;
    margin-top: 12px;
    padding: 12px;
    border: 1px solid #f0f0f0;
    border-radius: 4px;
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
    background: #f5f5f5;
    border-radius: 4px;
    font-size: 14px;
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
  .new-pipeline-spec-confirm {
    display: flex;
    align-items: center;
    gap: 8px;
    padding: 10px 12px;
    margin: 8px 0;
    background: #f6ffed;
    border: 1px solid #b7eb8f;
    border-radius: 6px;
    flex-wrap: wrap;
    .badge {
      background: #52c41a;
      color: #fff;
      padding: 2px 8px;
      border-radius: 4px;
      font-size: 12px;
    }
    .hint {
      color: #262626;
      font-size: 13px;
      flex: 1;
      min-width: 200px;
    }
  }
</style>
