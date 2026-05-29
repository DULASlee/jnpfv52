import request from '@/utils/request'

//app版本升级
export function versionUpgrade(appName) {
	return request({
		url: `/api/file/AppStartInfo/${appName}`,
	})
}
export function getAppVersion() {
	return request({
		url: `/api/app/Version`
	})
}
// 获取数据字典数据
export function getDictionaryDataAll() {
	return request({
		url: '/api/system/DictionaryData/All',
		options: {
			load: false
		}
	})
}
// 获取字典数据下拉框列表
export function getDictionaryDataSelector(dictionaryTypeId) {
	return request({
		url: `/api/system/DictionaryData/${dictionaryTypeId}/Data/Selector`,
		options: {
			load: false
		}
	})
}
// 获取关联表单数据详情
export function getRelationFormDetail(modelId, data) {
	return request({
		url: `/api/visualdev/OnlineDev/${modelId}/DataChange`,
		method: 'post',
		data
	})
}
// 获取关联表单弹窗列表
export function getRelationSelect(id, data, options) {
	return request({
		url: `/api/visualdev/Base/${id}/FieldDataSelect`,
		data,
		options: {
			load: false
		}
	})
}
// 获取弹窗选择远端接口数据
export function getPopSelect(id, data, options) {
	return request({
		url: `/api/system/DataInterface/${id}/Actions/List`,
		method: 'POST',
		data,
		options: {
			load: true
		}
	})
}
// 获取多条接口数据
export function getDataInterfaceDataInfoByIds(id, data) {
	return request({
		url: `/api/system/DataInterface/${id}/Actions/InfoByIds`,
		method: 'POST',
		data
	})
}
// 获取组织/公司下拉框列表
export function getOrganizeSelector() {
	return request({
		url: '/api/permission/Organize/Selector/0',
		options: {
			load: false
		}
	})
}
// 获取部门下拉框列表(公司+部门)
export function getDepartmentSelector() {
	return request({
		url: '/api/permission/Organize/Department/Selector/0',
		options: {
			load: false
		}
	})
}

// 获取部门下拉框列表(公司+部门)
export function selectAsyncList(data, id) {
	return request({
		url: `/api/permission/Organize/Department/SelectAsyncList/${id}`,
		options: {
			load: false
		},
		data
	})
}
// 获取部门下拉框列表(公司+部门)回写
export function selectedList(data) {
	return request({
		url: `/api/permission/Organize/SelectedList`,
		method: 'POST',
		options: {
			load: false
		},
		data
	})
}



// 获取岗位下拉列表（公司+部门+岗位）
export function getPositionSelector() {
	return request({
		url: '/api/permission/Position/Selector',
		options: {
			load: false
		}
	})
}
// 获取用户下拉框列表(公司+部门+用户)
export function getUserSelector() {
	return request({
		url: '/api/permission/Users/Selector',
		options: {
			load: false
		}
	})
}

// 通过部门id,岗位id,角色id,分组id,用户id获取用户列表(带分页)
export const getUsersByUserCondition = (data) => {
	return request({
		url: '/api/permission/Users/UserCondition',
		method: 'post',
		data
	})
}
// 通过部门id获取部门树形
export const getOrgByOrganizeCondition = (data) => {
	return request({
		url: `/api/permission/Organize/OrganizeCondition`,
		method: 'post',
		data
	})
}
// 通过部门id,岗位id获取岗位树形
export const getPositionByPositionCondition = (data) => {
	return request({
		url: `/api/permission/Position/PositionCondition`,
		method: 'post',
		data
	})
}

// 获取用户下拉框列表(用户选择加载)
export function getUserSelectorNew(organizeId, keyword) {
	return request({
		url: `/api/permission/Users/ImUser/Selector/${organizeId}`,
		method: 'POST',
		data: {
			keyword
		},
		options: {
			load: false
		}
	})
}
// 获取用户基本信息
export const getUserInfoList = ids => {
	return request({
		url: '/api/permission/Users/getUserList',
		method: 'post',
		data: {
			ids
		}
	})
}

