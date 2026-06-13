/**
 * UniApp 编译器
 *
 * 将 FormPageIR 编译为完整的 UniApp 小程序项目文件集合。
 * 支持平台：mp-weixin / mp-alipay / mp-douyin / h5
 *
 * @jnpf-generated v5.2.0 type=compiler platform=uniapp
 */

import type {
  CompileResult,
  CompilerConfig,
  FormPageIR,
  GeneratedProject,
  FieldIR,
} from "./types";

const DEFAULT_VERSION = "5.2.0";

export type UniPlatform = "mp-weixin" | "mp-alipay" | "mp-douyin" | "h5";

export class UniAppCompiler {
  private config: CompilerConfig;
  private platform: UniPlatform;

  constructor(
    config: Partial<CompilerConfig> & { entity: string },
    platform: UniPlatform = "mp-weixin",
  ) {
    this.config = {
      entity: config.entity,
      entityLabel: config.entityLabel ?? config.entity,
      apiBasePath: config.apiBasePath ?? `/api/${capitalize(config.entity)}`,
      generatorVersion: config.generatorVersion ?? DEFAULT_VERSION,
    };
    this.platform = platform;
  }

  // ==========================================================
  // 主流程
  // ==========================================================

  compile(ir: FormPageIR): CompileResult {
    const project: GeneratedProject = new Map();
    const warnings: string[] = [];
    const complexExpressions: string[] = [];

    // 检测复杂表达式
    for (const expr of ir.expressions ?? []) {
      if (expr.level === "complex") {
        complexExpressions.push(`${expr.id}: ${expr.body.slice(0, 100)}`);
        warnings.push(`表达式 ${expr.id} 为复杂级别，需人工迁移`);
      }
    }

    const e = this.config.entity;

    // 按序生成 7 类文件
    project.set(`types/${e}.ts`, this.generateTypes(ir));
    project.set(`api/${e}.ts`, this.generateApi(ir));
    project.set(`stores/${e}.ts`, this.generateStore(ir));
    project.set(`pages/${e}/list.vue`, this.generateListPage(ir));
    project.set(`pages/${e}/form.vue`, this.generateFormPage(ir));
    project.set(`pages/${e}/detail.vue`, this.generateDetailPage(ir));
    project.set(`pages-${e}.json`, this.generatePagesJson(ir));

    return { project, warnings, complexExpressions };
  }

  // ==========================================================
  // 类型生成
  // ==========================================================

  private generateTypes(ir: FormPageIR): string {
    const e = this.config.entity;
    const E = capitalize(e);
    const v = this.config.generatorVersion;
    const p = this.platform;
    const items = ir.fields
      .map(
        (f) =>
          `  /** ${f.label} */\n  ${f.model}${f.config.required ? "" : "?"}: ${this.mapFieldToTSType(f)}`,
      )
      .join("\n");

    return `// @jnpf-generated v${v} entity=${e} platform=${p} type=types
/* eslint-disable */

export interface ${E}Entity {
${items}
}
`;
  }

  // ==========================================================
  // API 生成
  // ==========================================================

  private generateApi(ir: FormPageIR): string {
    const e = this.config.entity;
    const E = capitalize(e);
    const v = this.config.generatorVersion;
    const p = this.platform;
    const base = this.config.apiBasePath;

    return `// @jnpf-generated v${v} entity=${e} platform=${p} type=api
/* eslint-disable */
import { createEntityApi } from '@/api/request';
import type { ${E}Entity } from '@/types/${e}';

const api = createEntityApi<${E}Entity>('${base}');

export default api;

export const {
  list: get${E}List,
  detail: get${E}Detail,
  create: create${E},
  update: update${E},
  delete: delete${E},
  batchDelete: batchDelete${E},
} = api;
`;
  }

  // ==========================================================
  // Store 生成
  // ==========================================================

