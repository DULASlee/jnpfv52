import AvueData from '@/page/view.vue';
import registerConfig from '@/registerConfig';
import registerLib from '@/registerLib';
import DataVVue3 from '@kjgl77/datav-vue3'

const install = (Vue, opt) => {
  Vue.component('avue-data', AvueData)
  opt.app.use(DataVVue3)
  registerLib(Vue);
}

// 如果是直接引入的
if (typeof window !== 'undefined' && window.Vue) {
  install(window.Vue)
}

export default {
  install,
  registerConfig
}

export {
  install,
  registerConfig
}