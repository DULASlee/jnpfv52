<template>
	<u-radio-group class="jnpf-radio" v-model="innerValue" :disabled="disabled"
		:wrap="direction == 'horizontal' ? false : true" @change="onChange">
		<u-radio v-for="(item, index) in options" :key="index" :name="item[props.value]">
			{{ item[props.label] }}
		</u-radio>
	</u-radio-group>
</template>
<script>
	export default {
		inheritAttrs: false,
		name: 'jnpfRadio',
		props: {
			modelValue: {
				type: [String, Number, Boolean],
			},
			direction: {
				type: String,
				default: "horizontal"
			},
			options: {
				type: Array,
				default: () => []
			},
			props: {
				type: Object,
				default: () => ({
					label: 'fullName',
					value: 'id'
				})
			},
			disabled: {
				type: Boolean,
				default: false
			}
		},
		data() {
			return {
				innerValue: ''
			}
		},
		watch: {
			modelValue: {
				handler(val) {
					this.innerValue = val
				},
				immediate: true,
			}
		},
		methods: {
			onChange(value, e) {
				const selectData = this.options.filter(o => value == o[this.props.value]) || []
				this.$emit('update:modelValue', value)
				this.$emit('change', value, selectData[0])
			},
		}
	}
</script>