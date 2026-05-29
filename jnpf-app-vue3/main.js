import App from './App'
import store from './store'
import uView from './uni_modules/vk-uview-ui';
import share from '@/utils/share'
import permission from '@/libs/permission'
import define from '@/utils/define'
import request from '@/utils/request'
import jnpf from '@/utils/jnpf'
import {
	setupI18n
} from '@/locale/setupI18n';

// #ifndef VUE3
import Vue from 'vue'
import './uni.promisify.adaptor'
Vue.config.productionTip = false
App.mpType = 'app'
// 添加实例属性
Object.assign(Vue.prototype, {
	define,
	request,
	jnpf,
	$permission: permission,
	$store: store
})

Vue.use(uView)
Vue.mixin(share)

const app = new Vue({
	...App
})
app.$mount()
// #endif

// #ifdef VUE3
import {
	createSSRApp
} from 'vue'
export function createApp() {
	const app = createSSRApp(App)

	app.use(store)
	app.use(uView)
	app.mixin(share)
	setupI18n(app);

	app.config.globalProperties.$permission = permission
	app.config.globalProperties.define = define
	app.config.globalProperties.request = request
	app.config.globalProperties.jnpf = jnpf

	return {
		app
	}
}
// #endif