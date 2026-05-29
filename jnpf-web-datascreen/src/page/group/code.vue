<template>
  <el-dialog v-model="innerVisible" :close-on-click-modal="false" :before-close="handleClose"
    class="avue-dialog" :title="title || '数据处理'" width="80%">
    <other-list>
      <div class="item">
        <el-button type="primary" @click="$refs.codeTip.open()"
          icon="el-icon-notebook">说明</el-button>
      </div>
      &nbsp;&nbsp;
      <div class="item">
        <el-button icon="el-icon-edit" type="primary" v-if="isCodeEdit"
          @click="$refs.code.handleOpen()">放大编辑器</el-button>
      </div>
    </other-list>
    <monaco-editor v-model="code" :language="language"></monaco-editor>
    <span class="avue-dialog__footer avue-dialog__footer--right">
      <el-button @click="setVisible(false)">取 消</el-button>
      <el-button type="primary" @click="submit">确 定</el-button>
    </span>
    <code-tip ref="codeTip" :type="type"></code-tip>
    <code-edit ref="code" v-model="form"></code-edit>
  </el-dialog>
</template>

<script>
import codeEdit from '@/page/components/code-edit.vue'
import { funEval } from "@/utils/utils";
import otherList from './other-list.vue'
import MonacoEditor from "@/page/components/monaco-editor";
import codeTip from "./code-tip.vue";
export default {
  inject: ["contain"],
  components: { codeEdit, MonacoEditor, codeTip, otherList },
  provide() {
    return {
      contain: this.contain,
    };
  },
  data() {
    return {
      form: {
        content: '',
        type: ''
      },
      code: "",
      innerVisible: false,
    };
  },
  props: {
    rules: {
      type: Boolean,
      default: true,
    },
    language: {
      type: String,
      default: "javascript",
    },
    title: String,
    visible: Boolean,
    type: String,
    modelValue: [String, Object, Array],
  },
  computed: {
    isCodeEdit() {
      let prop = this.contain.activeComponent.prop
      return ['vue', 'common', 'html'].includes(prop)
    }
  },
  watch: {
    code(val) {
      let prop = this.contain.activeComponent.prop
      if (prop == 'vue') {
        this.form.type = 0
      } else if (prop == 'common') {
        this.form.type = 1
      } else if (prop == 'html') {
        this.form.type = 2
      } else {
        this.form.type = ''
      }
      this.form.content = val
    },
    modelValue: {
      handler(val) {
        if (this.validatenull(val)) {
          if (
            ["dataFormatter", "stylesFormatter"].includes(this.type) &&
            this.validatenull(val)
          ) {
            this.code = `(data,params,refs)=>{
    return {}
}`;
          } else if (
            ["query", "header", "dataQuery", "dataHeader"].includes(
              this.type
            ) &&
            this.validatenull(val)
          ) {
            this.code = `(data)=>{
    return {}
}`;
          } else if (
            ["echartFormatter"].includes(this.type) &&
            this.validatenull(val)
          ) {
            this.code = `(data)=>{
    const myChart = this.myChart;
    const option={}
    return option
}`;
          } else if (['clickFormatter', 'dblClickFormatter', 'mouseEnterFormatter', 'mouseLeaveFormatter', 'dataBeforeFormatter', 'dataAfterFormatter'].includes(this.type) && this.validatenull(val)) {
            this.code = `(params,refs)=>{
    console.log(params,refs)
}`;
          } else if (
            ["labelFormatter", "formatter"].includes(this.type) &&
            this.validatenull(val)
          ) {
            this.code = `(name,data)=>{
    console.log(name,data)
    return ''
  }`
          } else if (['before'].includes(this.type) && this.validatenull(val)) {
            this.code = `(data)=>{
    return new Promise(resolve=>{
      resolve()
    })
}`;
          } else {
            this.code = val;
          }
        } else {
          this.code = val;
        }
      },
      immediate: true,
      deep: true,
    },
    visible: {
      handler(val) {
        this.innerVisible = val;
      },
      immediate: true,
    },
  },
  methods: {
    handleClose() {
      this.setVisible(false);
    },
    submit() {
      let value = this.code;
      if (!this.rules) {
        this.$emit("submit", value);
        this.setVisible(false);
      } else if (typeof value == 'object' || this.type == 'style') {
        this.$emit("submit", value);
        this.setVisible(false);
      } else {
        try {
          funEval(value);
          if (['data', 'column'].includes(this.type)) value = funEval(value);
          this.$emit('submit', value);
          this.setVisible(false)
        } catch (error) {
          console.log(error);
          this.$message.error('数据格式有误')
        }
      }
    },
    setVisible(val) {
      this.$emit("update:visible", val);
    },
  },
};
</script>