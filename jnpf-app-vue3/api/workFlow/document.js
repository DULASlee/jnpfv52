import request from '@/utils/request'
// 获取文档列表
export function getDocumentList(data) {
	return request({
		url: `/api/extend/Document?keyword=${data.keyword}&parentId=${data.parentId}`,
		method: 'get'
	})
}

// 文件下载
export function packDownload(data) {
	return request({
		url: `/api/extend/Document/PackDownload`,
		method: 'POST',
		data
	})
}
// 文件重命名
export function resetFileName(data) {
	return request({
		url: `/api/extend/Document/${data.id}`,
		method: 'PUT',
		data
	})
}
// 文件重命名详情
export function fileDetail(id) {
	return request({
		url: `/api/extend/Document/${id}`,
		method: 'get'
	})
}
// 新建文件夹
export function addFolder(data) {
	return request({
		url: `/api/extend/Document`,
		method: 'POST',
		data
	})
}
// 文件删除
export function batchDelete(data) {
	return request({
		url: `/api/extend/Document/BatchDelete`,
		method: 'POST',
		data
	})
}
// 文件回收站
export function trash(data) {
	return request({
		url: `/api/extend/Document/Trash`,
		method: 'GET',
		data
	})
}
// 文件回收站删除
export function trashDelete(data) {
	return request({
		url: `/api/extend/Document/Trash`,
		method: 'post',
		data
	})
}

// 文件还原
export function recovery(data) {
	return request({
		url: `/api/extend/Document/Trash/Actions/Recovery`,
		method: 'POST',
		data
	})
}

// 文件树列表
export function folderTree(data) {
	return request({
		url: `/api/extend/Document/FolderTree`,
		method: 'POST',
		data
	})
}

// 文件移动
export function folderMove(data) {
	return request({
		url: `/api/extend/Document/Actions/MoveTo/${data.id}`,
		method: 'PUT',
		data
	})
}

// 文件共享列表
export function shareFolderList(data) {
	return request({
		url: `/api/extend/Document/Share`,
		method: 'GET',
		data
	})
}
// 文件共享
export function shareFolder(data) {
	return request({
		url: `/api/extend/Document/Actions/Share`,
		method: 'POST',
		data
	})
}
// 共享给我
export function shareTome(data) {
	return request({
		url: `/api/extend/Document/ShareTome`,
		method: 'GET',
		data
	})
}
// 取消共享
export function cancelShare(data) {
	return request({
		url: `/api/extend/Document/Actions/CancelShare`,
		method: 'POST',
		data
	})
}
// 获取共享人员
export function shareUser(id) {
	return request({
		url: `/api/extend/Document/ShareUser/${id}`,
		method: 'GET'
	})
}
// 更新共享人员
export function shareAdjustment(data) {
	return request({
		url: `/api/extend/Document/Actions/ShareAdjustment/${data.ids}`,
		method: 'POST',
		data
	})
}