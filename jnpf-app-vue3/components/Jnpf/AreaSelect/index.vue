<template>
	<view class="jnpf-area-select">
		<u-input input-align='right' type="select" :select-open="selectShow" v-model="innerValue"
			:placeholder="placeholder" @click="openSelect" />
		<Tree v-if="selectShow" v-model="selectShow" :multiple="multiple" :props="props" :selectedData="selectedData"
			:level='level' :ids='modelValue' @confirm="handleConfirm" @close="handleClose()" />
	</view>
</template>

<script>
	import Tree from './Tree.vue';
	import {
		getProvinceSelectorInfoList
	} from '@/api/common.js'
	export default {
		name: 'jnpf-area-select',
		components: {
			Tree
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
			level: {
				type: Number,
				default: 2
			}
		},
		watch: {
			modelValue: {
				handler(val) {
					this.setDefault(val)
				},
				immediate: true
			}
		},
		data() {
			return {
				selectShow: false,
				innerValue: '',
				selectedData: []
			}
		},
		methods: {
			setDefault(id) {
				this.innerValue = ''
				this.selectedData = []
				if (!Array.isArray(id) || id.length === 0) return
				if (!this.multiple) id = [id]
				getProvinceSelectorInfoList(id).then(res => {
					const list = res.data
					let txt = ''
					for (let i = 0; i < list.length; i++) {
						txt += (i ? ',' : '') + list[i].join('/')
						this.selectedData.push(list[i].join('/'))
					}
					this.innerValue = txt
				})
			},
			openSelect() {
				if (this.disabled) return
				this.selectShow = true
			},
			handleClose() {
				this.selectShow = false
			},
			handleConfirm(e, selectId, selectData) {
				this.selectedData = e;
				let label = '';
				let value = [];
				this.defaultValue = value
				this.innerValue = e.join()
				if (!this.multiple) {
					this.$emit('update:modelValue', selectId[0])
					this.$emit('change', selectId[0], selectData)
				} else {
					this.$emit('update:modelValue', selectId)
					this.$emit('change', selectId, selectData)
				}
			},

		}
	}
</script>
<style lang="scss" scoped>
	.jnpf-area-select {
		width: 100%;
	}
</style>