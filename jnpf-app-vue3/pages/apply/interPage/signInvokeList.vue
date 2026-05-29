<template>
	<view class="">
		<NoData v-if="!signListInvoke.length"></NoData>
		<view class="page_v u-flex-col" v-else>
			<view v-for="(item,index) in signListInvoke" :key="index" :class="item.isDefault ? 'active' : '' "
				class="lists_box" @click="setMainSignInvoke(item,index)">
				<view class="signImgBox">
					<image :src="item.signImg" mode="scaleToFill" class="signImg"></image>
				</view>
				<view class="icon-checked-box" v-if="item.isDefault==1">
					<view class="icon-checked">
						<u-icon name="checkbox-mark" color="#fff" size="28"></u-icon>
					</view>
				</view>
			</view>
		</view>
		<!-- 底部按钮 -->
		<view class="flowBefore-actions">
			<u-button class="buttom-btn" @click.stop="eventLauncher('cancel')">{{$t('common.cancelText')}}</u-button>
			<u-button class="buttom-btn" type="primary" @click.stop="eventLauncher('confirm')">{{$t('common.okText')}}</u-button>
		</view>
	</view>
</template>

<script>
	import NoData from "@/components/noData";
	import {
		getSignImgList
	} from "@/api/common";

	export default {
		components: {
			NoData
		},
		data() {
			return {
				show: false,
				signListInvoke: [],
			};
		},
		async onLoad(config) {
			this.signListInvoke = await this.getSignData()
			this.clearChoose()
			let val = decodeURIComponent(config.signVal)
			if (val) {
				this.setMainSignInvokeWithValue(val)
			}
			uni.setStorageSync('sign-fieldKey', config.fieldKey)
		},
		methods: {
			getSignData() {
				return new Promise((resolve, reject) => {
					if (!this.signListInvoke.length) {
						getSignImgList().then(res => {
							resolve(res.data || [])
						})
					} else {
						resolve(this.signListInvoke)
					}
				})
			},
			clearChoose() {
				for (let i = 0; i < this.signListInvoke.length; i++) {
					let item = this.signListInvoke[i]
					item.isDefault = 0
					this.$set(this.signListInvoke, i, item)
				}
			},
			setMainSignInvokeWithValue(val) {
				for (let i = 0; i < this.signListInvoke.length; i++) {
					let item = this.signListInvoke[i]
					if (item.signImg === val) {
						this.setMainSignInvoke(item, i)
						break;
					}
				}
			},
			eventLauncher(type) {
				if (type === 'cancel') uni.navigateBack();
				if (type === 'confirm') {
					let choose = this.signListInvoke.filter(item => item.isDefault === 1)
					if (!choose || !choose.length) return this.$u.toast(`请选择签名`)
					this.$nextTick(() => uni.$emit('setSignValue', choose[0].signImg))
					uni.navigateBack()
				}
			},
			setMainSignInvoke(item, index) {
				this.clearChoose()
				item.isDefault = 1
				this.$set(this.signListInvoke, index, item)
			},
		}
	}
</script>

<style lang="scss">
	page {
		height: 100%;
		background-color: #f0f2f6;
	}

	.page_v {
		height: 100%;
		padding: 0 10px 60px;

		.active {
			border: 1rpx solid #2979FF;
			color: #2979FF;

			.icon-ym-organization {
				&::before {
					color: #2979FF !important;
				}
			}
		}

		.sign-mask {
			width: 100%;
			height: 200rpx;
			background: rgba(0, 0, 0, .3);
			position: absolute;
			top: 0;
			border-radius: 8rpx;
			display: flex;
			align-items: center;
			flex-direction: column;
			justify-content: center;

			.sign-mask-btn {
				width: 60%;
				display: flex;
			}
		}

		.lists_box {
			width: 100%;
			height: 200rpx;
			border-radius: 8rpx;
			position: relative;
			display: flex;
			flex-direction: column;
			justify-content: center;
			background-color: #FFFFFF;
			margin-top: 20rpx;

			.signImgBox {
				width: 100%;
				height: 100%;
				text-align: center;
				border-radius: 8rpx;

				.signImg {
					width: 100%;
					height: 100%;
					border-radius: 8rpx;
				}
			}

			.icon-checked-box {
				display: flex;
				width: 140rpx;
				height: 80rpx;
				position: absolute;
				transform: scale(0.9);
				right: -4rpx;
				bottom: -2rpx;
				flex-direction: row;
				align-items: center;


				.icon-checked {
					width: 44rpx;
					height: 44rpx;
					border: 40rpx solid #1890ff;
					border-left: 40rpx solid transparent;
					border-top: 40rpx solid transparent;
					border-bottom-right-radius: 12rpx;
					position: absolute;
					transform: scale(0.95);
					right: -8rpx;
					bottom: -6rpx;
				}
			}
		}
	}
</style>