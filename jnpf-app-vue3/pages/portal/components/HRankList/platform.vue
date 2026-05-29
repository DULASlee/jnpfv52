<template>
	<view class="stage" v-if="show" :key='key'>
		<view class="cup-top-box" v-if="platformData.styleType == 3">
			<view class="cup1-box cup-box" v-for="(item,index) in frontValue" :key="index">
				<view class="img">
					<image :src="imgList[index].url"></image>
				</view>
				<view class="top-bg"></view>
				<view class="cup-box-content" :class="'cup-box-content'+index">
					<text class="txt">{{item[props.label]}}</text>
					<text class="txt">{{item[props.value]}}</text>
				</view>
			</view>
		</view>
		<view class="medal-v" v-if="platformData.styleType == 4">
			<view class="medal-box u-flex">
				<view class="medal-box-inner" :class="'medal-box-inner'+index" v-for="(item,index) in frontValue"
					:key="index">
					<view class="img">
						<image :src="imgList2[index].url"></image>
					</view>
					<view class="u-flex-col txt-box">
						<text class='txt'>{{item[props.label]}}</text>
						<text class='txt'>{{item[props.value]}}</text>
					</view>
				</view>
			</view>
		</view>
	</view>
</template>

<script>
	import runnerUp from '@/pages/portal/static/image/runnerUp.png'
	import champion from '@/pages/portal/static/image/champion.png'
	import thirdPlace from '@/pages/portal/static/image/thirdPlace.png'
	import runnerUp2 from '@/pages/portal/static/image/runnerUp2.png'
	import champion2 from '@/pages/portal/static/image/champion2.png'
	import thirdPlace2 from '@/pages/portal/static/image/thirdPlace3.png'

	export default {
		props: {
			option: {
				type: Object,
				default: () => {}
			},
			props: {
				type: Object,
				default: () => ({
					label: 'label',
					value: 'value',
				})
			},
		},
		data() {
			return {
				platformData: {},
				show: false,
				key: +new Date(),
				imgList: [{
					url: runnerUp
				}, {
					url: champion
				}, {
					url: thirdPlace
				}],
				imgList2: [{
					url: runnerUp2
				}, {
					url: champion2
				}, {
					url: thirdPlace2
				}]
			}
		},
		computed: {
			frontValue() {
				return this.platformData.frontValue || []
			}
		},
		created() {
			this.platformData = JSON.parse(JSON.stringify(this.option)) || {}
			this.show = true
			this.key = +new Date()
		}
	}
</script>

<style lang="scss">
	.stage {
		.cup-top-box {
			display: flex;
			justify-content: center;
			align-items: flex-end;
			overflow: hidden;
			padding: 20rpx 0 30rpx;

			.cup-box {
				display: flex;
				flex-direction: column;
				align-items: center;

				.img {
					width: 100rpx;
					height: 96rpx;

					image {
						width: 100%;
						height: 100%;
					}
				}

				.top-bg {
					width: 230rpx;
					height: 0;
					border-bottom: 10rpx solid rgba(239, 233, 225, .39);
					border-left: 20rpx solid transparent;
					border-right: 10rpx solid transparent;
				}

				.cup-box-content {
					width: 230rpx;
					height: 90rpx;
					background: rgba(245, 241, 234, .59);
					display: -webkit-box;
					display: -ms-flexbox;
					display: flex;
					flex-direction: column;
					justify-content: center;
					align-items: center;
					color: #6f89ac;

					.txt {
						font-size: 20rpx;
						font-family: PingFang SC;
						font-weight: 400;
						color: #6F89AC;
					}
				}

				.cup-box-content1 {
					height: 110rpx;

					.txt {
						color: #CE7C1F;
					}
				}

				.cup-box-content0 {
					.txt {
						color: #6F89AC;
					}
				}

				.cup-box-content2 {
					.txt {
						color: #8D4112;
					}
				}
			}
		}

		.medal-v {
			padding: 80rpx 0 0;

			.medal-box {
				align-items: flex-end;
				padding: 0 20rpx;

				.medal-box-inner {
					width: 232rpx;
					height: 132rpx;
					border: 2rpx solid rgba(111, 137, 172, .59);
					position: relative;

					.txt-box {
						justify-content: center;
						width: 100%;
						height: 100%;
						align-items: center;
						margin-top: 10rpx;
					}

					.img {
						width: 100rpx;
						height: 96rpx;
						position: absolute;
						top: -60rpx;
						left: 50%;
						transform: translateX(-50%);

						image {
							width: 100%;
							height: 100%;
						}
					}

					.txt {
						font-size: 20rpx;
						font-family: PingFang SC;
						font-weight: 400;
						color: rgba(111, 137, 172, .59);
					}
				}

				.medal-box-inner1 {
					width: 252rpx;
					height: 176rpx;
					border: 2rpx solid rgba(206, 124, 31, .59);
					margin: 0 6rpx;
					position: relative;

					.txt {

						color: rgba(206, 124, 31, .59);
					}
				}

				.medal-box-inner2 {
					border: 2rpx solid rgba(141, 65, 18, .59);

					.txt-box {

						margin-top: 10rpx;
					}

					.txt {
						color: rgba(141, 65, 18, .59);
					}
				}
			}
		}
	}
</style>