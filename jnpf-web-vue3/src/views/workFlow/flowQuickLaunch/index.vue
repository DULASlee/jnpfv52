<template>
  <div class="jnpf-content-wrapper bg-white">
    <FlowList @select="onSelect" />
    <FlowParser @register="registerFlowParser" />
  </div>
</template>

<script lang="ts" setup>
  import FlowList from './FlowList.vue';
  import { useLazyPopup } from '/@/hooks/web/useLazyPopup';
  const { component: FlowParser, register: registerFlowParser, openPopup: openFlowParser } = useLazyPopup(
    () => import('/@/views/workFlow/components/FlowParser.vue'),
    'workFlow/FlowParser',
  );


  defineOptions({ name: 'WorkFlowAddFlow' });

  function onSelect(record) {
    const data = {
      id: '',
      flowId: record.id,
      opType: '-1',
    };
    openFlowParser(true, data);
  }
</script>
