<template>
  <div class="ai-chat-panel">
    <!-- ====== 顶栏：一行极简 ====== -->
    <div class="top-bar">
      <div class="top-bar-left">
        <!-- 模型选择器 -->
        <a-select
          v-model:value="selectedProvider"
          size="small"
          class="model-select"
          @change="handleProviderChange"
        >
          <a-select-option v-for="p in providers" :key="p.providerCode" :value="p.providerCode">
            {{ p.name }}
          </a-select-option>
        </a-select>
      </div>

      <div class="top-bar-center">
        <!-- 阶段进度：一行文字 -->
        <span class="stage-text">
          阶段 {{ currentStage }}/{{ stages.length }}: {{ stages[currentStage - 1]?.name }}
        </span>
        <!-- 阶段详情按钮 -->
        <a-popover trigger="click" placement="bottomRight">
          <template #content>
            <div class="stage-detail-popover">
              <div
                v-for="s in stages"
                :key="s.stage"
                class="stage-item"
                :class="{ active: s.stage === currentStage, completed: s.stage < currentStage }"
              >
                <span class="stage-dot" />
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
          >
            {{ prompt }}
          </span>
        </div>
      </div>

      <!-- 消息列表 -->
      <template v-for="msg in messages" :key="msg.id">
        <!-- 用户消息 -->
        <div v-if="msg.role === 'user'" class="msg-row user-row">
          <div class="msg-bubble user-bubble">
            <div class="bubble-text" v-html="renderMarkdown(msg.content)"></div>
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
              <div v-if="!msg.thinkingCollapsed" class="thinking-content">
                {{ msg.thinking }}
              </div>
            </div>

            <!-- AI 正文（Markdown 渲染） -->
            <div class="bubble-text" v-html="renderMarkdown(msg.content)"></div>

            <!-- 策略选项卡片（如果消息包含策略） -->
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

            <!-- 文档预览卡片（如果消息包含文档） -->
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

            <!-- 阶段确认操作卡片（AI 回复末尾，自然跟在内容后面） -->
            <div v-if="msg.stageConfirmable && !msg.stageConfirmed" class="stage-confirm-card">
              <div class="confirm-header">
                <CheckCircleOutlined />
                <span>阶段 {{ currentStage }}: {{ stages[currentStage - 1]?.name }} 分析完成</span>
              </div>
              <div class="confirm-actions">
                <a-button size="small" @click="handleRollback(currentStage - 1)" :disabled="currentStage <= 1">
                  ↩️ 回退修改
                </a-button>
                <a-button type="primary" size="small" @click="handleConfirmStage(msg)">
                  确认并推进 ▶️
                </a-button>
              </div>
            </div>

            <!-- 已确认标记 -->
            <div v-if="msg.stageConfirmed" class="stage-confirmed-badge">
              ✅ 已确认，进入 {{ stages[currentStage - 1]?.name }}
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

      <!-- AI 正在思考 -->
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

    <!-- ====== 滚动按钮（对话长时动态显示） ====== -->
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

    <!-- ====== 底部输入栏（固定） ====== -->
    <div class="input-bar">
      <a-upload
        :before-upload="handleUpload"
        :show-upload-list="false"
        multiple
      >
        <a-button class="attach-btn" type="text">
          <template #icon><PlusOutlined /></template>
        </a-button>
      </a-upload>

      <div class="input-wrapper">
        <a-textarea
          ref="textareaRef"
          v-model:value="inputText"
          :placeholder="inputPlaceholder"
          :auto-size="{ minRows: 1, maxRows: 5 }"
          @press-enter="handleEnter"
        />
        <div v-if="attachments.length > 0" class="attachment-list">
          <a-tag
            v-for="(file, idx) in attachments"
            :key="idx"
            closable
            @close="attachments.splice(idx, 1)"
          >
            📎 {{ file.name }}
          </a-tag>
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
import { ref, computed, onMounted, nextTick, watch } from 'vue';
import {
  PlusOutlined,
  SendOutlined,
  PauseOutlined,
  FileTextOutlined,
  UpOutlined,
  DownOutlined,
  RightOutlined,
  BulbOutlined,
  CheckCircleOutlined,
} from '@ant-design/icons-vue';
import { message as antMessage } from 'ant-design-vue';
import { defHttp } from '/@/utils/http/axios';
import IrPreviewCard from './chat/IrPreviewCard.vue';
import { buildSSEUrl } from '/@/utils/http/sseUrl';

