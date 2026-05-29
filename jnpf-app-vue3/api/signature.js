import request from '@/utils/request'


// 通过id获取签章下拉框列表
export function getListByIds(data) {
	return request({
		url: '/api/system/Signature/ListByIds',
		method: 'post',
		data
	})
}