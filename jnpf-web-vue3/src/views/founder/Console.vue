<template>
  <div class="jnpf-content-wrapper">
    <div class="jnpf-content-wrapper-center">
      <div class="jnpf-content-wrapper-content">
        <!-- TOTP Auth Section -->
        <a-card :title="t('founder.auth.title')" class="console-section">
          <template v-if="!isAuthenticated">
            <a-space direction="vertical" :size="16" style="width: 100%">
              <a-input v-model:value="totpEmail" placeholder="输入创始人邮箱" />
              <a-button type="primary" :loading="setupLoading" @click="handleSetupTotp"> 获取 TOTP 密钥和二维码 </a-button>
              <div v-if="totpSecret" class="totp-info">
                <p
                  >密钥: <a-typography-text copyable>{{ totpSecret }}</a-typography-text></p
                >
                <p v-if="qrCodeUrl"
                  >二维码 URL: <a-typography-text copyable>{{ qrCodeUrl }}</a-typography-text></p
                >
                <a-input-number v-model:value="totpCode" :min="0" :max="999999" placeholder="输入 6 位验证码" style="width: 200px" />
                <a-button type="primary" :loading="verifyLoading" @click="handleVerifyTotp" style="margin-left: 8px"> 验证并登录 </a-button>
              </div>
            </a-space>
          </template>
          <template v-else>
            <a-result status="success" title="已认证" sub-title="Founder 控制台全部功能已解锁" />
          </template>
        </a-card>

        <a-row :gutter="16" class="console-row">
          <!-- AI Model Config -->
          <a-col :span="12">
            <a-card :title="t('founder.model.title')" class="console-card">
              <a-form layout="vertical">
                <a-form-item label="主模型">
                  <a-input v-model:value="modelConfig.primaryModel" />
                </a-form-item>
                <a-form-item label="备用模型">
                  <a-input v-model:value="modelConfig.fallbackModel" />
                </a-form-item>
                <a-form-item label="Temperature">
                  <a-slider v-model:value="modelConfig.temperature" :min="0" :max="2" :step="0.1" />
                </a-form-item>
                <a-form-item label="Max Tokens">
                  <a-input-number v-model:value="modelConfig.maxTokens" :min="256" :max="32768" style="width: 100%" />
                </a-form-item>
                <a-button type="primary" :loading="modelSaving" @click="handleSaveModel"> 保存模型配置 </a-button>
              </a-form>
            </a-card>
          </a-col>

          <!-- Self-Play Status -->
          <a-col :span="12">
            <a-card :title="t('founder.selfplay.title')" class="console-card">
              <a-descriptions :column="1" bordered size="small">
                <a-descriptions-item label="状态">
                  <a-tag :color="selfPlayStatus.enabled ? 'green' : 'default'">
                    {{ selfPlayStatus.enabled ? '运行中' : '已暂停' }}
                  </a-tag>
                </a-descriptions-item>
                <a-descriptions-item label="轮次">{{ selfPlayStatus.rounds }}</a-descriptions-item>
                <a-descriptions-item label="通过率">{{ (selfPlayStatus.passRate * 100).toFixed(1) }}%</a-descriptions-item>
                <a-descriptions-item label="知识节点">{{ selfPlayStatus.knowledgeNodes }}</a-descriptions-item>
              </a-descriptions>
              <a-space style="margin-top: 12px">
                <a-button type="primary" :loading="selfPlayToggling" @click="handleToggleSelfPlay">
                  {{ selfPlayStatus.enabled ? '暂停' : '启动' }}
                </a-button>
                <a-button @click="handleRefreshStatus">刷新状态</a-button>
              </a-space>
            </a-card>
          </a-col>
        </a-row>

        <!-- Knowledge Graph Overview -->
        <a-card :title="t('founder.knowledge.title')" class="console-section">
          <a-row :gutter="16">
            <a-col :span="6">
              <a-statistic title="节点数" :value="knowledgeStats.nodeCount" />
            </a-col>
            <a-col :span="6">
              <a-statistic title="关系数" :value="knowledgeStats.edgeCount" />
            </a-col>
            <a-col :span="6">
              <a-statistic title="领域" :value="knowledgeStats.labelCount" />
            </a-col>
            <a-col :span="6">
              <a-statistic title="版本" :value="knowledgeStats.patchVersion" />
            </a-col>
          </a-row>
        </a-card>

        <!-- Recent Audit Logs -->
        <a-card :title="t('founder.audit.title')" class="console-section">
          <a-table :columns="auditColumns" :data-source="auditLogs" :loading="auditLoading" :pagination="false" size="small" row-key="id" />
          <a-button type="link" style="margin-top: 8px" @click="$router.push('/founder/audit-log')">查看全部</a-button>
        </a-card>
      </div>
    </div>
  </div>
</template>

