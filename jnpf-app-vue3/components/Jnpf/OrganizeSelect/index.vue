<template>
	<view class="jnpf-organize-select">
		<selectBox v-model="innerValue" :placeholder="placeholder" @openSelect="openSelect" :select-open="selectShow">
		</selectBox>
		<Tree v-model="selectShow" :multiple="multiple" :props="props" :selectedData="selectedData" :options="options"
			:ids='multiple?modelValue:[modelValue]' @close="handleClose" @confirm="handleConfirm"
			:selectType="selectType" />
	</view>
</template>

<script>
	import Tree from './Tree';
	import selectBox from '@/components/selectBox'
	import {
		useBaseStore
	} from '@/store/modules/base'
	import {
		getOrgByOrganizeCondition,
		selectedList
	} from '@/api/common.js'
	const baseStore = useBaseStore()
	export default {
		name: 'jnpf-organize-select',
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
			props: {
				type: Object,
				default: () => ({
					label: 'fullName',
					value: 'id',
					children: 'children',
					isLeaf: 'isLeaf'
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
			selectType: {
				type: String,
				default: 'all'
			},
			ableIds: {
				type: Array,
				default: () => []
			}
		},
		data() {
			return {
				selectShow: false,
				innerValue: '',
				selectedData: [],
				allList: [],
				options: []
			}
		},
		watch: {
			modelValue: {
				handler(val) {
					this.getOptions()
				},
				immediate: true
			},
		},
		methods: {
			async getOptions() {
				this.options = await baseStore.getDepartmentTree()
				this.allList = await this.treeToArray()
				if (this.selectType !== 'all') {
					const departIds = this.ableIds ? this.ableIds.map(o => o[o.length - 1]) : [];
					const query = {
						departIds
					};
					await getOrgByOrganizeCondition(query).then(res => {
						this.options = res.data.list || []
					})
				}
				if (!this.modelValue || !this.modelValue.length) {
					this.innerValue = ''
					this.selectedData = [];
					return
				}
				this.setDefault()
			},
			setDefault() {
				let val = this.multiple ? this.modelValue : [this.modelValue];
				let textList = []
				for (let i = 0; i < val.length; i++) {
					let item = val[i];
					inner: for (let j = 0; j < this.allList.length; j++) {
						if (item.toString() === this.allList[j].organizeIds.toString()) {
							item = this.allList[j].organize
							break inner
						}
					};
					textList.push(item)
				}
				this.selectedData = textList
				this.innerValue = this.selectedData.join(',')
			},
			async treeToArray() {
				let options = JSON.parse(JSON.stringify(this.options))
				let list = []
				const loop = (options) => {
					for (let i = 0; i < options.length; i++) {
						const item = options[i]
						list.push(item)
						if (item.hasChildren && item.children && Array.isArray(item.children)) {
							loop(item.children)
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
			handleConfirm(e, selectId) {
				this.$emit('update:modelValue', selectId)
				this.$emit('change', selectId, e)
			},
			handleClose() {
				this.selectShow = false
			}
		}
	}
</script>
<style lang="scss" scoped>
	.jnpf-organize-select {
		width: 100%;
	}
</style>