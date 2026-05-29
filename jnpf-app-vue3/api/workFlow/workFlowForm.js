import request from '@/utils/request'

// 新建表单
export function Create(data) {
	return request({
		url: `/api/workflow/task`,
		method: 'post',
		data,
		options: {
			load: true
		}
	})
}
// 修改表单
export function Update(data) {
	return request({
		url: `/api/workflow/task/${data.id}`,
		method: 'put',
		data
	})
}
//通过表单id获取流程id
export function getFormById(id) {
	return request({
		url: `/api/flowForm/Form/getFormById/${id}`,
		method: 'get'
	})
}
//查看发起表单
export function getStartFormInfo(id) {
	return request({
		url: `/api/workflow/task/ViewStartForm/${id}`,
		method: 'get'
	})
}