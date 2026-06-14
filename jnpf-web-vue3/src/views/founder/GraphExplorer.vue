<template>
  <div class="jnpf-content-wrapper">
    <div class="jnpf-content-wrapper-left">
      <a-card title="节点列表" size="small" :body-style="{ padding: '0' }">
        <div class="node-search">
          <a-input-search v-model:value="searchKeyword" placeholder="搜索节点..." @search="handleSearch" allow-clear />
        </div>
        <a-spin :spinning="nodeLoading">
          <a-list :data-source="nodeList" size="small" class="node-list">
            <template #renderItem="{ item }">
              <a-list-item class="node-item" :class="{ active: selectedNode?.id === item.id }" @click="selectNode(item)">
                <a-list-item-meta>
                  <template #title>
                    <a-tag :color="labelColor(item.label)" size="small">{{ item.label }}</a-tag>
                    {{ item.name }}
                  </template>
                  <template #description>{{ item.id }}</template>
                </a-list-item-meta>
              </a-list-item>
            </template>
          </a-list>
        </a-spin>
        <div class="node-pagination">
          <a-pagination v-model:current="nodePage" :total="nodeTotal" :page-size="20" size="small" @change="loadNodes" />
        </div>
      </a-card>
    </div>

    <div class="jnpf-content-wrapper-center">
      <div class="jnpf-content-wrapper-content">
        <!-- Stats -->
        <a-row :gutter="16" class="stats-row" v-if="stats">
          <a-col :span="6">
            <a-statistic title="总节点" :value="stats.nodeCount" />
          </a-col>
          <a-col :span="6">
            <a-statistic title="总关系" :value="stats.edgeCount" />
          </a-col>
          <a-col :span="6">
            <a-statistic title="版本" :value="stats.patchVersion" />
          </a-col>
          <a-col :span="6">
            <a-statistic title="领域" :value="Object.keys(stats.labels || {}).length" />
          </a-col>
        </a-row>

        <!-- Label Distribution -->
        <a-card title="标签分布" size="small" class="section-card" v-if="stats">
          <a-space wrap>
            <a-tag v-for="(count, label) in stats.labels" :key="String(label)" :color="labelColor(String(label))"> {{ label }}: {{ count }} </a-tag>
          </a-space>
        </a-card>

        <!-- Node Detail -->
        <a-card title="节点详情" size="small" class="section-card" v-if="selectedNode">
          <a-descriptions :column="2" bordered size="small">
            <a-descriptions-item label="ID">{{ selectedNode.id }}</a-descriptions-item>
            <a-descriptions-item label="Label">
              <a-tag :color="labelColor(selectedNode.label)">{{ selectedNode.label }}</a-tag>
            </a-descriptions-item>
            <a-descriptions-item label="名称">{{ selectedNode.name }}</a-descriptions-item>
            <a-descriptions-item label="创建时间">{{ selectedNode.creatorTime }}</a-descriptions-item>
          </a-descriptions>
          <a-collapse ghost style="margin-top: 8px" v-if="parsedProperties">
            <a-collapse-panel key="props" header="Properties (JSON)">
              <pre class="json-preview">{{ parsedProperties }}</pre>
            </a-collapse-panel>
          </a-collapse>
        </a-card>

        <!-- Edges Table -->
        <a-card title="关系列表" size="small" class="section-card">
          <a-table
            :columns="edgeColumns"
            :data-source="edgeList"
            :loading="edgeLoading"
            :pagination="{ current: edgePage, total: edgeTotal, pageSize: 20 }"
            size="small"
            row-key="id"
            @change="(pag: any) => loadEdges(pag.current)">
            <template #bodyCell="{ column, record }">
              <template v-if="column.key === 'relationType'">
                <a-tag>{{ record.relationType }}</a-tag>
              </template>
            </template>
          </a-table>
        </a-card>
      </div>
    </div>
  </div>
</template>

