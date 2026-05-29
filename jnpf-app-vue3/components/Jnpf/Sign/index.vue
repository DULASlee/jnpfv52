<template>
	<view class="jnpf-sign" :class="align=='right'?'flex-end':'flex-start'" v-if="signType === 'currency'">
		<view class="jnpf-sign-box">
			<template v-if="showBtn">
				<image class="jnpf-sign-img" :src="innerValue" mode="scaleToFill" v-show="innerValue"
					@tap.stop="handlePreviewImage(innerValue)" />
				<view class="jnpf-sign-btn" v-if="!detailed && !isInvoke" @click="addSign()">
					<i class="icon-ym icon-ym-signature" />
					<view class="title" v-if="!innerValue">{{ signTip }}</view>
				</view>
				<view class="jnpf-sign-btn" v-if="!detailed && isInvoke" @click="showSignMode()">
					<i class="icon-ym icon-ym-signature" />
					<view class="title" v-if="!innerValue">{{ signTip }}</view>
				</view>
			</template>

		</view>
	</view>
	<view class="u-flex signature-box" v-else>
		<view class="jnpf-sign-btn" @click="showSignMode()">
			<i class="icon-ym icon-ym-signature" />
			<view class="title">添加签名</view>
		</view>
		<image class="jnpf-sign-img" :src="modelValue" mode="scaleToFill" v-show="modelValue "
			@tap.stop="handlePreviewImage(modelValue )" />
	</view>
	<u-action-sheet :list="signOptions" v-model="signMode" @click="handleCommand"></u-action-sheet>
	<Sign ref="sign" @input="setValue" />
</template>
<script>
	import Sign from './Sign.vue'
	export default {
		name: 'jnpf-sign',
		emits: ['change', 'update:modelValue'],
		components: {
			Sign
		},
		props: {
			modelValue: {
				type: [String, Number, Boolean],
			},
			fieldKey: {
				type: String,
				default: ''
			},
			signTip: {
				type: String,
				default: '添加签名'
			},
			disabled: {
				type: Boolean,
				default: false
			},
			detailed: {
				type: Boolean,
				default: false
			},
			align: {
				type: String,
				default: 'right'
			},
			showBtn: {
				type: Boolean,
				default: true
			},
			defaultCurrent: {
				type: Boolean,
				default: false
			}, // 默认签名
			isInvoke: {
				type: Boolean,
				default: false
			}, // 调用签名
			signType: {
				type: String,
				default: 'currency'
			},
		},
		data() {
			return {
				innerValue: '',
				signMode: false,
				signList: [],
				signOptions: [{
					value: '1',
					text: '手写签名'
				}, {
					value: '2',
					text: '调用签名'
				}],
			}
		},
		created() {
			uni.$on('setSignValue', (res) => {
				let fieldKey = uni.getStorageSync('sign-fieldKey')
				if (this.fieldKey == fieldKey) this.setValue(res)
			})
		},
		onLoad(e) {
			this.$nextTick(async () => {
				await this.getSignData()
			})
		},
		onUnload() {
			uni.$off('setSignValue')
		},
		watch: {
			modelValue: {
				handler(val) {
					this.innerValue = val || ''
				},
				deep: true,
				immediate: true
			},
		},
		methods: {
			showSignMode() {
				if (this.disabled) return
				this.signMode = true
			},
			handleCommand(index) {
				if (index !== 1) return this.addSign()
				uni.navigateTo({
					url: '/pages/apply/interPage/signInvokeList' + '?fieldKey=' + this.fieldKey + '&signVal=' +
						encodeURIComponent(this.modelValue)
				})
			},
			addSign() {
				if (this.disabled) return
				this.$nextTick(() => {
					this.$refs.sign.showSignature();
				})
			},
			setValue(val) {
				if (!val) return
				this.innerValue = val
				this.$emit('update:modelValue', val)
				this.$emit('change', val)
			},
			handlePreviewImage(url) {
				// #ifndef MP
				uni.previewImage({
					urls: [url],
					current: url,
					success: () => {},
					fail: () => {
						uni.showToast({
							title: '预览图片失败',
							icon: 'none'
						});
					}
				});
				// #endif
			}
		}
	}
</script>

<style lang="scss">
	.signature-box {
		width: 100%;
		border-top: 1rpx solid #fbfbfc;

		justify-content: space-between;

		.jnpf-sign-btn {
			color: #2188ff;
			display: flex;
			flex-shrink: 0;
			height: 56rpx;

			.icon-ym-signature {
				font-size: 52rpx;
			}

			.title {
				line-height: 56rpx;
				font-size: 28rpx;
			}
		}

		.jnpf-sign-img {
			width: 160rpx;
			height: 56rpx;
			flex-shrink: 0;
			background-color: #fff !important;
		}
	}

	.jnpf-sign {
		width: 100%;
		display: flex;
		align-items: center;

		&.flex-end {
			justify-content: flex-end;
		}

		&.flex-start {
			justify-content: flex-start;
		}

		.jnpf-sign-box {
			display: flex;
		}

		.jnpf-sign-img {
			width: 160rpx;
			height: 80rpx;
			flex-shrink: 0;
		}

		.jnpf-sign-btn {
			color: #2188ff;
			width: 100%;
			display: flex;
			flex-shrink: 0;
			height: 56rpx;

			.icon-ym-signature {
				font-size: 52rpx;
			}

			.title {
				line-height: 56rpx;
				font-size: 28rpx;
			}
		}
	}
</style>