  private generateStore(ir: FormPageIR): string {
    const e = this.config.entity;
    const E = capitalize(e);
    const v = this.config.generatorVersion;
    const p = this.platform;

    return `// @jnpf-generated v${v} entity=${e} platform=${p} type=store
/* eslint-disable */
import { defineStore } from 'pinia';
import { ref, reactive } from 'vue';
import api from '@/api/${e}';
import type { ${E}Entity } from '@/types/${e}';

export const use${E}Store = defineStore('${e}', () => {
  const loading = ref(false);
  const list = ref<${E}Entity[]>([]);
  const current = ref<${E}Entity | undefined>();
  const pagination = reactive({ current: 1, pageSize: 20, total: 0 });

  async function loadList(params?: Record<string, unknown>) {
    loading.value = true;
    try {
      const method = api.list({
        currentPage: pagination.current,
        pageSize: pagination.pageSize,
        ...params,
      });
      list.value = await method.send();
    } finally {
      loading.value = false;
    }
  }

  async function loadDetail(id: string) {
    loading.value = true;
    try {
      const method = api.detail(id);
      current.value = await method.send();
    } finally {
      loading.value = false;
    }
  }

  async function save(data: Partial<${E}Entity>) {
    if (current.value?.id) {
      await api.update(String(current.value.id), data).send();
    } else {
      await api.create(data).send();
    }
  }

  async function remove(id: string) {
    await api.delete(id).send();
    await loadList();
  }

  return { loading, list, current, pagination, loadList, loadDetail, save, remove };
});
`;
  }

  // ==========================================================
  // 列表页生成
  // ==========================================================

  private generateListPage(ir: FormPageIR): string {
    const e = this.config.entity;
    const E = capitalize(e);
    const v = this.config.generatorVersion;
    const p = this.platform;
    const now = new Date().toISOString();
    const searchFields = ir.listConfig?.searchFields ?? [];
    const displayField = ir.fields[0]?.model ?? "id";

    const searchInputs = searchFields
      .map(
        (sf) =>
          `      <wd-input v-model="searchParams.${sf.field}" placeholder="请输入${sf.label}" clearable />`,
      )
      .join("\n");

    const searchParamDefaults = searchFields
      .map((sf) => `  ${sf.field}: '',`)
      .join("\n");

    return `<!-- @jnpf-generated v${v} entity=${e} platform=${p} type=list-page -->
<!-- generated: ${now} -->
<template>
  <view class="page-list">
    <view class="search-bar">
${searchInputs}
      <view class="search-actions">
        <wd-button type="primary" size="small" @click="handleSearch">查询</wd-button>
        <wd-button size="small" @click="handleReset">重置</wd-button>
      </view>
    </view>
    <wd-cell-group>
      <wd-cell v-for="item in store.list" :key="item.id" :title="String(item.${displayField} ?? '')" @click="handleDetail(item)">
        <template #value>
          <view class="cell-actions">
            <wd-button size="mini" @click.stop="handleEdit(item)">编辑</wd-button>
            <wd-button size="mini" type="error" @click.stop="handleDelete(item)">删除</wd-button>
          </view>
        </template>
      </wd-cell>
    </wd-cell-group>
    <wd-status-tip v-if="!store.loading && store.list.length === 0" tip="暂无数据" />
    <wd-loading v-if="store.loading" />
    <view class="fab" @click="handleAdd"><wd-icon name="add" size="24px" /></view>
  </view>
</template>

<script setup lang="ts">
import { reactive, onMounted } from 'vue';
import { onPullDownRefresh, onReachBottom } from '@dcloudio/uni-app';
import { use${E}Store } from '@/stores/${e}';

const store = use${E}Store();
const searchParams = reactive<Record<string, string>>({
${searchParamDefaults}
});

onMounted(() => store.loadList());

onPullDownRefresh(async () => {
  store.pagination.current = 1;
  await store.loadList();
  uni.stopPullDownRefresh();
});

onReachBottom(() => {
  if (store.list.length >= store.pagination.total) return;
  store.pagination.current++;
  store.loadList();
});

function handleSearch() {
  store.pagination.current = 1;
  store.loadList(searchParams);
}

function handleReset() {
  Object.keys(searchParams).forEach(k => (searchParams[k] = ''));
  handleSearch();
}

function handleAdd() {
  uni.navigateTo({ url: '/pages/${e}/form' });
}

function handleEdit(item: Record<string, unknown>) {
  uni.navigateTo({ url: \`/pages/${e}/form?id=\${item.id}\` });
}

function handleDetail(item: Record<string, unknown>) {
  uni.navigateTo({ url: \`/pages/${e}/detail?id=\${item.id}\` });
}

async function handleDelete(item: Record<string, unknown>) {
  const res = await uni.showModal({ title: '确认删除', content: '确定删除该${this.config.entityLabel}吗？' });
  if (res.confirm) {
    await store.remove(item.id as string);
    uni.showToast({ title: '删除成功', icon: 'success' });
  }
}

// @jnpf-gen:insert-point=custom-list-logic
// @jnpf-gen:end-insert-point=custom-list-logic
</script>

<style scoped lang="scss">
.page-list { padding: 24rpx; padding-bottom: 120rpx; }
.search-bar { margin-bottom: 24rpx; }
.search-actions { display: flex; gap: 16rpx; margin-top: 16rpx; }
.cell-actions { display: flex; gap: 8rpx; }
.fab {
  position: fixed; right: 40rpx; bottom: 120rpx;
  width: 100rpx; height: 100rpx; border-radius: 50%;
  background: #0083ff; display: flex; align-items: center; justify-content: center;
  color: #fff; box-shadow: 0 4px 16px rgba(0,0,0,0.2); z-index: 100;
}
</style>
`;
  }

