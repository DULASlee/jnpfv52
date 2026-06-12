/**
 * Stage 4：表单页生成器
 * IR fields → views/{entity}/form.vue
 */

import type { FormPageIR, FieldIR } from '../../ir/types';
import type { CompilerConfig } from './types';
import { registry } from '../../component-registry';

export function generateFormPage(ir: FormPageIR, config: CompilerConfig): string {
  const entity = capitalize(config.entity);
  const now = new Date().toISOString();
  const modalWidth = parseInt(ir.config.generalWidth);
  const labelCol = ir.config.labelWidth > 80 ? 6 : 4;
  const wrapperCol = 24 - labelCol;

  return `<!-- @jnpf-generated v${config.generatorVersion} entity=${config.entity} type=form-page -->
<!-- 生成时间：${now} -->
<!-- 此文件由 JNPF 代码生成器生成，可手动修改 -->

<template>
  <a-modal
    v-model:open="visible"
    :title="isEdit ? '编辑${config.entityLabel}' : '新增${config.entityLabel}'"
    :width="${isNaN(modalWidth) ? 800 : modalWidth}"
    @ok="handleSubmit"
    @cancel="handleCancel"
  >
    <a-form
      ref="formRef"
      :model="formData"
      :rules="rules"
      :label-col="{ span: ${labelCol} }"
      :wrapper-col="{ span: ${wrapperCol} }"
    >
${ir.fields.map(field => generateFieldTemplate(field)).join('\n')}
    </a-form>
    <!-- @jnpf-gen:insert-point=custom-form-fields -->
    <!-- @jnpf-gen:end-insert-point=custom-form-fields -->
  </a-modal>
</template>

<script setup lang="ts">
import { ref, reactive, watch, computed } from 'vue';
import { message } from 'ant-design-vue';
import type { FormInstance } from 'ant-design-vue';
import type { ${entity}Entity } from './types';
import { get${entity}Detail, create${entity}, update${entity} } from './api';
// @jnpf-gen:insert-point=custom-imports
// @jnpf-gen:end-insert-point=custom-imports

const props = defineProps<{
  visible: boolean;
  record?: ${entity}Entity;
}>();

const emit = defineEmits<{
  'update:visible': [value: boolean];
  success: [];
}>();

const visible = computed({
  get: () => props.visible,
  set: (val) => emit('update:visible', val),
});

const isEdit = computed(() => !!props.record?.id);
const formRef = ref<FormInstance>();

const formData = reactive<Record<string, unknown>>({
${ir.fields.map(f => `  ${f.model}: ${getDefaultValue(f)},`).join('\n')}
});

const rules = {
${ir.fields
  .filter(f => f.config.required)
  .map(f => `  ${f.model}: [{ required: true, message: '请输入${f.label}', trigger: '${f.validation?.[0]?.trigger || 'blur'}' }],`)
  .join('\n')}
};

watch(
  () => props.record,
  async record => {
    if (record?.id) {
      const res = await get${entity}Detail(record.id);
      Object.assign(formData, res.data);
    } else {
      resetForm();
    }
  },
  { immediate: true },
);

function resetForm() {
${ir.fields.map(f => `  formData.${f.model} = ${getDefaultValue(f)};`).join('\n')}
}

async function handleSubmit() {
  try {
    await formRef.value?.validateFields();
    if (isEdit.value) {
      await update${entity}(props.record!.id!, formData as any);
      message.success('更新成功');
    } else {
      await create${entity}(formData as any);
      message.success('创建成功');
    }
    emit('success');
  } catch {
    // 校验失败，不做处理
  }
}

function handleCancel() {
  resetForm();
  visible.value = false;
}

// @jnpf-gen:insert-point=custom-logic
// @jnpf-gen:end-insert-point=custom-logic
</script>
`;
}

function generateFieldTemplate(field: FieldIR): string {
  const entry = registry.resolve(field.component.jnpfKey);
  const pc = entry.pc;
  const required = field.config.required ? ' required' : '';
  const placeholder = field.config.placeholder || `请${pc.includes('select') ? '选择' : '输入'}${field.label}`;

  if (pc === 'a-select' || pc === 'a-cascader' || pc === 'a-tree-select') {
    return `      <a-form-item label="${field.label}" name="${field.model}"${required}>
        <${pc} v-model:value="formData.${field.model}" placeholder="${placeholder}" style="width: 100%" />
      </a-form-item>`;
  }

  if (pc === 'a-textarea') {
    return `      <a-form-item label="${field.label}" name="${field.model}"${required}>
        <${pc} v-model:value="formData.${field.model}" placeholder="${placeholder}" :rows="4" />
      </a-form-item>`;
  }

  if (pc === 'a-switch') {
    return `      <a-form-item label="${field.label}" name="${field.model}"${required}>
        <${pc} v-model:checked="formData.${field.model}" />
      </a-form-item>`;
  }

  if (pc === 'a-date-picker' || pc === 'a-time-picker') {
    return `      <a-form-item label="${field.label}" name="${field.model}"${required}>
        <${pc} v-model:value="formData.${field.model}" placeholder="${placeholder}" style="width: 100%" />
      </a-form-item>`;
  }

  if (pc === 'a-input-number') {
    return `      <a-form-item label="${field.label}" name="${field.model}"${required}>
        <${pc} v-model:value="formData.${field.model}" placeholder="${placeholder}" :min="${field.config.min ?? 'undefined'}" :max="${
      field.config.max ?? 'undefined'
    }" style="width: 100%" />
      </a-form-item>`;
  }

  return `      <a-form-item label="${field.label}" name="${field.model}"${required}>
        <a-input v-model:value="formData.${field.model}" placeholder="${placeholder}" />
      </a-form-item>`;
}

function getDefaultValue(field: FieldIR): string {
  if (field.config.defaultValue !== undefined && field.config.defaultValue !== null) {
    return JSON.stringify(field.config.defaultValue);
  }
  const typeMap: Record<string, string> = {
    JnpfInput: "''",
    JnpfTextarea: "''",
    JnpfInputNumber: '0',
    JnpfSwitch: 'false',
    JnpfDatePicker: "''",
    JnpfTimePicker: "''",
    JnpfRate: '0',
    JnpfSlider: '0',
    JnpfSelect: field.config.multiple ? '[]' : "''",
    JnpfRadio: "''",
    JnpfCheckbox: '[]',
    JnpfCascader: '[]',
    JnpfTreeSelect: "''",
    JnpfUploadImg: '[]',
    JnpfUploadFile: '[]',
    JnpfColorPicker: "''",
    JnpfEditor: "''",
  };
  return typeMap[field.component.jnpfKey] || "''";
}

function capitalize(s: string): string {
  return s.charAt(0).toUpperCase() + s.slice(1);
}
