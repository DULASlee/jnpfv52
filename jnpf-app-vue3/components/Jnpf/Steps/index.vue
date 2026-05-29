<template>
	<view class="steps-v">
		<u-steps :list="list" :mode="mode" name="title" :current="modelValue" :name="name" @change="onStepChange">
		</u-steps>
	</view>
</template>
<script>
	export default {
		props: {
			modelValue: {
				default: 0
			},
			// 自定义标题key
			name: {
				type: String,
				default: 'title'
			},
			mode: {
				type: String,
				default: 'dot'
			},
			current: {
				default: 0
			},
			list: {
				type: Array,
				default: () => []
			},
		},
		data() {
			return {
				stepCurrent: 0
			}
		},
		methods: {
			onStepChange(index) {
				this.$emit('change', index)
			},
		}
	}
</script>

<style lang="scss">
	.steps-v {
		background-color: #fff;
		padding: 20rpx 0;
		width: 100%;
		overflow-x: scroll;

		.u-steps .u-steps__item {
			min-width: unset;
		}

		.u-steps .u-steps__item .u-steps__item__text--row {
			width: 92px;
		}
	}
</style>