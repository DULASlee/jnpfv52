<template>
	<view class="jnpf-input">
		<template v-if="!detailed">
			<view class="input-content" :class="{'input-border':addonBefore||addonAfter}">
				<view class="input-left u-line-1" v-if="addonBefore">{{addonBefore}}</view>
				<view class="input-center">
					<u-input input-align='right' :border="false" v-model="innerValue"
						:type="showPassword?'password':'text'" :maxlength="maxlength||maxlength===0?maxlength:9999"
						:placeholder="placeholder" :disabled="disabled" :clearable='disabled ? false : clearable'
						@input="onInput" @blur="onBlur" />
				</view>
				<!-- #ifndef H5 -->
				<text class="icon-ym icon-ym-scanCode1" v-if="useScan" @click="scanCode" />
				<!-- #endif -->
				<view class="input-right u-line-1" v-if="addonAfter">{{addonAfter}}</view>
				<view class="input-count" v-if="showCount&&!addonBefore&&!addonAfter">
					<text>{{ innerValue?String(innerValue).length:0 }}</text><text
						v-if="maxlength">/{{ maxlength }}</text>
				</view>
			</view>
		</template>
		<view class="detail-text" :class="{ ellipsis: showOverflow ,['detail-text-'+align]:true}" v-else>
			<text class="detail-text-addon" v-if="addonBefore">{{ addonBefore }}</text>
			{{ maskedValue }}
			<text class="detail-text-addon" v-if="addonAfter">{{ addonAfter }}</text>
		</view>
	</view>
</template>
<script>
	import {
		useTextMask
	} from './useTextMask';
	export default {
		name: 'jnpf-input',
		props: {
			modelValue: {
				type: [String, Number],
				default: ''
			},
			placeholder: {
				type: String,
				default: '请输入'
			},
			maxlength: {
				type: Number,
				default: null
			},
			showPassword: {
				type: Boolean,
				default: false
			},
			disabled: {
				type: Boolean,
				default: false
			},
			clearable: {
				type: Boolean,
				default: false
			},
			detailed: {
				type: Boolean,
				default: false
			},
			showOverflow: {
				type: Boolean,
				default: false
			},
			addonBefore: {
				type: String,
				default: ''
			},
			addonAfter: {
				type: String,
				default: ''
			},
			align: {
				type: String,
				default: 'right'
			},
			useScan: {
				type: Boolean,
				default: false
			},
			useMask: {
				type: Boolean,
				default: false
			},
			maskConfig: {
				type: Object,
				default: () => {}
			},
			showCount: {
				type: Boolean,
				default: false
			},
		},
		data() {
			return {
				innerValue: '',
				maskedValue: '',
			}
		},
		watch: {
			modelValue: {
				handler(val) {
					this.innerValue = val
					if (!this.useMask) return (this.maskedValue = val);
					const {
						getMaskedText
					} = useTextMask(this.maskConfig);
					this.maskedValue = getMaskedText(val);
				},
				immediate: true,
			}
		},
		methods: {
			onInput(val) {
				this.$nextTick(() => {
					this.$emit('update:modelValue', val)
					this.$emit('change', val)
				})
			},
			onBlur(val) {
				this.$emit('blur', val)
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
			scanCode() {
				uni.scanCode({
					success: res => {
						if (!res.result || typeof res.result !== 'string') return
						this.onInput(res.result)
					}
				});
			}
		}
	}
</script>
<style lang="scss">
	.jnpf-input {
		width: 100%;

		.input-content {
			display: flex;
			border-radius: 10rpx;
			height: 74rpx;

			&.input-border {
				border: 1rpx solid rgb(220, 223, 230)
			}

			.input-center {
				flex: 1;
				// padding: 0 8rpx;
			}

			.input-left,
			.input-right {
				flex-shrink: 0;
				width: 128rpx;
				background-color: #f5f7fa;
				color: #909399;
				padding: 0 10rpx;
				text-align: center;
			}

			.input-left {
				border-right: 1rpx solid #dcdfe6;
				border-radius: 10rpx 0 0 10rpx;
			}

			.input-right {
				border-left: 1rpx solid #dcdfe6;
				border-radius: 0px 10px 10px 0px;
			}

			.icon-ym-scanCode1 {
				margin-right: 8rpx;
				color: #909399;
			}

			.input-count {
				color: #909399;
				font-size: 24rpx;
			}
		}

		.detail-text {
			word-break: break-all;
			text-align: right;

			.detail-text-addon {
				color: #909399;
			}

			&.ellipsis {
				overflow: hidden;
				white-space: nowrap;
				text-overflow: ellipsis;
			}

			&.detail-text-left {
				text-align: left;
			}
		}
	}
</style>