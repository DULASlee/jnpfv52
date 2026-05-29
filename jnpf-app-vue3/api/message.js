import request from '@/utils/request'

// 获取IM对话列表
export function getIMReply() {
	return request({
		url: '/api/message/imreply',
		options: {
			load: true
		}
	})
}
//获取消息列表
export function getMessageList(data) {
	return request({
		url: '/api/message',
		data,
		options: {
			load: false
		}
	})
}
//获取消息列表
export function getUnReadMsgNum(data) {
	return request({
		url: '/api/message/getUnReadMsgNum',
		data,
		options: {
			load: false
		}
	})
}
// 全部已读
export function MessageAllRead(data) {
	return request({
		url: '/api/message/Actions/ReadAll',
		method: 'POST',
		data
	})
}
//消息详情
export function getMessageDetail(id) {
	return request({
		url: `/api/message/ReadInfo/${id}`,
		method: 'get'
	})
}
// 判断是否有查看消息详情权限(消息通知用)
export function checkInfo(taskOperatorId, opType) {
	return request({
		url: `/api/workflow/operator/${taskOperatorId}/Info?opType=${opType}`,
		method: 'get'
	})
}
// 删除会话列表
export function relocation(id) {
	return request({
		url: `/api/message/imreply/relocation/${id}`,
		method: 'delete'
	})
}