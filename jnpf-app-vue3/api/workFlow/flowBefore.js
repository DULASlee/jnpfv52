import request from '@/utils/request'

// 获取待我审批信息
export function FlowTask(id, data) {
	return request({
		url: `/api/workflow/task/${id}`,
		method: 'get',
		data,
		options: {
			load: true
		}
	})
}

// 流程记录
export function recordList(data) {
	return request({
		url: `/api/workflow/operator/RecordList`,
		method: 'get',
		data
	})
}

// 获取流程任务
export function taskList(data) {
	return request({
		url: `/api/workflow/trigger/task/List`,
		method: 'get',
		data
	})
}

//减签列表
export function AddSignUserIdList(data, id) {
	return request({
		url: `/api/workflow/operator/AddSignUserIdList/${id}`,
		method: 'post',
		data
	})
}
//减签
export function ReduceApprover(data, id) {
	return request({
		url: `/api/workflow/operator/ReduceApprover/${id}`,
		method: 'post',
		data
	})
}
// 流程签收
export function SignFor(data) {
	return request({
		url: `/api/workflow/operator/Sign`,
		method: 'post',
		data
	})
}

// 退回
export function back(id) {
	return request({
		url: `/api/workflow/operator/SendBackNodeList/${id}`,
		method: 'get'
	})
}
// 确认退回
export function sendBack(data, id) {
	return request({
		url: `/api/workflow/operator/SendBack/${id}`,
		method: 'post',
		data
	})
}
// 开始办理
export function Transact(data) {
	return request({
		url: `/api/workflow/operator/Transact`,
		method: 'post',
		data
	})
}


// 待我审核审核
export function Audit(id, data) {
	return request({
		url: `/api/workflow/operator/Audit/${id}`,
		method: 'post',
		data,
		options: {
			load: true
		}
	})
}
// 待我审核退回
export function Reject(id, data) {
	return request({
		url: `/api/workflow/operator/Audit/${id}`,
		method: 'post',
		data
	})
}
// 撤回审核
export function auditRecall(id, data) {
	return request({
		url: `/api/workflow/operator/Recall/${id}`,
		method: 'post',
		data
	})
}
export function launchRecall(id, data) {
	return request({
		url: `/api/workflow/task/Recall/${id}`,
		method: 'PUT',
		data
	})
}

//减签
export function addSignUserIdList(id, data) {
	return request({
		url: `/api/workflow/operator/AddSignUserIdList/${id}`,
		method: 'POST',
		data
	})
}

// 驳回审核
export function cancel(id, data) {
	return request({
		url: `/api/workflow/Engine/FlowBefore/Cancel/${id}`,
		method: 'post',
		data
	})
}
// 待我审核转审
export function Transfer(id, data) {
	return request({
		url: `/api/workflow/operator/Transfer/${id}`,
		method: 'post',
		data
	})
}
// 审批汇总
export function getRecordList(id, data) {
	return request({
		url: `/api/workflow/Engine/FlowBefore/RecordList/${id}`,
		method: 'get',
		data
	})
}
// 待我审核保存草稿
export function saveAudit(id, data) {
	return request({
		url: `/api/workflow/operator/SaveAudit/${id}`,
		method: 'post',
		data
	})
}
export function saveAssist(id, data) {
	return request({
		url: `/api/workflow/operator/AssistSave/${id}`,
		method: 'post',
		data
	})
}

// 判断是否有候选人
export function Candidates(id, data) {
	return request({
		url: `/api/workflow/operator/CandidateNode/${id}`,
		method: 'post',
		data
	})
}
// 获取候选人列表（分页）
export function CandidateUser(id, data) {
	return request({
		url: `/api/workflow/operator/CandidateUser/${id}`,
		method: 'post',
		data
	})
}
// 获取审批退回类型
export function RejectList(id) {
	return request({
		url: `/api/workflow/Engine/FlowBefore/RejectList/${id}`,
		method: 'get'
	})
}
//协办
export function Assist(id, data) {
	return request({
		url: `/api/workflow/operator/Assist/${id}`,
		method: 'post',
		data
	})
}
// 加签
export function FreeApprover(id, data) {
	return request({
		url: `/api/workflow/operator/AddSign/${id}`,
		method: 'post',
		data
	})
}
// 返回多个子流程信息
export function SubFlowInfo(id) {
	return request({
		url: `/api/workflow/Engine/FlowBefore/SubFlowInfo/${id}`,
		method: 'get'
	})
}

// 获取流程实例相关人员（分页）
export function getTaskUserList(taskId, data) {
	return request({
		url: `/api/workflow/task/TaskUserList/${taskId}`,
		method: 'get',
		data
	})
}