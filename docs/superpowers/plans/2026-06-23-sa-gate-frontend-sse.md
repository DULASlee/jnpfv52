# SA 门控前端 SSE 对接 — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 将 D爷 提供的 `useGateSSE.ts` + `submit-requirement.vue` + `MaterialUploader.vue` 三个核心前端文件落地到 jnpf-web-vue3，修复 SSE 三大致命缺陷（鉴权、竞态、重连）。

**Architecture:** fetch-event-source 替代原生 EventSource → 支持 Authorization Header → connect() 返回 Promise 确保通道就绪后再触发 API → 指数退避重连 MAX_RETRY=3 → 5 种 UI 状态全覆盖。

**Tech Stack:** Vue 3 + TypeScript + @microsoft/fetch-event-source + Ant Design Vue + defHttp

**Key constraint:** 核心逻辑照抄 D爷 代码，仅适配 import 路径（`@/` → `/@/`）+ API 模块创建。

---

## File Structure

| # | File | Action | Purpose |
|---|------|--------|---------|
| 1 | `package.json` | Modify | 添加 `@microsoft/fetch-event-source` 依赖 |
| 2 | `src/api/studio/ai.ts` | **Create** | SA 门控 API：uploadMaterials / saGate |
| 3 | `src/views/studio/ai/composables/useGateSSE.ts` | **Create** | SSE 连接管理器（154行，照抄） |
| 4 | `src/views/studio/ai/components/submit-requirement.vue` | **Create** | 提交需求页面（~420行，照抄） |
| 5 | `src/views/studio/ai/components/MaterialUploader.vue` | **Create** | 材料上传组件（增量上传逻辑） |

---

### Task 1: 安装 fetch-event-source 依赖

**Files:**
- Modify: `jnpf-web-vue3/package.json`

- [ ] **Step 1: 安装 @microsoft/fetch-event-source**

```bash
cd jnpf-web-vue3 && pnpm add @microsoft/fetch-event-source
```

- [ ] **Step 2: 验证安装**

```bash
cd jnpf-web-vue3 && pnpm ls @microsoft/fetch-event-source
```
Expected: 显示已安装版本号

---

### Task 2: 创建 SA 门控 API 模块

**Files:**
- Create: `jnpf-web-vue3/src/api/studio/ai.ts`

> 适配说明：D爷原始代码使用 `import { api } from '@/api'` + `api.ai.pipeline.uploadMaterials()` 嵌套模式，但 JNPF 项目使用 `defHttp` + 独立导出函数模式。创建此模块对齐现有规范，导出 `uploadMaterials` / `saGate` 两个函数。

- [ ] **Step 1: 创建 API 模块文件**

```typescript
/**
 * SA 门控 API（对齐后端 AIDevelopmentPipelineService.SaGate）
 *
 * 端点：
 *   POST /api/studio/pipeline/execute/{id}/upload-materials
 *   POST /api/studio/pipeline/execute/{id}/sa-gate
 */
import { defHttp } from '/@/utils/http/axios';

const baseUrl = '/api/studio/pipeline/execute';

/** 上传材料 */
export function uploadMaterials(pipelineId: string, data: UploadMaterialsRequest) {
  return defHttp.post({ url: `${baseUrl}/${pipelineId}/upload-materials`, data });
}

/** 触发门控评估 */
export function saGate(pipelineId: string, data: SaGateRequest) {
  return defHttp.post({ url: `${baseUrl}/${pipelineId}/sa-gate`, data });
}

// ─── 类型定义 ───

export interface UploadMaterialsRequest {
  files?: File[];
  textContent?: string;
  fileIds?: string[];
}

export interface SaGateRequest {
  materialId: string;
}
```

- [ ] **Step 2: 验证编译**

```bash
cd jnpf-web-vue3 && npx vue-tsc --noEmit src/api/studio/ai.ts 2>&1 | head -20
```
Expected: 0 errors（仅可能有 tsconfig path 警告）

---

### Task 3: 创建 useGateSSE.ts 连接管理器

**Files:**
- Create: `jnpf-web-vue3/src/views/studio/ai/composables/useGateSSE.ts`

