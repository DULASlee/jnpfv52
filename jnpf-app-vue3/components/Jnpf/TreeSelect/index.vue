<template>
	<view class="jnpf-tree-select">
		<u-input type="select" input-align='right' :select-open="selectShow" v-model="innerValue"
			:placeholder="placeholder" @click="openSelect" />
		<Tree v-if="selectShow" v-model="selectShow" :default-value="defaultValue" :options="options"
			:multiple="multiple" :lastLevel="lastLevel" :lastLevelKey="lastLevelKey" :lastLevelValue="lastLevelValue"
			:props="props" :filterable="filterable" @close="handleClose" @confirm="handleConfirm" />
	</view>
</template>

<script>
	import Tree from './Tree';
	export default {
		name: 'jnpf-tree-select',
		components: {
			Tree
		},
		props: {
			modelValue: {
				default: ''
			},
			options: {
				type: Array,
				default: () => []
			},
			placeholder: {
				type: String,
				default: '请选择'
			},
			props: {
				type: Object,
				default: () => ({
					label: 'fullName',
					value: 'id',
					children: 'children'
				})
			},
			disabled: {
				type: Boolean,
				default: false
			},
			filterable: {
				type: Boolean,
				default: false
			},
			multiple: {
				type: Boolean,
				default: false
			},
			// 只能选择最后一层的数值
			lastLevel: {
				type: Boolean,
				default: false
			},
			// 只能选择最后一层的数值时，需要根据lastLevelKey来判断是否最后一层
			lastLevelKey: {
				type: String,
				default: "hasChildren"
			},
			lastLevelValue: {
				default: false
			},
		},
		data() {
			return {
				selectShow: false,
				innerValue: '',
				defaultValue: []
			}
		},
		watch: {
			options() {
				this.setDefault()
			},
			modelValue: {
				handler(val) {
					this.setDefault()
				},
				immediate: true
			}
		},
		methods: {
			setDefault() {
				if (!this.modelValue || !this.options) {
					this.defaultValue = []
					this.innerValue = ''
					return
				}
				const value = typeof(this.modelValue) === 'string' ? this.modelValue.split(',') : this.modelValue
				let label = ''
				const loop = (value, list) => {
					for (let i = 0; i < list.length; i++) {
						let item = list[i]
						if (value === item[this.props.value]) {
							label += (!label ? '' : ',') + item[this.props.label]
							break
						}
						if (item.children && Array.isArray(item.children) && item.children.length) {
							loop(value, item.children)
						}
					}
				}
				for (let i = 0; i < value.length; i++) {
					loop(value[i], this.options)
				}
				this.innerValue = label
				this.defaultValue = value
			},
			openSelect() {
				if (this.disabled) return
				this.selectShow = true
			},
			handleClose() {
				this.selectShow = false
			},
			handleConfirm(e) {
				let label = ''
				let value = []
				for (let i = 0; i < e.length; i++) {
					label += (!label ? '' : ',') + e[i][this.props.label]
					value.push(e[i][this.props.value])
				}
				this.defaultValue = value
				this.innerValue = label
				if (!this.multiple) {
					this.$emit('update:modelValue', value[0])
					this.$emit('change', value[0], e[0])
				} else {
					this.$emit('update:modelValue', value)
					this.$emit('change', value, e)
				}
			}
		}
	}
</script>
<style lang="scss" scoped>
	.jnpf-tree-select {
		width: 100%;
	}
</style>