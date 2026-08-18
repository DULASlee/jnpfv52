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
  import { useGateSSE, type SemanticFitnessResult } from '../composables/useGateSSE';
  import { uploadMaterials, saGate } from '/@/api/studio/ai';

  const props = defineProps<{ pipelineId: string }>();

  // ═══════════════════════════════════════
  // SSE 管理器
  // ═══════════════════════════════════════

  const { gateStatus, gateResult, gateError, connect: connectSSE, reset: resetGate, setTraceId } = useGateSSE();

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
      const result: any = await uploadMaterials(props.pipelineId, {
        files: uploadedFiles.value,
        textContent: userText.value,
      });
      const materialId = result?.materialId || result?.data?.materialId;
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
          errorCode: 'AUTH_EXPIRED',
        };
      } else if (httpStatus === 403) {
        gateError.value = {
          message: '无权执行此操作，请联系管理员。',
          errorCode: 'FORBIDDEN',
        };
      } else if (httpStatus === 413) {
        gateError.value = {
          message: '上传文件过大，请压缩后重试。',
          errorCode: 'FILE_TOO_LARGE',
        };
      } else if (httpStatus === 429) {
        gateError.value = {
          message: '请求过于频繁，请稍后重试。',
          errorCode: 'RATE_LIMITED',
        };
      } else {
        gateError.value = {
          message: error?.response?.data?.message || error?.message || '材料提交失败，请检查网络。',
          errorCode: 'API_SUBMIT_ERR',
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
          errorCode: data.errorCode || 'UNKNOWN',
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
      业务事件: 'blue',
      角色: 'green',
      数据实体: 'orange',
      字段: 'purple',
      流程: 'cyan',
      系统: 'red',
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
          show-count />

        <!-- 文件上传 -->
        <div class="upload-area" style="margin-top: 16px">
          <a-upload-dragger
            :multiple="true"
            :max-count="10"
            :before-upload="
              (file: File) => {
                uploadedFiles.push(file);
                return false;
              }
            "
            :file-list="
              uploadedFiles.map((f, i) => ({
                uid: `${i}`,
                name: f.name,
                status: 'done',
              }))
            "
            @remove="(_, index) => uploadedFiles.splice(index, 1)">
            <p class="ant-upload-drag-icon">📄</p>
            <p class="ant-upload-text">点击或拖拽文件到此区域上传</p>
            <p class="ant-upload-hint"> 支持 Word / Excel / PDF / TXT / 图片（截图），最多10个文件，单文件≤20MB </p>
          </a-upload-dragger>
        </div>

        <!-- 提交按钮 -->
        <a-button
          type="primary"
          size="large"
          block
          style="margin-top: 24px"
          :disabled="!userText.trim() && uploadedFiles.length === 0"
          :loading="false"
          @click="submitMaterials">
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
          <p
            >评估评分：<strong>{{ gateResult?.score ?? '-' }}/100</strong></p
          >

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

          <p style="margin-top: 16px; color: #52c41a"> 材料评估通过，点击下方按钮进入需求分析阶段。 </p>
        </template>

        <template #extra>
          <a-button type="primary" size="large" @click="enterStage1"> 进入需求分析 → </a-button>
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
            <a-tag :color="(gateResult?.score ?? 0) >= 50 ? 'orange' : 'red'" style="margin-left: 8px">
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
                  <span v-if="item.evidence" class="evidence"> （依据：{{ item.evidence }}） </span>
                </a-list-item>
              </template>
            </a-list>
          </div>

          <!-- ❌ 缺失要素（关键反馈——每项都有具体HowToFix） -->
          <div v-if="gateResult?.missing?.length" class="gate-missing">
            <h4>❌ 需要补充的关键要素</h4>
            <div v-for="(item, index) in gateResult.missing" :key="index" style="margin-bottom: 12px">
              <a-alert :type="item.severity === 'critical' ? 'error' : 'warning'" show-icon>
                <template #message>
                  <strong>{{ item.category }}</strong
                  >：{{ item.description }}
                </template>
                <template #description>
                  <div class="how-to-fix">
                    <strong>📌 如何补充：</strong>
                    <!-- ⚠️ 安全红线：必须使用 {{ }} 文本插值，禁止改为 v-html -->
                    <!-- howToFix 由 LLM 生成，存在 HTML 注入风险 -->
                    <p style="margin-top: 4px; white-space: pre-wrap">{{ item.howToFix }}</p>
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
                <div style="white-space: pre-wrap">{{ gateResult.nextStepGuidance }}</div>
              </template>
            </a-alert>
          </div>
        </template>

        <template #extra>
          <a-space>
            <a-button type="primary" @click="resetAndResubmit"> 补充材料后重新提交 </a-button>
            <a-button @click="viewOriginalMaterial"> 查看已提交的材料 </a-button>
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
          <p v-if="gateError?.errorCode" class="error-code"> 错误代码：{{ gateError.errorCode }} </p>
        </template>
        <template #extra>
          <a-space>
            <a-button type="primary" @click="retryGate"> 重试 </a-button>
            <a-button @click="resetAndResubmit"> 重新提交材料 </a-button>
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
