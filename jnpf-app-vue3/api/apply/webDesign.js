import request from '@/utils/request'
// 获取列表表单配置JSON
export function getConfig(modelId, encryption) {
	return request({
		url: `/api/visualdev/ShortLink/${modelId}/Config?encryption=${encryption}`,
		method: 'GET'
	})
}
// 获取数据详情
export function getDataChange(modelId, id, encryption) {
	return request({
		url: `/api/visualdev/ShortLink/${modelId}/${id}/DataChange` + (encryption ? '?encryption=' +
			encryption : ''),
		method: 'GET'
	})
}
export function createModel(modelId, data, encryption) {
	return request({
		url: `/api/visualdev/ShortLink/${modelId}?encryption=${encryption}`,
		method: 'POST',
		data
	})
}
// 表单外链表单信息
export function getShortLink(id, encryption) {
	return request({
		url: `/api/visualdev/ShortLink/getConfig/${id}?encryption=${encryption}`,
		method: 'GET'
	})
}
//表单外链密码验证
export function checkPwd(data) {
	return request({
		url: `/api/visualdev/ShortLink/checkPwd`,
		method: 'POST',
		data
	})
}
// 表单外链列表
export function listLink(id, data, encryption) {
	return request({
		url: `/api/visualdev/ShortLink/${id}/ListLink?encryption=${encryption}`,
		method: 'POST',
		data
	})
}
