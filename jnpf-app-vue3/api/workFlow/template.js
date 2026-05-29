import request from '@/utils/request'

// 获取发起
export function getFlowLaunchList(data) {
	return request({
		url: `/api/workflow/task`,
		method: 'get',
		data,
		options: {
			load: false
		}
	})
}
// 删除流程发起
export function delFlowLaunch(id) {
	return request({
		url: '/api/workflow/task/' + id,
		method: 'delete',
	});
}
// 获取待签
export function getOperatorList(data) {
	return request({
		url: `/api/workflow/operator/List/${data.category}`,
		method: 'get',
		data,
		options: {
			load: false
		}
	})
}



// 获取流程发起列表
// export function TreeList() {
// 	return request({
// 		url: `/api/workflow/template/TreeList`,
// 		method: 'get'
// 	})
// }

// 获取流程发起列表
// export function OperatorList() {
// 	return request({
// 		url: `api/workflow/operator/List/0`,
// 		method: 'get'
// 	})
// }