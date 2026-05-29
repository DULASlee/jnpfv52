<template>
	<view class="jnpf-select">
		<selectBox v-model="innerValue" :placeholder="placeholder" @openSelect="openSelect" :select-open="selectShow"
			v-if="isFlow">
		</selectBox>
		<MultSelect :list="list" :show="selectShow" :value-name="props.value" :label-name="props.label"
			:defaultValue="defaultValue" @confirm="selectConfirm" @close="close" :filterable="filterable"
			:multiple="multiple" />
	</view>
</template>
<script>
	import MultSelect from '@/components/MultSelect'
	import selectBox from '@/components/selectBox'
	export default {
		name: 'jnpf-select',
		components: {
			MultSelect,
			selectBox
		},
		props: {
			modelValue: {
				type: [String, Number, Array],
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
			placeholder: {
				type: String,
				default: '请选择'
			},
			multiple: {
				type: Boolean,
				default: false
			},
			disabled: {
				type: Boolean,
				default: false
			},
			filterable: {
				type: Boolean,
				default: false
			},
			isFlow: {
				type: Boolean,
				default: true
			},
		},
		data() {
			return {
				innerValue: '',
				defaultValue: [],
				selectShow: false,
			}
		},
		watch: {
			modelValue: {
				immediate: true,
				handler(val) {
					this.setDefault()
				},
			},
			options: {
				immediate: true,
				handler(val) {
					this.setDefault()
				},
			}
		},
		computed: {
			list() {
				return this.options
			}
		},
		methods: {
			openSelect() {
				if (this.disabled) return
				this.selectShow = true
			},
			selectConfirm(e) {
				if (this.multiple) {
					this.innerValue = e.label
					this.defaultValue = e.value
					this.$emit('update:modelValue', e.value || [])
					this.$emit('change', e.value || [], e.list || [])
				} else {
					if (!e.length) return
					const selectData = e[0]
					this.innerValue = selectData[this.props.label]
					this.defaultValue = [e[0][this.props.value]]
					this.$emit('update:modelValue', selectData[this.props.value])
					this.$emit('change', selectData[this.props.value], selectData)
				}
			},
			setDefault() {
				if (!this.options.length) return this.innerValue = ''
				if (this.multiple) {
					this.innerValue = ''
					this.defaultValue = []
					if (!this.modelValue || !this.modelValue.length) return
					this.defaultValue = this.modelValue
					for (let i = 0; i < this.options.length; i++) {
						const item = this.options[i]
						for (let j = 0; j < this.modelValue.length; j++) {
							const it = this.modelValue[j]
							if (item[this.props.value] == it) {
								if (!this.innerValue) {
									this.innerValue += item[this.props.label]
								} else {
									this.innerValue += ',' + item[this.props.label]
								}
							}
						}
					}
				} else {
					this.innerValue = ''
					this.defaultValue = []
					if (!this.modelValue && this.modelValue !== 0) return
					for (let i = 0; i < this.options.length; i++) {
						if (this.options[i][this.props.value] == this.modelValue) {
							this.defaultValue = [this.modelValue]
							this.innerValue = this.options[i][this.props.label]
							return
						}
					}
				}
			},
			close() {
				this.selectShow = false
			}
		}
	}
</script>
<style lang="scss" scoped>
	.jnpf-select {
		width: 100%;

		.u-input__content__field-wrapper__field {
			overflow: auto;
		}
	}
</style>