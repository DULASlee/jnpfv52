<!-- @jnpf-generated v1.0.0 entity=student type=form-page -->
<!-- 生成时间：2026-06-16T06:22:20.748Z -->
<!-- 此文件由 JNPF 代码生成器生成，可手动修改 -->

<template>
  <a-modal
    v-model:open="visible"
    :title="isEdit ? '编辑学生管理' : '新增学生管理'"
    :width="800"
    @ok="handleSubmit"
    @cancel="handleCancel"
  >
    <a-form
      ref="formRef"
      :model="formData"
      :rules="rules"
      :label-col="{ span: 6 }"
      :wrapper-col="{ span: 18 }"
    >
      <a-form-item label="姓名" name="employeeName" required>
        <a-input v-model:value="formData.employeeName" placeholder="请输入姓名" />
      </a-form-item>
      <a-form-item label="性别" name="gender" required>
        <a-input v-model:value="formData.gender" placeholder="请输入性别" />
      </a-form-item>
      <a-form-item label="出生日期" name="birthDate">
        <a-date-picker v-model:value="formData.birthDate" placeholder="请输入出生日期" style="width: 100%" />
      </a-form-item>
      <a-form-item label="邮箱" name="email">
        <a-input v-model:value="formData.email" placeholder="请输入邮箱" />
      </a-form-item>
      <a-form-item label="手机号" name="phone">
        <a-input v-model:value="formData.phone" placeholder="请输入手机号" />
      </a-form-item>
      <a-form-item label="部门" name="department" required>
        <a-input v-model:value="formData.department" placeholder="请输入部门" />
      </a-form-item>
      <a-form-item label="岗位" name="position">
        <a-input v-model:value="formData.position" placeholder="请输入岗位" />
      </a-form-item>
      <a-form-item label="直属上级" name="manager">
        <a-input v-model:value="formData.manager" placeholder="请输入直属上级" />
      </a-form-item>
      <a-form-item label="入职日期" name="entryDate" required>
        <a-date-picker v-model:value="formData.entryDate" placeholder="请输入入职日期" style="width: 100%" />
      </a-form-item>
      <a-form-item label="薪资" name="salary">
        <a-input-number v-model:value="formData.salary" placeholder="请输入薪资" :min="undefined" :max="undefined" style="width: 100%" />
      </a-form-item>
      <a-form-item label="是否试用期" name="isProbation">
        <a-switch v-model:checked="formData.isProbation" />
      </a-form-item>
      <a-form-item label="技能标签" name="skills">
        <a-input v-model:value="formData.skills" placeholder="请输入技能标签" />
      </a-form-item>
      <a-form-item label="头像" name="avatar">
        <a-input v-model:value="formData.avatar" placeholder="请输入头像" />
      </a-form-item>
      <a-form-item label="简历附件" name="resume">
        <a-input v-model:value="formData.resume" placeholder="请输入简历附件" />
      </a-form-item>
    </a-form>
    <!-- @jnpf-gen:insert-point=custom-form-fields -->
    <!-- @jnpf-gen:end-insert-point=custom-form-fields -->
  </a-modal>
</template>

<script setup lang="ts">
import { ref, reactive, watch, computed } from 'vue';
import { message } from 'ant-design-vue';
import type { FormInstance } from 'ant-design-vue';
import type { StudentEntity } from './types';
import { getStudentDetail, createStudent, updateStudent } from './api';
// @jnpf-gen:insert-point=custom-imports
// @jnpf-gen:end-insert-point=custom-imports

const props = defineProps<{
  visible: boolean;
  record?: StudentEntity;
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
  employeeName: '',
  gender: '',
  birthDate: '',
  email: '',
  phone: '',
  department: '',
  position: '',
  manager: '',
  entryDate: '',
  salary: 0,
  isProbation: true,
  skills: [],
  avatar: [],
  resume: [],
});

const rules = {
  employeeName: [{ required: true, message: '请输入姓名', trigger: 'blur' }],
  gender: [{ required: true, message: '请输入性别', trigger: 'blur' }],
  department: [{ required: true, message: '请输入部门', trigger: 'blur' }],
  entryDate: [{ required: true, message: '请输入入职日期', trigger: 'blur' }],
};

watch(
  () => props.record,
  async record => {
    if (record?.id) {
      const res = await getStudentDetail(record.id);
      Object.assign(formData, res.data);
    } else {
      resetForm();
    }
  },
  { immediate: true },
);

function resetForm() {
  formData.employeeName = '';
  formData.gender = '';
  formData.birthDate = '';
  formData.email = '';
  formData.phone = '';
  formData.department = '';
  formData.position = '';
  formData.manager = '';
  formData.entryDate = '';
  formData.salary = 0;
  formData.isProbation = true;
  formData.skills = [];
  formData.avatar = [];
  formData.resume = [];
}

async function handleSubmit() {
  try {
    await formRef.value?.validateFields();
    if (isEdit.value) {
      await updateStudent(props.record!.id!, formData as any);
      message.success('更新成功');
    } else {
      await createStudent(formData as any);
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
