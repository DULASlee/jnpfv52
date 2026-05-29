<template>
	<view class="">
	</view>
</template>

<script>
	import {
		useUserStore
	} from '@/store/modules/user'
	export default {
		data() {
			return {
				token: ''
			}
		},
		onLoad(e) {
			this.token = e.token
			this.init()
		},
		methods: {
			init() {
				const userStore = useUserStore()
				userStore.setToken(this.token)
				userStore.getCurrentUser().then((res) => {
					uni.hideLoading()
					uni.switchTab({
						url: '/pages/index/index'
					});
					this.show = false
				}).catch(() => {
					uni.hideLoading()
					uni.switchTab({
						url: '/pages/login/index'
					});
				})
			}
		}
	}
</script>

<style>
</style>