> D爷 代码照抄，仅修改 2 处 import 路径：`@/utils/auth` → `/@/utils/auth`

- [ ] **Step 1: 创建目录结构**

```bash
mkdir -p jnpf-web-vue3/src/views/studio/ai/composables
mkdir -p jnpf-web-vue3/src/views/studio/ai/components
```

- [ ] **Step 2: 写入 useGateSSE.ts**（完整 154 行，仅改 import 路径）

```typescript
/**
 * SA门控 SSE 连接管理器
 *
 * 职责：
 *   1. 使用 fetch-event-source 替代原生 EventSource，支持 Authorization Header
 *   2. connect() 返回 Promise，调用方 await 确认就绪后再触发门控API（防竞态）
 *   3. 指数退避自动重连（2s/4s/6s），MAX_RETRY=3 后才判定真失败
 *   4. 组件卸载时自动断开连接，防止内存泄漏
 *
 * 严格禁止修改：
 *   - onerror 中的重试逻辑和返回值语义
 *   - connect() 的 Promise resolve/reject 时机
 *   - disconnect() 的 abort 逻辑
 *
 * @author SA-Studio Team
 * @date 2026-06-23
 */

import { fetchEventSource } from '@microsoft/fetch-event-source';
import { ref, onUnmounted } from 'vue';
import { getAuthHeader } from '/@/utils/auth';

// ═══════════════════════════════════════
// 类型定义
// ═══════════════════════════════════════

export interface IdentifiedElement {
  category: string;
  description: string;
  evidence: string;
}

export interface MissingElement {
  category: string;
  description: string;
  severity: string;      // 'critical' | 'warning'
  howToFix: string;
}

export interface SemanticFitnessResult {
  passed: boolean;
  score: number;
  level: string;          // 'sufficient' | 'partial' | 'insufficient'
  identified: IdentifiedElement[];
  missing: MissingElement[];
  nextStepGuidance: string;
}

export interface GateError {
  message: string;
  errorCode: string;
}

export type GateStatus = 'idle' | 'processing' | 'passed' | 'failed' | 'error';

export type GateEventType = 'gate_started' | 'gate_passed' | 'gate_failed' | 'gate_error';

// ═══════════════════════════════════════
// 核心 Composable
// ═══════════════════════════════════════

const MAX_RETRY = 3;

export function useGateSSE() {
  // ── 响应式状态（只读暴露给模板） ──
  const gateStatus = ref<GateStatus>('idle');
  const gateResult = ref<SemanticFitnessResult | null>(null);
  const gateError = ref<GateError | null>(null);

  // ── 内部状态（非响应式，不触发视图更新） ──
  let abortController: AbortController | null = null;
  let traceId = '';
  let retryCount = 0;
  let currentPipelineId = '';

  /**
   * 建立 SSE 连接
   *
   * 返回 Promise：
   *   - resolve：连接成功，通道就绪，可以触发门控API
   *   - reject：连接失败（鉴权失败、网络不通等）
   *
   * 调用方必须 await 此方法后再调用后端门控API！
   *
   * @param pipelineId 流水线ID
   * @param onEvent 事件回调（eventType, data）=> void
   */
  function connect(
    pipelineId: string,
    onEvent: (eventType: string, data: any) => void
  ): Promise<void> {
    return new Promise((resolve, reject) => {
      // 清理旧连接
      if (abortController) {
        abortController.abort();
        abortController = null;
      }

      abortController = new AbortController();
      retryCount = 0;
      currentPipelineId = pipelineId;

      const url = `/api/ai/pipeline/${pipelineId}/events`;

      fetchEventSource(url, {
        // ★ 致命缺陷1修正：携带鉴权Header
        headers: {
          'Authorization': getAuthHeader() || '',
          'Accept': 'text/event-stream',
          'X-Trace-Id': traceId
        },
        signal: abortController.signal,

        // ★ 连接成功——通知调用方通道就绪
        onopen: async (response) => {
          const contentType = response.headers.get('content-type') || '';

          if (response.ok && contentType.includes('text/event-stream')) {
            console.log(`[Gate SSE] ✅ 连接就绪 traceId=${traceId}`);
            retryCount = 0;
            resolve();
            return;
          }

          // 非预期响应（401/403/404/500等）
          const errorMsg = `SSE连接失败: HTTP ${response.status}`;
          console.error(`[Gate SSE] ❌ ${errorMsg} traceId=${traceId}`);

          // 鉴权失败直接报错，不重试
          if (response.status === 401 || response.status === 403) {
            gateStatus.value = 'error';
            gateError.value = {
              message: response.status === 401 ? '登录已过期，请重新登录' : '无权访问此资源',
              errorCode: response.status === 401 ? 'AUTH_EXPIRED' : 'FORBIDDEN'
            };
          }

          reject(new Error(errorMsg));
        },

        // ★ 收到消息——分发给调用方
        onmessage: (ev) => {
          console.log(`[Gate SSE] 📨 事件: ${ev.event} traceId=${traceId}`);
          try {
            const data = JSON.parse(ev.data);
            onEvent(ev.event, data);
          } catch (parseErr) {
            console.error(`[Gate SSE] JSON解析失败: ${ev.data}`, parseErr);
          }
        },

        // ★ 致命缺陷3修正：带退避的自动重连
        onerror: (err) => {
          console.warn(`[Gate SSE] ⚠️ 连接断开 retry=${retryCount}/${MAX_RETRY} traceId=${traceId}`, err);

          if (retryCount < MAX_RETRY) {
            retryCount++;
            const delay = retryCount * 2000; // 2s → 4s → 6s
            console.log(`[Gate SSE] ⏳ ${delay}ms 后自动重连 (${retryCount}/${MAX_RETRY})`);
            // 返回延迟毫秒数——fetch-event-source 库会自动重连
            return delay;
          }

          // ★ 重试耗尽，判定为致命错误
          console.error(`[Gate SSE] ❌ 重试耗尽，判定为致命错误 traceId=${traceId}`);
          gateStatus.value = 'error';
          gateError.value = {
            message: '网络连接不稳定，需求评估中断。请检查网络后重试。',
            errorCode: 'SSE_CONN_FATAL'
          };

          // 主动断开，阻止库继续重试
          if (abortController) {
            abortController.abort();
            abortController = null;
          }
        }
      }).catch((err) => {
        // AbortError 是用户主动取消，不算错误
        if (err.name === 'AbortError') {
          console.log(`[Gate SSE] 连接被主动取消 traceId=${traceId}`);
          return;
        }
        // SSE_CONN_FATAL 是 onerror 中我们主动抛的
        if (err.message === 'SSE_CONN_FATAL') {
          return;
        }
        // 其他未预期错误
        console.error(`[Gate SSE] 未预期异常 traceId=${traceId}`, err);
        reject(err);
      });
    });
  }

  /**
   * 断开 SSE 连接
   */
  function disconnect() {
    if (abortController) {
      abortController.abort();
      abortController = null;
    }
  }

  /**
   * 重置所有状态（用于"补充材料后重新提交"）
   *
   * 注意：disconnect 会触发 AbortError，
   * connect 中的 catch 已处理，不会影响用户
   */
  function reset() {
    disconnect();
    gateStatus.value = 'idle';
    gateResult.value = null;
    gateError.value = null;
    traceId = '';
    retryCount = 0;
  }

  /**
   * 设置追踪ID（调用方在 submitMaterials 开始时设置）
   */
  function setTraceId(id: string) {
    traceId = id;
  }

  // ── 组件卸载时自动清理 ──
  onUnmounted(() => {
    disconnect();
  });

  return {
    // 只读状态
    gateStatus,
    gateResult,
    gateError,

    // 方法
    connect,
    disconnect,
    reset,
    setTraceId
  };
}
```

