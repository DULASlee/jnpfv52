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

// #ifdef H5
// 演示联调：强制 API 走 5000，避免旧发行包 baseURL 为空导致请求落在 3800 并被当成页面路由
define.baseURL = 'http://localhost:5000'
define.comUploadUrl = define.baseURL + '/api/file/Uploader/'
define.webSocketUrl = 'ws://localhost:5000/api/message/websocket'
// #endif

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