// ====== Props / Emits ======
const props = withDefaults(defineProps<{
  pipelineId?: number;
  initialMessage?: string;
}>(), {
  pipelineId: 0,
  initialMessage: '',
});

const emit = defineEmits(['pipeline-complete', 'new-chat']);

// ====== 状态 ======
const currentStage = ref(1);
const messages = ref<any[]>([]);
const inputText = ref('');
const loading = ref(false);
const attachments = ref<File[]>([]);
const selectedProvider = ref('deepseek');
const providers = ref<any[]>([]);
const selectedStrategy = ref<number>(-1);
const abortController = ref<AbortController | null>(null);
const pipelineId = ref<number>(props.pipelineId || 0);
const chatStreamRef = ref<HTMLElement>();
const textareaRef = ref();

// 滚动按钮状态
const showScrollUp = ref(false);
const showScrollDown = ref(false);
const showScrollButtons = ref(false);
const autoScroll = ref(true);

// 阶段配置
const stages = ref([
  { stage: 1, name: '需求分析', status: 'active' },
  { stage: 2, name: '架构设计', status: 'pending' },
  { stage: 3, name: '总体设计', status: 'pending' },
  { stage: 4, name: '自动开发', status: 'pending' },
  { stage: 5, name: '交付验证', status: 'pending' },
]);

// 快速提示词
const quickPrompts = [
  '我需要一个进销存管理系统',
  '帮我做一个审批工作流平台',
  '设计一个设备巡检系统',
  '我想要一个客户管理 CRM',
];

// 思考中动画文字
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

let thinkingTimer: any = null;
watch(loading, (val) => {
  if (val) {
    thinkingIndex.value = 0;
    thinkingTimer = setInterval(() => { thinkingIndex.value++; }, 2500);
  } else {
    clearInterval(thinkingTimer);
  }
});

const inputPlaceholder = computed(() => {
  if (loading.value) return 'AI 正在思考中...';
  switch (currentStage.value) {
    case 1: return '描述你的业务需求，或回答 AI 的追问...';
    case 2: return '对架构方案有疑问？或确认后推进...';
    case 3: return '对详细设计有修改意见？或确认后推进...';
    default: return '输入消息...';
  }
});

// ====== 生命周期 ======
onMounted(async () => {
  await loadProviders();
  if (pipelineId.value > 0) {
    await loadPipelineState();
    if (messages.value.length === 0 && props.initialMessage) {
      await sendMessage(props.initialMessage);
    }
  }
});

// ====== 方法 ======

async function loadProviders() {
  try {
    const res = await defHttp.get({ url: '/api/studio/pipeline/providers' });
    providers.value = (res?.items ?? []).filter((p: any) => p.enabled);
    if (providers.value.length > 0) {
      selectedProvider.value = providers.value[0].providerCode;
    }
  } catch { /* 忽略 */ }
}

