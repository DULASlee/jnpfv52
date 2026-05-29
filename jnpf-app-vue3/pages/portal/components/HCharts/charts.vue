<template>
	<view class="charts-v">
		<view class="qiun-title-bar u-flex" :style="{'justify-content':config.option.chartTitle.titleLeft}"
			v-if="config.option.chartTitle.titleText || config.option.chartTitle.titleSubtext">
			<view class="u-flex-col titleBox" :style="{'background-color':config.option.chartTitle.titleBgColor}">
				<view class="tit"
					:style="{'margin-bottom':config.option.chartTitle.titleSubtext?'8rpx':0,'font-size':config.option.chartTitle.titleTextStyleFontSize,'font-weight':config.option.chartTitle.titleTextStyleFontWeight,'color':config.option.chartTitle.titleTextStyleColor}">
					{{config.option.chartTitle.titleText}}
				</view>
				<view class="tit2"
					:style="{'font-size':config.option.chartTitle.titleSubtextStyleFontSize,'font-weight':config.option.chartTitle.titleSubtextStyleFontWeight,'color':config.option.chartTitle.titleSubtextStyleColor}">
					{{config.option.chartTitle.titleSubtext}}
				</view>
			</view>
		</view>
		<view class="regionStep" v-if="regionStep.length >1">
			<text v-for="(item,index) in regionStep" @click="regionStepClick(item,index)" :key="index"
				:style="{'font-size':config.option.drillDownFontSize*2+'rpx','color':config.option.drillDownColor,'font-weight':config.option.drillDownFontWeight?700:400}">
				{{item.name}}
				<u-icon name="arrow-right" v-if="index!=regionStep.length-1" class="icon"></u-icon>
			</text>
		</view>
		<view class="charts-box">
			<qiun-data-charts v-if="config.option.chartData.series.length" :type="config.option.chartData.type"
				:chartData="config.option.chartData" :ontouch="true" :opts="config.option.chartData.opts"
				@complete="complete" @getIndex="getIndex" :style="{'background-color':config.option.bgColor}"
				:connectNulls="true" />
			<view class="" v-if="config.jnpfKey==='mapChart' && config.option.chartData.series.length>0 && loading"
				:key="key">
				<block v-for="(item, index) in config.option.markPoints" :key="index">
					<view :class="config.option.styleType == 2?'points-box2':'points-box'"
						:style="{top:(item.y - 5 ) + 'px',left:(item.x - 5) +'px'}"></view>
				</block>
			</view>
		</view>
	</view>
</template>
<script>
	export default {
		props: {
			loading: {
				type: Boolean,
				default: false
			},
			config: {
				type: Object,
				default: () => {}
			},
			markPoints: {
				type: Array,
				default: () => []
			},
			regionStep: {
				type: Array,
				default: () => []
			}
		},
		data() {
			return {
				key: +new Date()
			}
		},
		methods: {
			complete(e) {
				this.$emit('complete', e)
			},
			getIndex(e) {
				return this.$emit('getIndex', e)
			},
			regionStepClick(item, index) {
				if (index < this.regionStep.length - 1) return this.$emit('regionStepClick', item, index)
			}
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