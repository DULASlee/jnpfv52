<template>
	<view class="jnpf-input-number">
		<!-- 数字输入 -->
		<view v-if="!detailed">
			<u-number-box v-if="controls" v-model="innerValue" :step="step" :min="min" :max="max" :key="key"
				:disabled="disabled" :positive-integer="false" :input-height="60" @blur="onNumberBlur"
				@change="onChange" />
			<view v-else class="input-content" :class="{'input-border':addonBefore||addonAfter}">
				<view class="input-left u-line-1" v-if="addonBefore">{{addonBefore}}</view>
				<view class="input-center">
					<u-input v-model="innerValue" :placeholder="placeholder"
						:input-align='addonBefore || addonAfter? "center":"right"' :disabled="disabled"
						:clearable="false" @focus="onFocus" @blur="onBlur" @input="onInputChange">
					</u-input>
				</view>
				<view class="input-right u-line-1" v-if="addonAfter">{{addonAfter}}</view>
			</view>
		</view>
		<!-- 详情 -->
		<view v-else>
			<view class="detail-content u-flex">
				<view class="detail-left u-line-1" v-if="addonBefore&&!controls">{{addonBefore}}</view>
				<view class="detail-center">{{thousands?jnpf.thousandsFormat(innerValue) :innerValue}}</view>
				<view class="detail-right u-line-1" v-if="addonAfter&&!controls">{{addonAfter}}</view>
			</view>
		</view>
		<!-- 大写金额 -->
		<view class="amount-chinese-name" v-if="isAmountChinese&&getChineseName">{{getChineseName}}</view>
	</view>

</template>
<script>
	/**
	 * inputNumber 数字输入框
	 * @modelValue v-model
	 * @min {Number} 最小值
	 * @max {Number} 最大值
	 * @step {Number} 每次点击改变的间隔大小
	 * @placeholder {String} 占位符
	 * @addonBefore {String} 前缀
	 * @addonAfter {String} 后缀
	 * @thousands {Boolean} 金额千位符
	 * @isAmountChinese {Boolean} 金额大写
	 * @detailed {Boolean} 详情
	 */

	export default {
		name: 'jnpf-input-number',
		props: {
			modelValue: {
				type: [Number, String],
				default: ''
			},
			min: {
				type: Number,
				default: -999999999999999
			},
			max: {
				type: Number,
				default: 999999999999999
			},
			step: {
				type: Number,
				default: 1
			},
			disabled: {
				type: Boolean,
				default: false
			},
			addonBefore: {
				default: ''
			},
			addonAfter: {
				default: ''
			},
			precision: {
				type: Number
			},
			controls: {
				type: Boolean,
				default: false
			},
			thousands: {
				type: Boolean,
				default: false
			},
			isAmountChinese: {
				type: Boolean,
				default: false
			},
			detailed: {
				type: Boolean,
				default: false
			},
			type: {
				default: ''
			},
			placeholder: {
				default: '请输入'
			},
		},
		data() {
			return {
				innerValue: null,
				key: +new Date()
			}
		},
		watch: {
			modelValue: {
				handler(val) {
					this.setValue(val)
				},
				immediate: true
			},
			getChineseName(val) {
				uni.$emit('initCollapse')
			}
		},
		computed: {
			getChineseName() {
				if (!this.isAmountChinese || (!this.getNumberValue && this.getNumberValue !== 0)) return ""
				return this.jnpf.getAmountChinese(this.getNumberValue)
			},
			getNumberValue() {
				return this.handleConvertNum(this.innerValue)
			},
		},
		methods: {
			setValue(val) {
				this.innerValue = (!val && val !== 0) || isNaN(val) ? null : Number(val);
				if (!this.innerValue && this.innerValue !== 0) return
				if (this.innerValue < this.min) this.innerValue = this.min
				if (this.innerValue > this.max) this.innerValue = this.max
				if (!isNaN(this.precision)) {
					const value = Number(this.getNumberValue).toFixed(this.precision)
					this.innerValue = this.controls ? Number(value) : value
				}
				if (this.thousands) this.innerValue = this.jnpf.thousandsFormat(this.innerValue)
			},
			onChange() {
				this.setValue(this.innerValue)
				this.$nextTick(() => {
					this.$emit('update:modelValue', this.innerValue)
					this.$emit('change', this.innerValue)
				})
			},
			onInputChange() {
				this.$nextTick(() => {
					this.$emit('change', this.innerValue)
				})
			},
			onNumberBlur() {
				this.key = +new Date()
				this.setValue(this.innerValue)
				this.$emit('blur', this.innerValue)
			},
			onBlur(val) {
				this.setValue(this.getNumberValue)
				this.$emit('blur', this.getNumberValue)
				this.$emit('update:modelValue', this.getNumberValue)
			},
			onFocus() {
				if (!this.innerValue) return
				if (this.innerValue.toString().indexOf('e+') > -1) return
				this.innerValue = !isNaN(this.precision) ? Number(this.getNumberValue).toFixed(this.precision) : this
					.getNumberValue
			},
			handleConvertNum(val) {
				if (!val && val !== 0) return null
				let num = this.$u.deepClone(val.toString().split("."))
				const arr2 = num.length > 1 ? num[1].split("").filter(o => (!isNaN(o))).join('') : []
				let arr = num[0].split("").filter(o => (!isNaN(o))).join('');
				let res = num[1] ? arr + '.' + arr2 : Number(arr)
				return val.toString().indexOf('-') != -1 ? Number('-' + res) : res
			}
		}
	}
</script>
<style lang="scss" scoped>
	.jnpf-input-number {
		width: 100%;
		display: flex;
		flex-direction: column;
		align-items: flex-end;

		:deep(.u-number-input) {
			width: 150rpx !important;
		}

		.input-content {
			display: flex;
			border-radius: 10rpx;
			height: 74rpx;

			&.input-border {
				border: 1rpx solid rgb(220, 223, 230)
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
		}

		.detail-content {
			.detail-left {
				max-width: 128rpx;
				padding-right: 16rpx;
				color: #909399;
			}

			.detail-right {
				max-width: 128rpx;
				padding-left: 16rpx;
				color: #909399;
			}
		}

		.amount-chinese-name {
			color: #999999;
			line-height: 40rpx;
			padding: 10rpx 10rpx 0 0;
		}
	}
</style>