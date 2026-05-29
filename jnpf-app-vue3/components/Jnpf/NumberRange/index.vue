<template>
	<view class="jnpf-num-range">
		<u-input input-align='right' v-model="min" :placeholder="$t('component.jnpf.numberRange.min')" type="number" @blur="onblur($event,'min')" />
		<text class="separator">-</text>
		<u-input input-align='right' v-model="max" :placeholder="$t('component.jnpf.numberRange.max')" type="number" @blur="onblur($event,'max')" />
	</view>
</template>

<script>
	export default {
		name: 'jnpf-number-range',
		props: {
			modelValue: {
				type: Array,
				default: () => []
			},
			disabled: {
				type: Boolean,
				default: false
			},
			precision: {
				type: Number,
				default: undefined
			},
		},
		data() {
			return {
				min: '',
				max: ''
			}
		},
		watch: {
			modelValue: {
				handler(val) {
					if (Array.isArray(val) && val.length === 2) {
						this.min = val[0]
						this.max = val[1]
					} else {
						this.min = ''
						this.max = ''
					}
				},
				immediate: true,
			},
			min(val) {
				this.onChange()
			},
			max(val) {
				this.onChange()
			}
		},
		methods: {
			onblur(e, type) {
				this[type] = !isNaN(this.precision) && (e == '0' || e) ? Number(e).toFixed(this.precision) : e
			},
			onChange() {
				if ((!this.min && this.min !== 0) && (!this.max && this.max !== 0)) return this.$emit('update:modelValue',
					[])
				this.$emit('update:modelValue', [this.min, this.max])
			}
		}
	}
</script>
<style lang="scss" scoped>
	.jnpf-num-range {
		width: 100%;
		display: flex;
		justify-content: space-between;
		align-items: center;

		.separator {
			margin: 0 20rpx;
			flex-shrink: 0;
		}
	}
</style>