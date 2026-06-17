<template>
  <div class="rule-editor">
    <h2>业务规则配置中心</h2>

    <!-- Tab switcher -->
    <a-tabs v-model:active-key="activeTab">
      <a-tab-pane key="decision-table" tab="决策表">
        <div class="toolbar">
          <strong>决策表编辑器</strong>
          <div class="toolbar-actions">
            <a-button size="small" @click="addCondition">+ 条件列</a-button>
            <a-button size="small" @click="addAction">+ 动作列</a-button>
            <a-button size="small" @click="addRow">+ 规则行</a-button>
            <a-button size="small" danger @click="resetTable">重置</a-button>
          </div>
        </div>

        <div class="table-scroll">
          <table class="decision-table">
            <thead>
              <tr>
                <th v-for="(col, ci) in conditions" :key="'c-' + ci" class="cond-th">
                  <a-input v-model:value="col.label" size="small" style="width: 100%" />
                </th>
                <th class="sep-th"></th>
                <th v-for="(col, ai) in actions" :key="'a-' + ai" class="act-th">
                  <a-input v-model:value="col.label" size="small" style="width: 100%" />
                </th>
                <th class="op-th">来源</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="(row, ri) in rows" :key="ri">
                <td v-for="(col, ci) in conditions" :key="'c-' + ci">
                  <a-select v-model:value="row.conditions[ci]" size="small" style="width: 100%" placeholder="选择...">
                    <a-select-option value="yes">是</a-select-option>
                    <a-select-option value="no">否</a-select-option>
                    <a-select-option value="any">任意</a-select-option>
                  </a-select>
                </td>
                <td class="sep-cell">→</td>
                <td v-for="(col, ai) in actions" :key="'a-' + ai">
                  <a-input v-model:value="row.actions[ai]" size="small" />
                </td>
                <td>
                  <a-tag :color="sourceColor(row.source)">
                    {{ sourceLabel(row.source) }}
                  </a-tag>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </a-tab-pane>

      <a-tab-pane key="rule-chain" tab="规则链">
        <div class="toolbar">
          <strong>规则链编辑器</strong>
          <a-button size="small" @click="addChainRule">+ 添加规则</a-button>
        </div>
        <div v-for="(rule, ri) in chainRules" :key="ri" class="chain-rule">
          <div class="rule-header">
            <span class="rule-num">#{{ ri + 1 }}</span>
            <a-tag :color="sourceColor(rule.source)">{{ sourceLabel(rule.source) }}</a-tag>
            <a-button size="small" type="link" danger @click="chainRules.splice(ri, 1)">删除</a-button>
          </div>
          <div class="rule-body">
            <div class="field">
              <label>条件 (WHEN)</label>
              <a-textarea v-model:value="rule.condition" :rows="2" placeholder="e.g. orderAmount > 10000" />
            </div>
            <div class="field">
              <label>动作 (THEN)</label>
              <a-textarea v-model:value="rule.action" :rows="2" placeholder="e.g. setApprovalRequired(true)" />
            </div>
          </div>
        </div>
      </a-tab-pane>
    </a-tabs>
  </div>
</template>

<script setup lang="ts">
  import { ref, reactive, onMounted } from 'vue';
  import { defHttp } from '/@/utils/http/axios';

  const activeTab = ref('decision-table');

  // --- Decision Table ---
  const conditions = reactive([{ label: '条件1' }, { label: '条件2' }]);
  const actions = reactive([{ label: '动作1' }]);
  const rows = reactive([
    { conditions: ['yes', 'any'], actions: ['执行A'], source: 'ai' },
    { conditions: ['no', 'yes'], actions: ['执行B'], source: 'human' },
  ]);

  function addCondition() {
    conditions.push({ label: `条件${conditions.length + 1}` });
  }
  function addAction() {
    actions.push({ label: `动作${actions.length + 1}` });
  }
  function addRow() {
    rows.push({
      conditions: conditions.map(() => 'any'),
      actions: actions.map(() => ''),
      source: 'human',
    });
  }
  function resetTable() {
    conditions.splice(0, conditions.length, { label: '条件1' }, { label: '条件2' });
    actions.splice(0, actions.length, { label: '动作1' });
    rows.splice(0, rows.length);
  }

  // --- Rule Chain ---
  const chainRules = reactive([{ condition: '', action: '', source: 'ai' }]);

  function addChainRule() {
    chainRules.push({ condition: '', action: '', source: 'human' });
  }

  // --- Helpers ---
  function sourceColor(s: string) {
    return { ai: 'blue', human: 'green', mixed: 'purple' }[s] || 'default';
  }
  function sourceLabel(s: string) {
    return { ai: 'AI生成', human: '人工', mixed: '混合' }[s] || s;
  }
</script>

<style scoped lang="less">
  .rule-editor {
    max-width: 1100px;
    margin: 0 auto;
    padding: 24px;

    h2 {
      margin: 0 0 16px;
    }

    .toolbar {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 12px;
      .toolbar-actions {
        display: flex;
        gap: 6px;
      }
    }

    .table-scroll {
      overflow-x: auto;
    }

    .decision-table {
      width: 100%;
      border-collapse: collapse;
      font-size: 13px;

      th,
      td {
        padding: 6px 4px;
        border: 1px solid #f0f0f0;
        text-align: center;
        min-width: 80px;
      }
      .cond-th {
        background: #e6f7ff;
      }
      .act-th {
        background: #f6ffed;
      }
      .sep-th,
      .sep-cell {
        background: #fafafa;
        width: 30px;
        color: #1890ff;
        font-weight: bold;
      }
      .op-th {
        background: #fafafa;
        width: 80px;
      }
    }

    .chain-rule {
      background: #fff;
      border: 1px solid #f0f0f0;
      border-radius: 8px;
      margin-bottom: 12px;
      padding: 12px 16px;

      .rule-header {
        display: flex;
        align-items: center;
        gap: 8px;
        margin-bottom: 8px;
        .rule-num {
          font-weight: 600;
          color: #1890ff;
        }
      }

      .rule-body {
        display: grid;
        grid-template-columns: 1fr 1fr;
        gap: 12px;
        .field label {
          display: block;
          font-size: 12px;
          color: #888;
          margin-bottom: 4px;
        }
      }
    }
  }
</style>