  // ==========================================================
  // 表单页生成
  // ==========================================================

  private generateFormPage(ir: FormPageIR): string {
    const e = this.config.entity;
    const E = capitalize(e);
    const v = this.config.generatorVersion;
    const p = this.platform;
    const now = new Date().toISOString();
    const label = this.config.entityLabel;

    const formFields = ir.fields.map((f) => this.renderFormField(f)).join("\n");

    const defaultValues = ir.fields
      .map((f) => {
        const appComp = f.component?.app ?? "uni-easyinput";
        if (appComp === "switch") return `  ${f.model}: false,`;
        if (f.component?.jnpfKey === "JnpfInputNumber")
          return `  ${f.model}: 0,`;
        return `  ${f.model}: '',`;
      })
      .join("\n");

    return `<!-- @jnpf-generated v${v} entity=${e} platform=${p} type=form-page -->
<!-- generated: ${now} -->
<template>
  <view class="page-form">
    <wd-form ref="formRef" :model="formData">
      <wd-cell-group>
${formFields}
      </wd-cell-group>
    </wd-form>

    <!-- @jnpf-gen:insert-point=custom-form-fields -->
    <!-- @jnpf-gen:end-insert-point=custom-form-fields -->

    <view class="form-actions">
      <wd-button type="primary" block @click="handleSubmit">
        {{ isEdit ? '更新' : '提交' }}
      </wd-button>
      <wd-button block @click="handleCancel">取消</wd-button>
    </view>
  </view>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue';
import api from '@/api/${e}';

const formRef = ref();
const isEdit = ref(false);
const formData = reactive<Record<string, unknown>>({
${defaultValues}
});

onMounted(async () => {
  const pages = getCurrentPages();
  const page = pages[pages.length - 1];
  const id = (page as Record<string, unknown>)?.options?.id as string | undefined;
  if (id) {
    isEdit.value = true;
    try {
      const detail = await api.detail(id).send();
      if (detail) Object.assign(formData, detail);
    } catch {
      uni.showToast({ title: '加载数据失败', icon: 'none' });
    }
  }
});

async function handleSubmit() {
  try {
    await formRef.value?.validate();
    const data = { ...formData } as Record<string, unknown>;
    const pages = getCurrentPages();
    const page = pages[pages.length - 1];
    const id = (page as Record<string, unknown>)?.options?.id as string | undefined;
    if (id) {
      await api.update(id, data).send();
    } else {
      await api.create(data).send();
    }
    uni.showToast({ title: isEdit.value ? '更新成功' : '创建成功', icon: 'success' });
    setTimeout(() => uni.navigateBack(), 1500);
  } catch {
    // 校验失败
  }
}

function handleCancel() {
  uni.navigateBack();
}

// @jnpf-gen:insert-point=custom-form-logic
// @jnpf-gen:end-insert-point=custom-form-logic
</script>

<style scoped lang="scss">
.page-form { padding: 24rpx; }
.form-actions { margin-top: 48rpx; display: flex; flex-direction: column; gap: 16rpx; }
</style>
`;
  }

  // ==========================================================
  // 详情页生成
  // ==========================================================

