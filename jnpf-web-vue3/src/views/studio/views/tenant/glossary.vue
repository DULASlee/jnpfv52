<template>
  <div class="glossary">
    <h2>业务术语表</h2>
    <a-spin :spinning="loading">
      <!-- Toolbar -->
      <div class="toolbar">
        <a-input-search v-model:value="keyword" placeholder="搜索术语" style="width: 240px" @search="loadList" />
        <a-button type="primary" @click="showCreate = true">+ 新增术语</a-button>
      </div>

      <!-- Glossary list -->
      <a-table :columns="columns" :data-source="list" :pagination="{ pageSize: 10 }" row-key="f_Id" size="small">
        <template #bodyCell="{ column, record }">
          <template v-if="column.key === 'actions'">
            <a-button size="small" type="link" @click="editItem(record)">编辑</a-button>
            <a-popconfirm title="确认删除?" @confirm="deleteItem(record.f_Id)">
              <a-button size="small" type="link" danger>删除</a-button>
            </a-popconfirm>
          </template>
        </template>
      </a-table>
    </a-spin>

    <!-- Create/Edit Modal -->
    <a-modal v-model:visible="showCreate" :title="editingId ? '编辑术语' : '新增术语'" width="600px" @ok="handleSave">
      <a-form layout="vertical">
        <a-form-item label="术语名称" required>
          <a-input v-model:value="form.term" />
        </a-form-item>
        <a-form-item label="定义" required>
          <a-textarea v-model:value="form.definition" :rows="4" />
        </a-form-item>
        <a-form-item label="同义词">
          <a-input v-model:value="form.synonyms" placeholder="多个同义词用逗号分隔" />
        </a-form-item>
        <a-form-item label="使用示例">
          <a-textarea v-model:value="form.examples" :rows="3" />
        </a-form-item>
      </a-form>
    </a-modal>
  </div>
</template>

<script setup lang="ts">
  import { ref, reactive, onMounted } from 'vue';
  import { defHttp } from '/@/utils/http/axios';

  const loading = ref(false);
  const list = ref<any[]>([]);
  const keyword = ref('');
  const showCreate = ref(false);
  const editingId = ref<number | null>(null);

  const form = reactive({
    term: '',
    definition: '',
    synonyms: '',
    examples: '',
  });

  const columns = [
    { title: '术语', dataIndex: 'f_Term', width: 150 },
    { title: '定义', dataIndex: 'f_Definition' },
    { title: '同义词', dataIndex: 'f_Synonyms', width: 150 },
    { title: '操作', key: 'actions', width: 120 },
  ];

  async function loadList() {
    loading.value = true;
    try {
      const res: any = await defHttp.get({
        url: '/api/studio/tenant/glossary',
        data: { keyword: keyword.value },
      });
      list.value = res?.data?.items || res?.data || [];
    } catch {
      list.value = [];
    }
    loading.value = false;
  }

  function editItem(record: any) {
    editingId.value = record.f_Id;
    form.term = record.f_Term;
    form.definition = record.f_Definition;
    form.synonyms = record.f_Synonyms || '';
    form.examples = record.f_Examples || '';
    showCreate.value = true;
  }

  async function deleteItem(id: number) {
    try {
      await defHttp.delete({ url: `/api/studio/tenant/glossary/${id}/delete` });
      loadList();
    } catch (e: any) {
      console.error(e);
    }
  }

  async function handleSave() {
    try {
      if (editingId.value) {
        await defHttp.put({ url: `/api/studio/tenant/glossary/${editingId.value}/update`, data: form });
      } else {
        await defHttp.post({ url: '/api/studio/tenant/glossary/create', data: form });
      }
      showCreate.value = false;
      editingId.value = null;
      Object.assign(form, { term: '', definition: '', synonyms: '', examples: '' });
      loadList();
    } catch (e: any) {
      console.error(e);
    }
  }

  onMounted(loadList);
</script>

<style scoped lang="less">
  .glossary {
    max-width: 1100px;
    margin: 0 auto;
    padding: 24px;
  }
  h2 {
    margin: 0 0 16px;
  }
  .toolbar {
    display: flex;
    gap: 8px;
    margin-bottom: 16px;
    align-items: center;
  }
</style>
