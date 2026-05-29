<template>
	<view class="jnpf-dep-select">
		<selectBox v-model="innerValue" :placeholder="placeholder" @openSelect="openSelect" :select-open="selectShow">
		</selectBox>
		<Tree v-if="selectShow" v-model="selectShow" :multiple="multiple" :props="props" :selectedData="selectedData"
			:options="options" :ids="multiple ? modelValue : [modelValue]" @close="handleClose" @confirm="handleConfirm"
			:selectType="selectType" />
	</view>
</template>
<script>
	import selectBox from '@/components/selectBox'
	import Tree from './Tree.vue';
	import {
		selectedList,
		getOrgByOrganizeCondition
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
			},
			type: {
				type: String,
				default: 'user'
			},
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
				let val = this.multiple ? this.modelValue : [this.modelValue];
				if (this.selectType !== 'all') {
					this.options = await baseStore.getDepartmentTree()
					this.allList = await this.treeToArray()
					this.options = []
					let query = {
						'departIds': this.ableIds
					}
					await getOrgByOrganizeCondition(query).then(res => {
						this.options = res.data.list
						if (!val || !val.length) return
						this.setDefault()
					})
				} else {
					let ids = val
					const query = {
						ids
					};
					if (!this.modelValue) return
					selectedList(query).then(res => {
						this.options = res.data.list || []
						this.selectedData = []
						this.$nextTick(() => {
							if (this.multiple) {
								this.innerValue = this.options.map(o => o.fullName).join(',') || '';
								this.options.map(o => {
									this.selectedData.push({
										'fullName': o.fullName,
										'id': o.id
									})
								})
							} else {
								this.innerValue = this.options[0]?.fullName || ''
								this.selectedData.push({
									'fullName': this.options[0]?.fullName,
									'id': this.options[0]?.id
								})
							}
						})
					})
				}
			},
			setDefault() {
				this.selectedData = [];
				let val = this.multiple ? this.modelValue : [this.modelValue];
				let txt = ''
				for (let i = 0; i < val.length; i++) {
					inner: for (let j = 0; j < this.allList.length; j++) {
						if (this.allList[j].id === val[i]) {
							this.selectedData.push({
								'fullName': this.allList[j].fullName,
								'id': this.allList[j].id
							})
							break inner
						}
					}
				}
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
					this.$emit('update:modelValue', selectId)
					this.$emit('change', e[0], selectId)
				} else {
					this.$emit('update:modelValue', selectId)
					this.$emit('change', e, selectId)
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