<script lang="ts" setup>
  import { reactive, ref, onMounted } from 'vue';
  import { useI18n } from '/@/hooks/web/useI18n';
  import { useMessage } from '/@/hooks/web/useMessage';
  import {
    setupTotp,
    verifyTotp,
    setFounderToken,
    getFounderToken,
    configureModel,
    getSelfPlayStatus,
    toggleSelfPlay,
    getAuthLogs,
  } from '/@/api/founder/index';
  import { getKnowledgeStats } from '/@/api/founder/knowledge';

  defineOptions({ name: 'FounderConsole' });

  const { t } = useI18n();
  const { createMessage } = useMessage();

  // ── Auth State ──
  const isAuthenticated = ref(!!getFounderToken());
  const totpEmail = ref('');
  const totpSecret = ref('');
  const qrCodeUrl = ref('');
  const totpCode = ref<number | null>(null);
  const setupLoading = ref(false);
  const verifyLoading = ref(false);

  // ── Model Config ──
  const modelConfig = reactive({
    primaryModel: 'deepseek-v4-pro',
    fallbackModel: '',
    temperature: 0.7,
    maxTokens: 4096,
  });
  const modelSaving = ref(false);

  // ── Self-Play ──
  const selfPlayStatus = reactive({
    enabled: false,
    rounds: 0,
    passRate: 0,
    knowledgeNodes: 0,
  });
  const selfPlayToggling = ref(false);

  // ── Knowledge Stats ──
  const knowledgeStats = reactive({
    nodeCount: 0,
    edgeCount: 0,
    labelCount: 0,
    patchVersion: 0,
  });

  // ── Audit Logs ──
  const auditLoading = ref(false);
  const auditLogs = ref<any[]>([]);
  const auditColumns = [
    { title: '时间', dataIndex: 'creatorTime', width: 160 },
    { title: '操作', dataIndex: 'action', ellipsis: true },
    { title: '结果', dataIndex: 'result', width: 100 },
    { title: 'IP', dataIndex: 'ipAddress', width: 130 },
  ];

  // ── Methods ──

  async function handleSetupTotp() {
    if (!totpEmail.value) {
      createMessage.warning('请输入邮箱');
      return;
    }
    setupLoading.value = true;
    try {
      const res: any = await setupTotp(totpEmail.value);
      const data = res.data || res;
      totpSecret.value = data.secret;
      qrCodeUrl.value = data.qrCodeUrl;
      createMessage.success('TOTP 密钥已生成，请用 Google Authenticator 扫描');
    } catch {
      createMessage.error('TOTP 设置失败');
    } finally {
      setupLoading.value = false;
    }
  }

  async function handleVerifyTotp() {
    if (!totpCode.value) {
      createMessage.warning('请输入验证码');
      return;
    }
    verifyLoading.value = true;
    try {
      const res: any = await verifyTotp(totpEmail.value, totpCode.value);
      const data = res.data || res;
      if (data.token) {
        setFounderToken(data.token);
        isAuthenticated.value = true;
        createMessage.success(`已认证，token 有效期 ${data.expiresIn / 3600} 小时`);
      }
    } catch {
      createMessage.error('TOTP 验证失败');
    } finally {
      verifyLoading.value = false;
    }
  }

  async function handleSaveModel() {
    modelSaving.value = true;
    try {
      await configureModel({ ...modelConfig });
      createMessage.success('模型配置已保存');
    } catch {
      createMessage.error('保存失败');
    } finally {
      modelSaving.value = false;
    }
  }

  async function handleToggleSelfPlay() {
    selfPlayToggling.value = true;
    try {
      const res: any = await toggleSelfPlay(!selfPlayStatus.enabled);
      const data = res.data || res;
      selfPlayStatus.enabled = data.selfPlayEnabled;
      createMessage.success(data.message);
    } catch {
      createMessage.error('操作失败');
    } finally {
      selfPlayToggling.value = false;
    }
  }

  async function handleRefreshStatus() {
    try {
      const [spRes, ksRes, alRes]: any[] = await Promise.all([getSelfPlayStatus(), getKnowledgeStats(), getAuthLogs({ pageSize: 5 })]);
      const sp = spRes.data || spRes;
      Object.assign(selfPlayStatus, sp);

      const ks = ksRes.data || ksRes;
      knowledgeStats.nodeCount = ks.nodeCount || 0;
      knowledgeStats.edgeCount = ks.edgeCount || 0;
      knowledgeStats.labelCount = Object.keys(ks.labels || {}).length;
      knowledgeStats.patchVersion = ks.patchVersion || 0;

      const al = alRes.data || alRes;
      auditLogs.value = al.list || [];
    } catch {
      // 静默失败
    }
  }

  onMounted(() => {
    if (isAuthenticated.value) {
      handleRefreshStatus();
    }
  });
</script>

<style lang="less" scoped>
  .console-section {
    margin-bottom: 16px;
  }

  .console-row {
    margin-bottom: 0;
  }

  .console-card {
    height: 100%;
  }

  .totp-info {
    margin-top: 12px;
    padding: 16px;
    background: #fafbfc;
    border-radius: 4px;
  }

  :deep(.ant-card-head-title) {
    font-weight: 600;
  }
</style>
