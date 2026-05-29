@@ -0,0 +1,70 @@
<template>
	<view>
		<web-view :src="url"></web-view>
	</view>
</template>

<script>
	import {
		getTicketStatus,
		socialsLogin
	} from '@/api/common.js'
	import {
		useUserStore
	} from '@/store/modules/user'

	export default {
		data() {
			return {
				show: false,
				url: '',
				ticket: '',
				ssoTimer: null,
				tenantUserInfo: [],
			}
		},
		onLoad(option) {
			this.url = uni.getStorageSync('ssoUrl')
			this.ticket = option.ssoTicket
			this.ssoTimer = setInterval(() => {
				this.getTicketStatus()
			}, 1000)
		},
		onUnload() {
			this.clearTimer()
		},
		methods: {
			getTicketStatus() {
				const userStore = useUserStore()
				if (!this.ticket) return
				getTicketStatus(this.ticket).then(res => {
					if (res.data.status != 2) {
						this.clearTimer()
						// 登录成功
						if (res.data.status == 1) {
							uni.showLoading({
								title: '登录中'
							})
							userStore.setToken(res.data.value)
							userStore.getCurrentUser().then((res) => {
								uni.hideLoading()
								uni.reLaunch({
									url: '/pages/index/index'
								});
							}).catch(() => {
								uni.hideLoading()
								uni.reLaunch({
									url: '/pages/login/index'
								});
							})
						} else if (res.data.status == 4) {
							uni.setStorageSync('ssoTicket', this.ticket)
							uni.reLaunch({
								url: '/pages/login/index'
							});

						} else if (res.data.status == 6) {
							let tenantUserInfo = JSON.parse(res.data.value)
							if (tenantUserInfo.length == 1) {
								uni.showLoading({
									title: '登录中'
								})
								userStore.setToken(res.data.value)
								userStore.getCurrentUser().then((res) => {
									uni.hideLoading()
									uni.reLaunch({
										url: '/pages/index/index'
									});
								}).catch(() => {
									uni.hideLoading()
									uni.reLaunch({
										url: '/pages/login/index'
									})
								})
							} else {
								uni.reLaunch({
									url: '/pages/login/index?data=' + JSON.stringify(tenantUserInfo)
								})
							}
						} else if (res.data.status == 7) {
							this.$u.toast('第三方账号未绑定账号，请绑定后重试')
							setTimeout(() => {
								uni.reLaunch({
									url: '/pages/login/index'
								})
							}, 600)
						} else {
							this.show = false
							this.ssoUrl = ''
							uni.showToast({
								title: res.data.value || '操作超时，请重新点击登录',
								icon: 'none'
							})
							uni.reLaunch({
								url: '/pages/login/index'
							});
						}
					}

				})
			},
			clearTimer() {
				if (this.ssoTimer) {
					clearInterval(this.ssoTimer)
					this.ssoTimer = null
				}
			}
		}
	}
</script>