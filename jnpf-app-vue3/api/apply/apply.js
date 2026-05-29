import request from '@/utils/request'
// 获取应用菜单
export function getMenuList(data) {
	return request({
		url: '/api/app/Menu',
		method: 'get',
		data,
		options: {
			load: false
		}
	})
}
// 获取常用 1-流程 2-应用
export function getUsualList() {
	return request({
		url: '/api/system/MenuData',
		method: 'get',
		options: {
			load: false
		}
	})
}
//流程常用更多
export function getCommonFlowTree(id) {
	return request({
		url: `/api/workflow/template/CommonFlowTree`,
		method: 'get'
	})
}
// 获取常用流程
export function getFlowUsualList(data) {
	return request({
		url: '/api/workflow/template/Selector',
		method: 'get',
		data,
		options: {
			load: false
		}
	})
}
// 获取常用全部 1-流程 2-应用
export function getAppDataList(data) {
	return request({
		url: '/api/system/MenuData/getAppDataList',
		data,
		options: {
			load: false
		}
	})
}
// 获取添加流程
export function setCommonFlow(id) {
	return request({
		url: `/api/workflow/template/SetCommonFlow/${id}`,
		method: 'post'
	})
}
export function addUsual(id) {
	return request({
		url: `/api/system/MenuData/${id}`,
		method: 'post'
	})
}
export function delUsual(id) {
	return request({
		url: `/api/system/MenuData/${id}`,
		method: 'delete'
	})
}
export function getFlowList(data) {
	return request({
		url: '/api/workflow/template/Selector',
		data
	})
}
export function getDataList(data) {
	return request({
		url: `/api/system/MenuData/getDataList`,
		data
	})
}
export function getChildList(id) {
	return request({
		url: `/api/app/Menu/getChildList/${id}`
	})
}
export function getChildMenuList(data) {
	return request({
		url: '/api/app/Menu/getMenuList?keyword=' + data.keyword,
		data
	})
}