import {
	defineStore
} from 'pinia';
import {
	logout,
	getCurrentUser
} from '@/api/common.js'
import store from '../index'
import permission from '@/libs/permission'

export const useUserStore = defineStore({
	id: ' user',
	state: () => ({
		token: "",
		userInfo: {},
		menuList: [],
	}),
	getters: {
		getToken() {
			return this.token
		},
	},
	actions: {
		setToken(token) {
			this.token = token
			uni.setStorageSync('token', token)
		},
		setCid(cid) {
			this.cid = cid
			uni.setStorageSync('cid', cid)
		},
		setUserInfo(userInfo) {
			this.userInfo = userInfo
			uni.setStorageSync('userInfo', userInfo)
		},
		setMenuList(menuList) {
			this.menuList = menuList
			uni.setStorageSync('menuList', menuList)
		},
		getCurrentUser() {
			return new Promise((resolve, reject) => {
				getCurrentUser().then(res => {
					const userInfo = res.data.userInfo || {}
					const permissionList = res.data.permissionList || []
					const sysConfigInfo = res.data.sysConfigInfo || {}
					const sysVersion = sysConfigInfo.sysVersion || ''
					const copyright = sysConfigInfo.copyright || ''
					uni.setStorageSync('sysVersion', sysVersion)
					uni.setStorageSync('permissionList', permissionList)
					permission && permission.updatePermissionList()
					uni.setStorageSync('sysConfigInfo', sysConfigInfo)
					uni.setStorageSync('copyright', copyright)
					this.setUserInfo(userInfo)
					this.setMenuList(res.data.menuList)
					let menuList = res.data.menuList
					if (!menuList.length && !userInfo.workflowEnabled) {
						uni.showToast({
							title: '您的权限不足，请联系管理员',
							icon: 'none'
						})
						uni.removeStorageSync('token')
						reject()
						setTimeout(() => {
							uni.reLaunch({
								url: '/pages/login/index'
							})
						}, 500)
						return
					}
					resolve(userInfo)
				}).catch(error => {
					reject(error)
				})
			})
		},
		logout() {
			return new Promise((resolve, reject) => {
				logout().then(() => {
					this.setToken('')
					this.setCid('')
					this.setUserInfo({})
					this.resetToken()
					resolve()
				}).catch(error => {
					reject(error)
				})
			})
		},
		resetToken() {
			uni.removeStorageSync('token')
			uni.removeStorageSync('cid')
			uni.removeStorageSync('userInfo')
			uni.removeStorageSync('permissionList')
			uni.removeStorageSync('sysVersion')
			uni.removeStorageSync('dynamicModelExtra')
		}
	},
});

export function useUserStoreWithOut() {
	return useUserStore(store);
}