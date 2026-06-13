<template>
  <BasicDrawer v-bind="$attrs" width="500px" @register="registerDrawer" title="操作详情" destroyOnClose>
    <a-form class="!mx-20px !mt-20px" :colon="false" :model="dataForm" :labelCol="{ style: { width: '80px' } }">
      <a-form-item label="操作人">
        <p>{{ dataForm.userName }}</p>
      </a-form-item>
      <a-form-item label="操作类型">
        <p>{{ dataForm.operationType }}</p>
      </a-form-item>
      <a-form-item label="操作时间">
        <p>{{ formatToDateTime(dataForm.creatorTime, 'YYYY-MM-DD HH:mm:ss') }}</p>
      </a-form-item>
      <a-form-item label="IP地址">
        <p>{{ dataForm.ipAddress }}</p>
      </a-form-item>
      <a-form-item label="操作结果">
        <a-tag :color="dataForm.operationResult == 1 ? 'success' : 'error'">
          {{ dataForm.operationResult == 1 ? '成功' : '失败' }}
        </a-tag>
      </a-form-item>
      <a-form-item label="操作模块">
        <p>{{ dataForm.moduleName }}</p>
      </a-form-item>
      <a-form-item label="请求方式">
        <p>{{ dataForm.requestMethod }}</p>
      </a-form-item>
      <a-form-item label="请求地址">
        <p>{{ dataForm.requestUrl }}</p>
      </a-form-item>
      <a-form-item label="耗时(毫秒)">
        <p>{{ dataForm.requestDuration }}</p>
      </a-form-item>
      <a-collapse v-model:activeKey="activeName" ghost expandIconPosition="right">
        <a-collapse-panel key="1" header="请求参数">
          <div class="jnpf-code-box" v-if="dataForm.requestParam">{{ dataForm.requestParam }}</div>
        </a-collapse-panel>
        <a-collapse-panel key="2" header="返回结果">
          <div class="jnpf-code-box" v-if="dataForm.jsons">{{ dataForm.jsons }}</div>
        </a-collapse-panel>
      </a-collapse>
    </a-form>
  </BasicDrawer>
</template>
<script lang="ts" setup>
  import { reactive, toRefs } from 'vue';
  import { getOperationLogInfo } from '/@/api/extend/operationLog';
  import { BasicDrawer, useDrawerInner } from '/@/components/Drawer';
  import { formatToDateTime } from '/@/utils/dateUtil';

  const state = reactive({
    activeName: '',
    dataForm: {} as any,
  });
  const { dataForm, activeName } = toRefs(state);
  const [registerDrawer] = useDrawerInner(init);

  function init(data) {
    getOperationLogInfo(data.id).then(res => {
      state.dataForm = res.data;
    });
  }
</script>
<style lang="less" scoped>
  .ant-collapse {
    border-top: 1px solid @border-color-base1;
    .ant-collapse-item {
      border-bottom: 1px solid @border-color-base1;
    }
  }
  .jnpf-code-box {
    background: #848484;
    padding: 15px;
    color: #fff;
    font-size: 12px;
    border-radius: 4px;
  }
</style>
