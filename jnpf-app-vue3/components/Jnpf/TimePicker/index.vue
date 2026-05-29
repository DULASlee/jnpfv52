<template>
	<view class="jnpf-time-pickeer">
		<JnpfDatePicker v-model="value" :scene="scene" :inputType="inputType" :placeholder="placeholder"
			:disabled="disabled" :type="type" :startTime="startTime" :endTime="endTime" :format="format"
			:selectType='selectType' @change="change" />
	</view>
</template>
<script>
	export default {
		name: 'jnpf-time-pickeer',
		props: {
			scene: {
				type: String,
				default: 'form'
			},
			inputType: {
				type: String,
				default: 'select'
			},
			modelValue: {
				type: [String, Number],
				default: ''
			},
			placeholder: {
				type: String,
				default: '请选择'
			},
			disabled: {
				type: Boolean,
				default: false
			},
			type: {
				type: String,
				default: 'time'
			},
			startTime: {
				type: [String, Number],
				default: 0
			},
			selectType: {
				type: String,
				default: ''
			},
			endTime: {
				type: [String, Number],
				default: 0
			},
			format: {
				type: String,
				default: 'yyyy-MM-dd HH:mm:ss'
			},
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
		},
		methods: {
			change(value, type) {
				this.$emit('change', value, type)
			}
		}
	}
</script>
<style lang="scss" scoped>
	.jnpf-time-pickeer {
		width: 100%;
	}
</style>