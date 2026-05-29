<template>
	<view class="steps-v">
		<view class="steps-box">
			<view class="steps-line" :style="isUpper? lowerStyle:upperStyle" v-for="(item,index) in list" :key="index"
				@click="jump(item)">
				<view class="steps-item">
					<view class="steps-content" :style="{'width':isCrad?'270rpx':'250rpx'}">
						<view class="u-font-24 steps-content-hd" v-if="isCrad">
							<text>{{item.title}}</text>
						</view>
						<view class="u-font-24" :class="isCrad?'steps-title':'steps-title2'">
							<text class="name u-line-2">{{item.content}}</text>
							<text class="time">{{item.timestamp}}</text>
						</view>
					</view>
					<view class="steps-node" :style="isUpper?lowerDotStyle:upperDotStyle">
						<view class="steps-node-dot" v-if="index==0" :style="{'background':'rgba(62, 213, 56, 0.39)'}">
						</view>
						<view class="steps-node-dot" v-else-if="index==list.length-1"
							:style="{'background':'rgba(228, 231, 237, 0.39)'}"></view>
						<view class="steps-node-dot" v-else :style="{'background':'rgba(25, 144, 250, 0.39)'}"></view>
					</view>
				</view>
			</view>
		</view>
	</view>
</template>

<script>
	export default {
		props: {
			list: {
				type: Array,
				default () {
					return []
				}
			},
			isCrad: {
				type: Boolean,
				default () {
					return false
				}
			},
			isUpper: {
				type: Boolean,
				default () {
					return true
				}
			},
		},
		data() {
			return {
				upperStyle: {
					'padding-top': '40rpx',
					'border-top': '2rpx solid #DEDEDE'
				},
				upperDotStyle: {
					'top': '-54rpx'
				},
				lowerStyle: {
					'padding-bottom': '40rpx',
					'border-bottom': '2rpx solid #DEDEDE'
				},
				lowerDotStyle: {
					'bottom': '-52rpx'
				}
			}
		},
		methods: {
			jump(item) {
				this.jnpf.solveAddressParam(item, this.config)
				this.jnpf.jumpLink(item.urlAddress)
			},
		}
	}
</script>

<style lang="scss">
	.steps-v {
		height: 100%;
		overflow-x: scroll;

		.steps-box {
			padding: 20rpx;
			display: flex;

			.steps-line {
				.steps-item {
					width: 100%;
					position: relative;
					display: flex;
					flex-direction: column;

					.steps-node {
						position: absolute;
						left: 12rpx;
						transform-origin: 0;
						transform: translateX(-50%);
						display: flex;
						flex-direction: row;
						align-items: center;
						justify-content: center;
						z-index: 1;
						font-size: 24rpx;
						background-color: rgb(255, 255, 255);
						z-index: 9;

						.steps-node-dot {
							width: 28rpx;
							height: 28rpx;
							border: 4rpx solid #FFFFFF;
							box-shadow: 0 6rpx 12rpx rgba(2, 7, 28, 0.16);
							border-radius: 50%;
							display: flex;
							flex-direction: row;
						}
					}

					.steps-content {
						.steps-content-hd {
							width: 90%;
							height: 44rpx;
							color: #606266;
							margin-bottom: 4rpx;
						}

						.steps-title2 {
							line-height: 36rpx;
							display: flex;
							flex-direction: column;
							width: 90%;
							height: 120rpx;
							justify-content: space-between;
							border-radius: 8px;
						}

						.steps-title {
							line-height: 36rpx;
							display: flex;
							flex-direction: column;
							width: 90%;
							height: 170rpx;
							background: rgba(255, 255, 255, 0.39);
							box-shadow: 0px 6px 20px rgba(0, 0, 0, 0.1);
							border-radius: 8px;
							padding: 22rpx;
							justify-content: space-between;

							.name {
								margin-bottom: 6rpx;
								letter-spacing: 2rpx;
								color: #303133;
							}

							.time {

								color: #606266;
							}
						}
					}
				}
			}
		}
	}
</style>