<template>
	<view class="contacts-v" v-show="show">
		<view class="contactusBanner">
			<image :src="contactus" mode="widthFix"></image>
		</view>
		<view class="contactus u-flex-col">
			<view class="u-flex items u-m-b-20" v-for="(item,index) in list" @click="Jump(index)" :key="index">
				<view :class="item.bcg" style="" class="items-iconBox u-m-r-50 u-padding-15">
					<u-icon :name="item.icon" color="#ffffff" size="54"></u-icon>
				</view>
				<view class="u-flex-col u-flex-1">
					<text>{{item.name}}</text>
					<text class="againColor">{{item.title}}</text>
				</view>
				<view>
					<u-icon name="arrow-right" color="#969799" size="28"></u-icon>
				</view>
			</view>
			<view class="serviceTime u-flex-col u-p-l-32 u-p-b-20 u-p-t-20">
				<text class="u-font-xl" type='title'>服务时间</text>
				<text class="textSize">工作日：{{workingHours}}</text>
				<text class="textSize">节假日：{{holidayWorkingHours}}</text>
			</view>
		</view>
		<view class="copyright">{{copyright}}</view>
		<u-popup v-model="showPopup" mode="center">
			<view class="center-box">
				<image class="image" :src="wechat_qrcode" :data-path="wechat_qrcode" @longpress="saveImage" />
			</view>
		</u-popup>
	</view>
</template>

<script>
	import resources from '@/libs/resources.js'
	export default {
		onHide() { uni.hideLoading() },
		onUnload() { uni.hideLoading() },
		data() {
			return {
				contactus: resources.banner.contactus,
				wechat_qrcode: resources.common.wechat_qrcode,
				holidayWorkingHours: '9:30-12:00 ，13:30-17:30',
				workingHours: '8:30-12:00 ，13:00-20:00',
				tell: '400-6868-969',
				url: 'https://www.baobab.com',
				showPopup: false,
				list: [{
						name: '微信公众号',
						title: '扫码关注官网微信公众号',
						icon: 'weixin-fill',
						bcg: 'u-type-success-bg'
					},
					{
						name: '服务热线',
						title: '400-6868-969',
						icon: 'kefu-ermai',
						bcg: 'u-type-warning-bg'
					},
					{
						name: '官方网站',
						title: 'www.baobab.com',
						icon: 'ie',
						bcg: 'u-type-primary-bg'
					}
				],
				copyright: 'Copyright © 2024 面包树科技有限公司出品',
				show: false
			}
		},
		onLoad() {
			uni.showLoading({
				title: '加载中'
			});
			this.copyright = uni.getStorageSync('copyright') || 'Copyright © 2024 面包树科技有限公司出品'
			setTimeout(() => {
				uni.hideLoading()
				this.show = true
			}, 800)

		},
		methods: {
			Jump(index) {
				switch (index) {
					case 0:
						this.showPopup = true
						break;
					case 1:
						uni.makePhoneCall({
							phoneNumber: this.tell,
						})
						break;
					case 2:
						// #ifdef APP-PLUS
						plus.runtime.openURL(this.url);
						// #endif
						// #ifndef APP-PLUS
						uni.navigateTo({
							url: '/pages/apply/externalLink/index?fullName=面包树科技有限公司&url=' + encodeURIComponent(
								this.url)
						})
						// #endif
						break;
				}
			},
			saveImage(e) {
				// #ifdef APP-PLUS
				uni.getImageInfo({
					src: e.currentTarget.dataset.path,
					success: (res) => {
						uni.saveImageToPhotosAlbum({
							filePath: res.path,
							success: function() {
								helper.msg('保存成功', 'success');
							}
						});
					}
				});
				// #endif
			},
		}
	}
</script>

<style lang="scss">
	page {
		width: 100%;
		background-color: #f0f2f6;
	}

	.contacts-v {
		width: 100%;

		.contactusBanner {
			width: 100%;
			height: 280rpx;

			image {
				width: 100%;
				height: 100%;
			}
		}

		.contactus {
			margin: 18rpx 16rpx 0;

			.againColor {
				color: #909399;
			}

			.items {
				padding: 20rpx 32rpx;
				background-color: #FFFFFF;
				justify-content: start;
				border-radius: 8rpx;

				.items-iconBox {
					border-radius: 50%;
					height: 88rpx;
					width: 88rpx;
					display: flex;
					align-items: center;
					justify-content: center;
				}
			}

			.serviceTime {
				background-color: #FFFFFF;
				border-radius: 8rpx;
			}
		}

		.center-box {
			width: 420rpx;
			height: 420rpx;

			image {
				width: 100%;
				height: 100%;
			}
		}

		.textSize {
			height: 66rpx;
			line-height: 66rpx;
			color: #909399;
		}

		text[type="title"] {
			height: 86rpx;
			line-height: 86rpx;
			font-weight: 700;
		}
	}
</style>
