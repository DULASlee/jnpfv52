import request from '@/utils/request'
//获取常用语
export function getSelector() {
	return request({
		url: `/api/system/CommonWords/Selector?type=App`,
		method: 'get'
	})
}

// 获取常用语列表
export function commonWords(data) {
	return request({
		url: `/api/system/CommonWords`,
		method: 'get',
		data
	})
}
// 审批常用语新建
export function Create(data) {
	return request({
		url: '/api/system/CommonWords',
		method: 'post',
		data
	})
}
// 审批常用语编辑
export function Update(data) {
	return request({
		url: `/api/system/CommonWords/${data.id}`,
		method: 'put',
		data
	})
}
// 删除审批常用语详情
export function Delete(id) {
	return request({
		url: `/api/system/CommonWords/${id}`,
		method: 'DELETE'
	})
}