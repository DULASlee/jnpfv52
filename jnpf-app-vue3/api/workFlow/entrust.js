import request from '@/utils/request'

// 获取流程委托列表
export function FlowDelegateList(data) {
	return request({
		url: `/api/workflow/delegate`,
		method: 'get',
		data
	})
}
// 获取流程委托信息
export function FlowDelegateInfo(id) {
	return request({
		url: `/api/workflow/delegate/${id}`,
		method: 'get'
	})
}
// 删除流程委托
export function DeleteDelagate(id) {
	return request({
		url: `/api/workflow/delegate/${id}`,
		method: 'DELETE'
	})
}
// 新建流程委托
export function Create(data) {
	return request({
		url: `/api/workflow/delegate`,
		method: 'post',
		data
	})
}
// 更新流程委托
export function Update(data) {
	return request({
		url: `/api/workflow/delegate/${data.id}`,
		method: 'PUT',
		data
	})
}

// 获取流程的所有委托人
export function getDelegateUser(id) {
	return request({
		url: `/api/workflow/delegate/UserList?templateId=${id}`,
		method: 'get'
	})
}

// 获取一个委托终止
export function entrustStop(id) {
	return request({
		url: `/api/workflow/delegate/Stop/${id}`,
		method: 'put',
		data: {}
	})
}

// 获取用户下拉框列表
export const getListByAuthorize = (organizeId, keyword) => {
	return request({
		url: `/api/permission/Users/GetListByAuthorize/${organizeId}`,
		method: 'post',
		data: {
			keyword
		}
	})
}
// 获取用户下拉框列表
export function getPrincipalDetails(id) {
	return request({
		url: `/api/workflow/delegate/Info/${id}`,
		method: 'get'

	})
}
// 接受委托
export function entrustHandle(id, data) {
	return request({
		url: `/api/workflow/delegate/Notarize/${id}?type=${data.type}`,
		method: 'post',
		data: {}
	})
}