<template>
	<view class="jnpf-group-title" :style="{'text-align':contentPosition}" @click="handleClick()">{{content}}
		<u-icon :name="helpMessage? 'question-circle-fill':''" class="u-m-l-10" color="#a0acb7"></u-icon>
	</view>
</template>
<script>
	export default {
		name: 'jnpf-group-title',
		props: {
			content: {
				type: String,
				default: ''
			},
			helpMessage: {
				type: String,
				default: ''
			},
			contentPosition: {
				type: String,
				default: 'left'
			},
		},
		methods: {
			handleClick() {
				if (!this.helpMessage) return
				this.$emit('groupIcon')
			}
		}
	}
</script>
<style lang="scss" scoped>
	.jnpf-group-title {
		width: 100%;
		color: #333333;
		font-size: 32rpx;
		line-height: 70rpx;
		margin: 0;
		padding: 0 20rpx;
	}
</style>