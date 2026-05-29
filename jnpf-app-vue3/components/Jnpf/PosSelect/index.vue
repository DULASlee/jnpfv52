<template>
	<view class="jnpf-dep-select">
		<selectBox v-model="innerValue" :placeholder="placeholder" @openSelect="openSelect" :select-open="selectShow">
		</selectBox>
		<Tree v-if="selectShow" v-model="selectShow" :multiple="multiple" :props="props" :selectedData="selectedData"
			:options="options" :ids="multiple?modelValue:[modelValue]" title='岗位选择' @close="handleClose"
			@confirm="handleConfirm" />
	</view>
</template>

<script>
	import Tree from './Tree.vue';
	import selectBox from '@/components/selectBox'
	import {
		getPositionByPositionCondition
	} from '@/api/common.js'
	import {
		useBaseStore
	} from '@/store/modules/base'
	const baseStore = useBaseStore()
	export default {
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
				immediate: true,
				handler(val) {
					this.getOptions()
					if (!val || !val.length) {
						this.innerValue = ''
						this.selectedData = [];
					}
				}
			}
		},
		methods: {
			async getOptions() {
				this.options = await baseStore['getPositionTree']()
				this.allList = await this.treeToArray()
				if (this.selectType !== 'all') {
					this.options = []
					let query = {}
					query.ids = this.ableIds
					await getPositionByPositionCondition(query).then(res => {
						this.options = res.data.list
					})
				}
				if (!this.modelValue || !this.modelValue.length) return
				this.setDefault()
			},
			setDefault() {
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
				let txt = ''
				for (let i = 0; i < this.selectedData.length; i++) {
					if (!!this.selectedData[i].lastFullName) {
						txt += (i ? ',' : '') + this.selectedData[i].lastFullName
					} else {
						txt += (i ? ',' : '') + this.selectedData[i].fullName
					}
				}
				this.innerValue = txt
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
	.jnpf-dep-select {
		width: 100%;
	}
</style>