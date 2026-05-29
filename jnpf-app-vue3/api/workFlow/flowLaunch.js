import request from '@/utils/request'
// 撤销
export function Revoke(id, data) {
	return request({
		url: `/api/workflow/task/Revoke/${id}`,
		method: 'PUT',
		data
	})
}
// 发起催办
export function Press(id) {
	return request({
		url: `/api/workflow/task/Press/${id}`,
		method: 'post'
	})
}