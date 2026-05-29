import request from '@/utils/request'

// 获取流程引擎列表
export function FlowEngineList(data) {
	return request({
		url: `/api/workflow/Engine/flowTemplate`,
		method: 'get',
		data
	})
}
// 获取流程引擎信息
export function FlowEngineInfo(id) {
	return request({
		url: `/api/workflow/Engine/flowTemplate/${id}`,
		method: 'get'
	})
}

//获取流程引擎分页
export function getFlowSelector(data) {
	return request({
		url: `/api/workflow/template/Selector`,
		method: 'get',
		data,
		options: {
			load: false
		}
	})
}

//表单预览
export function flowForm(id) {
	return request({
		url: `/api/flowForm/Form/${id}`,
		method: 'get'
	})
}

// 列表ListAll
export function FlowEngineListAll() {
	return request({
		url: `/api/workflow/Engine/flowTemplate/ListAll`,
		method: 'get',
		options: {
			load: false
		}
	})
}
// 流程引擎下拉框
export function FlowEngineSelector(type) {
	return request({
		url: `/api/workflow/Engine/flowTemplate/Selector`,
		method: 'get',
		data: {
			type
		}
	})
}
// 获取流程评论列表
export function getCommentList(data) {
	return request({
		url: `/api/workflow/comment`,
		method: 'get',
		data
	})
}
// 新建流程评论
export function createComment(data) {
	return request({
		url: `/api/workflow/comment`,
		method: 'post',
		data
	})
}
// 删除流程评论
export function delComment(id) {
	return request({
		url: `/api/workflow/comment/${id}`,
		method: 'delete'
	})
}
// 委托可选全部流程
export function FlowEngineAll(data) {
	return request({
		url: `/api/workflow/Engine/flowTemplate/getflowAll`,
		method: 'get',
		data
	})
}
// 获取引擎id
export function getFlowIdByCode() {
	return request({
		url: `/api/extend/CrmOrder`,
		method: 'get'
	})
}
// 获取待办未读
export function getFlowTodoCount(data) {
	return request({
		url: `/api/visualdev/Dashboard/FlowTodoCount`,
		method: 'post',
		data
	})
}

// 委托 通过list<flowId>获取流程引擎列表
export function getFlowEngineListByIds(data) {
	return request({
		url: `/api/workflow/template/GetFlowList`,
		method: 'post',
		data
	})
}
// 获取流程版本Id和发起节点表单id(大流程id)
export function getFlowStartFormId(id) {
	return request({
		url: `/api/workflow/template/StartFormId/${id}`,
		method: 'get',
	})
}