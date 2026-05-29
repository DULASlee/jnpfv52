<template>
	<view class="jnpf-group-select">
		<selectBox v-model="innerValue" :placeholder="placeholder" @openSelect="openSelect" :select-open="selectShow">
		</selectBox>
		<Tree v-model="selectShow" :options="options" :multiple="multiple" :props="props" :selectedData="selectedData"
			:selectId="!multiple ? [modelValue] : modelValue" @close="handleClose" @confirm="handleConfirm" />
	</view>
</template>

<script>
	import Tree from './Tree.vue';
	import selectBox from '@/components/selectBox'
	import {
		getGroupCondition
	} from '@/api/common.js'
	import {
		useBaseStore
	} from '@/store/modules/base'
	const baseStore = useBaseStore()
	export default {
		name: 'jnpf-group-select',
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
			},
		},
		data() {
			return {
				selectShow: false,
				innerValue: '',
				options: [],
				selectedData: [],
				allList: [],
			}
		},
		watch: {
			modelValue: {
				handler(val) {
					this.getOptions()
				},
				immediate: true
			}
		},
		methods: {
			getOptions() {
				this.selectType === 'custom' ? this.getGroupCondition() : this.getAllOptions()
			},
			setDefault() {
				if (!this.modelValue || !this.modelValue.length) return this.setNullValue();
				this.selectedData = [];
				let val = this.multiple ? this.modelValue : [this.modelValue];
				for (let i = 0; i < val.length; i++) {
					inner: for (let j = 0; j < this.allList.length; j++) {
						if (this.allList[j].id === val[i]) {
							this.selectedData.push(this.allList[j])
							break inner
						}
					}
				}
				this.innerValue = this.selectedData.map(o => o.fullName).join();
			},
			async getAllOptions() {
				this.options = await baseStore.getGroupTree()
				this.allList = await this.treeToArray()
				this.setDefault()
			},
			getGroupCondition() {
				let query = {
					ids: this.ableIds
				}
				getGroupCondition(query).then(res => {
					this.options = res.data.list || [];
					this.allList = this.treeToArray()

					this.setDefault()
				})
			},
			setNullValue() {
				this.innerValue = '';
				this.selectedData = [];
			},
			treeToArray() {
				let options = JSON.parse(JSON.stringify(this.options))
				let list = []
				const loop = (options) => {
					for (let i = 0; i < options.length; i++) {
						const item = options[i]
						list.push(item)
						if (item.children && Array.isArray(item.children)) loop(item.children)
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
				if (!this.multiple) {
					this.$emit('update:modelValue', selectId[0])
					this.$emit('change', selectId[0], e[0])
				} else {
					this.$emit('update:modelValue', selectId)
					this.$emit('change', selectId, e)
				}
			},
			handleClose() {
				this.selectShow = false
			}
		}
	}
</script>
<style lang="scss" scoped>
	.jnpf-group-select {
		width: 100%;
	}
</style>