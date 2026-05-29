<template>
	<view class="jnpf-tree-select">
		<u-input input-align='right' type="select" :select-open="selectShow" v-model="innerValue"
			:placeholder="placeholder" @click="openSelect" />
		<Tree v-model="selectShow" :options="options" :multiple="multiple" :props="props" :selectedData="selectedData"
			:selectType="selectType" :query='query' :clearable="clearable" @close="handleClose"
			@confirm="handleConfirm" />
	</view>
</template>

<script>
	import Tree from './Tree.vue';
	import {
		getSelectedList,
	} from '@/api/common.js'
	export default {
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
			ableDepIds: {
				type: Array,
				default: () => []
			},
			ableIds: {
				type: Array,
				default: () => []
			},
			selectType: {
				type: String,
				default: 'all'
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
			clearable: {
				type: Boolean,
				default: false
			},
			multiple: {
				type: Boolean,
				default: false
			},
		},
		data() {
			return {
				selectShow: false,
				innerValue: '',
				selectedData: [],
				query: {
					ids: this.ableIds
				},
			}
		},
		watch: {
			modelValue: {
				handler(val) {
					this.getSelectedList(val)
				},
				immediate: true
			}
		},
		methods: {
			openSelect() {
				if (this.disabled) return
				this.selectShow = true
			},
			getSelectedList(id) {
				if (!id || !id.length) {
					this.innerValue = ''
					this.selectedData = []
					return
				}
				let ids = this.multiple ? id : [id]
				getSelectedList(ids).then(res => {
					let subVal = ''
					let resList = res.data.list || [];
					let txt = ''
					let arrSubVal = []
					for (let i = 0; i < resList.length; i++) {
						txt += (i ? ',' : '') + resList[i].fullName
						subVal = resList[i].id + '--' + resList[i].type
						if (this.multiple) arrSubVal.push(subVal)
					}
					this.innerValue = txt
					this.selectedData = resList
				})
			},
			handleClose() {
				this.selectShow = false
			},
			handleConfirm(id, selectList) {
				if (!this.multiple) {
					this.$emit('update:modelValue', id[0])
					this.$emit('change', id[0], selectList[0])
				} else {
					this.$emit('update:modelValue', id)
					this.$emit('change', id, selectList)
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