async function loadPipelineState() {
  if (!pipelineId.value) return;
  try {
    const res = await defHttp.get({ url: `/api/studio/pipeline/execute/${pipelineId.value}` });
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

// 发送消息（SSE 流式）
async function sendMessage(content: string) {
  if (!content.trim()) return;
  loading.value = true;
  autoScroll.value = true;

  // 添加用户消息
  messages.value.push({
    id: Date.now(),
    role: 'user',
    content,
    time: new Date().toLocaleTimeString(),
  });
  scrollToBottom();

  // 创建 Pipeline（如果还没有）
  if (!pipelineId.value || pipelineId.value === 0) {
    try {
      const res = await defHttp.post({
        url: '/api/studio/pipeline/execute/create',
        data: { requirement: content },
      });
      pipelineId.value = res.pipelineId || res.id;
    } catch (e: any) {
      antMessage.error('创建失败: ' + (e?.message || '未知错误'));
      loading.value = false;
      return;
    }
  }

  // 添加 AI 消息占位
  const aiMsgId = Date.now() + 1;
  const aiMsg: any = {
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

  try {
    abortController.value = new AbortController();

    // SSE 流式请求
    const sseUrl = buildSSEUrl(`/api/studio/pipeline/execute/${pipelineId.value}/events`);
    const response = await fetch(sseUrl, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({
        message: content,
        stage: currentStage.value,
        provider: selectedProvider.value,
      }),
      signal: abortController.value.signal,
    });

    if (!response.ok) {
      throw new Error(`HTTP ${response.status}`);
    }

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
                msg.thinking += (data.content || '');
                break;
              case 'token':
              case 'delta':
                msg.content += (data.content || data.delta?.content || '');
                if (autoScroll.value) scrollToBottom();
                break;
              case 'strategy':
                msg.strategies = data.strategies || [];
                break;
              case 'document':
                msg.document = data.document;
                break;
              case 'ir':
                msg.ir = data.ir;
                break;
              case 'stage_complete':
                msg.stageConfirmable = true;
                break;
              case 'done':
                break;
              case 'error':
                msg.content += `\n\n⚠️ ${data.content || 'AI 响应异常'}`;
                break;
            }
          } catch { /* 忽略解析错误 */ }
        }
      }
    }
  } catch (e: any) {
    if (e.name === 'AbortError') {
      const msg = messages.value.find(m => m.id === aiMsgId);
      if (msg) msg.content += '\n\n⏹️ [已停止生成]';
    } else {
      const msg = messages.value.find(m => m.id === aiMsgId);
      if (msg && !msg.content) msg.content = `⚠️ ${e.message || '发送失败，请重试'}`;
    }
  } finally {
    loading.value = false;
    abortController.value = null;
    if (autoScroll.value) scrollToBottom();
  }
}

// 用户发送
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

// 停止生成
function handleStop() {
  abortController.value?.abort();
}

// 快速提示词
function handleQuickPrompt(prompt: string) {
  inputText.value = prompt;
  nextTick(() => handleSend());
}

// 选择策略
function handleSelectStrategy(idx: number, strategy: any) {
  selectedStrategy.value = idx;
  inputText.value = `我选择：${strategy.title}。${strategy.description || ''}`;
  nextTick(() => handleSend());
}

// 确认阶段推进
async function handleConfirmStage(msg: any) {
  try {
    await defHttp.post({
      url: `/api/studio/pipeline/execute/stage/${pipelineId.value}/confirm`,
      data: { stage: currentStage.value, approved: true },
    });

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
      content: `✅ 已进入阶段 ${currentStage.value}: ${stages.value[currentStage.value - 1]?.name}`,
    });
    scrollToBottom();

    // 自动触发下一阶段
    sendMessage(`请开始阶段 ${currentStage.value}：${stages.value[currentStage.value - 1]?.name}`);
  } catch (e: any) {
    antMessage.error('确认失败: ' + (e?.message || ''));
  }
}

// 回退
async function handleRollback(targetStage: number) {
  if (targetStage < 1) return;
  currentStage.value = targetStage;
  updateStageStatus();
  messages.value.push({
    id: Date.now(),
    role: 'system',
    content: `↩️ 已回退到阶段 ${targetStage}: ${stages.value[targetStage - 1]?.name}`,
  });
  scrollToBottom();
}

// 附件上传
function handleUpload(file: File) {
  attachments.value.push(file);
  return false;
}

// 新建对话
function handleNewChat() {
  pipelineId.value = 0;
  messages.value = [];
  inputText.value = '';
  attachments.value = [];
  currentStage.value = 1;
  updateStageStatus();
  emit('new-chat');
}

// 切换模型
function handleProviderChange(code: string) {
  selectedProvider.value = code;
}