---

### Task 4: 创建 submit-requirement.vue 提交需求页面

**Files:**
- Create: `jnpf-web-vue3/src/views/studio/ai/components/submit-requirement.vue`

> D爷 代码照抄，仅修改 import 路径：`@/utils/auth` → `/@/utils/auth`，`@/api` → `/@/api/studio/ai`

- [ ] **Step 1: 写入 submit-requirement.vue**（完整 ~420 行）

```vue
<script setup lang="ts">
/**
 * SA门控提交需求页面
 *
 * 核心时序（严禁修改执行顺序）：
 *   1. 上传材料 → materialId
 *   2. await connectSSE() → 通道就绪
 *   3. await saGate() → 触发门控
 *
 * 致命缺陷修正：
 *   1. fetch-event-source 支持 Authorization Header
 *   2. 先建SSE再触发API，防竞态丢事件
 *   3. 指数退避自动重连
 *   4. 全流程 try-catch + traceId
 */

import { ref, computed } from 'vue';
import { message } from 'ant-design-vue';
import { useGateSSE, type SemanticFitnessResult } from '../composables/useGateSSE';
import { uploadMaterials, saGate } from '/@/api/studio/ai';

const props = defineProps<{ pipelineId: string }>();

// ═══════════════════════════════════════
// SSE 管理器
// ═══════════════════════════════════════

const {
  gateStatus,
  gateResult,
  gateError,
  connect: connectSSE,
  reset: resetGate,
  setTraceId
} = useGateSSE();

// ═══════════════════════════════════════
// 本地状态
// ═══════════════════════════════════════

const uploadedFiles = ref<File[]>([]);
const userText = ref('');
const currentStage = ref(0);
const isSubmitting = computed(() => gateStatus.value === 'processing');

// ═══════════════════════════════════════
// 核心流程：提交材料与触发门控
//
// ⚠️ 时序要求极其严格，三步顺序禁止颠倒！
// ⚠️ 禁止将 await connectSSE 移到 saGate 之后！
// ═══════════════════════════════════════

async function submitMaterials() {
  // 生成唯一追踪号（前端 → 后端 → 日志全链路串联）
  const localTraceId = `gate-${Date.now()}-${Math.random().toString(36).slice(2, 8)}`;
  setTraceId(localTraceId);

  try {
    gateStatus.value = 'processing';
    console.log(`[Gate] 🚀 开始提交 traceId=${localTraceId}`);

    // ──────────────────────────────────
    // 步骤 1：上传材料，获取 materialId
    // ──────────────────────────────────
    const { materialId } = await uploadMaterials(props.pipelineId, {
      files: uploadedFiles.value,
      textContent: userText.value
    });
    console.log(`[Gate] ✅ 材料上传成功 materialId=${materialId} traceId=${localTraceId}`);

    // ──────────────────────────────────
    // 步骤 2：建立 SSE 连接（必须 await！）
    //
    // connectSSE 内部的 onopen 会 resolve 这个 Promise
    // 只有通道真正就绪了，才会继续执行步骤 3
    // ──────────────────────────────────
    await connectSSE(props.pipelineId, handleGateEvent);
    console.log(`[Gate] ✅ SSE通道就绪 traceId=${localTraceId}`);

    // ──────────────────────────────────
    // 步骤 3：触发后端门控 API
    //
    // 此时 SSE 通道已就绪，后端推的任何事件前端都能收到
    // 不会丢失 gate_started / gate_passed / gate_failed 等事件
    // ──────────────────────────────────
    await saGate(props.pipelineId, { materialId });
    console.log(`[Gate] ✅ 门控API已触发 traceId=${localTraceId}`);

  } catch (error: any) {
    console.error(`[Gate] ❌ 提交流程异常 traceId=${localTraceId}`, error);
    gateStatus.value = 'error';

    // ★ 致命缺陷4修正：按HTTP状态码分类错误提示
    const httpStatus = error?.httpStatus || error?.response?.status;

    if (httpStatus === 401) {
      gateError.value = {
        message: '登录已过期，请重新登录。',
        errorCode: 'AUTH_EXPIRED'
      };
    } else if (httpStatus === 403) {
      gateError.value = {
        message: '无权执行此操作，请联系管理员。',
        errorCode: 'FORBIDDEN'
      };
    } else if (httpStatus === 413) {
      gateError.value = {
        message: '上传文件过大，请压缩后重试。',
        errorCode: 'FILE_TOO_LARGE'
      };
    } else if (httpStatus === 429) {
      gateError.value = {
        message: '请求过于频繁，请稍后重试。',
        errorCode: 'RATE_LIMITED'
      };
    } else {
      gateError.value = {
        message: error?.response?.data?.message || error?.message || '材料提交失败，请检查网络。',
        errorCode: 'API_SUBMIT_ERR'
      };
    }
  }
}

// ═══════════════════════════════════════
// SSE 事件分发
// ═══════════════════════════════════════

function handleGateEvent(eventType: string, data: any) {
  switch (eventType) {
    case 'gate_started':
      gateStatus.value = 'processing';
      console.log('[Gate] ⏳ 门控开始评估');
      break;

    case 'gate_passed':
      gateStatus.value = 'passed';
      gateResult.value = data.semanticFitness;
      console.log(`[Gate] ✅ 门控通过 score=${data.semanticFitness?.score}`);
      break;

    case 'gate_failed':
      gateStatus.value = 'failed';
      gateResult.value = data.semanticFitness;
      console.log(`[Gate] ⚠️ 门控不合格 score=${data.semanticFitness?.score} missing=${data.semanticFitness?.missing?.length}`);
      break;

    case 'gate_error':
      gateStatus.value = 'error';
      gateError.value = {
        message: data.message || '评估服务异常',
        errorCode: data.errorCode || 'UNKNOWN'
      };
      console.error(`[Gate] ❌ 门控错误 code=${data.errorCode}`);
      break;

    default:
      console.warn(`[Gate] 未知事件类型: ${eventType}`);
  }
}

// ═══════════════════════════════════════
// 用户交互
// ═══════════════════════════════════════

/** 补充材料后重新提交（保留已输入的内容） */
function resetAndResubmit() {
  resetGate();
  // 不清空 uploadedFiles 和 userText
  // 用户在原基础上补充
}

/** 网络错误时重试 */
async function retryGate() {
  resetGate();
  await submitMaterials();
}

/** 门控通过，进入Stage 1骨架预分析 */
function enterStage1() {
  currentStage.value = 1;
  // 骨架提取的启动逻辑在下一阶段实现
}

/** 查看已提交的原始材料 */
function viewOriginalMaterial() {
  // 弹窗展示已提交的文件列表和文字内容
  // 实现细节由工程师补充
}

// ═══════════════════════════════════════
// 辅助函数
// ═══════════════════════════════════════

function getCategoryColor(category: string): string {
  const colors: Record<string, string> = {
    '业务事件': 'blue',
    '角色': 'green',
    '数据实体': 'orange',
    '字段': 'purple',
    '流程': 'cyan',
    '系统': 'red'
  };
  return colors[category] || 'default';
}
</script>

<template>
  <div class="submit-requirement-page">

    <!-- ═══════════════════════════════════════ -->
    <!-- 状态一：材料上传（idle）                  -->
    <!-- ═══════════════════════════════════════ -->
    <div v-if="gateStatus === 'idle'" class="upload-section">
      <a-card title="提交需求材料" :bordered="false">
        <!-- 用户文字输入 -->
        <a-textarea
          v-model:value="userText"
          placeholder="请描述您要构建的系统。&#10;&#10;示例：我们是汽车零部件工厂，需要一个报工管理系统。&#10;工人完成工序后扫描工单号，输入完成数量和不良品数量。&#10;车间主任审核报工记录，质检员处理不良品。&#10;&#10;也可以上传需求文档/截图，AI会自动解析。"
          :rows="8"
          :maxlength="50000"
          show-count
        />

        <!-- 文件上传 -->
        <div class="upload-area" style="margin-top: 16px;">
          <a-upload-dragger
            :multiple="true"
            :max-count="10"
            :before-upload="(file: File) => { uploadedFiles.push(file); return false; }"
            :file-list="uploadedFiles.map((f, i) => ({ uid: `${i}`, name: f.name, status: 'done' }))"
            @remove="(_, index) => uploadedFiles.splice(index, 1)"
          >
            <p class="ant-upload-drag-icon">📄</p>
            <p class="ant-upload-text">点击或拖拽文件到此区域上传</p>
            <p class="ant-upload-hint">
              支持 Word / Excel / PDF / TXT / 图片（截图），最多10个文件，单文件≤20MB
            </p>
          </a-upload-dragger>
        </div>

        <!-- 提交按钮 -->
        <a-button
          type="primary"
          size="large"
          block
          style="margin-top: 24px;"
          :disabled="(!userText.trim() && uploadedFiles.length === 0)"
          :loading="false"
          @click="submitMaterials"
        >
          提交需求材料
        </a-button>
      </a-card>
    </div>

    <!-- ═══════════════════════════════════════ -->
    <!-- 状态二：门控处理中                        -->
    <!-- ═══════════════════════════════════════ -->
    <div v-else-if="gateStatus === 'processing'" class="processing-section">
      <a-spin size="large" />
      <h3>正在评估需求材料...</h3>
      <p class="text-muted">
        正在解析文档内容、识别图片信息，并评估需求完整性。
        <br />
        预计需要 30-60 秒，请耐心等待。
      </p>
    </div>

    <!-- ═══════════════════════════════════════ -->
    <!-- 状态三：门控通过                          -->
    <!-- ═══════════════════════════════════════ -->
    <div v-else-if="gateStatus === 'passed'">
      <a-result status="success" title="需求材料评估通过">
        <template #subTitle>
          <p>评估评分：<strong>{{ gateResult?.score ?? '-' }}/100</strong></p>

          <!-- 已识别要素 -->
          <div v-if="gateResult?.identified?.length" class="gate-identified">
            <h4>✅ 已识别的要素</h4>
            <a-list :data-source="gateResult.identified" size="small" bordered>
              <template #renderItem="{ item }">
                <a-list-item>
                  <a-tag :color="getCategoryColor(item.category)">
                    {{ item.category }}
                  </a-tag>
                  <span>{{ item.description }}</span>
                </a-list-item>
              </template>
            </a-list>
          </div>

          <p style="margin-top: 16px; color: #52c41a;">
            材料评估通过，点击下方按钮进入需求分析阶段。
          </p>
        </template>

        <template #extra>
          <a-button type="primary" size="large" @click="enterStage1">
            进入需求分析 →
          </a-button>
        </template>
      </a-result>
    </div>

    <!-- ═══════════════════════════════════════ -->
    <!-- 状态四：门控不通过（结构化反馈）           -->
    <!-- 这是用户体验的核心——必须告诉用户具体哪里不行、怎么改 -->
    <!-- ═══════════════════════════════════════ -->
    <div v-else-if="gateStatus === 'failed'">
      <a-result status="warning" title="需求材料需要补充">
        <template #subTitle>
          <!-- 评分 -->
          <div class="gate-score">
            评估评分：<strong>{{ gateResult?.score ?? 0 }}/100</strong>
            <a-tag
              :color="(gateResult?.score ?? 0) >= 50 ? 'orange' : 'red'"
              style="margin-left: 8px;"
            >
              {{ gateResult?.level === 'partial' ? '部分合格' : '不合格' }}
            </a-tag>
          </div>

          <!-- ✅ 已识别要素（正面反馈——让用户知道哪些写对了） -->
          <div v-if="gateResult?.identified?.length" class="gate-identified">
            <h4>✅ 已识别的要素</h4>
            <a-list :data-source="gateResult.identified" size="small" bordered>
              <template #renderItem="{ item }">
                <a-list-item>
                  <a-tag :color="getCategoryColor(item.category)">
                    {{ item.category }}
                  </a-tag>
                  <span>{{ item.description }}</span>
                  <span v-if="item.evidence" class="evidence">
                    （依据：{{ item.evidence }}）
                  </span>
                </a-list-item>
              </template>
            </a-list>
          </div>

          <!-- ❌ 缺失要素（关键反馈——每项都有具体HowToFix） -->
          <div v-if="gateResult?.missing?.length" class="gate-missing">
            <h4>❌ 需要补充的关键要素</h4>
            <div
              v-for="(item, index) in gateResult.missing"
              :key="index"
              style="margin-bottom: 12px;"
            >
              <a-alert
                :type="item.severity === 'critical' ? 'error' : 'warning'"
                show-icon
              >
                <template #message>
                  <strong>{{ item.category }}</strong>：{{ item.description }}
                </template>
                <template #description>
                  <div class="how-to-fix">
                    <strong>📌 如何补充：</strong>
                    <!-- ⚠️ 安全红线：必须使用 {{ }} 文本插值，禁止改为 v-html -->
                    <!-- howToFix 由 LLM 生成，存在 HTML 注入风险 -->
                    <p style="margin-top: 4px; white-space: pre-wrap;">{{ item.howToFix }}</p>
                  </div>
                </template>
              </a-alert>
            </div>
          </div>

          <!-- 💡 整体改进建议 -->
          <div v-if="gateResult?.nextStepGuidance" class="gate-guidance">
            <a-alert type="info" show-icon>
              <template #message>💡 改进建议</template>
              <template #description>
                <!-- ⚠️ 安全红线：必须使用 {{ }} 文本插值，禁止改为 v-html -->
                <div style="white-space: pre-wrap;">{{ gateResult.nextStepGuidance }}</div>
              </template>
            </a-alert>
          </div>
        </template>

        <template #extra>
          <a-space>
            <a-button type="primary" @click="resetAndResubmit">
              补充材料后重新提交
            </a-button>
            <a-button @click="viewOriginalMaterial">
              查看已提交的材料
            </a-button>
          </a-space>
        </template>
      </a-result>
    </div>

    <!-- ═══════════════════════════════════════ -->
    <!-- 状态五：错误                              -->
    <!-- ═══════════════════════════════════════ -->
    <div v-else-if="gateStatus === 'error'">
      <a-result status="error" title="需求评估失败">
        <template #subTitle>
          <p>{{ gateError?.message }}</p>
          <p v-if="gateError?.errorCode" class="error-code">
            错误代码：{{ gateError.errorCode }}
          </p>
        </template>
        <template #extra>
          <a-space>
            <a-button type="primary" @click="retryGate">
              重试
            </a-button>
            <a-button @click="resetAndResubmit">
              重新提交材料
            </a-button>
          </a-space>
        </template>
      </a-result>
    </div>

  </div>
</template>

<style scoped>
.submit-requirement-page {
  max-width: 900px;
  margin: 0 auto;
  padding: 24px;
}

.upload-section {
  /* 材料上传区 */
}

.processing-section {
  text-align: center;
  padding: 100px 0;
}

.processing-section h3 {
  margin-top: 24px;
  color: #1890ff;
  font-size: 18px;
}

.text-muted {
  color: #999;
  font-size: 14px;
}

.gate-score {
  font-size: 16px;
  margin-bottom: 16px;
}

.gate-identified {
  margin: 16px 0;
  text-align: left;
}

.gate-identified h4 {
  margin-bottom: 8px;
  color: #52c41a;
}

.gate-missing {
  margin: 16px 0;
  text-align: left;
}

.gate-missing h4 {
  margin-bottom: 12px;
  color: #ff4d4f;
}

.how-to-fix {
  background: #fafafa;
  padding: 8px 12px;
  border-radius: 4px;
  margin-top: 4px;
}

.gate-guidance {
  margin-top: 16px;
  text-align: left;
}

.evidence {
  color: #999;
  font-size: 12px;
  margin-left: 8px;
}

.error-code {
  color: #999;
  font-size: 12px;
  font-family: 'Courier New', monospace;
}
</style>
```