// 获取我的下属
export const getSubordinates = (keyword) => {
	return request({
		url: '/api/permission/Users/getSubordinates',
		method: 'post',
		data: {
			keyword
		}
	})
}
// 获取当前组织用户
export const getOrganization = (keyword) => {
	return request({
		url: '/api/permission/Users/getOrganization',
		method: 'get',
		data: {
			keyword,
			organizeId: '0'
		}
	})
}
//获取用户详情
export function getUesrDetail(id) {
	return request({
		url: '/api/app/User/' + id,
		method: 'GET'
	})
}
// 获取所有用户列表
export function getUserAll() {
	return request({
		url: '/api/permission/Users/All',
		options: {
			load: false
		}
	})
}
// 获取通讯录用户列表(分页)
export function getImUser(data, options) {
	return request({
		url: '/api/permission/Users/ImUser',
		data,
		options: {
			load: false
		}
	})
}
// 获取接口数据
export function getDataInterfaceRes(id, data) {
	return request({
		url: `/api/system/DataInterface/${id}/Actions/Preview`,
		method: 'post',
		options: {
			load: false
		},
		data: data || {}
	})
}
// 用户登录
export function login(data) {
	return request({
		url: '/api/oauth/Login',
		method: 'post',
		data,
		header: {
			'Content-Type': 'application/x-www-form-urlencoded',
		}
	})
}
// 账号注销
export function accountCancel(token) {
	return request({
		url: '/api/oauth/logoutCurrentUser',
		method: 'post',
		token,
		header: {
			'Content-Type': 'application/x-www-form-urlencoded',
		}
	})
}
//获取验证码
export function clickSms(account) {
	return request({
		url: 'https://app.jnpfsoft.com/api/Saas/Tenant/SmsCode/' + account,
		method: 'GET',
		header: {
			'Content-Type': 'application/x-www-form-urlencoded',
		}
	})
}
//验证码登录
export function loginSms(data) {
	return request({
		url: 'https://app.jnpfsoft.com/api/Saas/Tenant/LoginSms',
		method: 'POST',
		data,
		header: {
			'Content-Type': 'application/x-www-form-urlencoded',
		}
	})
}
// 退出登录
export function logout() {
	return request({
		url: '/api/oauth/Logout'
	})
}

// 获取当前用户信息
export function getCurrentUser() {
	return request({
		url: '/api/oauth/CurrentUser?type=' + 'app',
		options: {
			load: false
		}
	})
}
// 修改密码信息发送
export function updatePasswordMessage() {
	return request({
		url: '/api/oauth/updatePasswordMessage',
		method: 'POST',
		data: {},
		options: {
			load: false
		}
	})
}

// 用户登录测试
export function login2(data) {
	return request({
		url: '/api/oauth',
		method: 'GET',
		data,
		options: {
			load: false
		}
	})
}
export function getBillNumber(enCode) {
	return request({
		url: `/api/system/BillRule/BillNumber/${enCode}`,
		method: 'GET',
		options: {
			load: false
		}
	})
}
// 获取系统配置
export function getSystemConfig() {
	return request({
		url: '/api/system/SysConfig',
		method: 'GET'
	})
}
// 获取下载文件链接
export function getDownloadUrl(type, fileId) {
	return request({
		url: `/api/file/Download/${type}/${fileId}`,
		method: 'GET'
	})
}
// 修改当前用户密码
export function updatePassword(data) {
	return request({
		url: '/api/permission/Users/Current/Actions/ModifyPassword',
		method: 'POST',
		data
	})
}
// 获取我的下属
export function getSubordinate(id) {
	return request({
		url: `/api/permission/Users/Current/Subordinate/${id ? id : '0'}`,
		method: 'GET'
	})
}
// 获取默认配置
export function getConfig(account) {
	return request({
		url: `/api/oauth/getConfig/${account}`,
		method: 'get'
	})
}
// 获取行政区划下拉框数据
export function getProvinceSelector(id) {
	return request({
		url: `/api/system/Area/${id}/Selector/0`,
		method: 'GET'
	})
}
// 获取行政区划下拉框数据
export function getProvinceSelectorInfoList(idsList) {
	return request({
		url: `/api/system/Area/GetAreaByIds`,
		method: 'post',
		data: {
			idsList
		}
	})
}
// 设置主要组织、主要岗位
export function setMajor(data) {
	return request({
		url: `/api/permission/Users/Current/major`,
		method: 'put',
		data
	})
}
// 获取当前用户所有组织
export function getUserOrganizes(data) {
	return request({
		url: `/api/permission/Users/Current/getUserOrganizes`,
		method: 'GET',
		data
	})
}
// 获取当前用户所有岗位
export function getUserPositions(data) {
	return request({
		url: `/api/permission/Users/Current/getUserPositions`,
		method: 'GET',
		data
	})
}
// 获取当前用户个人资料
export function UserSettingInfo() {
	return request({
		url: '/api/permission/Users/Current/BaseInfo',
		method: 'GET'
	})
}
// 更新当前用户个人资料
export function UpdateUser(data) {
	return request({
		url: '/api/permission/Users/Current/BaseInfo',
		method: 'PUT',
		data
	})
}
// 更新当前用户头像
export function UpdateAvatar(name) {
	return request({
		url: `/api/permission/Users/Current/Avatar/${name}`,
		method: 'PUT'
	})
}
// 获取分组下拉框列表
export const getGroupSelector = () => {
	return request({
		url: '/api/permission/Group/Selector',
		method: 'GET'
	})
}
// 获取分组列表
export const getGroupCondition = (data) => {
	return request({
		url: '/api/permission/Group/GroupCondition',
		method: 'POST',
		data
	})
}
// 获取角色下拉框列表
export const getRoleSelector = () => {
	return request({
		url: '/api/permission/Role/Selector',
		method: 'GET',
		options: {
			load: false
		}
	})
}
// 获取角色下拉框列表
export const getRoleCondition = (data) => {
	return request({
		url: '/api/permission/Role/RoleCondition',
		method: 'POST',
		data
	})
}
// app第三方登录
export const getCallback = (data) => {
	return request({
		url: `/api/oauth/socials/app/callback/${data.source}?uuid=` + data.uuid,
		method: 'GET'
	})
}
// 第三方登录
export function otherlogin(data, ticket) {
	return request({
		url: `/api/oauth/socials/render/${data}` + '?ticket=' + ticket,
		method: 'get'
	})
}
// 获取登陆配置
export function getLoginConfig() {
	return request({
		url: '/api/oauth/getLoginConfig',
	})
}
//获取登录票据
export function getTicket() {
	return request({
		url: `/api/oauth/getTicket`,
		method: 'GET',
	})
}
// 轮询获取登陆状态
export function getTicketStatus(ticket) {
	return request({
		url: `/api/oauth/getTicketStatus/${ticket}`,
		method: 'GET',
	})
}
// 获取第三方列表
export const getSocialsUserList = () => {
	return request({
		url: '/api/permission/socials/login',
		method: 'GET'
	})
}
// 第三方登录回调列表后点击登录
export function socialsLogin(data) {
	return request({
		url: `/api/oauth/Login/socials`,
		data,
		header: {
			'Content-Type': 'application/x-www-form-urlencoded'
		},
	})
}
// 获取签名列表
export const getSignImgList = () => {
	return request({
		url: '/api/permission/Users/Current/SignImg',
		method: 'GET',
		options: {
			load: false
		}
	})
}
// 新增签名
export const createSignImg = (data) => {
	return request({
		url: '/api/permission/Users/Current/SignImg',
		method: 'post',
		data,
		options: {
			load: false
		}
	})
}
// 设置默认签名
export const setDefSignImg = (id) => {
	return request({
		url: `/api/permission/Users/Current/${id}/SignImg`,
		method: 'put',
		options: {
			load: false
		}
	})
}
// 删除签名
export const delSignImg = (id) => {
	return request({
		url: `/api/permission/Users/Current/${id}/SignImg`,
		method: 'delete',
		options: {
			load: false
		}
	})
}
// 获取选中组织、岗位、角色、用户基本信息
export const getSelectedList = ids => {
	return request({
		url: '/api/permission/Users/getSelectedList',
		method: 'post',
		data: {
			ids
		}
	})
}
// 通过组织、岗位、角色、用户ids获取选中用户基本信息
export const getSelectedUserList = data => {
	return request({
		url: '/api/permission/Users/getSelectedUserList',
		method: 'post',
		data
	})
}
// 委托选择用户
export const getReceiveUserList = data => {
	return request({
		url: '/api/permission/Users/ReceiveUserList',
		method: 'get',
		data
	})
}
// 获取用户下拉框列表
export const getListByAuthorize = (organizeId, keyword) => {
	return request({
		url: `/api/permission/Users/GetListByAuthorize/${organizeId}`,
		method: 'post',
		data: {
			keyword
		}
	})
}

