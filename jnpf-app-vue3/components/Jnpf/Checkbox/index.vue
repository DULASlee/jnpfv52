<template>
	<u-checkbox-group class="jnpf-checkbox" :disabled="disabled" :wrap="direction == 'horizontal' ? false : true"
		@change="onChange">
		<u-checkbox v-model="item.checked" v-for="(item, index) in optionList" :key="index" :name="item[props.value]">
			{{item[props.label]}}
		</u-checkbox>
	</u-checkbox-group>
</template>
<script>
	export default {
		name: 'jnpf-checkbox',
		inheritAttrs: false,
		props: {
			modelValue: {
				type: Array,
				default: () => []
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
				optionList: []
			}
		},
		watch: {
			modelValue: {
				handler(val) {
					if (!val || !val?.length) return this.setColumnData()
					this.setDefault()
				},
				immediate: true,
			},
			options: {
				handler(val) {
					this.setColumnData()
				},
				immediate: true,
			}
		},
		methods: {
			setDefault() {
				if (!this.modelValue || !this.modelValue?.length) return
				outer: for (let i = 0; i < this.modelValue.length; i++) {
					inner: for (let j = 0; j < this.optionList.length; j++) {
						if (this.modelValue[i] === this.optionList[j][this.props.value]) {
							this.optionList[j].checked = true
							break inner
						}
					}
				}
			},
			setColumnData() {
				this.optionList = this.options.map(o => ({
					...o,
					checked: false
				}))
				this.setDefault()
			},
			onChange(value) {
				const selectData = this.optionList.filter(o => o.checked) || []
				this.$emit('update:modelValue', value)
				this.$emit('change', value, selectData)
			},
		}
	}
</script>