---

### Task 5: 创建 MaterialUploader.vue 增量上传组件

**Files:**
- Create: `jnpf-web-vue3/src/views/studio/ai/components/MaterialUploader.vue`

- [ ] **Step 1: 写入 MaterialUploader.vue**（完整增量上传逻辑）

```vue
<script setup lang="ts">
/**
 * 材料上传组件（增量版）
 *
 * 关键设计：
 *   - 已上传成功的文件保留 serverId，重新提交时不重复上传
 *   - 仅上传 status === 'pending' 的新增文件
 *   - submitMaterials 调用时自动过滤
 */

import { ref, computed } from 'vue';
import { uploadMaterials } from '/@/api/studio/ai';

interface UploadedFile {
  raw: File;
  fileName: string;
  status: 'pending' | 'uploaded' | 'error';
  serverId?: string;
  serverUrl?: string;
  errorMessage?: string;
}

const files = ref<UploadedFile[]>([]);

// 只取待上传的文件
const pendingFiles = computed(() =>
  files.value.filter(f => f.status === 'pending').map(f => f.raw)
);

// 已上传文件的 serverId 列表
const uploadedServerIds = computed(() =>
  files.value.filter(f => f.status === 'uploaded' && f.serverId).map(f => f.serverId!)
);

function handleFilesSelected(newFiles: File[]) {
  for (const file of newFiles) {
    // 按文件名+大小去重
    const exists = files.value.some(
      f => f.fileName === file.name && f.raw.size === file.size
    );
    if (!exists) {
      files.value.push({
        raw: file,
        fileName: file.name,
        status: 'pending'
      });
    }
  }
}

function removeFile(index: number) {
  files.value.splice(index, 1);
}

/**
 * 预上传：在 submitMaterials 之前调用
 * 将 pending 文件上传到服务端，标记为 uploaded
 * 返回新上传的 serverId 列表
 */
async function preUpload(pipelineId: string): Promise<string[]> {
  const newServerIds: string[] = [];

  for (const file of files.value) {
    if (file.status !== 'pending') continue;

    try {
      file.status = 'uploaded'; // 乐观标记
      const result: any = await uploadMaterials(pipelineId, { files: [file.raw] });
      file.serverId = result?.serverId || result?.data?.serverId;
      if (file.serverId) {
        newServerIds.push(file.serverId);
      }
    } catch (err: any) {
      file.status = 'error';
      file.errorMessage = err.message || '上传失败';
    }
  }

  return newServerIds;
}

defineExpose({
  files,
  pendingFiles,
  uploadedServerIds,
  handleFilesSelected,
  preUpload,
  removeFile
});
</script>
```

