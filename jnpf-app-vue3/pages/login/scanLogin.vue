<template>
	<view class="scanLogin-v">
		<view class="scanLogin-icon">
			<view class="icon-ym icon-ym-pc"></view>
		</view>
		<view class="title">登录确认</view>
		<view class="tip">请确认是否本人操作</view>
		<view class="tip">并确保二维码来源安全</view>
		<view class="scanLogin-actions">
			<u-button class="buttom-btn" type="primary" @click="handelConfirmLogin" v-if="!expired">确认登录</u-button>
			<u-button class="buttom-btn" type="primary" @click="reScan" v-if="expired">重新扫码登录</u-button>
			<text class="goBackText" @click="goBack()">取消</text>
		</view>
	</view>
</template>

<script>
	import {
		setCodeCertificateStatus,
		confirmLogin
	} from '@/api/common.js'

	export default {
		data() {
			return {
				ticket: '',
				expired: false
			}
		},
		onLoad(option) {
			this.init(option.id || '')
		},
		methods: {
			init(ticket) {
				this.ticket = ticket
				this.expired = false
				setCodeCertificateStatus(ticket, '1')
			},
			goBack() {
				setCodeCertificateStatus(this.ticket, '-1').then(res => {
					uni.navigateBack()
				})
			},
			reScan() {
				uni.scanCode({
					success: res => {
						let url = ""
						if (this.isJSON(res.result.trim())) {
							const result = JSON.parse(res.result.trim())
							if (result.t === 'login') this.init(result.id || '')
						}
					}
				});
			},
			isJSON(str) {
				try {
					var obj = JSON.parse(str);
					if (typeof obj == 'object' && obj) {
						return true;
					} else {
						return false;
					}
				} catch (e) {
					return false;
				}
			},
			handelConfirmLogin() {
				confirmLogin(this.ticket).then(res => {
					if (res.data.status === -1) {
						uni.showToast({
							title: '二维码已失效，请重新扫码登录',
							icon: 'none'
						})
						this.expired = true
						return;
					}
					if (res.data.status === 2) {
						uni.showToast({
							title: '登录成功',
							icon: 'none',
							complete: () => {
								setTimeout(() => {
									uni.navigateBack()
								}, 1500)
							}
						})
					}
				})
			}
		}
	}
</script>

<style lang="scss">
	page {
		width: 100%;
		height: 100%;
	}

	.scanLogin-v {
		height: 100%;
		position: relative;
		text-align: center;
		padding-top: 160rpx;

		.scanLogin-icon {
			height: 140rpx;
			width: 140rpx;
			margin: 0 auto 64rpx;
			display: flex;
			justify-content: center;
			align-items: center;
			border-radius: 50%;
			border: 4rpx solid #2979ff;
			color: #2979ff;

			.icon-ym-pc {
				font-size: 80rpx;
			}
		}

		.title {
			font-size: 40rpx;
			font-weight: 600;
			line-height: 56rpx;
			margin-bottom: 30rpx;
		}

		.tip {
			font-size: 28rpx;
			color: #7E7E7E;
			line-height: 44rpx;
		}

		.scanLogin-actions {
			margin-top: 270rpx;
			padding: 0 64rpx;

			.buttom-btn {
				margin-bottom: 20rpx;
			}

			.goBackText {
				line-height: 80rpx;
			}
		}
	}
</style>