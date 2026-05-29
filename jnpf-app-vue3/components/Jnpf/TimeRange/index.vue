<template>
	<view class="jnpf-time-range">
		<JnpfDateRange v-model="value" :placeholder="placeholder" :format='format' :disabled="disabled" :type="type" />
	</view>
</template>
<script>
	export default {
		name: 'jnpf-time-range',
		props: {
			modelValue: {
				type: Array,
				default: () => []
			},
			placeholder: {
				type: String,
				default: '请选择时间范围'
			},
			disabled: {
				type: Boolean,
				default: false
			},
			format: {
				type: String,
				default: 'yyyy-MM-dd HH:mm:ss'
			},
			type: {
				type: String,
				default: 'time'
			}
		},
		data() {
			return {
				value: ""
			}
		},
		watch: {
			modelValue: {
				handler(val) {
					this.value = val
				},
				immediate: true
			},
			value(val) {
				this.$emit('update:modelValue', val)
			}
		}
	}
</script>
<style lang="scss" scoped>
	.jnpf-time-range {
		width: 100%;
	}
</style>