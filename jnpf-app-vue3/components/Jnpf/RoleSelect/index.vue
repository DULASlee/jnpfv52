<template>
	<view class="jnpf-role-select">
		<selectBox v-model="innerValue" :placeholder="placeholder" @openSelect="openSelect" :select-open="selectShow">
		</selectBox>
		<Tree v-model="selectShow" :options="options" :multiple="multiple" :props="props" :selectedData="selectedData"
			:selectId="!multiple ? [modelValue] : modelValue" @close="handleClose" @confirm="handleConfirm">
		</Tree>
	</view>
</template>
<script>
	import Tree from './Tree.vue';
	import selectBox from '@/components/selectBox'
	import {
		getRoleSelector,
		getRoleCondition
	} from '@/api/common.js'
	import {
		useBaseStore
	} from '@/store/modules/base'
	const baseStore = useBaseStore()
	export default {
		name: 'jnpf-role-select',
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
				options: [],
				selectedData: [],
				hasRole: [],
			}
		},
		watch: {
			modelValue(val) {
				this.setDefault()
			}
		},
		created() {
			this.innerValue = ""
			this.getOptions()
		},
		methods: {
			async getOptions() {
				this.options = await baseStore.getRoleTree()
				this.treeToArray(this.options)
				this.selectType === 'all' ? this.setDefault() : this.getRoleCondition()
			},
			getRoleCondition() {
				this.options = []
				let query = {
					ids: this.ableIds
				}
				getRoleCondition(query).then(res => {
					this.options = res.data.list || [];
					this.setDefault()
				})
			},
			treeToArray(options) {
				for (let i = 0; i < options.length; i++) {
					let item = options[i]
					inner: for (let key in item) {
						if (item[key] === 'role') this.hasRole.push(item)
						if (key == 'children' && item[key] != null && item[key].length > 0) {
							this.treeToArray(item[key])
							break inner
						}
					}
				}
			},
			setDefault(value) {
				if (!this.modelValue || !this.modelValue.length) {
					this.innerValue = ''
					this.selectedData = [];
					return
				}
				let val = this.multiple ? this.modelValue : [this.modelValue];
				this.innerValue = ''
				this.selectedData = []
				for (let i = 0; i < val.length; i++) {
					inner: for (let j = 0; j < this.hasRole.length; j++) {
						if (this.hasRole[j].id === val[i]) {
							this.selectedData.push(this.hasRole[j])
							break inner
						}
					}
				}
				let txt = ''
				for (let i = 0; i < this.selectedData.length; i++) {
					txt += (i ? ',' : '') + this.selectedData[i].fullName
				}
				this.innerValue = txt
			},
			handleClose() {
				this.selectShow = false
			},
			handleConfirm(e, selectId) {
				this.selectedData = [];
				this.innerValue = '';
				this.innerValue = e.map(o => o.fullName).join(',')
				if (!this.multiple) {
					this.$emit('update:modelValue', selectId[0])
					this.$emit('change', selectId[0], e[0])
				} else {
					this.$emit('update:modelValue', selectId)
					this.$emit('change', selectId, e)
				}
			},
			openSelect() {
				if (this.disabled) return
				this.selectShow = true
			},
		}
	}
</script>
<style lang="scss" scoped>
	.jnpf-role-select {
		width: 100%;
	}
</style>