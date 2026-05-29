<template>
	<view class="charts-v">
		<view class="charts-box">
			<qiun-data-charts :type="config.option.type" :chartData="config.option.chartData" :ontouch="true"
				:opts="config.option.opts" :style="{'background-color':config.option.bgColor?config.option.bgColor:''}"
				:connectNulls="true" />
		</view>
	</view>
</template>
<script>
	export default {
		props: {
			config: {
				type: Object,
				default: () => {}
			},
		},
		data() {
			return {}
		},
		methods: {
			complete(e) {
				this.$emit('complete', e)
			},
			getIndex(e) {
				return this.$emit('getIndex', e)
			},
		}
	}
</script>


<style lang="scss">
	.charts-v {
		background-color: #fff;
		box-sizing: border-box;
		position: relative;
	}

	.qiun-title-bar {
		width: 100%;
		z-index: 9;
		text-align: center;
		position: absolute;
		top: 40rpx;
		margin: 20rpx 0;

		.titleBox {
			.tit {
				// margin-bottom: 10rpx;
			}
		}
	}

	.regionStep {
		max-width: 100%;
		max-height: 100%;
		margin-bottom: 60rpx;
		position: absolute;
		top: 0rpx;
		z-index: 9999;

		.icon {
			margin: 0 8rpx;
		}
	}

	.charts-box {
		width: 100%;
		height: 660rpx;
		// margin: 0px auto 20rpx;
		position: relative;

		.charts-legend {
			position: absolute;
			bottom: 0px;
			left: 20rpx;
			font-size: 20rpx;

			.legend-item {
				display: inline-block;
				width: 30rpx;
				height: 20rpx;
				margin-right: 10rpx;
				background-color: #0D9FD8;
			}
		}

		.points-box {
			position: absolute;
			width: 20rpx;
			height: 20rpx;
			border-radius: 50%;
			background-color: #0D9FD8;
			animation: warn 1.5s ease-out 0s infinite;
		}

		.points-box2 {
			position: absolute;
			box-shadow: 0 0 24rpx 28rpx rgba(13, 159, 261, 0.3);
		}
	}

	@keyframes warn {
		0% {
			transform: scale(0.5);
			opacity: 1;
		}

		30% {
			opacity: 1;
		}

		100% {
			transform: scale(1.4);
			opacity: 0.3;
		}
	}
</style>