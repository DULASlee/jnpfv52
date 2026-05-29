<template>
	<view class="jnpf-switch">
		<u-switch v-model="innerValue" :active-value="activeValue" :inactive-value="inactiveValue" :disabled="disabled"
			@change="onChange" :size="size"></u-switch>
	</view>

</template>
<script>
	export default {
		name: 'jnpf-switch',
		props: {
			modelValue: {
				type: [String, Number, Boolean],
			},
			activeValue: {
				type: [String, Number, Boolean],
				default: 1
			},
			inactiveValue: {
				type: [String, Number, Boolean],
				default: 0
			},
			size: {
				default: 40
			},
			disabled: {
				type: Boolean,
				default: false
			},
		},
		data() {
			return {
				innerValue: ''
			}
		},
		watch: {
			modelValue: {
				handler(val) {
					this.innerValue = !!Number(val)
				},
				immediate: true,
			}
		},
		methods: {
			onChange(val) {
				this.$emit('update:modelValue', val)
				this.$emit('change', val)
			},
		}
	}
</script>
<style scoped lang="scss">
	.jnpf-switch {
		width: 100%;
		display: flex;
		justify-content: flex-end;
		align-items: center;
	}
</style>