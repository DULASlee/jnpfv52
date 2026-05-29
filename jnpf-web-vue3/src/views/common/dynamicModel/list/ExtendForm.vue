<template>
  <div class="extend-form">
    <Parser ref="parserRef" :formConf="formConf" :key="key" v-if="loading" />
  </div>
</template>
<script lang="ts" setup>
  import { getConfigData, getModelInfo } from '/@/api/onlineDev/visualDev';
  import { reactive, toRefs, nextTick, ref } from 'vue';
  import { createAsyncComponent } from '/@/utils/factory/createAsyncComponent';
  import { useMessage } from '/@/hooks/web/useMessage';
  import { useUserStore } from '/@/store/modules/user';
  import dayjs from 'dayjs';
  import { getDateTimeUnit } from '/@/utils/jnpf';

  interface State {
    formConf: any;
    formData: any;
    config: any;
    loading: boolean;
    key: number;
    formOperates: any[];
  }

  const { createMessage } = useMessage();
  const userStore = useUserStore();
  const parserRef = ref<any>(null);
  const state = reactive<State>({
    formConf: {},
    formData: {},
    config: {},
    loading: false,
    key: +new Date(),
    formOperates: [],
  });
  const { formConf, key, loading } = toRefs(state);
  const Parser = createAsyncComponent(() => import('/@/components/FormGenerator/src/components/Parser.vue'));

  defineExpose({ init, requestDetails });

  function fillFormData(form, data) {
    const userInfo = userStore.getUserInfo;
    const currDate = new Date();
    const loop = (list, parent?) => {
      for (let i = 0; i < list.length; i++) {
        const item = list[i];
        if (item.__vModel__) {
          if (item.__config__.defaultCurrent) {
            if (item.__config__.jnpfKey === 'datePicker') {
              item.__config__.defaultValue = dayjs(currDate).startOf(getDateTimeUnit(item.format)).valueOf();
            }
            if (item.__config__.jnpfKey === 'timePicker') {
              item.__config__.defaultValue = dayjs(currDate).format(item.format || 'HH:mm:ss');
            }
            if (item.__config__.jnpfKey === 'organizeSelect' && userInfo.organizeIdList?.length) {
              item.__config__.defaultValue = item.multiple ? [userInfo.organizeIdList] : userInfo.organizeIdList;
            }
            if (item.__config__.jnpfKey === 'depSelect' && userInfo.departmentId) {
              item.__config__.defaultValue = item.multiple ? [userInfo.departmentId] : userInfo.departmentId;
            }
            if (item.__config__.jnpfKey === 'userSelect' && userInfo.userId) {
              item.__config__.defaultValue = item.multiple ? [userInfo.userId] : userInfo.userId;
            }
            if (item.__config__.jnpfKey === 'usersSelect' && userInfo.userId) {
              item.__config__.defaultValue = item.multiple ? [userInfo.userId + '--user'] : userInfo.userId + '--user';
            }
            if (item.__config__.jnpfKey === 'posSelect' && userInfo.positionIds?.length) {
              item.__config__.defaultValue = item.multiple ? userInfo.positionIds.map(o => o.id) : userInfo.positionIds[0].id;
            }
            if (item.__config__.jnpfKey === 'roleSelect' && userInfo.roleIds?.length) {
              item.__config__.defaultValue = item.multiple ? userInfo.roleIds : userInfo.roleIds[0];
            }
            if (item.__config__.jnpfKey === 'groupSelect' && userInfo.groupIds?.length) {
              item.__config__.defaultValue = item.multiple ? userInfo.groupIds : userInfo.groupIds[0];
            }
          }
          const val = Object.prototype.hasOwnProperty.call(data, item.__vModel__) ? data[item.__vModel__] : item.__config__.defaultValue;
          item.__config__.defaultValue = val;
          if (!state.config.isPreview && state.config.useFormPermission) {
            const id = item.__config__.isSubTable ? parent.__vModel__ + '-' + item.__vModel__ : item.__vModel__;
            let noShow = true;
            if (state.formOperates?.length) {
              noShow = !state.formOperates.some(o => o.enCode === id);
            }
            noShow = item.__config__.noShow ? item.__config__.noShow : noShow;
            item.__config__.noShow = noShow;
          }
        }
        if (item.__config__?.children && Array.isArray(item.__config__.children)) {
          loop(item.__config__.children, item);
        }
      }
    };
    loop(form.fields);
    form.formData = data;
  }

  function init(data) {
    state.config = data;
    state.formData = {};
    nextTick(() => {
      if (!state.config.modelId) return;
      getConfigData(state.config.modelId).then(res => {
        if (res.code !== 200 || !res.data) return createMessage.error(res.msg || '请求出错，请重试');
        if (!res.data.formData) return;
        const parsed = JSON.parse(res.data.formData);
        const fields = parsed.fields;
        state.formConf = { ...parsed, fields, labelWidth: 80 };
        state.loading = true;
      });
    });
  }

  function requestDetails(data) {
    if (!data?.modelId || data.id === undefined) return;
    getModelInfo(data.modelId, data.id).then(res => {
      if (!res.data?.data) {
        state.formData = {};
        state.loading = true;
        state.key = +new Date();
        return;
      }
      state.formData = JSON.parse(res.data.data);
      fillFormData(state.formConf, state.formData);
      nextTick(() => {
        state.loading = true;
        state.key = +new Date();
      });
    });
  }
</script>
