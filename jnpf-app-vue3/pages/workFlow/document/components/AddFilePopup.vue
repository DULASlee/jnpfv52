<template>
	<u-popup class="jnpf-select" :maskCloseAble="maskCloseAble" mode="bottom" v-model="showPopup"
		:safeAreaInsetBottom="safeAreaInsetBottom" @close="close">
		<view class="u-select">
			<view class="u-select__header" @touchmove.stop.prevent="">
				<view class="u-select__header__cancel u-select__header__btn" :style="{ color: cancelColor }"
					hover-class="u-hover-class" :hover-stay-time="150" @tap="close()"
					style="width: 60rpx;text-align: center;">
					<text v-if="cancelBtn">{{cancelText}}</text>
				</view>
				<view class="u-select__header__title" style="flex: 1;text-align: center;">
					{{title}}
				</view>
				<view class="u-select__header__confirm u-select__header__btn" :style="{ color: confirmColor }"
					style="width: 60rpx;text-align: center;" hover-class="u-hover-class" :hover-stay-time="150"
					@touchmove.stop="" @tap.stop="handleConfirm()">
					<text v-if="confirmBtn">{{confirmText}}</text>
				</view>
			</view>

			<view class="u-select__body u-select__body__multiple">
				<scroll-view :scroll-y="true" style="height: 100%">
					<view class="u-flex u-p-l-20 u-p-r-20" style="height: 100rpx;border-bottom: 1rpx solid #f0f2f6;"
						@click="radioGroupChange('add')">
						<text class="u-m-r-20 u-font-28 icon-ym icon-ym-add-folder"></text>
						<text>新建文件夹</text>
					</view>
					<!-- #ifndef APP-HARMONY -->
					<view class="u-flex u-p-l-20 u-p-r-20 uploadFileBtn" @click="radioGroupChange('up')">
						<JnpfUploadFileComment ref="lsjUpload" height="100rpx" childId="upload" :size="10"
							:parentId="parentId" @callback="onCallback">
						</JnpfUploadFileComment>
					</view>
					<!-- #endif -->
				</scroll-view>
			</view>
		</view>
	</u-popup>
</template>
<script>
	import resources from '@/libs/resources.js'
	export default {
		props: {
			height: {
				type: [Number, String],
				default: ''
			},
			cancelBtn: {
				type: Boolean,
				default: true
			},
			confirmBtn: {
				type: Boolean,
				default: true
			},
			show: {
				type: Boolean,
				default: false
			},
			cancelColor: {
				type: String,
				default: '#606266'
			},
			confirmColor: {
				type: String,
				default: '#2979ff'
			},
			safeAreaInsetBottom: {
				type: Boolean,
				default: false
			},
			maskCloseAble: {
				type: Boolean,
				default: true
			},
			title: {
				type: String,
				default: ''
			},
			parentId: {
				type: [String, Number],
				default: 0
			},
			cancelText: {
				type: String,
				default: '取消'
			},
			confirmText: {
				type: String,
				default: '确认'
			}
		},
		data() {
			return {
				showPopup: false,
				icon: resources.message.nodata,
				option: {},
				id: ''
			}
		},
		watch: {
			show: {
				handler(val) {
					this.showPopup = val
				},
				immediate: true
			}
		},
		computed: {
			baseURL() {
				return this.define.baseURL
			},
			token() {
				return uni.getStorageSync('token')
			},
		},
		methods: {
			//文件上传
			onCallback(e) {
				this.$emit('onCallback', e)
			},
			// 获取默认选中的值
			radioGroupChange(e) {
				this.$emit('confirm', e)
			},
			close() {
				this.$emit('close');
			},
		}
	}
</script>
<style scoped lang="scss">
	.notData-box {
		width: 100%;
		height: 100%;
		justify-content: center;
		align-items: center;
		margin-top: -50px;

		.notData-inner {
			width: 286rpx;
			height: 222rpx;
			align-items: center;

			.iconImg {
				width: 100% !important;
				height: 100% !important;
			}
		}

		.notData-inner-text {
			color: #909399;
		}
	}

	.jnpf-select {
		width: 100%;

		.u-select {
			&__header {
				display: flex;
				flex-direction: row;
				align-items: center;
				justify-content: space-between;
				height: 40px;
				padding: 0 20px;
				position: relative;

				::after {
					content: "";
					position: absolute;
					border-bottom: 0.5px solid #eaeef1;
					transform: scaleY(0.5);
					bottom: 0;
					right: 0;
					left: 0;
				}
			}

			&__body {
				width: 100%;

				overflow: hidden;
				background-color: #fff;

				&__picker-view {
					height: 100%;
					box-sizing: border-box;

					&__item {
						display: flex;
						align-items: center;
						justify-content: center;
						font-size: 32rpx;
						padding: 0 8rpx;
					}
				}


			}
		}
	}
</style>