import define from './define'
import {
	useLocale
} from '@/locale/useLocale';

const {
	getBackLocale
} = useLocale();
const host = define.baseURL
const defaultOpt = {
	load: true
}

// 示例
// async xxxx(code) {
//   var res = await this.request({
// 		url: '/api/System/DictionaryData/All',
// 		method: 'GET',
// 		data,
// 		options: {
// 			load: false
// 		}
// 	})
//   if (!res) return
//   console.log(res)
// }

function request(config) {
	config.options = Object.assign(defaultOpt, config.options)
	const token = uni.getStorageSync('token') || ''
	const locale = getBackLocale()
	let header = {
		"Content-Type": "application/json;charset=UTF-8",
		"jnpf-origin": "app",
		"vue-version": "3",
		"Accept-Language": locale,
		...config.header
	}
	if (token) header['Authorization'] = token
	let url = config.url.indexOf('http') > -1 ? config.url : host + config.url
	let body = config.data || null
	const contentType = (header['Content-Type'] || header['content-type'] || '').toLowerCase()
	if (
		body &&
		typeof body === 'object' &&
		contentType.includes('application/x-www-form-urlencoded')
	) {
		body = Object.keys(body)
			.map((key) => `${encodeURIComponent(key)}=${encodeURIComponent(body[key] ?? '')}`)
			.join('&')
	}

	if (config.options.load) {
		uni.showLoading({
			title: config.options.loadText || '正在加载'
		})
	}

	return new Promise((resolve, reject) => {
		const showLoad = config.options.load
		uni.request({
			url: url,
			data: body,
			method: (config.method || 'GET').toUpperCase(),
			header: header,
			timeout: define.timeout,
			success: res => {
				if (showLoad) uni.hideLoading()
				if (res.statusCode === 200) {
					if (res.data && res.data.code == 200) {
						resolve(res.data)
					} else {
						ajaxError(res.data || {})
						reject((res.data && res.data.msg) || '请求失败')
					}
				} else {
					const errMsg = (res.data && res.data.msg) || `HTTP ${res.statusCode}`
					ajaxError(res.data || { msg: errMsg })
					reject(errMsg)
				}
			},
			fail: err => {
				if (showLoad) uni.hideLoading()
				uni.showToast({
					title: '连接服务器失败',
					icon: 'none',
				})
				reject(err.errMsg || err)
			}
		})
	})
}

function ajaxError(data) {
	uni.showToast({
		title: data.msg || '请求出错，请重试',
		icon: 'none',
		complete() {
			if (data.code === 600 || data.code === 601 || data.code === 602) {
				setTimeout(() => {
					uni.removeStorageSync('token')
					uni.removeStorageSync('cid')
					uni.removeStorageSync('userInfo')
					uni.removeStorageSync('permissionList')
					uni.removeStorageSync('sysVersion')
					uni.removeStorageSync('dynamicModelExtra')
					uni.reLaunch({
						url: '/pages/login/index'
					})
				}, 1500)
			}
		}
	})
}

export default request