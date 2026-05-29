<template>
	<view class="logError-v u-flex-col">
		<view class="error-prompt">
			<view class="error-prompt-title u-p-l-20 u-p-r-20">
				错误提示：
			</view>
			<view class="u-p-20 error-content">
				{{errorTip}}
			</view>
		</view>
		<view class="error-prompt">
			<view class="error-prompt-title u-p-l-20 u-p-r-20">
				错误内容：
			</view>
			<view class="u-p-20 error-content">
				{{errorData}}
			</view>
		</view>
	</view>
</template>

<script>
	export default {
		data() {
			return {
				config: {},
				errorData: [],
				errorTip: {}
			}
		},
		onLoad(e) {
			this.config = JSON.parse(this.jnpf.base64.decode(e.data))
			this.errorData = JSON.parse(this.config.errorData)
			this.errorTip = JSON.parse(this.config.errorTip)
		}
	}
</script>

<style lang="scss">
	.logError-v {
		.error-prompt {
			.error-prompt-title {
				width: 100%;
				height: 80rpx;
				background-color: #edeff6;
				line-height: 80rpx;
			}

			.error-content {
				line-height: 50rpx;
			}
		}
	}
</style>