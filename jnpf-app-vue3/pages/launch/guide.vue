<template>
	<view class="guide-v">
		<view class="content">
			<swiper class="swiper" @change="onChange">
				<swiper-item>
					<view class="swiper-item">
						<view class="swiper-item-img">
							<image class="itemImg" :src="guide1"></image>
						</view>
					</view>
				</swiper-item>
				<swiper-item>
					<view class="swiper-item">
						<view class="swiper-item-img">
							<image class="itemImg" :src="guide2"></image>
						</view>
					</view>
				</swiper-item>
				<swiper-item>
					<view class="swiper-item">
						<view class="swiper-item-img">
							<image class="itemImg" :src="guide3"></image>
						</view>
						<view class="swiper-item-btn" @click="setLaunchFlag()">立即体验</view>
					</view>
				</swiper-item>
			</swiper>
			<view class="jump-over" @click="setLaunchFlag()">跳过</view>
			<view class="bannerDots" v-if="currenTab!=3">
				<view class="banner-dot" v-for="(item,index) in bannerDot" :key="index"
					:class="{'active':index===currenTab}">
				</view>
			</view>
		</view>
	</view>
</template>

<script>
	import resources from '@/libs/resources.js'
	export default {
		data() {
			return {
				bannerDot: [0, 1, 2],
				currenTab: 0,
				guide1: resources.guide.guide1,
				guide2: resources.guide.guide2,
				guide3: resources.guide.guide3
			}
		},
		methods: {
			setLaunchFlag() {
				uni.setStorageSync('launchFlag', true)
				uni.reLaunch({
					url: '/pages/login/index'
				})
			},
			onChange(e) {
				this.currenTab = e.detail.current
			},
		}
	}
</script>
<style lang="scss">
	page {
		width: 100%;
		height: 100%;
	}

	.guide-v {
		width: 100%;
		height: 100%;

		.status-bar {
			height: var(--status-bar-height);
			width: 100%;
			background-color: #FFFFFF;

			.top-view {
				height: var(--status-bar-height);
				width: 100%;
				position: fixed;
				background-color: #FFFFFF;
				top: 0;
				z-index: 999;
			}
		}

		.content {
			width: 100%;
			height: 100%;
			background-size: 100% auto;
			padding: 0;
			touch-action: none;
			position: fixed;
		}

		.swiper {
			width: 100%;
			height: 100% !important;
			background: #FFFFFF;
		}

		.itemImg {
			width: 100%;
			height: 100%;
		}

		.swiper-item {
			width: 100%;
			height: 100%;
		}

		.swiper-item-img {
			width: 100%;
			height: 100%;
		}

		.swiper-item-text {
			.swiper-item-title {
				line-height: 130rpx;
				font-size: 87rpx;
				color: $u-type-primary;
				font-weight: 500;
			}

			.swiper-item-content {
				font-size: 28rpx;
				color: #666666;
			}
		}

		.bannerDots {
			width: 100%;
			height: 16rpx;
			display: flex;
			position: fixed;
			bottom: 8%;
			z-index: 99;
			left: 50%;
			transform: translate(-50%);
			align-items: center;
			justify-content: center;

			.banner-dot {
				width: 16rpx;
				height: 16rpx;
				border-radius: 50%;
				background: #CACACA;
				margin: 0 10rpx;

				&.active {
					width: 40rpx;
					height: 16rpx;
					background: $u-type-primary;
					border-radius: 8rpx;
				}
			}
		}

		.jump-over {
			position: absolute;
			z-index: 999;
			right: 46rpx;
			top: 86rpx;
			width: 128rpx;
			height: 54rpx;
			line-height: 54rpx;
			color: #fff;
			border-radius: 27rpx;
			text-align: center;
			font-size: 32rpx;
			background: rgba(123, 123, 123, 0.42);
		}

		.swiper-item-btn {
			position: absolute;
			right: 7rem;
			bottom: 10rem;
			text-align: center;
			width: 296rpx;
			height: 84rpx;
			background-color: #E8F2FF;
			opacity: 1;
			border-radius: 50rpx;
			line-height: 84rpx;
			color: #3463FF;
			font-size: 28rpx;
			z-index: 99999;
			letter-spacing: 2rpx;
		}
	}
</style>