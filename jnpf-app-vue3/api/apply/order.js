import request from '@/utils/request'

// 删除订单
export function Delete(id) {
	return request({
		url: `/api/extend/CrmOrder/${id}`,
		method: 'DELETE'
	})
}
// 获取订单列表
export function getOrderList(data, options) {
	return request({
		url: `/api/extend/CrmOrder`,
		method: 'get',
		data,
		options
	})
}
// 获取商品列表
export function getGoodsList(data) {
	return request({
		url: `/api/extend/CrmOrder/Goods`,
		method: 'get',
		data,
		options: {
			load: false
		}
	})
}
// 获取客户列表
export function getCustomerList(keyword) {
	return request({
		url: `/api/extend/CrmOrder/Customer`,
		method: 'get',
		data: {
			keyword: keyword || ''
		},
		options: {
			load: false
		}
	})
}