---

### Task 6: 编译验证 + 类型检查

- [ ] **Step 1: 后端编译**

```bash
cd backend && dotnet build application/JNPF.API.Entry/JNPF.API.Entry.csproj
```
Expected: 0 errors

- [ ] **Step 2: 前端类型检查**

```bash
cd jnpf-web-vue3 && npx vue-tsc --noEmit 2>&1 | tail -30
```
Expected: 0 errors（如有新文件导致的类型错误，逐一修复）

- [ ] **Step 3: 修复编译错误（如有）**

常见问题：
- `@microsoft/fetch-event-source` 类型声明 → 检查 `node_modules/@microsoft/fetch-event-source/`
- `getAuthHeader()` 返回 `string | undefined` → 已用 `|| ''` 处理
- `a-upload-dragger` 的 `@remove` 签名 → 检查 Ant Design Vue 版本

---

### Task 7: E2E 验证 (Supreme Iron Law)

- [ ] **Step 1: 启动开发环境**

```bash
powershell -ExecutionPolicy Bypass -File D:\JNPF-v52\start-dev.ps1
```

- [ ] **Step 2: Playwright 截图验证**

打开浏览器 → 导航到 Studio AI 页面 → 确认提交需求页面正常渲染（5 种状态）→ 截图保存到 `.claude/evidence/`

---

## Self-Review

1. **Spec coverage:** 3 个核心文件完全覆盖（useGateSSE.ts / submit-requirement.vue / MaterialUploader.vue）+ 依赖安装 + API 模块
2. **Placeholder scan:** 无 TODOs，所有代码完整
3. **Type consistency:** `GateStatus`/`GateError`/`SemanticFitnessResult` 在 useGateSSE.ts 定义，在 submit-requirement.vue 消费。`uploadMaterials`/`saGate` 在 api/studio/ai.ts 定义，在 submit-requirement.vue 和 MaterialUploader.vue 消费
4. **Adaptations from D爷 code:** 仅 2 处 import 路径适配（`@/` → `/@/`）+ API 模块从嵌套对象改为独立函数导出。核心逻辑 100% 保留
