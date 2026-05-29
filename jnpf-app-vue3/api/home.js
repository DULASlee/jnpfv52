import request from '@/utils/request'


// 获取通知公告
export function getNotice(data) {
	return request({
		url: '/api/visualdev/Dashboard/Notice',
		method: 'post',
		data,
		options: {
			load: false
		}
	})
}
// 获取未读邮件
export function getEmail() {
	return request({
		url: '/api/visualdev/Dashboard/Email',
		method: 'get',
		options: {
			load: false
		}
	})
}
// 获取待办事项
export function getFlowTodo(type) {
	return request({
		url: `/api/visualdev/Dashboard/FlowTodo?type=${type}`,
		method: 'get',
		options: {
			load: false
		}
	})
}
// 获取我的待办事项
export function getMyFlowTodo() {
	return request({
		url: '/api/visualdev/Dashboard/MyFlowTodo',
		method: 'get',
		options: {
			load: false
		}
	})
}