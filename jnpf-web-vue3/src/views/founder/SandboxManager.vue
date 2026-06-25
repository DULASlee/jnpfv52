<template>
  <div class="jnpf-content-wrapper">
    <div class="jnpf-content-wrapper-center">
      <div class="jnpf-content-wrapper-content">
        <BasicTable @register="registerTable">
          <template #tableTitle>
            <a-space>
              <a-button type="primary" preIcon="ant-design:plus-outlined" @click="handleCreate"> 创建沙箱 </a-button>
              <a-button danger @click="handleDestroyAll" :loading="destroyingAll"> 销毁全部 </a-button>
              <a-button @click="reload">刷新</a-button>
            </a-space>
          </template>
          <template #bodyCell="{ column, record }">
            <template v-if="column.key === 'status'">
              <a-tag :color="statusColor(record.status)">{{ record.status }}</a-tag>
            </template>
            <template v-if="column.key === 'action'">
              <a-space>
                <a-button size="small" @click="handleDeploy(record)">部署</a-button>
                <a-popconfirm title="确认销毁此沙箱？" @confirm="handleDestroy(record.id)">
                  <a-button size="small" danger>销毁</a-button>
                </a-popconfirm>
              </a-space>
            </template>
          </template>
        </BasicTable>

        <!-- Create Sandbox Modal -->
        <a-modal v-model:visible="createVisible" title="创建沙箱" @ok="confirmCreate" :confirm-loading="creating">
          <a-form layout="vertical">
            <a-form-item label="租户 ID" required>
              <a-input v-model:value="createForm.tenantId" placeholder="输入租户 ID" />
            </a-form-item>
            <a-form-item label="CPU 核数">
              <a-input-number v-model:value="createForm.cpuLimit" :min="1" :max="8" style="width: 100%" />
            </a-form-item>
            <a-form-item label="内存限制">
              <a-select v-model:value="createForm.memoryLimit">
                <a-select-option value="2Gi">2 GiB</a-select-option>
                <a-select-option value="4Gi">4 GiB</a-select-option>
                <a-select-option value="8Gi">8 GiB</a-select-option>
              </a-select>
            </a-form-item>
            <a-form-item label="超时时间（秒）">
              <a-input-number v-model:value="createForm.timeoutSeconds" :min="60" :max="3600" style="width: 100%" />
            </a-form-item>
          </a-form>
        </a-modal>
      </div>
    </div>
  </div>
</template>

<script lang="ts" setup>
  import { reactive, ref } from 'vue';
  import { BasicTable, useTable } from '/@/components/Table';
  import type { BasicColumn } from '/@/components/Table';
  import { useMessage } from '/@/hooks/web/useMessage';
  import { getSandboxList, createSandbox, destroySandbox } from '/@/api/founder/sandbox';

  defineOptions({ name: 'FounderSandboxManager' });

  const { createMessage } = useMessage();

  const columns: BasicColumn[] = [
    { title: '沙箱 ID', dataIndex: 'id', width: 140 },
    { title: '租户', dataIndex: 'config', customRender: ({ text }) => text?.tenantId || '-', width: 120 },
    { title: '状态', dataIndex: 'status', key: 'status', width: 100 },
    { title: 'URL', dataIndex: 'url', ellipsis: true },
    { title: '创建时间', dataIndex: 'createdAt', width: 170, format: 'date|YYYY-MM-DD HH:mm:ss' },
    { title: '操作', key: 'action', width: 150 },
  ];

  const [registerTable, { reload }] = useTable({
    api: getSandboxList,
    columns,
    showIndexColumn: false,
    immediate: true,
  });

  // ── Create ──
  const createVisible = ref(false);
  const creating = ref(false);
  const createForm = reactive({
    tenantId: '',
    cpuLimit: 1,
    memoryLimit: '4Gi',
    timeoutSeconds: 300,
  });

  function handleCreate() {
    createForm.tenantId = '';
    createForm.cpuLimit = 1;
    createForm.memoryLimit = '4Gi';
    createForm.timeoutSeconds = 300;
    createVisible.value = true;
  }

  async function confirmCreate() {
    if (!createForm.tenantId) {
      createMessage.warning('请输入租户 ID');
      return;
    }
    creating.value = true;
    try {
      await createSandbox({ ...createForm });
      createMessage.success('沙箱创建成功');
      createVisible.value = false;
      reload();
    } catch {
      createMessage.error('创建失败');
    } finally {
      creating.value = false;
    }
  }

  // ── Destroy ──
  const destroyingAll = ref(false);

  async function handleDestroy(id: string) {
    try {
      await destroySandbox(id);
      createMessage.success('沙箱已销毁');
      reload();
    } catch {
      createMessage.error('销毁失败');
    }
  }

  async function handleDestroyAll() {
    destroyingAll.value = true;
    try {
      const res: any = await getSandboxList();
      const list = res.data?.list || res.list || [];
      await Promise.all(list.map((s: any) => destroySandbox(s.id)));
      createMessage.success(`已销毁 ${list.length} 个沙箱`);
      reload();
    } catch {
      createMessage.error('批量销毁失败');
    } finally {
      destroyingAll.value = false;
    }
  }

  function handleDeploy(_record: any) {
    createMessage.info('部署功能通过 API 调用: POST /api/sandbox/{id}/deploy');
  }

  function statusColor(status: string) {
    switch (status) {
      case 'ready':
        return 'green';
      case 'creating':
      case 'testing':
        return 'blue';
      case 'destroying':
        return 'orange';
      case 'destroyed':
        return 'default';
      case 'error':
        return 'red';
      default:
        return 'default';
    }
  }
</script>
