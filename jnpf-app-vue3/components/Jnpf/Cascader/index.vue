<template>
	<view class="jnpf-cascader">
		<selectBox v-model="innerValue" :placeholder="placeholder" @openSelect="openSelect" :select-open="selectShow">
		</selectBox>
		<Tree v-if="selectShow" v-model="selectShow" :multiple="multiple" :props="props" :selectList="selectList"
			:options="options" :selectedId="!multiple ? [modelValue] : modelValue" :filterable='filterable'
			:selectData="selectData" :clearable="clearable" :showAllLevels="showAllLevels" @close="handleClose"
			@confirm="handleConfirm" />
	</view>
</template>

<script>
	import selectBox from '@/components/selectBox'
	import Tree from './Tree';
	export default {
		name: 'jnpf-cascader',
		components: {
			Tree,
			selectBox
		},
		props: {
			modelValue: {
				default: ''
			},
			placeholder: {
				type: String,
				default: '请选择'
			},
			options: {
				type: Array,
				default: () => []
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
			multiple: {
				type: Boolean,
				default: false
			},
			showAllLevels: {
				type: Boolean,
				default: true
			},
			filterable: {
				type: Boolean,
				default: false
			},
			clearable: {
				type: Boolean,
				default: false
			},
		},
		watch: {
			modelValue: {
				handler(val) {
					this.setDefault(this.modelValue)
				},
				immediate: true
			},
			options: {
				handler(val) {
					this.setDefault(this.modelValue)
				},
				deep: true
			}
		},
		data() {
			return {
				selectShow: false,
				innerValue: '',
				selectList: [],
				selectData: [],
				allList: []
			}
		},
		methods: {
			async setDefault(value) {
				this.innerValue = ''
				this.selectData = []
				this.selectList = []
				if (!value || !value?.length) return
				this.allList = await this.treeToArray(value)
				if (!this.multiple) value = [value]
				let txt = []
				for (let i = 0; i < value.length; i++) {
					let val = uni.$u.deepClone(value[i])
					for (let j = 0; j < val.length; j++) {
						inner: for (let k = 0; k < this.allList.length; k++) {
							if (val[j] === this.allList[k][this.props.value]) {
								val[j] = this.allList[k][this.props.label];
								this.selectData.push(this.allList[k])
								break;
							}
						}
					}
					txt.push(val)
				}
				this.selectList = txt.map(o => this.showAllLevels ? o.join('/') : o[o.length - 1])
				this.innerValue = this.selectList.join()
			},
			async treeToArray() {
				let options = uni.$u.deepClone(this.options)
				let list = []
				const loop = (options) => {
					for (let i = 0; i < options.length; i++) {
						const item = options[i]
						list.push(item)
						if (item[this.props.children] && Array.isArray(item[this.props.children])) {
							loop(item[this.props.children])
						}
					}
				}
				loop(options)
				return list
			},
			openSelect() {
				if (this.disabled) return
				this.selectShow = true
			},
			handleConfirm(e, selectId, selectData) {
				this.selectList = e;
				this.innerValue = e.join()
				if (!this.multiple) {
					this.$emit('update:modelValue', selectId[0])
					this.$emit('change', selectId[0], selectData[0])
				} else {
					this.$emit('update:modelValue', selectId)
					this.$emit('change', selectId, selectData)
				}
			},
			handleClose() {
				this.selectShow = false
			}
		}
	}
</script>
<style lang="scss" scoped>
	.jnpf-cascader {
		width: 100%;
	}
</style>