  private generateDetailPage(ir: FormPageIR): string {
    const e = this.config.entity;
    const E = capitalize(e);
    const v = this.config.generatorVersion;
    const p = this.platform;
    const now = new Date().toISOString();
    const label = this.config.entityLabel;

    const detailFields = ir.fields
      .map(
        (f) =>
          `      <wd-cell title="${f.label}" :value="String(data.${f.model} ?? '')" />`,
      )
      .join("\n");

    return `<!-- @jnpf-generated v${v} entity=${e} platform=${p} type=detail-page -->
<!-- generated: ${now} -->
<template>
  <view class="page-detail">
    <wd-cell-group title="${label} 详情">
${detailFields}
    </wd-cell-group>

    <!-- @jnpf-gen:insert-point=custom-detail-fields -->
    <!-- @jnpf-gen:end-insert-point=custom-detail-fields -->

    <view class="detail-actions">
      <wd-button type="primary" block @click="handleEdit">编辑</wd-button>
    </view>
  </view>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import api from '@/api/${e}';

const data = ref<Record<string, unknown>>({});

onMounted(async () => {
  const pages = getCurrentPages();
  const page = pages[pages.length - 1];
  const id = (page as Record<string, unknown>)?.options?.id as string | undefined;
  if (!id) {
    uni.showToast({ title: '缺少参数', icon: 'none' });
    setTimeout(() => uni.navigateBack(), 1500);
    return;
  }
  try {
    const detail = await api.detail(id).send();
    if (detail) data.value = detail as Record<string, unknown>;
  } catch {
    uni.showToast({ title: '加载数据失败', icon: 'none' });
  }
});

function handleEdit() {
  const id = data.value.id as string;
  if (!id) return;
  uni.navigateTo({ url: \`/pages/${e}/form?id=\${id}\` });
}

// @jnpf-gen:insert-point=custom-detail-logic
// @jnpf-gen:end-insert-point=custom-detail-logic
</script>

<style scoped lang="scss">
.page-detail { padding: 24rpx; }
.detail-actions { margin-top: 48rpx; }
</style>
`;
  }

  // ==========================================================
  // pages.json 片段生成
  // ==========================================================

  private generatePagesJson(ir: FormPageIR): string {
    const e = this.config.entity;
    const label = this.config.entityLabel;
    const v = this.config.generatorVersion;
    const p = this.platform;

    const obj = {
      _comment: `@jnpf-generated v${v} entity=${e} platform=${p}`,
      pages: [
        {
          path: `pages/${e}/list`,
          style: {
            navigationBarTitleText: `${label}列表`,
            enablePullDownRefresh: true,
          },
        },
        {
          path: `pages/${e}/form`,
          style: { navigationBarTitleText: `${label}表单` },
        },
        {
          path: `pages/${e}/detail`,
          style: { navigationBarTitleText: `${label}详情` },
        },
      ],
    };

    return JSON.stringify(obj, null, 2);
  }

  // ==========================================================
  // 工具方法
  // ==========================================================

  /** 渲染单个表单字段为 wd 组件 */
  private renderFormField(field: FieldIR): string {
    const appComp = field.component?.app ?? "uni-easyinput";
    const isRequired = field.config.required ? " required" : "";
    const model = field.model;
    const label = field.label;

    let input: string;
    switch (appComp) {
      case "uni-data-select":
        input = `          <wd-select-picker v-model="formData.${model}" :columns="${model}Options" placeholder="请选择${label}" />`;
        break;
      case "uni-datetime-picker":
        input = `          <wd-datetime-picker v-model="formData.${model}" placeholder="请选择${label}" />`;
        break;
      case "switch":
        input = `          <wd-switch v-model="formData.${model}" active-value="1" inactive-value="0" />`;
        break;
      default: {
        // uni-easyinput 或其他默认输入
        const isNumber = field.component?.jnpfKey === "JnpfInputNumber";
        const typeAttr = isNumber ? ' type="number"' : "";
        input = `          <wd-input v-model="formData.${model}" placeholder="请输入${label}"${typeAttr} />`;
      }
    }

    return `        <wd-cell title="${label}"${isRequired}>\n${input}\n        </wd-cell>`;
  }

  /** jnpfKey → TypeScript 类型 */
  mapFieldToTSType(field: FieldIR): string {
    const key = field.component?.jnpfKey ?? "";
    const multiple = field.config?.multiple;

    const map: Record<string, string> = {
      JnpfInput: "string",
      JnpfTextarea: "string",
      JnpfInputNumber: "number",
      JnpfSwitch: "boolean",
      JnpfDatePicker: "string",
      JnpfTimePicker: "string",
      JnpfRate: "number",
      JnpfSlider: "number",
      JnpfSelect: multiple ? "string[]" : "string",
      JnpfRadio: "string",
      JnpfCheckbox: "string[]",
      JnpfUploadImg: "string[]",
      JnpfUploadFile: "string[]",
      JnpfEditor: "string",
    };

    return map[key] ?? "unknown";
  }
}

/** 首字母大写 */
function capitalize(s: string): string {
  return s.charAt(0).toUpperCase() + s.slice(1);
}
