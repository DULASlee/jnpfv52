<template>
  <div class="ai-wb">
    <div class="wb-bar"
      ><h2>AI 对话工作台</h2>
      <a-space>
        <a-select v-model:value="targets" mode="multiple" size="small" style="width: 240px" placeholder="编译目标">
          <a-select-option value="vue3-web">Vue3 Web</a-select-option>
          <a-select-option value="uniapp-weixin">微信小程序</a-select-option>
          <a-select-option value="uniapp-h5">H5 移动端</a-select-option>
          <a-select-option value="dashboard">大屏</a-select-option>
          <a-select-option value="workflow">工作流</a-select-option>
        </a-select>
        <a-switch v-model:checked="aiMode" checked-children="AI" un-checked-children="专家" />
      </a-space>
    </div>
    <div class="wb-body">
      <div class="wb-left"
        ><a-steps direction="vertical" :current="stage" size="small"
          ><a-step v-for="s in ['需求', '架构', '设计', '生成', '交付']" :key="s" :title="s" /></a-steps
      ></div>
      <div class="wb-chat" ref="cr"
        ><div v-for="(m, i) in msgs" :key="i" :class="['msg', m.r]">
          <!-- eslint-disable-next-line vue/no-v-html -->
          <div v-html="md(m.c)"></div>
          <div class="t">{{ m.t }}</div></div
        >
        <div v-if="cres.length" class="cr-box"
          ><a-card size="small" title="编译结果"
            ><a-tag v-for="r in cres" :key="r.n" :color="r.ok ? 'green' : 'red'">{{ r.n }}:{{ r.f }}文件</a-tag></a-card
          ></div
        >
      </div>
      <div class="wb-right"
        ><a-textarea v-model:value="inp" :rows="2" placeholder="描述需求..." /><a-space style="margin-top: 8px">
          <a-button type="primary" @click="send" :loading="ld">发送</a-button>
          <a-button v-if="stage >= 3" @click="compile" :loading="cl">编译</a-button>
          <a-button v-if="stage >= 4" type="primary" @click="dl">下载ZIP</a-button></a-space
        ></div
      >
    </div>
  </div>
</template>
<script setup lang="ts">
  import { ref, nextTick } from 'vue';
  import { message } from 'ant-design-vue';
  import { useCompile } from '/@/core/ai/integration/use-compile';
  import type { CompileTarget } from '/@/core/compiler/targets';
  import type { FormPageIR } from '/@/core/ir/types';
  const stage = ref(0);
  const aiMode = ref(true);
  const inp = ref('');
  const ld = ref(false);
  const cl = ref(false);
  const msgs = ref<Array<{ r: string; c: string; t: string }>>([]);
  const cr = ref<HTMLDivElement>();
  const targets = ref<CompileTarget[]>(['vue3-web']);
  const cres = ref<Array<{ n: string; ok: boolean; f: number }>>([]);
  const { compileMulti, download } = useCompile();
  const resps = ['**需求分析完成**', '**架构设计完成**', '**UI/DB设计完成**', '**编译就绪**', '**交付就绪**'];
  function md(t: string) {
    return t.replace(/\*\*(.+?)\*\*/g, '<b>$1</b>').replace(/\n/g, '<br>');
  }
  function now() {
    return new Date().toLocaleTimeString('zh-CN', { hour: '2-digit', minute: '2-digit' });
  }
  async function send() {
    const t = inp.value.trim();
    if (!t || ld.value) return;
    msgs.value.push({ r: 'user', c: t, t: now() });
    inp.value = '';
    ld.value = true;
    await new Promise(r => setTimeout(r, 300));
    msgs.value.push({ r: 'assistant', c: resps[stage.value] ?? resps[0], t: now() });
    ld.value = false;
    nextTick(() => {
      if (cr.value) cr.value.scrollTop = cr.value.scrollHeight;
    });
  }
  async function compile() {
    cl.value = true;
    try {
      const ir: FormPageIR = {
        type: 'form',
        id: 'wb',
        name: 'Page',
        config: {
          labelPosition: 'right',
          labelWidth: 100,
          labelSuffix: '：',
          size: 'default',
          disabled: false,
          span: 24,
          gutter: 16,
          colon: true,
          popupType: 'general',
          generalWidth: '800px',
          fullScreenWidth: '100%',
          drawerWidth: '520px',
          hasCancelBtn: true,
          cancelButtonText: '取消',
          hasConfirmBtn: true,
          confirmButtonText: '保存',
          hasConfirmAndAddBtn: false,
          hasPrintBtn: false,
          printButtonText: '打印',
          primaryKeyPolicy: 'snowflake',
          tablePolicy: 'auto',
          concurrencyLock: false,
          logicalDelete: true,
        },
        fields: [
          {
            id: 'f1',
            model: 'name',
            label: '名称',
            component: { jnpfKey: 'JnpfInput', pc: 'a-input', app: 'uni-easyinput', legacyApp: 'uni-easyinput' },
            config: {
              required: true,
              defaultValue: '',
              placeholder: '请输入',
              disabled: false,
              readonly: false,
              hidden: false,
              span: 12,
              labelWidth: null,
              maxlength: 100,
              showWordLimit: true,
              clearable: true,
              min: null,
              max: null,
              precision: null,
              step: null,
              multiple: false,
              options: [],
              dictType: null,
              relationData: null,
              style: {},
            },
            validation: [],
            events: {},
          },
        ],
        databaseFields: [],
        expressions: [],
        aiHints: { domain: 'wb' },
      };
      const r = await compileMulti({ entity: 'wb_page', name: 'WB Page', ir }, targets.value);
      cres.value = r.results.map(x => ({ n: x.target, ok: x.success, f: x.response.project?.size ?? 0 }));
      msgs.value.push({ r: 'assistant', c: `**编译完成** ${r.successCount}/${r.totalTargets}成功`, t: now() });
      stage.value = 4;
    } catch (e) {
      message.error(`编译失败:${(e as Error).message}`);
    } finally {
      cl.value = false;
    }
  }
  async function dl() {
    try {
      await download({ entity: 'proj', name: 'AI Project', ir: {} as never }, targets.value);
      message.success('ZIP已触发');
    } catch (e) {
      message.error(`下载失败:${(e as Error).message}`);
    }
  }
</script>
<style lang="less" scoped>
  .ai-wb {
    height: 100vh;
    display: flex;
    flex-direction: column;
    background: #f0f2f5;
  }
  .wb-bar {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 8px 16px;
    background: #fff;
    h2 {
      margin: 0;
      font-size: 16px;
    }
  }
  .wb-body {
    flex: 1;
    display: flex;
    overflow: hidden;
    padding: 12px;
    gap: 12px;
  }
  .wb-left {
    width: 140px;
    flex-shrink: 0;
    background: #fff;
    border-radius: 4px;
    padding: 12px;
  }
  .wb-chat {
    flex: 1;
    background: #fff;
    border-radius: 4px;
    padding: 16px;
    overflow-y: auto;
  }
  .wb-right {
    width: 300px;
    flex-shrink: 0;
    background: #fff;
    border-radius: 4px;
    padding: 12px;
  }
  .msg {
    margin-bottom: 12px;
    padding: 8px 12px;
    border-radius: 6px;
    &.user {
      background: #e6f7ff;
    }
    &.assistant {
      background: #f6ffed;
    }
    .t {
      font-size: 11px;
      color: #8c8c8c;
      margin-top: 4px;
    }
  }
  .cr-box {
    margin-top: 12px;
  }
</style>
