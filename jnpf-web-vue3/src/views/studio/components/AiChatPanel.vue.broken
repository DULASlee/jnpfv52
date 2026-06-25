<template>
  <div class="ai-chat-panel">
    <!-- ====== 顶栏 ====== -->
    <div class="top-bar">
      <div class="top-bar-left">
        <a-select
          v-model:value="selectedProvider"
          size="small"
          class="model-select"
          :options="providerOptions"
        />
      </div>

      <div class="top-bar-center">
        <span class="stage-text">
          阶段 {{ currentStageIndex + 1 }}/{{ stages.length }}：{{ stages[currentStageIndex]?.name }}
        </span>
        <a-popover trigger="click" placement="bottomRight">
          <template #content>
            <div class="stage-detail-popover">
              <div
                v-for="(s, i) in stages"
                :key="s.key"
                class="stage-item"
                :class="{ active: i === currentStageIndex, completed: i < currentStageIndex }"
              >
                <span class="stage-dot" />
                <span class="stage-name">{{ i + 1 }}. {{ s.name }}</span>
                <a-tag v-if="i < currentStageIndex" color="green" size="small">完成</a-tag>
                <a-tag v-else-if="i === currentStageIndex" color="blue" size="small">进行中</a-tag>
                <a-tag v-else color="default" size="small">待执行</a-tag>
              </div>
            </div>
          </template>
          <a-button size="small" type="link">阶段详情 ▾</a-button>
        </a-popover>
      </div>

      <div class="top-bar-right">
        <a-button size="small" @click="handleNewChat">
          <template #icon><PlusOutlined /></template>
          新对话
        </a-button>
      </div>
    </div>

    <!-- ====== 对话流 ====== -->
    <div class="chat-stream" ref="chatStreamRef" @scroll="handleScroll">
      <!-- 欢迎界面 -->
      <div v-if="messages.length === 0" class="welcome-section">
        <div class="welcome-icon">🤖</div>
        <h2>AI 架构顾问</h2>
        <p class="welcome-desc">
          你好！请描述你的业务需求，我会通过多轮对话帮你梳理清楚，然后为你生成完整的软件系统。
        </p>
        <p class="welcome-hint">我会主动追问关键问题，确保需求没有模糊之处。</p>
        <div class="quick-prompts">
          <span
            v-for="prompt in quickPrompts"
            :key="prompt"
            class="quick-prompt"
            @click="handleQuickPrompt(prompt)"
          >{{ prompt }}</span>
        </div>
      </div>

      <!-- 消息列表 -->
      <template v-for="msg in messages" :key="msg.id">
        <!-- 用户消息 -->
        <div v-if="msg.role === 'user'" class="msg-row user-row">
          <div class="msg-bubble user-bubble">
            <div class="bubble-text" v-html="renderMarkdown(msg.content)" />
          </div>
          <div class="avatar user-avatar">U</div>
        </div>

        <!-- AI 消息 -->
        <div v-else-if="msg.role === 'assistant'" class="msg-row ai-row">
          <div class="avatar ai-avatar">AI</div>
          <div class="msg-bubble ai-bubble">
            <!-- 思考过程（可折叠） -->
            <div v-if="msg.thinking" class="thinking-block">
              <div class="thinking-header" @click="msg.thinkingCollapsed = !msg.thinkingCollapsed">
                <span class="thinking-label">
                  <BulbOutlined />
                  {{ msg.thinkingCollapsed ? '展开思考过程' : '收起思考过程' }}
                </span>
                <DownOutlined v-if="!msg.thinkingCollapsed" class="thinking-arrow" />
                <RightOutlined v-else class="thinking-arrow" />
              </div>
              <div v-if="!msg.thinkingCollapsed" class="thinking-content">{{ msg.thinking }}</div>
            </div>

            <!-- 正文 -->
            <div class="bubble-text" v-html="renderMarkdown(msg.content)" />

            <!-- 策略卡片 -->
            <div v-if="msg.strategies && msg.strategies.length > 0" class="strategy-cards">
              <div
                v-for="(strategy, idx) in msg.strategies"
                :key="idx"
                class="strategy-card"
                :class="{ selected: selectedStrategy === idx }"
                @click="handleSelectStrategy(idx, strategy)"
              >
                <div class="strategy-icon">{{ idx === 0 ? '🟢' : '🔵' }}</div>
                <div class="strategy-info">
                  <div class="strategy-title">{{ strategy.title }}</div>
                  <div class="strategy-desc">{{ strategy.description }}</div>
                </div>
              </div>
            </div>

            <!-- 文档卡片 -->
            <div v-if="msg.document" class="doc-card">
              <FileTextOutlined class="doc-icon" />
              <div class="doc-info">
                <div class="doc-name">{{ msg.document.name }}</div>
                <div class="doc-size">{{ msg.document.size || '' }}</div>
              </div>
              <div class="doc-actions">
                <a-button size="small" type="link" @click="previewDoc(msg.document)">预览</a-button>
                <a-button size="small" type="link" @click="downloadDoc(msg.document)">下载</a-button>
              </div>
            </div>

            <!-- IR 预览卡片 -->
            <IrPreviewCard v-if="msg.ir" :ir-data="msg.ir" />

            <!-- 阶段确认卡片 -->
            <div v-if="msg.stageConfirmable && !msg.stageConfirmed" class="stage-confirm-card">
              <div class="confirm-header">
                <CheckCircleOutlined />
                <span>阶段 {{ currentStageIndex + 1 }}：{{ stages[currentStageIndex]?.name }} 分析完成</span>
              </div>
              <div class="confirm-actions">
                <a-button size="small" :disabled="currentStageIndex <= 0" @click="handleRollback">
                  ↩️ 回退修改
                </a-button>
                <a-button type="primary" size="small" @click="handleConfirmStage(msg)">
                  确认并推进 ▶️
                </a-button>
              </div>
            </div>

            <!-- 已确认 -->
            <div v-if="msg.stageConfirmed" class="stage-confirmed-badge">
              ✅ 已确认，进入 {{ stages[currentStageIndex]?.name }}
            </div>
          </div>
        </div>

        <!-- 系统消息 -->
        <div v-else-if="msg.role === 'system'" class="system-msg">
          <div class="system-line" />
          <span class="system-text">{{ msg.content }}</span>
          <div class="system-line" />
        </div>
      </template>

      <!-- AI 思考中动画 -->
      <div v-if="loading" class="msg-row ai-row">
        <div class="avatar ai-avatar">AI</div>
        <div class="msg-bubble ai-bubble thinking-bubble">
          <div class="thinking-animation">
            <span class="thinking-text">{{ thinkingLabel }}</span>
            <span class="dots"><span /><span /><span /></span>
          </div>
        </div>
      </div>
    </div>

    <!-- ====== 滚动辅助按钮 ====== -->
    <div class="scroll-buttons" v-show="showScrollButtons">
      <a-button
        v-show="showScrollUp"
        class="scroll-btn"
        shape="circle"
        size="small"
        @click="scrollToTop"
      >
        <template #icon><UpOutlined /></template>
      </a-button>
      <a-button
        v-show="showScrollDown"
        class="scroll-btn"
        shape="circle"
        size="small"
        type="primary"
        @click="scrollToBottom"
      >
        <template #icon><DownOutlined /></template>
      </a-button>
    </div>

    <!-- ====== 底部输入栏 ====== -->
    <div class="input-bar">
      <a-upload :before-upload="handleUpload" :show-upload-list="false" multiple>
        <a-button class="attach-btn" type="text">
          <template #icon><PaperClipOutlined /></template>
        </a-button>
      </a-upload>

      <div class="input-wrapper">
        <a-textarea
          ref="textareaRef"
          v-model:value="inputText"
          :placeholder="inputPlaceholder"
          :auto-size="{ minRows: 1, maxRows: 5 }"
          :disabled="loading"
          @press-enter="handleEnter"
        />
        <div v-if="attachments.length > 0" class="attachment-list">
          <a-tag
            v-for="(file, idx) in attachments"
            :key="idx"
            closable
            @close="attachments.splice(idx, 1)"
          >📎 {{ file.name }}</a-tag>
        </div>
      </div>

      <a-button
        class="send-btn"
        :type="loading ? 'default' : 'primary'"
        :danger="loading"
        shape="circle"
        size="large"
        @click="loading ? handleStop() : handleSend()"
      >
        <template #icon>
          <SendOutlined v-if="!loading" />
          <PauseOutlined v-else />
        </template>
      </a-button>
    </div>
  </div>
