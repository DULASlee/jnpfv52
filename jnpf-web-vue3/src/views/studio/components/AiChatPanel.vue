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
        <a-button size="small" @click="handleNewChat">
          <template #icon><PlusOutlined /></template>
          新对话
        </a-button>
      </div>
    </div>

    <!-- ====== 中间：对话流（核心区域，占满全部空间） ====== -->
    <div class="chat-stream" ref="chatStreamRef" @scroll="handleScroll">
      <!-- 欢迎界面 -->
      <div v-if="messages.length === 0" class="welcome-card">
        <div class="welcome-icon">🤖</div>
        <h2>AI 架构顾问</h2>
        <p>你好！我是你的 AI 架构顾问。请描述你的业务需求，我会通过多轮对话帮你梳理清楚，然后生成完整的软件系统。</p>
        <p class="hint">我会主动追问关键问题，确保需求没有模糊之处。</p>
        <div class="quick-start-label">快速开始：</div>
        <div class="quick-prompts">
          <span v-for="p in quickPrompts" :key="p" class="quick-prompt" @click="handleQuickPrompt(p)">{{ p }}</span>
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
            <!-- 思考过程（折叠） -->
            <div v-if="msg.thinking" class="thinking-block">
              <div class="thinking-header" @click="msg.thinkingCollapsed = !msg.thinkingCollapsed">
                <span>💭 思考过程{{ msg.thinkingCollapsed ? '（可折叠）' : '' }}</span>
                <span>{{ msg.thinkingCollapsed ? '▸' : '▾' }}</span>
              </div>
              <div v-if="!msg.thinkingCollapsed" class="thinking-content">{{ msg.thinking }}</div>
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

      <!-- AI 正在思考 -->
      <div v-if="loading" class="msg-card ai-card">
        <div class="card-avatar ai-card-avatar">🤖</div>
        <div class="card-body">
          <div class="thinking-anim">
            <span class="thinking-label">{{ thinkingLabel }}</span>
            <span class="dots"><i></i><i></i><i></i></span>
          </div>
        </div>
      </div>
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
        <a-button class="attach-btn" type="text">
          <template #icon><PlusOutlined /></template>
        </a-button>
      </a-upload>
      <div class="input-wrap">
        <a-textarea
          ref="textareaRef"
          v-model:value="inputText"
          :placeholder="inputPlaceholder"
          :auto-size="{ minRows: 1, maxRows: 5 }"
          @press-enter="handleEnter" />
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
  import { ref, computed, onMounted, nextTick, watch } from 'vue';
  import { PlusOutlined, SendOutlined, PauseOutlined, UpOutlined, DownOutlined } from '@ant-design/icons-vue';
  import { message as antMessage } from 'ant-design-vue';
  import { defHttp } from '/@/utils/http/axios';
  import IrPreviewCard from './chat/IrPreviewCard.vue';
  import { buildFetchSseUrl } from '/@/utils/http/sseUrl';
  import { getToken } from '/@/utils/auth';
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

  // ====== Props / Emits ======
  const props = withDefaults(defineProps<{ pipelineId?: number; initialMessage?: string }>(), { pipelineId: 0, initialMessage: '' });
  const emit = defineEmits(['pipeline-complete', 'new-chat']);

  // ====== 状态 ======
  const currentStage = ref(1);
  const messages = ref<any[]>([]);
  const inputText = ref('');
  const loading = ref(false);
  const attachments = ref<File[]>([]);
  const selectedProvider = ref('deepseek');
  const providers = ref<any[]>([]);
  const selectedStrategy = ref(-1);
  const abortController = ref<AbortController | null>(null);
  const pipelineId = ref(props.pipelineId || 0);
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

  const quickPrompts = ['我需要一个进销存管理系统', '帮我做一个审批工作流平台', '设计一个设备巡检系统', '我想要一个客户管理 CRM'];

  const thinkingLabels = ['正在分析您的需求...', '正在理解业务领域...', '正在梳理业务规则...', '正在生成领域模型...', '正在设计方案策略...', '正在组织输出...'];
  const thinkingIndex = ref(0);
  const thinkingLabel = computed(() => thinkingLabels[thinkingIndex.value % thinkingLabels.length]);
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
    if (pipelineId.value > 0) {
      await loadPipelineState();
      if (messages.value.length === 0 && props.initialMessage) await sendMessage(props.initialMessage);
    }
  });

  // ====== 方法 ======
  async function loadProviders() {
    try {
      const res = await defHttp.get({ url: '/api/studio/pipeline/execute/providers' });
      providers.value = (res?.items ?? []).filter((p: any) => p.enabled);
      if (providers.value.length > 0) selectedProvider.value = providers.value[0].providerCode;
    } catch {}
  }

  async function loadPipelineState() {
    if (!pipelineId.value) return;
    try {
      const res = await defHttp.get({ url: '/api/studio/pipeline/execute/' + pipelineId.value });
      currentStage.value = res.currentStage || 1;
      messages.value = (res.messages || []).map((m: any) => ({
        id: m.id || Date.now(),
        role: m.role,
        content: m.content || '',
        thinking: m.thinking || '',
        thinkingCollapsed: true,
        strategies: m.strategies || [],
        document: m.document || null,
        ir: m.ir || null,
        stageConfirmable: m.stageConfirmable || false,
        stageConfirmed: m.stageConfirmed || false,
      }));
      updateStageStatus();
      scrollToBottom();
    } catch (e) {
      console.error('加载状态失败', e);
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
  async function sendMessage(content: string) {
    if (!content.trim()) return;
    loading.value = true;
    autoScroll.value = true;

    messages.value.push({ id: Date.now(), role: 'user', content, time: new Date().toLocaleTimeString() });
    scrollToBottom();

    if (!pipelineId.value) {
      try {
        const res = await defHttp.post({ url: '/api/studio/pipeline/execute/create', data: { requirement: content } });
        // RESTfulResult 可能包在 data 中，兼容 camelCase / PascalCase
        const data = res?.data || res;
        pipelineId.value = data?.pipelineId || data?.PipelineId || data?.id || data?.Id || pipelineId.value;
        if (!pipelineId.value) {
          antMessage.error('流水线创建失败：未获取到有效ID');
          loading.value = false;
          return;
        }
      } catch (e: any) {
        antMessage.error('创建失败: ' + (e?.message || '未知错误'));
        loading.value = false;
        return;
      }
    }

    const aiMsgId = Date.now() + 1;
    messages.value.push({
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
    });

    try {
      // Step 1: POST /execute 发送消息，启动后台 LLM 流（用 defHttp 走 axios 代理）
      await defHttp.post({
        url: '/api/studio/pipeline/execute/' + pipelineId.value + '/execute',
        data: {
          message: content,
          stageName: stages.value[currentStage.value - 1]?.code || 'requirement',
          provider: selectedProvider.value,
        },
      });

      // Step 2: GET /events 读取 SSE 流
      abortController.value = new AbortController();
      const sseUrl = buildFetchSseUrl('/api/studio/pipeline/execute/' + pipelineId.value + '/events');
      const sseHeaders: Record<string, string> = { Accept: 'text/event-stream' };
      const token = getToken();
      if (token) sseHeaders['Authorization'] = token.startsWith('Bearer ') ? token : `Bearer ${token}`;

      const response = await fetch(sseUrl, {
        method: 'GET',
        headers: sseHeaders,
        signal: abortController.value.signal,
      });
      if (!response.ok) throw new Error('HTTP ' + response.status);

      const reader = response.body?.getReader();
      const decoder = new TextDecoder();
      let buffer = '';
      if (reader) {
        while (true) {
          const { done, value } = await reader.read();
          if (done) break;
          buffer += decoder.decode(value, { stream: true });
          const lines = buffer.split('\n');
          buffer = lines.pop() || '';
          for (const line of lines) {
            if (!line.startsWith('data: ') || line === 'data: [DONE]') continue;
            try {
              const data = JSON.parse(line.substring(6));
              const msg = messages.value.find(m => m.id === aiMsgId);
              if (!msg) continue;
              switch (data.type) {
                case 'thinking':
                case 'info':
                  msg.thinking += (data.data || data.content || '') + '\n';
                  break;
                case 'token':
                case 'delta':
                  msg.content += data.data || data.content || data.delta?.content || '';
                  if (autoScroll.value) scrollToBottom();
                  break;
                case 'strategy':
                  msg.strategies = data.data || data.strategies || [];
                  break;
                case 'document':
                  msg.document = data.data || data.document;
                  break;
                case 'ir':
                  msg.ir = data.data || data.ir;
                  break;
                case 'stage_complete':
                  msg.stageConfirmable = true;
                  break;
                case 'done':
                  break;
                case 'error':
                  msg.content += '\n\n⚠️ ' + (data.data || data.content || 'AI 响应异常');
                  break;
              }
            } catch {}
          }
        }
      }
    } catch (e: any) {
      if (e.name === 'AbortError') {
        const msg = messages.value.find(m => m.id === aiMsgId);
        if (msg) msg.content += '\n\n⏹️ [已停止生成]';
      } else {
        const msg = messages.value.find(m => m.id === aiMsgId);
        if (msg && !msg.content) msg.content = '⚠️ ' + (e.message || '发送失败，请重试');
      }
    } finally {
      loading.value = false;
      abortController.value = null;
      if (autoScroll.value) scrollToBottom();
    }
  }

  function handleSend() {
    const content = inputText.value.trim();
    if (!content || loading.value) return;
    inputText.value = '';
    attachments.value = [];
    sendMessage(content);
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

  function handleQuickPrompt(prompt: string) {
    inputText.value = prompt;
    nextTick(() => handleSend());
  }

  function handleSelectStrategy(idx: number, s: any) {
    selectedStrategy.value = idx;
    inputText.value = '我选择：' + s.title + '。' + (s.description || '');
    nextTick(() => handleSend());
  }

  async function handleConfirmStage(msg: any) {
    try {
      await defHttp.post({ url: '/api/studio/pipeline/execute/stage/' + pipelineId.value + '/confirm', data: { stage: currentStage.value, approved: true } });
      msg.stageConfirmed = true;
      if (currentStage.value >= 5) {
        emit('pipeline-complete', { stage: 5 });
        antMessage.success('全部阶段已完成！');
        return;
      }
      currentStage.value++;
      updateStageStatus();
      messages.value.push({
        id: Date.now(),
        role: 'system',
        content: '✅ 已进入阶段 ' + currentStage.value + ': ' + stages.value[currentStage.value - 1]?.name,
      });
      scrollToBottom();
      sendMessage('请开始阶段 ' + currentStage.value + '：' + stages.value[currentStage.value - 1]?.name);
    } catch (e: any) {
      antMessage.error('确认失败: ' + (e?.message || ''));
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
    pipelineId.value = 0;
    messages.value = [];
    inputText.value = '';
    attachments.value = [];
    currentStage.value = 1;
    updateStageStatus();
    emit('new-chat');
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
</script>

<style scoped lang="less">
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
    .quick-start-label {
      font-size: 13px;
      color: #999;
      margin-bottom: 8px;
    }
    .quick-prompts {
      display: flex;
      flex-wrap: wrap;
      justify-content: center;
      gap: 8px;
      .quick-prompt {
        padding: 8px 16px;
        border: 1px solid #e8e8e8;
        border-radius: 6px;
        font-size: 13px;
        color: #333;
        cursor: pointer;
        background: #fff;
        transition: all 0.2s;
        &:hover {
          border-color: #1890ff;
          color: #1890ff;
          background: #f0f7ff;
        }
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
