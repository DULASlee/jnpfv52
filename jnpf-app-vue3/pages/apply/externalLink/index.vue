<template>
	<view>
		<web-view :src="url"></web-view>
	</view>
</template>

<script>
	export default {
		data() {
			return {
				url: ''
			}
		},
		onLoad(option) {
			this.url = decodeURIComponent(option.url)
			const title = option.fullName || ''
			if (option.type == 5 || !title) return
			uni.setNavigationBarTitle({
				title
			})
		}
	}
</script>