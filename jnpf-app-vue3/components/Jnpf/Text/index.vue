<template>
	<view class="jnpf-text" :style="getTextstyle">{{content}}</view>
</template>
<script>
	export default {
		name: 'jnpf-text',
		props: {
			textStyle: {
				type: Object,
				default: () => ({})
			},
			content: {
				default: ''
			}
		},
		computed: {
			getTextstyle() {
				return {
					...this.textStyle,
					'line-height': this.textStyle['line-height'] * 2 + 'rpx',
					'font-size': this.textStyle['font-size'] * 2 + 'rpx'
				}
			}
		}
	}
</script>
<style lang="scss" scoped>
	.jnpf-text {
		padding: 20rpx 32rpx;
	}
</style>