<script lang="ts" setup>
  import { ref, computed, onMounted } from 'vue';
  import { getKnowledgeNodes, getKnowledgeNodeDetail, getKnowledgeEdges, getKnowledgeStats } from '/@/api/founder/knowledge';

  defineOptions({ name: 'FounderGraphExplorer' });

  // ── Stats ──
  const stats = ref<any>(null);

  // ── Nodes ──
  const nodeList = ref<any[]>([]);
  const nodeTotal = ref(0);
  const nodePage = ref(1);
  const nodeLoading = ref(false);
  const searchKeyword = ref('');
  const selectedNode = ref<any>(null);

  const parsedProperties = computed(() => {
    if (!selectedNode.value?.properties) return null;
    try {
      return JSON.stringify(JSON.parse(selectedNode.value.properties), null, 2);
    } catch {
      return selectedNode.value.properties;
    }
  });

  // ── Edges ──
  const edgeList = ref<any[]>([]);
  const edgeTotal = ref(0);
  const edgePage = ref(1);
  const edgeLoading = ref(false);

  const edgeColumns = [
    { title: '源节点', dataIndex: 'sourceNodeId', ellipsis: true, width: 150 },
    { title: '目标节点', dataIndex: 'targetNodeId', ellipsis: true, width: 150 },
    { title: '关系类型', dataIndex: 'relationType', key: 'relationType', width: 120 },
    { title: '创建时间', dataIndex: 'creatorTime', width: 160 },
  ];

  // ── Load ──

  async function loadStats() {
    try {
      const res: any = await getKnowledgeStats();
      stats.value = res.data || res;
    } catch {
      // silent
    }
  }

  async function loadNodes(page = 1) {
    nodeLoading.value = true;
    nodePage.value = page;
    try {
      const res: any = await getKnowledgeNodes({
        currentPage: page,
        pageSize: 20,
      });
      const data = res.data || res;
      nodeList.value = data.list || [];
      nodeTotal.value = data.pagination?.total || 0;
    } catch {
      nodeList.value = [];
    } finally {
      nodeLoading.value = false;
    }
  }

  async function loadEdges(page = 1) {
    edgeLoading.value = true;
    edgePage.value = page;
    try {
      const res: any = await getKnowledgeEdges({
        currentPage: page,
        pageSize: 20,
      });
      const data = res.data || res;
      edgeList.value = data.list || [];
      edgeTotal.value = data.pagination?.total || 0;
    } catch {
      edgeList.value = [];
    } finally {
      edgeLoading.value = false;
    }
  }

  async function handleSearch(keyword: string) {
    if (!keyword) {
      await loadNodes(1);
      return;
    }
    nodeLoading.value = true;
    try {
      const res: any = await getKnowledgeNodes({ currentPage: 1, pageSize: 50 });
      const data = res.data || res;
      const list = data.list || [];
      const kw = keyword.toLowerCase();
      nodeList.value = list.filter((n: any) => n.name?.toLowerCase().includes(kw) || n.label?.toLowerCase().includes(kw));
      nodeTotal.value = nodeList.value.length;
    } catch {
      nodeList.value = [];
    } finally {
      nodeLoading.value = false;
    }
  }

  async function selectNode(item: any) {
    try {
      const res: any = await getKnowledgeNodeDetail(item.id);
      selectedNode.value = res.data || res;
    } catch {
      selectedNode.value = item;
    }
  }

  function labelColor(label: string) {
    const colors: Record<string, string> = {
      entity: 'blue',
      rule: 'purple',
      pattern: 'green',
      'anti-pattern': 'red',
      component: 'orange',
    };
    return colors[label] || 'default';
  }

  onMounted(() => {
    loadStats();
    loadNodes();
    loadEdges();
  });
</script>

<style lang="less" scoped>
  .node-search {
    padding: 8px;
  }
  .node-list {
    max-height: calc(100vh - 280px);
    overflow-y: auto;
  }
  .node-item {
    cursor: pointer;
    &:hover {
      background: #f0f5ff;
    }
    &.active {
      background: #e6f0ff;
      border-left: 3px solid #1890ff;
    }
  }
  .node-pagination {
    padding: 8px;
    text-align: center;
  }
  .stats-row {
    margin-bottom: 16px;
  }
  .section-card {
    margin-bottom: 16px;
  }
  .json-preview {
    font-size: 12px;
    background: #fafbfc;
    padding: 12px;
    border-radius: 4px;
    overflow-x: auto;
    max-height: 300px;
  }
</style>
