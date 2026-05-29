<template>
	<view class="jnpf-tree-select">
		<u-input input-align='right' type="select" :select-open="selectShow" v-model="innerValue"
			:placeholder="placeholder" @click="openSelect" v-if="isInput" />
		<Tree v-model="selectShow" :options="options" :multiple="multiple" :selectedData="selectedData"
			:clearable="clearable" ref="userTree" @close="handleClose" @confirm="handleConfirm" :scope="scope"
			:entrustType="entrustType" />
	</view>
</template>

<script>
	import Tree from './Tree.vue';
	import {
		getUserInfoList
	} from '@/api/common.js'
	export default {
		components: {
			Tree
		},
		props: {
			modelValue: {
				default: ''
			},
			isInput: {
				type: Boolean,
				default: true
			},
			options: {
				type: Array,
				default: () => []
			},
			placeholder: {
				type: String,
				default: '请选择'
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
			entrustType: {
				type: [String, Number],
				default: 0
			},
			scope: {
				type: Number,
				default: 1
			}
		},
		data() {
			return {
				selectShow: false,
				innerValue: '',
				selectedData: []
			}
		},
		watch: {
			modelValue: {
				handler(val) {
					this.setDefault()
				},
				immediate: true
			}
		},
		methods: {
			setDefault() {
				if (!this.modelValue || !this.modelValue.length) return this.setNullValue();
				this.selectedData = []
				const ids = this.multiple ? this.modelValue : [this.modelValue];
				if (!Array.isArray(ids)) return;
				getUserInfoList(ids).then(res => {
					if (!this.modelValue || !this.modelValue.length) return this.setNullValue();
					const list = res.data.list
					this.selectedData = list
					this.innerValue = this.selectedData.map(o => o.fullName).join();
				})
			},
			setNullValue() {
				this.innerValue = '';
				this.selectedData = [];
			},
			openSelect() {
				if (this.disabled) return
				this.selectShow = true
			},
			handleClose() {
				this.selectShow = false
			},
			handleConfirm(e) {
				this.selectedData = e;
				let label = ''
				let value = []
				for (let i = 0; i < e.length; i++) {
					label += (!label ? '' : ',') + e[i].fullName
					value.push(e[i].id)
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