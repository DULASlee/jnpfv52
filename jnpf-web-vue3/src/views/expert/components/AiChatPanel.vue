<template>
  <div class="ai-chat-panel">
    <!-- ====== 顶部：模型选择 + 阶段指示 + 操作按钮 ====== -->
    <div class="chat-header">
      <div class="header-left">
        <a-select v-model:value="selectedProvider" size="small" class="model-selector" @change="handleProviderChange">
          <a-select-option v-for="p in providers" :key="p.providerCode" :value="p.providerCode">
            {{ p.name }}
          </a-select-option>
        </a-select>
        <a-tag :color="stageColor" class="stage-tag"> 阶段 {{ currentStageIdx + 1 }}: {{ currentStageLabel }} </a-tag>
      </div>
      <div class="header-right">
        <a-button size="small" @click="handleNewChat">
          <template #icon><PlusOutlined /></template>
          新对话
        </a-button>
        <a-button v-if="stageReadyForConfirm" type="primary" size="small" @click="handleConfirmStage"> 确认推进到下一阶段 → </a-button>
      </div>
    </div>

    <!-- ====== 中间上部：消息列表 ====== -->
    <div ref="messagesRef" class="messages-area">
      <!-- 空状态：欢迎语 + 快速提示词 -->
      <div v-if="messages.length === 0 && !streaming" class="welcome">
        <div class="welcome-icon">🤖</div>
        <h3>AI 架构顾问</h3>
        <p>描述你的业务需求，我会通过多轮对话帮你梳理清楚，然后为你生成完整的软件系统。</p>
        <div class="quick-prompts">
          <a-tag v-for="prompt in quickPrompts" :key="prompt" class="quick-prompt" @click="inputText = prompt">
            {{ prompt }}
          </a-tag>
        </div>
      </div>

      <!-- 消息列表 -->
      <template v-for="msg in messages" :key="msg.id">
        <!-- 用户消息（右对齐，蓝色气泡） -->
        <div v-if="msg.role === 'user'" class="message-row user-row">
          <div class="message-bubble user-bubble">
            <div class="bubble-content">{{ msg.content }}</div>
          </div>
          <a-avatar class="user-avatar" style="background-color: #87d068">U</a-avatar>
        </div>

        <!-- AI 消息（左对齐，灰色气泡） -->
        <div v-else-if="msg.role === 'assistant'" class="message-row ai-row">
          <a-avatar class="ai-avatar" style="background-color: #1890ff">AI</a-avatar>
          <div class="message-bubble ai-bubble">
            <!-- eslint-disable-next-line vue/no-v-html -->
            <div class="bubble-content" v-html="renderMarkdown(msg.content)"></div>
            <IrPreviewCard v-if="msg.ir" :ir-data="msg.ir" />
            <div v-if="msg.document" class="doc-actions">
              <FileTextOutlined />
              <span>{{ msg.document.name }}</span>
              <a-button type="link" size="small" @click="previewDoc(msg.document)">预览</a-button>
              <a-button type="link" size="small" @click="downloadDoc(msg.document)">下载</a-button>
            </div>
          </div>
        </div>

        <!-- 系统消息（居中分割线） -->
        <div v-else-if="msg.role === 'system'" class="system-message">
          <a-divider />
          <span>{{ msg.content }}</span>
          <a-divider />
        </div>
      </template>

      <!-- AI 思考中动画 -->
      <div v-if="loading" class="message-row ai-row">
        <a-avatar class="ai-avatar" style="background-color: #1890ff">AI</a-avatar>
        <div class="message-bubble ai-bubble thinking">
          <div class="thinking-content">
            <span class="thinking-text">{{ thinkingText }}</span>
            <span class="thinking-dots"><span></span><span></span><span></span></span>
          </div>
        </div>
      </div>
    </div>

    <!-- ====== 中间下部：5 阶段进度条（折叠式） ====== -->
    <div class="stage-bar-wrapper">
      <PipelineStageBar
        :stages="pipelineStages"
        :waiting="loading"
        :can-rollback="currentStageIdx > 0"
        :can-confirm="stageReadyForConfirm"
        @confirm="handleStageConfirm"
        @rollback="handleRollback"
        @ask="handleAsk"
        @select-stage="handleSelectStage" />
    </div>

    <!-- ====== 底部：用户输入栏 ====== -->
    <div class="input-bar">
      <!-- 附件上传（+ 按钮） -->
      <a-upload :before-upload="handleUpload" :show-upload-list="false" multiple>
        <a-button class="attach-btn" type="text">
          <template #icon><PlusOutlined /></template>
        </a-button>
      </a-upload>

      <!-- 输入框 -->
      <div class="input-wrapper">
        <a-textarea
          ref="textareaRef"
          v-model:value="inputText"
          :placeholder="inputPlaceholder"
          :auto-size="{ minRows: 1, maxRows: 6 }"
          :disabled="loading"
          @press-enter="handleEnter" />
        <div v-if="attachments.length > 0" class="attachment-list">
          <a-tag v-for="(file, idx) in attachments" :key="idx" closable @close="attachments.splice(idx, 1)">
            {{ file.name }}
          </a-tag>
        </div>
      </div>

      <!-- 发送/停止按钮（双态切换） -->
      <a-button
        class="send-stop-btn"
        :type="loading ? 'default' : 'primary'"
        :danger="loading"
        shape="circle"
        size="large"
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
  import { ref, reactive, computed, onMounted, onUnmounted, nextTick, watch } from 'vue';
  import { PlusOutlined, SendOutlined, PauseOutlined, FileTextOutlined } from '@ant-design/icons-vue';
  import { message } from 'ant-design-vue';
  import PipelineStageBar, { type StageInfo } from '../../studio/components/PipelineStageBar.vue';
  import IrPreviewCard from '../../studio/components/chat/IrPreviewCard.vue';
  import { useSSE } from '../../studio/composables/useSSE';
  import { defHttp } from '/@/utils/http/axios';

  defineOptions({ name: 'AiChatPanel' });

  const props = defineProps<{
    pipelineId?: number;
    initialMessage?: string;
  }>();

  const emit = defineEmits<{
    'pipeline-complete': [data: { stage: number }];
    'new-chat': [];
  }>();

  // ── 模型供应商 ──
  const selectedProvider = ref('deepseek');
  const providers = ref<any[]>([]);

  // ── 状态 ──
  interface ChatMessage {
    id: number;
    role: 'user' | 'assistant' | 'system';
    content: string;
    contentType?: 'text' | 'ir' | 'document';
    ir?: any;
    document?: any;
    time: string;
  }
  const messages = ref<ChatMessage[]>([]);
  const inputText = ref('');
  const loading = ref(false);
  const streaming = ref(false);
  const streamText = ref('');
  const stageReadyForConfirm = ref(false);
  const attachments = ref<File[]>([]);
  const messagesRef = ref<HTMLElement | null>(null);
  const textareaRef = ref();
  const abortFlag = ref(false);

  // ── 5 阶段配置 ──
  const pipelineStages = reactive<StageInfo[]>([
    { key: 'requirement', label: '需求分析', status: 'running' },
    { key: 'architecture', label: '架构设计', status: 'pending' },
    { key: 'design', label: '总体设计', status: 'pending' },
    { key: 'development', label: '自动开发', status: 'pending' },
    { key: 'delivery', label: '交付验证', status: 'pending' },
  ]);

  const currentStageIdx = ref(0);
  const currentStageLabel = computed(() => pipelineStages[currentStageIdx.value]?.label || '—');
  const stageColor = computed(() => {
    const colors = ['blue', 'cyan', 'geekblue', 'purple', 'green'];
    return colors[currentStageIdx.value] || 'blue';
  });

  // ── 快速提示词 ──
  const quickPrompts = ['我需要一个进销存管理系统', '帮我做一个审批工作流平台', '设计一个设备巡检系统', '我想要一个客户管理 CRM'];

  const inputPlaceholder = computed(() => {
    if (loading.value) return 'AI 正在思考中...';
    return '有问题，尽管问，Enter 发送，Shift+Enter 换行';
  });

  // ── 思考文字轮播 ──
  const thinkingTexts = ['正在分析您的需求...', '正在理解业务领域...', '正在梳理业务规则...', '正在生成领域模型...', '正在设计方案策略...', '正在组织输出...'];
  const thinkingIndex = ref(0);
  const thinkingText = computed(() => thinkingTexts[thinkingIndex.value % thinkingTexts.length]);
  let thinkingTimer: ReturnType<typeof setInterval> | null = null;

  watch(loading, val => {
    if (val) {
      thinkingTimer = setInterval(() => {
        thinkingIndex.value++;
      }, 2000);
    } else {
      if (thinkingTimer) clearInterval(thinkingTimer);
    }
  });

  // SSE 清理
  let sseDisconnect: (() => void) | null = null;
  let connectTimer: ReturnType<typeof setTimeout> | null = null;
  /** 仅用于 SSE HTTP 连接建立（onopen），不限制 LLM 首 token 等待（后端 TimeoutMs=120s） */
  const SSE_CONNECT_TIMEOUT_MS = 15000;

  function clearConnectTimer() {
    if (connectTimer) {
      clearTimeout(connectTimer);
      connectTimer = null;
    }
  }

  function failStream(aiMsgId: number, text: string, disconnect: () => void) {
    clearConnectTimer();
    const aiMsg = messages.value.find(m => m.id === aiMsgId);
    if (aiMsg && !aiMsg.content) aiMsg.content = text;
    streaming.value = false;
    loading.value = false;
    disconnect();
  }

  // ── 生命周期 ──
  onMounted(async () => {
    await loadProviders();
    if (props.pipelineId) {
      await loadPipelineState();
      if (messages.value.length === 0 && props.initialMessage) {
        sendMessage(props.initialMessage);
      }
    }
  });

  onUnmounted(() => {
    if (sseDisconnect) sseDisconnect();
    clearConnectTimer();
    if (thinkingTimer) clearInterval(thinkingTimer);
  });

  // ── 加载供应商列表 ──
  async function loadProviders() {
    try {
      const res: any = await defHttp.get({ url: '/api/studio/pipeline/providers' });
      providers.value = (res?.data?.items || res?.items || []).filter((p: any) => p.enabled);
      if (providers.value.length > 0 && !selectedProvider.value) {
        selectedProvider.value = providers.value[0].providerCode;
      }
    } catch {
      /* 忽略 */
    }
  }

  // ── 加载流水线状态 ──
  async function loadPipelineState() {
    try {
      const res: any = await defHttp.get({ url: `/api/studio/pipeline/execute/${props.pipelineId}` });
      const detail = res?.data || res;
      if (detail?.currentStage) {
        const idx = pipelineStages.findIndex(s => s.key === detail.currentStage);
        if (idx >= 0) updateStageUI(idx);
      }
      if (detail?.messages) {
        messages.value = detail.messages.map((m: any) => ({
          id: m.id || Date.now(),
          role: m.role,
          content: m.content,
          contentType: m.contentType || 'text',
          ir: m.ir || null,
          document: m.document || null,
          time: new Date(m.createTime || Date.now()).toLocaleTimeString(),
        }));
      }
      scrollToBottom();
    } catch {
      /* 流水线可能尚未创建 */
    }
  }

  // ── 更新阶段 UI ──
  function updateStageUI(idx: number) {
    for (let i = 0; i < pipelineStages.length; i++) {
      if (i < idx) pipelineStages[i].status = 'completed';
      else if (i === idx) pipelineStages[i].status = 'running';
      else pipelineStages[i].status = 'pending';
    }
    currentStageIdx.value = idx;
  }

  // ── 发送消息（SSE 流式） ──
  async function sendMessage(content: string) {
    if (!content.trim()) return;
    loading.value = true;
    stageReadyForConfirm.value = false;
    abortFlag.value = false;

    messages.value.push({
      id: Date.now(),
      role: 'user',
      content,
      contentType: 'text',
      time: new Date().toLocaleTimeString(),
    });
    scrollToBottom();

    const aiMsgId = Date.now() + 1;
    messages.value.push({
      id: aiMsgId,
      role: 'assistant',
      content: '',
      contentType: 'text',
      time: new Date().toLocaleTimeString(),
    });

    // 先连 SSE 再触发 execute，避免 channel 竞态；token 经 buildEventSourceUrl ?token= 传递
    try {
      connectSSE(aiMsgId);
      await defHttp.post({
        url: `/api/studio/pipeline/execute/${props.pipelineId}/execute`,
        data: {
          message: content,
          stageName: pipelineStages[currentStageIdx.value]?.key || 'requirement',
          provider: selectedProvider.value,
        },
      });
    } catch {
      /* 忽略 /execute 同步返回错误，直接读 SSE */
    }
  }

  // ── SSE 流式连接 ──
  function connectSSE(aiMsgId: number) {
    if (sseDisconnect) sseDisconnect();
    clearConnectTimer();

    streamText.value = '';
    streaming.value = true;

    let sseOpened = false;

    const { connect, disconnect } = useSSE({
      url: `/api/studio/pipeline/execute/${props.pipelineId}/events`,
      onOpen: () => {
        sseOpened = true;
        clearConnectTimer();
      },
      onMessage: (msg: any) => {
        if (abortFlag.value) {
          disconnect();
          return;
        }

        const aiMsg = messages.value.find(m => m.id === aiMsgId);

        if (msg.type === 'chunk' || msg.type === 'token') {
          streamText.value += msg.data || msg.content || '';
          if (aiMsg) aiMsg.content = streamText.value;
          scrollToBottom();
        } else if (msg.type === 'ir_update' && aiMsg) {
          try {
            aiMsg.ir = typeof msg.data === 'string' ? JSON.parse(msg.data) : msg.data;
          } catch {
            /* ignore */
          }
        } else if (msg.type === 'stage_change') {
          const idx = pipelineStages.findIndex(s => s.key === (msg.stage || 'requirement'));
          if (idx >= 0) updateStageUI(idx);
        } else if (msg.type === 'done') {
          clearConnectTimer();
          if (aiMsg && streamText.value) {
            aiMsg.content = streamText.value;
            streamText.value = '';
          }
          stageReadyForConfirm.value = true;
          streaming.value = false;
          loading.value = false;
          disconnect();
          scrollToBottom();
        } else if (msg.type === 'error') {
          failStream(aiMsgId, msg.data || '⚠️ 请求失败，请重试', disconnect);
        }
      },
      onGiveUp: () => {
        if (streaming.value) {
          failStream(aiMsgId, '⚠️ 连接中断，请重试', disconnect);
        }
      },
    });

    sseDisconnect = disconnect;
    connectTimer = setTimeout(() => {
      connectTimer = null;
      if (!sseOpened && streaming.value) {
        failStream(aiMsgId, '⚠️ 无法连接流式服务，请重试', disconnect);
      }
    }, SSE_CONNECT_TIMEOUT_MS);
    connect();
  }

  // ── 用户操作 ──
  function handleSend() {
    const content = inputText.value.trim();
    if (!content || loading.value) return;
    inputText.value = '';
    attachments.value = [];
    sendMessage(content);
  }

  function handleStop() {
    abortFlag.value = true;
    if (sseDisconnect) sseDisconnect();
    loading.value = false;
    streaming.value = false;
    message.info('已停止生成');
  }

  function handleEnter(e: KeyboardEvent) {
    if (!e.shiftKey) {
      e.preventDefault();
      handleSend();
    }
  }

  function handleUpload(file: File) {
    attachments.value.push(file);
    return false; // 阻止自动上传
  }

  // ── 阶段操作 ──
  async function handleConfirmStage() {
    try {
      await defHttp.post({
        url: `/api/studio/pipeline/execute/stage/${props.pipelineId}/confirm`,
        data: { stage: currentStageIdx.value + 1, approved: true },
      });

      if (currentStageIdx.value >= pipelineStages.length - 1) {
        emit('pipeline-complete', { stage: 5 });
        message.success('全部阶段已完成！');
        return;
      }

      const nextIdx = currentStageIdx.value + 1;
      updateStageUI(nextIdx);
      stageReadyForConfirm.value = false;

      messages.value.push({
        id: Date.now(),
        role: 'system',
        content: `✅ 阶段 ${nextIdx} 已确认，进入「${pipelineStages[nextIdx].label}」`,
        contentType: 'text',
        time: new Date().toLocaleTimeString(),
      });
      scrollToBottom();

      sendMessage(`请开始阶段 ${nextIdx + 1}：${pipelineStages[nextIdx].label}`);
    } catch (e) {
      console.error('确认失败', e);
    }
  }

  // PipelineStageBar 的 confirm 回调（带 feedback 参数）
  async function handleStageConfirm(feedback: string) {
    if (feedback) {
      messages.value.push({
        id: Date.now(),
        role: 'user',
        content: feedback,
        contentType: 'text',
        time: new Date().toLocaleTimeString(),
      });
    }
    await handleConfirmStage();
  }

  function handleRollback() {
    const prev = Math.max(0, currentStageIdx.value - 1);
    updateStageUI(prev);
    stageReadyForConfirm.value = false;
    messages.value.push({
      id: Date.now(),
      role: 'system',
      content: `↩️ 已回退到阶段 ${prev + 1}：${pipelineStages[prev].label}`,
      contentType: 'text',
      time: new Date().toLocaleTimeString(),
    });
    scrollToBottom();
  }

  function handleAsk(feedback: string) {
    if (feedback) {
      inputText.value = feedback;
      handleSend();
    }
  }

  function handleSelectStage(_key: string) {
    /* 查看阶段详情 */
  }

  // ── 其他操作 ──
  function handleNewChat() {
    emit('new-chat');
  }

  function handleProviderChange(_code: string) {
    // 无需额外操作
  }

  function previewDoc(doc: any) {
    if (doc.previewUrl) window.open(doc.previewUrl, '_blank');
  }

  function downloadDoc(doc: any) {
    const a = document.createElement('a');
    a.href = doc.downloadUrl || '#';
    a.download = doc.name || 'download';
    a.click();
  }

  // ── Markdown 渲染 ──
  function renderMarkdown(text: string): string {
    if (!text) return '';
    return text
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')
      .replace(/\*\*(.+?)\*\*/g, '<strong>$1</strong>')
      .replace(/\*(.+?)\*/g, '<em>$1</em>')
      .replace(/`([^`]+)`/g, '<code>$1</code>')
      .replace(/^- (.+)$/gm, '<li>$1</li>')
      .replace(/\n/g, '<br>');
  }

  // ── 滚动到底部 ──
  function scrollToBottom() {
    nextTick(() => {
      if (messagesRef.value) {
        messagesRef.value.scrollTop = messagesRef.value.scrollHeight;
      }
    });
  }
</script>

<style scoped lang="less">
  .ai-chat-panel {
    display: flex;
    flex-direction: column;
    height: 100%;
    background: #fff;
    font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
  }

  /* ====== 顶部 ====== */
  .chat-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 8px 16px;
    border-bottom: 1px solid #f0f0f0;
    background: #fafafa;
    flex-shrink: 0;

    .header-left {
      display: flex;
      align-items: center;
      gap: 12px;
    }

    .model-selector {
      width: 180px;
    }

    .header-right {
      display: flex;
      gap: 8px;
    }
  }

  /* ====== 消息区 ====== */
  .messages-area {
    flex: 1;
    min-height: 0;
    overflow-y: auto;
    padding: 20px 16px;
  }

  /* 欢迎语 */
  .welcome {
    text-align: center;
    padding: 60px 40px 40px;

    .welcome-icon {
      font-size: 48px;
      margin-bottom: 16px;
    }

    h3 {
      font-size: 20px;
      margin-bottom: 8px;
      color: #333;
    }

    p {
      color: #666;
      font-size: 14px;
      max-width: 480px;
      margin: 0 auto 24px;
      line-height: 1.6;
    }

    .quick-prompts {
      display: flex;
      flex-wrap: wrap;
      justify-content: center;
      gap: 8px;

      .quick-prompt {
        cursor: pointer;
        padding: 6px 16px;
        font-size: 13px;
        border-radius: 16px;
        transition: all 0.2s;

        &:hover {
          color: #1890ff;
          border-color: #1890ff;
          background: #e6f7ff;
        }
      }
    }
  }

  /* 消息行 */
  .message-row {
    display: flex;
    gap: 10px;
    margin-bottom: 20px;
    max-width: 85%;
  }

  .user-row {
    flex-direction: row-reverse;
    margin-left: auto;
  }

  .ai-row {
    margin-right: auto;
  }

  /* 消息气泡 */
  .message-bubble {
    border-radius: 14px;
    padding: 12px 16px;
    font-size: 14px;
    line-height: 1.65;
    max-width: 100%;
    word-break: break-word;
  }

  .user-bubble {
    background: #1890ff;
    color: #fff;
    border-bottom-right-radius: 4px;
  }

  .ai-bubble {
    background: #f5f5f5;
    color: #333;
    border-bottom-left-radius: 4px;

    &.thinking {
      background: #f0f7ff;
      border: 1px solid #d6e8fa;
    }
  }

  /* 思考中动画 */
  .thinking-content {
    display: flex;
    align-items: center;
    gap: 8px;

    .thinking-text {
      color: #1890ff;
      font-size: 13px;
    }

    .thinking-dots {
      display: flex;
      gap: 3px;

      span {
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

  /* 系统消息 */
  .system-message {
    text-align: center;
    color: #999;
    font-size: 12px;
    padding: 8px 0;
  }

  /* 文档操作 */
  .doc-actions {
    margin-top: 12px;
    padding: 8px 12px;
    background: #fff;
    border: 1px solid #e8e8e8;
    border-radius: 6px;
    display: flex;
    align-items: center;
    gap: 8px;
    font-size: 13px;
  }

  /* 内联样式穿透 */
  :deep(.ant-avatar) {
    flex-shrink: 0;
  }

  :deep(code) {
    background: rgba(0, 0, 0, 0.06);
    padding: 2px 6px;
    border-radius: 4px;
    font-size: 13px;
  }

  :deep(strong) {
    font-weight: 600;
  }

  :deep(li) {
    margin-left: 16px;
  }

  /* ====== 进度条区 ====== */
  .stage-bar-wrapper {
    flex-shrink: 0;
    border-top: 1px solid #f0f0f0;
  }

  /* ====== 输入栏 ====== */
  .input-bar {
    display: flex;
    align-items: flex-end;
    gap: 8px;
    padding: 10px 16px;
    border-top: 1px solid #f0f0f0;
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
      color: #888;

      &:hover {
        border-color: #1890ff;
        color: #1890ff;
      }
    }

    .input-wrapper {
      flex: 1;

      :deep(.ant-input) {
        border: none;
        box-shadow: none;
        resize: none;
        font-size: 14px;
        padding: 6px 0;
        line-height: 1.5;
      }

      .attachment-list {
        display: flex;
        gap: 4px;
        flex-wrap: wrap;
        padding-top: 4px;
      }
    }

    .send-stop-btn {
      width: 38px;
      height: 38px;
      flex-shrink: 0;
    }
  }
</style>