// 获取默认当前值部门ID
export function getDefaultCurrentValueDepartmentId(data) {
	return request({
		url: `/api/permission/Organize/getDefaultCurrentValueDepartmentId`,
		method: 'post',
		data
	})
}

// 获取默认当前值部门ID（同步）
export async function getDefaultCurrentValueDepartmentIdAsync(data) {
	return new Promise(resolve => {
		request({
			url: `/api/permission/Organize/getDefaultCurrentValueDepartmentId`,
			method: 'post',
			data
		}).then(ret => {
			resolve(ret)
		})
	})
}

// 获取默认当前值用户ID
export function getDefaultCurrentValueUserId(data) {
	return request({
		url: `/api/permission/Users/getDefaultCurrentValueUserId`,
		method: 'post',
		data
	})
}

// 获取默认当前值用户ID（同步）
export async function getDefaultCurrentValueUserIdAsync(data) {
	return new Promise(resolve => {
		request({
			url: `/api/permission/Users/getDefaultCurrentValueUserId`,
			method: 'post',
			data
		}).then(ret => {
			resolve(ret)
		})
	})
}

// 查询附近数据
export function getAroundList(data) {
	return request({
		url: '/api/system/Location/around',
		method: 'get',
		data
	})
}
//根据关键字查询附近数据
export function getTextList(data) {
	return request({
		url: '/api/system/Location/text',
		method: 'get',
		data
	})
}
//逆地理编码
export function getAddress(data) {
	return request({
		url: '/api/system/Location/regeo',
		method: 'get',
		data
	})
}
// 更改扫码凭证状态-已扫码
export function setCodeCertificateStatus(ticket, status) {
	return request({
		url: `/api/oauth/setCodeCertificateStatus/${ticket}/${status}`,
		method: 'GET',
		options: {
			load: false
		}
	})
}
// 扫码确认登录
export function confirmLogin(ticket) {
	return request({
		url: `/api/oauth/confirmLogin/${ticket}`,
		method: 'GET',
		options: {
			load: false
		}
	})
}
export function getLangJson(locale) {
	const header = locale ? {
		"Accept-Language": locale
	} : {}
	return request({
		url: '/api/system/BaseLang/LangJson',
		method: 'GET',
		header
	})
}