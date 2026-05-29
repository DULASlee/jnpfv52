<template>
	<view class="jnpf-link" :style="textStyle" @click="handleClick()">
		<text>{{content}}</text>
	</view>
</template>
<script>
	export default {
		name: 'jnpf-link',
		props: {
			content: {
				type: String,
				default: '文本链接'
			},
			href: {
				type: String,
				default: ''
			},
			target: {
				type: String,
				default: ''
			},
			textStyle: {
				type: Object,
				default: {}
			}
		},
		methods: {
			handleClick(event) {
				if (!this.href) return this.$u.toast("未配置跳转链接")
				if (this.target === '_self') {
					uni.navigateTo({
						url: '/pages/apply/externalLink/index?url=' + this.href,
						fail: (err) => {
							this.$u.toast("暂无此页面")
						}
					})
				} else {
					// #ifdef APP
					plus.runtime.getProperty(plus.runtime.appid, (wgtinfo) => {
						plus.runtime.openURL(this.href)
					})
					// #endif
					// #ifndef APP
					uni.navigateTo({
						url: '/pages/apply/externalLink/index?url=' + this.href,
						fail: (err) => {
							this.$u.toast("暂无此页面")
						}
					})
					// #endif
				}
			}
		}
	}
</script>
<style lang="scss" scoped>
	.jnpf-link {
		width: 100%;
		color: #1890ff;
		text-align: right;
	}
</style>