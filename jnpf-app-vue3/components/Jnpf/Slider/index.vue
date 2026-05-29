<template>
	<u-slider class="jnpf-slider" v-model="innerValue" :step="step" :min="min" :max="max" :disabled="disabled"
		@end="end">
		<view class="slider-badge-button">{{innerValue}}</view>
	</u-slider>
</template>
<script>
	export default {
		name: 'jnpf-slider',
		props: {
			modelValue: {
				type: [Number, String],
				default: 0
			},
			min: {
				type: Number,
				default: 0
			},
			max: {
				type: Number,
				default: 100
			},
			step: {
				type: Number,
				default: 1
			},
			disabled: {
				type: Boolean,
				default: false
			},
		},
		data() {
			return {
				innerValue: 0
			}
		},
		watch: {
			modelValue: {
				handler(val) {
					this.innerValue = val
				},
				immediate: true,
				deep: true
			},
			innerValue: {
				handler(val) {
					this.$emit('update:modelValue', val)
					this.$emit('change', val)
				},
				immediate: true,
				deep: true
			}
		},
		methods: {
			end() {
				this.$emit('update:modelValue', this.innerValue)
				this.$emit('change', this.innerValue)
			}
		}
	}
</script>
<style lang="scss" scoped>
	.jnpf-slider {
		width: 100%;

		.slider-badge-button {
			padding: 4rpx 6rpx;
			background-color: $u-type-primary;
			color: #fff;
			border-radius: 10rpx;
			font-size: 22rpx;
			line-height: 1;
		}
	}
</style>