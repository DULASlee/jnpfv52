import request from '@/utils/request'
//门户列表
export function PortalList() {
	return request({
		url: `/api/visualdev/Portal/Selector?platform=App&type=1`,
		method: 'get',
		options: {
			load: false
		}
	})
}
//更新门户
export function SetPortal(id) {
	return request({
		url: `/api/visualdev/Portal/${id}/Actions/SetDefault?platform=App`,
		method: 'put',
		options: {
			load: false
		}
	})
}
//门户列表切换后列表
export function auth(id) {
	return request({
		url: `/api/visualdev/Portal/${id}/auth?platform=App`,
		method: 'get',
		options: {
			load: false
		}
	})
}
//获取门户地图数据
export function geojson(code) {
	return request({
		url: `/api/system/atlas/geojson?code=${code}&hasChildren=true`,
		method: 'get',
		options: {
			load: false
		}
	})
}
//获取省市区树
export function getAtlas() {
	return request({
		url: `/api/system/atlas`,
		method: 'get',
		options: {
			load: false
		}
	})
}

// 获取发送配置列表列表
export const getMsgTemplate = data => {
	return request({
		url: '/api/message/SendMessageConfig/getSendConfigList',
		method: 'GET',
		data,
		options: {
			load: false
		}
	})
}
// 预览
export const getPreviewPortal = id => {
	return request({
		url: '/api/visualdev/Portal/' + id,
		method: 'GET',
		options: {
			load: false
		}
	})
}