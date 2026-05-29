import request from '@/utils/request'
// 获取列表表单配置JSON
export function getConfigData(modelId, type) {
	return request({
		url: `/api/visualdev/OnlineDev/${modelId}/Config?type=${type}`,
		method: 'GET'
	})
}
// 获取数据列表
export function getModelList(modelId, data, options) {
	return request({
		url: `/api/visualdev/OnlineDev/${modelId}/List`,
		method: 'POST',
		data,
		options: {
			load: false
		}
	})
}
// 添加数据
export function createModel(modelId, data) {
	return request({
		url: `/api/visualdev/OnlineDev/${modelId}`,
		method: 'POST',
		data
	})
}
// 修改数据
export function updateModel(modelId, data) {
	return request({
		url: `/api/visualdev/OnlineDev/${modelId}/${data.id}`,
		method: 'PUT',
		data
	})
}
// 获取数据信息
export function getModelInfo(modelId, id) {
	return request({
		url: `/api/visualdev/OnlineDev/${modelId}/${id}`,
		method: 'GET'
	})
}
// 删除数据
export function deteleModel(data, id) {
	return request({
		url: `/api/visualdev/OnlineDev/batchDelete/${id}`,
		method: 'POST',
		data
	})
}
// 表单外链
export function getConfig(id) {
	return request({
		url: `/api/visualdev/OnlineDev/${id}/Config`,
		method: 'GET'
	})
}
// 修改记录
export function getOnlineLog(modelId, id) {
	return request({
		url: `/api/visualdev/OnlineLog?modelId=${modelId}&dataId=${id}`,
		method: 'GET'
	})
}
//自定义按钮发起流程
export function launchFlow(data, id) {
	return request({
		url: `/api/visualdev/OnlineDev/${id}/actionLaunchFlow`,
		method: 'POST',
		data
	})
}
// 获取关联表单数据详情
export function getDataChange(data, modelId) {
	return request({
		url: `/api/visualdev/OnlineDev/${modelId}/DataChange`,
		method: 'post',
		data
	})
}