</template>

<script setup lang="ts">
  import { ref, computed, onMounted, onUnmounted, nextTick, watch } from 'vue';
  import {
    PlusOutlined,
    SendOutlined,
    PauseOutlined,
    FileTextOutlined,
    PaperClipOutlined,
    UpOutlined,
    DownOutlined,
    RightOutlined,
    BulbOutlined,
    CheckCircleOutlined,
  } from '@ant-design/icons-vue';
  import { message as antMessage } from 'ant-design-vue';
  import { defHttp } from '/@/utils/http/axios';
  import { getToken } from '/@/utils/auth';
  import { buildFetchSseUrl } from '/@/utils/http/sseUrl';
  import IrPreviewCard from './chat/IrPreviewCard.vue';

  defineOptions({ name: 'AiChatPanel' });

  // ── Props / Emits ──
  const props = withDefaults(
    defineProps<{ pipelineId?: number; initialMessage?: string }>(),
    { pipelineId: 0, initialMessage: '' },
  );
  const emit = defineEmits<{ 'pipeline-complete': [data: { stage: number }]; 'new-chat': [] }>();

  // ── 阶段配置 ──
  const STAGE_KEYS = ['requirement', 'architecture', 'design', 'development', 'delivery'];
  const stages = ref([
    { key: 'requirement',  name: '需求分析' },
    { key: 'architecture', name: '架构设计' },
    { key: 'design',       name: '总体设计' },
    { key: 'development',  name: '自动开发' },
    { key: 'delivery',     name: '交付验证' },
  ]);

  // ── 状态 ──
  interface ChatMsg {
    id: number;
    role: 'user' | 'assistant' | 'system';
    content: string;
    thinking?: string;
    thinkingCollapsed?: boolean;
    strategies?: any[];
    document?: any;
    ir?: any;
    stageConfirmable?: boolean;
    stageConfirmed?: boolean;
  }

  const activePipelineId = ref(props.pipelineId ?? 0);
  const currentStageIndex = ref(0);
  const messages = ref<ChatMsg[]>([]);
  const inputText = ref('');
  const loading = ref(false);
  const attachments = ref<File[]>([]);
  const selectedProvider = ref('deepseek');
  const providers = ref<any[]>([]);
  const selectedStrategy = ref(-1);
  const chatStreamRef = ref<HTMLElement>();
  const textareaRef = ref();
  const autoScroll = ref(true);
  const showScrollUp = ref(false);
  const showScrollDown = ref(false);
  const showScrollButtons = ref(false);

  let abortController: AbortController | null = null;
  let connectTimer: ReturnType<typeof setTimeout> | null = null;

  const SSE_CONNECT_TIMEOUT_MS = 15000;

  // ── 供应商选项 ──
  const providerOptions = computed(() =>
    providers.value.map(p => ({ label: p.name, value: p.providerCode })),
  );

  // ── 快速提示词 ──
  const quickPrompts = [
    '我需要一个进销存管理系统',
    '帮我做一个审批工作流平台',
    '设计一个设备巡检系统',
    '我想要一个客户管理 CRM',
  ];

  // ── 思考动画 ──
  const thinkingLabels = [
    '正在分析您的需求...',
    '正在理解业务领域...',
    '正在梳理业务规则...',
    '正在生成领域模型...',
    '正在设计方案策略...',
    '正在组织输出...',
  ];
  const thinkingIndex = ref(0);
  const thinkingLabel = computed(() => thinkingLabels[thinkingIndex.value % thinkingLabels.length]);
  let thinkingTimer: ReturnType<typeof setInterval> | null = null;

  watch(loading, val => {
    if (val) {
      thinkingIndex.value = 0;
      thinkingTimer = setInterval(() => { thinkingIndex.value++; }, 2500);
    } else {
      if (thinkingTimer) { clearInterval(thinkingTimer); thinkingTimer = null; }
    }
  });

  // ── 输入框占位符 ──
  const inputPlaceholder = computed(() => {
    if (loading.value) return 'AI 正在思考中...';
    const names = ['描述你的业务需求，或回答 AI 的追问...', '对架构方案有疑问？或确认后推进...', '对详细设计有修改意见？或确认后推进...'];
    return names[currentStageIndex.value] ?? '输入消息...';
  });

  // ── 生命周期 ──
  onMounted(async () => {
    await loadProviders();
    if (activePipelineId.value > 0) {
      await loadPipelineState();
      if (messages.value.length === 0 && props.initialMessage) {
        sendMessage(props.initialMessage);
      }
    }
  });

  onUnmounted(() => {
    abortController?.abort();
    if (thinkingTimer) clearInterval(thinkingTimer);
    if (connectTimer) clearTimeout(connectTimer);
  });

  // ── 加载供应商 ──
  async function loadProviders() {
    try {
      const res: any = await defHttp.get({ url: '/api/studio/pipeline/providers' });
      providers.value = (res?.data?.items ?? res?.items ?? []).filter((p: any) => p.enabled);
      if (providers.value.length > 0) selectedProvider.value = providers.value[0].providerCode;
    } catch { /* 忽略 */ }
  }

  // ── 加载流水线状态 ──
  async function loadPipelineState() {
    if (!activePipelineId.value) return;
    try {
      const res: any = await defHttp.get({ url: `/api/studio/pipeline/execute/${activePipelineId.value}` });
      const detail = res?.data ?? res;
      const stageIdx = STAGE_KEYS.indexOf(detail?.currentStage ?? '');
      if (stageIdx >= 0) currentStageIndex.value = stageIdx;
      if (Array.isArray(detail?.messages)) {
        messages.value = detail.messages.map((m: any) => ({
          id: m.id ? Number(m.id) : Date.now(),
          role: m.role as ChatMsg['role'],
          content: m.content ?? '',
          thinking: '',
          thinkingCollapsed: true,
          strategies: [],
          document: null,
          ir: null,
          stageConfirmable: false,
          stageConfirmed: false,
        }));
        scrollToBottom();
      }
    } catch { /* 新流水线，正常 */ }
  }

  // ── 核心发送函数（先连 SSE，再触发 execute，用 fetch 携带 Authorization） ──
  async function sendMessage(content: string) {
    if (!content.trim() || loading.value) return;
    loading.value = true;
    autoScroll.value = true;

    messages.value.push({ id: Date.now(), role: 'user', content });
    scrollToBottom();

    // 首次发送：创建 Pipeline
    if (!activePipelineId.value) {
      try {
        const res: any = await defHttp.post({
          url: '/api/studio/pipeline/execute/create',
          data: { requirement: content },
        });
        const d = res?.data ?? res;
        activePipelineId.value = d?.PipelineId ?? d?.pipelineId ?? d?.id ?? 0;
        if (!activePipelineId.value) {
          antMessage.error('创建流水线失败，请重试');
          loading.value = false;
          return;
        }
      } catch (e: any) {
        antMessage.error('创建失败：' + (e?.message ?? ''));
        loading.value = false;
        return;
      }
    }

    // AI 消息占位
    const aiMsgId = Date.now() + 1;
    const aiMsg: ChatMsg = {
      id: aiMsgId,
      role: 'assistant',
      content: '',
      thinking: '',
      thinkingCollapsed: false,
      strategies: [],
      document: null,
      ir: null,
      stageConfirmable: false,
      stageConfirmed: false,
    };
    messages.value.push(aiMsg);

    // 1. 先用 fetch 打开 GET /events 读流（携带 Authorization 头）
    abortController = new AbortController();

    let sseOpened = false;
    connectTimer = setTimeout(() => {
      connectTimer = null;
      if (!sseOpened && loading.value) {
        const m = messages.value.find(x => x.id === aiMsgId);
        if (m && !m.content) m.content = '⚠️ 无法连接流式服务，请重试';
        loading.value = false;
        abortController?.abort();
      }
    }, SSE_CONNECT_TIMEOUT_MS);

    try {
      // 2. 同时触发 POST /execute（不等待其完成，让后端异步写 channel）
      defHttp.post({
        url: `/api/studio/pipeline/execute/${activePipelineId.value}/execute`,
        data: {
          message: content,
          stageName: stages.value[currentStageIndex.value]?.key ?? 'requirement',
          provider: selectedProvider.value,
        },
      }).catch(() => { /* 忽略 execute 同步错误 */ });

      // 3. fetch GET /events，用 Authorization 头传 token
      const eventsUrl = buildFetchSseUrl(
        `/api/studio/pipeline/execute/${activePipelineId.value}/events`,
      );
      const response = await fetch(eventsUrl, {
        method: 'GET',
        headers: {
          Authorization: String(getToken() ?? ''),
          Accept: 'text/event-stream',
          'Cache-Control': 'no-cache',
        },
        signal: abortController.signal,
      });

      if (!response.ok) {
        throw new Error(`SSE HTTP ${response.status}`);
      }

      sseOpened = true;
      if (connectTimer) { clearTimeout(connectTimer); connectTimer = null; }

      // 4. 逐行解析 SSE
      const reader = response.body!.getReader();
      const decoder = new TextDecoder();
      let buf = '';

      while (true) {
        const { done, value } = await reader.read();
        if (done) break;

        buf += decoder.decode(value, { stream: true });
        const lines = buf.split('\n');
        buf = lines.pop() ?? '';

        for (const line of lines) {
          if (!line.startsWith('data: ')) continue;
          const raw = line.slice(6).trim();
          if (raw === '[DONE]' || raw === '') continue;

          let evt: any;
          try { evt = JSON.parse(raw); } catch { continue; }

          const target = messages.value.find(x => x.id === aiMsgId);
          if (!target) continue;

          // 后端实际类型：chunk / done / error
          // 架构师扩展类型：token、delta、thinking、strategy、document、ir、stage_complete
          switch (evt.type) {
            case 'chunk':
            case 'token':
              target.content += evt.data ?? evt.content ?? '';
              if (autoScroll.value) scrollToBottom();
              break;
            case 'delta':
              target.content += evt.delta?.content ?? evt.content ?? '';
              if (autoScroll.value) scrollToBottom();
              break;
            case 'thinking':
              target.thinking = (target.thinking ?? '') + (evt.data ?? evt.content ?? '');
              break;
            case 'strategy':
              target.strategies = evt.strategies ?? [];
              break;
            case 'document':
              target.document = evt.document ?? null;
              break;
            case 'ir':
              target.ir = evt.ir ?? null;
              break;
            case 'stage_complete':
              target.stageConfirmable = true;
              break;
            case 'done':
              target.stageConfirmable = true;
              loading.value = false;
              scrollToBottom();
              return;
            case 'error':
              if (!target.content) target.content = `⚠️ ${evt.data ?? evt.content ?? 'AI 响应异常'}`;
              loading.value = false;
              return;
          }
        }
      }
    } catch (e: any) {
      if (e?.name === 'AbortError') {
        const m = messages.value.find(x => x.id === aiMsgId);
        if (m) m.content += (m.content ? '\n\n' : '') + '⏹️ [已停止生成]';
      } else {
        const m = messages.value.find(x => x.id === aiMsgId);
        if (m && !m.content) m.content = `⚠️ ${e?.message ?? '连接失败，请重试'}`;
      }
    } finally {
      if (connectTimer) { clearTimeout(connectTimer); connectTimer = null; }
      loading.value = false;
      if (autoScroll.value) scrollToBottom();
    }
  }

  // ── 用户操作 ──
  function handleSend() {
    const content = inputText.value.trim();
    if (!content || loading.value) return;
    inputText.value = '';
    attachments.value = [];
    sendMessage(content);
  }

  function handleEnter(e: KeyboardEvent) {
    if (!e.shiftKey) { e.preventDefault(); handleSend(); }
  }

  function handleStop() {
    abortController?.abort();
    loading.value = false;
  }

  function handleQuickPrompt(prompt: string) {
    inputText.value = prompt;
    nextTick(handleSend);
  }

  function handleSelectStrategy(idx: number, strategy: any) {
    selectedStrategy.value = idx;
    inputText.value = `我选择：${strategy.title}。${strategy.description ?? ''}`;
    nextTick(handleSend);
  }

  // ── 阶段确认 ──
  async function handleConfirmStage(msg: ChatMsg) {
    try {
      await defHttp.post({
        url: `/api/studio/pipeline/execute/stage/${activePipelineId.value}/confirm`,
        data: { approved: true, comment: '' },
      });
      msg.stageConfirmed = true;
      if (currentStageIndex.value >= stages.value.length - 1) {
        emit('pipeline-complete', { stage: stages.value.length });
        antMessage.success('全部阶段已完成！');
        return;
      }
      currentStageIndex.value++;
      messages.value.push({
        id: Date.now(),
        role: 'system',
        content: `✅ 已进入阶段 ${currentStageIndex.value + 1}：${stages.value[currentStageIndex.value]?.name}`,
      });
      scrollToBottom();
      sendMessage(`请开始阶段 ${currentStageIndex.value + 1}：${stages.value[currentStageIndex.value]?.name}`);
    } catch (e: any) {
      antMessage.error('确认失败：' + (e?.message ?? ''));
    }
  }

  // ── 回退 ──
  async function handleRollback() {
    if (currentStageIndex.value <= 0) return;
    try {
      const targetKey = stages.value[currentStageIndex.value - 1]?.key ?? 'requirement';
      await defHttp.post({
        url: `/api/studio/pipeline/execute/${activePipelineId.value}/rollback`,
        data: { targetStage: targetKey, reason: '用户主动回退' },
      });
      currentStageIndex.value--;
      messages.value.push({
        id: Date.now(),
        role: 'system',
        content: `↩️ 已回退到阶段 ${currentStageIndex.value + 1}：${stages.value[currentStageIndex.value]?.name}`,
      });
      scrollToBottom();
    } catch (e: any) {
      antMessage.error('回退失败：' + (e?.message ?? ''));
    }
  }

  // ── 新对话 ──
  function handleNewChat() {
    abortController?.abort();
    activePipelineId.value = 0;
    messages.value = [];
    inputText.value = '';
    attachments.value = [];
    currentStageIndex.value = 0;
    selectedStrategy.value = -1;
    emit('new-chat');
  }

  // ── 附件 ──
  function handleUpload(file: File) {
    attachments.value.push(file);
    return false;
  }

  // ── 文档操作 ──
  function previewDoc(doc: any) { window.open(doc.previewUrl, '_blank'); }
  function downloadDoc(doc: any) {
    const a = document.createElement('a');
    a.href = doc.downloadUrl;
    a.download = doc.name;
    a.click();
  }

  // ── Markdown 渲染 ──
  function renderMarkdown(text: string): string {
    if (!text) return '';
    return text
      .replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;')
      .replace(/^### (.+)$/gm, '<h3>$1</h3>')
      .replace(/^## (.+)$/gm, '<h2>$1</h2>')
      .replace(/^# (.+)$/gm, '<h1>$1</h1>')
      .replace(/\*\*(.+?)\*\*/g, '<strong>$1</strong>')
      .replace(/\*(.+?)\*/g, '<em>$1</em>')
      .replace(/`([^`]+)`/g, '<code>$1</code>')
      .replace(/^\s*[-*] (.+)$/gm, '<li>$1</li>')
      .replace(/^\s*\d+\. (.+)$/gm, '<li>$1</li>')
      .replace(/(<li>.*<\/li>\n?)+/g, m => `<ul>${m}</ul>`)
      .replace(/\n/g, '<br>');
  }

  // ── 滚动控制 ──
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
    const { scrollTop, scrollHeight, clientHeight } = el;
    showScrollUp.value = scrollTop > 200;
    showScrollDown.value = scrollHeight - scrollTop - clientHeight > 200;
    showScrollButtons.value = showScrollUp.value || showScrollDown.value;
    autoScroll.value = scrollHeight - scrollTop - clientHeight < 100;
  }
</script>

<style scoped lang="less">
.ai-chat-panel {
  display: flex;
  flex-direction: column;
  height: 100%;
  width: 100%;
  overflow: hidden;
  background: #fff;
  font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'PingFang SC', sans-serif;
  position: relative;
}

/* ── 顶栏 ── */
.top-bar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  height: 48px;
  padding: 0 16px;
  border-bottom: 1px solid #f0f0f0;
  background: #fafafa;
  flex-shrink: 0;
  .top-bar-left, .top-bar-right { display: flex; align-items: center; }
  .top-bar-center { display: flex; align-items: center; gap: 8px; }
  .model-select { width: 180px; }
  .stage-text { font-size: 13px; color: #555; font-weight: 500; }
}

.stage-detail-popover {
  min-width: 210px;
  .stage-item {
    display: flex; align-items: center; gap: 8px; padding: 6px 0;
    .stage-dot { width: 8px; height: 8px; border-radius: 50%; background: #d9d9d9; flex-shrink: 0; }
    &.active   .stage-dot { background: #1890ff; }
    &.completed .stage-dot { background: #52c41a; }
    .stage-name { flex: 1; font-size: 13px; }
  }
}

/* ── 对话流 ── */
.chat-stream {
  flex: 1;
  overflow-y: auto;
  overflow-x: hidden;
  padding: 20px 0 12px;
  scroll-behavior: smooth;
  min-height: 0;
}

/* 欢迎区 */
.welcome-section {
  max-width: 640px;
  margin: 0 auto;
  padding: 48px 24px 32px;
  text-align: center;
  .welcome-icon { font-size: 48px; margin-bottom: 16px; }
  h2 { font-size: 22px; margin-bottom: 10px; font-weight: 600; color: #222; }
  .welcome-desc { color: #444; font-size: 15px; line-height: 1.7; margin-bottom: 6px; }
  .welcome-hint { color: #1890ff; font-size: 13px; margin-bottom: 24px; }
  .quick-prompts {
    display: flex; flex-wrap: wrap; justify-content: center; gap: 10px;
    .quick-prompt {
      padding: 7px 16px; border: 1px solid #e0e0e0; border-radius: 20px;
      font-size: 13px; color: #444; cursor: pointer; transition: all 0.2s;
      &:hover { border-color: #1890ff; color: #1890ff; background: #f0f7ff; }
    }
  }
}

/* 消息行 */
.msg-row {
  display: flex; gap: 12px; padding: 6px 24px;
  max-width: 900px; margin: 0 auto; width: 100%; box-sizing: border-box;
}
.user-row { flex-direction: row-reverse; .msg-bubble { max-width: 72%; } }
.ai-row { .msg-bubble { max-width: 86%; } }

/* 头像 */
.avatar {
  width: 32px; height: 32px; border-radius: 50%;
  display: flex; align-items: center; justify-content: center;
  font-size: 12px; font-weight: 700; flex-shrink: 0; color: #fff;
}
.user-avatar { background: #52c41a; }
.ai-avatar   { background: #1890ff; }

/* 气泡 */
.msg-bubble {
  border-radius: 12px; padding: 11px 15px;
  font-size: 14px; line-height: 1.75; word-break: break-word;
}
.user-bubble {
  background: #1890ff; color: #fff; border-bottom-right-radius: 4px;
  :deep(.bubble-text) { color: #fff; }
  :deep(code) { background: rgba(255,255,255,0.2); border-radius: 3px; padding: 0 4px; }
}
.ai-bubble {
  background: #f7f7f8; color: #222; border-bottom-left-radius: 4px;
  :deep(h1), :deep(h2), :deep(h3) { margin: 8px 0 4px; font-weight: 600; }
  :deep(ul) { padding-left: 18px; margin: 4px 0; }
  :deep(li) { margin: 2px 0; }
  :deep(code) { background: #eee; border-radius: 3px; padding: 0 4px; font-size: 13px; }
  :deep(strong) { color: #111; }
}

/* 思考过程 */
.thinking-block {
  margin-bottom: 10px; border: 1px solid #e8e8e8; border-radius: 8px;
  background: #fff; overflow: hidden;
  .thinking-header {
    display: flex; align-items: center; justify-content: space-between;
    padding: 7px 12px; cursor: pointer; user-select: none;
    &:hover { background: #fafafa; }
    .thinking-label { font-size: 12px; color: #888; display: flex; align-items: center; gap: 4px; }
    .thinking-arrow { font-size: 10px; color: #aaa; }
  }
  .thinking-content {
    padding: 0 12px 8px; font-size: 12px; color: #999;
    line-height: 1.6; white-space: pre-wrap;
  }
}

/* 思考中动画 */
.thinking-bubble { background: #f0f7ff !important; border: 1px solid #d6e8fa; }
.thinking-animation {
  display: flex; align-items: center; gap: 10px;
  .thinking-text { color: #1890ff; font-size: 13px; }
  .dots {
    display: flex; gap: 4px;
    span {
      width: 6px; height: 6px; border-radius: 50%; background: #1890ff;
      animation: dot-bounce 1.4s infinite ease-in-out both;
      &:nth-child(1) { animation-delay: -0.32s; }
      &:nth-child(2) { animation-delay: -0.16s; }
    }
  }
}
@keyframes dot-bounce {
  0%, 80%, 100% { transform: scale(0); }
  40% { transform: scale(1); }
}

/* 策略卡片 */
.strategy-cards { display: flex; flex-direction: column; gap: 8px; margin-top: 12px; }
.strategy-card {
  display: flex; align-items: flex-start; gap: 12px; padding: 11px 14px;
  border: 1.5px solid #e8e8e8; border-radius: 8px; cursor: pointer;
  transition: all 0.18s; background: #fff;
  &:hover { border-color: #1890ff; background: #f0f7ff; }
  &.selected { border-color: #1890ff; background: #e6f4ff; }
  .strategy-icon { font-size: 18px; flex-shrink: 0; margin-top: 1px; }
  .strategy-title { font-weight: 600; font-size: 14px; margin-bottom: 3px; }
  .strategy-desc  { font-size: 13px; color: #666; }
}

/* 文档卡片 */
.doc-card {
  display: flex; align-items: center; gap: 12px;
  margin-top: 12px; padding: 11px 14px;
  border: 1px solid #e8e8e8; border-radius: 8px; background: #fff;
  .doc-icon { font-size: 24px; color: #1890ff; flex-shrink: 0; }
  .doc-info { flex: 1; .doc-name { font-weight: 500; font-size: 14px; } .doc-size { font-size: 12px; color: #999; } }
  .doc-actions { display: flex; gap: 4px; }
}

/* 阶段确认卡片 */
.stage-confirm-card {
  margin-top: 16px; padding: 12px 16px;
  border: 1px solid #b7eb8f; border-radius: 8px; background: #f6ffed;
  .confirm-header {
    display: flex; align-items: center; gap: 8px;
    margin-bottom: 12px; font-size: 14px; font-weight: 500; color: #389e0d;
  }
  .confirm-actions { display: flex; justify-content: flex-end; gap: 8px; }
}
.stage-confirmed-badge {
  margin-top: 10px; padding: 7px 12px; background: #f0f0f0;
  border-radius: 6px; font-size: 13px; color: #52c41a; text-align: center;
}

/* 系统消息 */
.system-msg {
  display: flex; align-items: center; gap: 12px;
  padding: 10px 48px; max-width: 900px; margin: 0 auto;
  .system-line { flex: 1; height: 1px; background: #ebebeb; }
  .system-text  { font-size: 12px; color: #aaa; white-space: nowrap; }
}

/* ── 滚动辅助 ── */
.scroll-buttons {
  position: absolute; right: 20px; bottom: 76px;
  display: flex; flex-direction: column; gap: 8px; z-index: 10;
  .scroll-btn { box-shadow: 0 2px 8px rgba(0,0,0,.15); }
}

/* ── 底部输入栏 ── */
.input-bar {
  display: flex; align-items: flex-end; gap: 10px;
  padding: 10px 20px 14px; border-top: 1px solid #f0f0f0;
  background: #fff; flex-shrink: 0;

  .attach-btn {
    width: 36px; height: 36px; border: 1px dashed #d0d0d0; border-radius: 50%;
    display: flex; align-items: center; justify-content: center; flex-shrink: 0;
    color: #888;
    &:hover { border-color: #1890ff; color: #1890ff; }
  }

  .input-wrapper {
    flex: 1; min-width: 0;
    :deep(textarea.ant-input) {
      border: none; box-shadow: none; resize: none;
      font-size: 14px; padding: 6px 0; background: transparent;
    }
    &::before {
      content: ''; display: block; border-bottom: 1px solid #e8e8e8;
    }
    .attachment-list { display: flex; gap: 4px; flex-wrap: wrap; padding-top: 4px; }
  }

  .send-btn { width: 38px; height: 38px; flex-shrink: 0; }
}
</style>