// Markdown 渲染
function renderMarkdown(text: string): string {
  if (!text) return '';
  return text
    .replace(/^### (.*$)/gm, '<h3>$1</h3>')
    .replace(/^## (.*$)/gm, '<h2>$1</h2>')
    .replace(/^# (.*$)/gm, '<h1>$1</h1>')
    .replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>')
    .replace(/\*(.*?)\*/g, '<em>$1</em>')
    .replace(/`(.*?)`/g, '<code>$1</code>')
    .replace(/^\s*[-*] (.*$)/gm, '<li>$1</li>')
    .replace(/^\s*(\d+)\. (.*$)/gm, '<li>$2</li>')
    .replace(/\n/g, '<br>');
}

// 文档操作
function previewDoc(doc: any) { window.open(doc.previewUrl, '#'); }
function downloadDoc(doc: any) {
  const a = document.createElement('a');
  a.href = doc.downloadUrl;
  a.download = doc.name;
  a.click();
}

// ====== 滚动控制 ======
function scrollToBottom() {
  nextTick(() => {
    if (chatStreamRef.value) {
      chatStreamRef.value.scrollTop = chatStreamRef.value.scrollHeight;
    }
  });
}

function scrollToTop() {
  chatStreamRef.value?.scrollTo({ top: 0, behavior: 'smooth' });
}

function handleScroll() {
  const el = chatStreamRef.value;
  if (!el) return;

  const scrollTop = el.scrollTop;
  const scrollHeight = el.scrollHeight;
  const clientHeight = el.clientHeight;

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

/* ====== 顶栏 ====== */
.top-bar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  height: 48px;
  padding: 0 16px;
  border-bottom: 1px solid #f0f0f0;
  background: #fafafa;
  flex-shrink: 0;

  .top-bar-left { display: flex; align-items: center; }
  .top-bar-center { display: flex; align-items: center; gap: 8px; }
  .top-bar-right { display: flex; align-items: center; }
  .model-select { width: 180px; }
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
    &.active .stage-dot { background: #1890ff; }
    &.completed .stage-dot { background: #52c41a; }
    .stage-name { flex: 1; font-size: 13px; }
  }
}

/* ====== 对话流（核心区域） ====== */
.chat-stream {
  flex: 1;
  overflow-y: auto;
  overflow-x: hidden;
  padding: 24px 0;
  scroll-behavior: smooth;
}

/* 欢迎界面 */
.welcome-section {
  max-width: 640px;
  margin: 0 auto;
  padding: 60px 24px 40px;
  text-align: center;

  .welcome-icon { font-size: 48px; margin-bottom: 16px; }
  h2 { font-size: 22px; margin-bottom: 12px; font-weight: 600; }
  .welcome-desc { color: #333; font-size: 15px; line-height: 1.6; margin-bottom: 8px; }
  .welcome-hint { color: #1890ff; font-size: 13px; margin-bottom: 24px; }
  .quick-prompts {
    display: flex;
    flex-wrap: wrap;
    justify-content: center;
    gap: 8px;
    .quick-prompt {
      padding: 8px 16px;
      border: 1px solid #e8e8e8;
      border-radius: 20px;
      font-size: 13px;
      color: #333;
      cursor: pointer;
      transition: all 0.2s;
      &:hover {
        border-color: #1890ff;
        color: #1890ff;
        background: #f0f7ff;
      }
    }
  }
}

/* 消息行 */
.msg-row {
  display: flex;
  gap: 12px;
  padding: 8px 24px;
  max-width: 880px;
  margin: 0 auto;
  width: 100%;
  box-sizing: border-box;
}
.user-row {
  flex-direction: row-reverse;
  .msg-bubble { max-width: 75%; }
}
.ai-row {
  .msg-bubble { max-width: 85%; }
}

/* 头像 */
.avatar {
  width: 32px;
  height: 32px;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 12px;
  font-weight: 600;
  flex-shrink: 0;
  color: #fff;
}
.user-avatar { background: #52c41a; }
.ai-avatar { background: #1890ff; }

/* 消息气泡 */
.msg-bubble {
  border-radius: 12px;
  padding: 12px 16px;
  font-size: 14px;
  line-height: 1.7;
  word-break: break-word;
}
.user-bubble {
  background: #1890ff;
  color: #fff;
  border-bottom-right-radius: 4px;
  .bubble-text { color: #fff; }
}
.ai-bubble {
  background: #f7f7f8;
  color: #333;
  border-bottom-left-radius: 4px;
}

/* 思考过程（折叠） */
.thinking-block {
  margin-bottom: 12px;
  border: 1px solid #e8e8e8;
  border-radius: 8px;
  background: #fff;
  overflow: hidden;
  .thinking-header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: 8px 12px;
    cursor: pointer;
    user-select: none;
    &:hover { background: #fafafa; }
    .thinking-label { font-size: 12px; color: #888; }
    .thinking-arrow { font-size: 10px; color: #888; }
  }
  .thinking-content {
    padding: 0 12px 8px;
    font-size: 12px;
    color: #999;
    line-height: 1.6;
    white-space: pre-wrap;
  }
}

/* 思考中动画 */
.thinking-bubble {
  background: #f0f7ff !important;
  border: 1px solid #d6e8fa;
}
.thinking-animation {
  display: flex;
  align-items: center;
  gap: 8px;
  .thinking-text { color: #1890ff; font-size: 13px; }
  .dots {
    display: flex;
    gap: 4px;
    span {
      width: 6px;
      height: 6px;
      border-radius: 50%;
      background: #1890ff;
      animation: bounce 1.4s infinite ease-in-out both;
      &:nth-child(1) { animation-delay: -0.32s; }
      &:nth-child(2) { animation-delay: -0.16s; }
    }
  }
}
@keyframes bounce {
  0%, 80%, 100% { transform: scale(0); }
  40% { transform: scale(1); }
}

/* 策略选项卡片 */
.strategy-cards {
  display: flex;
  flex-direction: column;
  gap: 8px;
  margin-top: 12px;
}
.strategy-card {
  display: flex;
  align-items: flex-start;
  gap: 12px;
  padding: 12px;
  border: 1px solid #e8e8e8;
  border-radius: 8px;
  cursor: pointer;
  transition: all 0.2s;
  background: #fff;
  &:hover {
    border-color: #1890ff;
    background: #f0f7ff;
  }
  &.selected {
    border-color: #1890ff;
    background: #e6f4ff;
  }
  .strategy-icon { font-size: 18px; flex-shrink: 0; margin-top: 2px; }
  .strategy-info {
    .strategy-title { font-weight: 600; font-size: 14px; margin-bottom: 4px; }
    .strategy-desc { font-size: 13px; color: #666; }
  }
}

/* 文档卡片 */
.doc-card {
  display: flex;
  align-items: center;
  gap: 12px;
  margin-top: 12px;
  padding: 12px;
  border: 1px solid #e8e8e8;
  border-radius: 8px;
  background: #fff;
  .doc-icon { font-size: 24px; color: #1890ff; }
  .doc-info { flex: 1; .doc-name { font-weight: 500; font-size: 14px; } .doc-size { font-size: 12px; color: #999; } }
  .doc-actions { display: flex; gap: 4px; }
}

/* 阶段确认卡片（在 AI 回复末尾） */
.stage-confirm-card {
  margin-top: 16px;
  padding: 12px 16px;
  border: 1px solid #b7eb8f;
  border-radius: 8px;
  background: #f6ffed;
  .confirm-header {
    display: flex;
    align-items: center;
    gap: 8px;
    margin-bottom: 12px;
    font-size: 14px;
    font-weight: 500;
    color: #52c41a;
  }
  .confirm-actions {
    display: flex;
    justify-content: flex-end;
    gap: 8px;
  }
}

.stage-confirmed-badge {
  margin-top: 12px;
  padding: 8px 12px;
  background: #f0f0f0;
  border-radius: 6px;
  font-size: 13px;
  color: #52c41a;
  text-align: center;
}

/* 系统消息 */
.system-msg {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 12px 48px;
  max-width: 880px;
  margin: 0 auto;
  .system-line { flex: 1; height: 1px; background: #e8e8e8; }
  .system-text { font-size: 12px; color: #999; white-space: nowrap; }
}

/* ====== 滚动按钮 ====== */
.scroll-buttons {
  position: absolute;
  right: 24px;
  bottom: 80px;
  display: flex;
  flex-direction: column;
  gap: 8px;
  z-index: 10;
  .scroll-btn {
    box-shadow: 0 2px 8px rgba(0, 0, 0, 0.15);
  }
}

/* ====== 底部输入栏 ====== */
.input-bar {
  display: flex;
  align-items: flex-end;
  gap: 8px;
  padding: 12px 24px;
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
    &:hover { border-color: #1890ff; color: #1890ff; }
  }

  .input-wrapper {
    flex: 1;
    :deep(textarea.ant-input) {
      border: none;
      box-shadow: none;
      resize: none;
      font-size: 14px;
      padding: 6px 0;
    }
    .attachment-list {
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
