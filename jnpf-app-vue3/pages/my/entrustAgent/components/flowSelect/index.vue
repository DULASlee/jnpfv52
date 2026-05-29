<template>
	<view class="jnpf-tree-select">
		<u-input input-align='right' type="select" :select-open="selectShow" v-model="innerValue"
			:placeholder="placeholder" @click="openSelect"></u-input>
		<flowSelect v-model="selectShow" @confirm="selectConfirm" :multiple="multiple" :selectedData="selectedData"
			:clearable="clearable" ref="flowSelect" :toUserId="toUserId" :isAuthority='current'>
		</flowSelect>
	</view>
</template>

<script>
	import flowSelect from './Select.vue';
	import {
		getFlowEngineListByIds
	} from '@/api/workFlow/flowEngine.js'

	export default {
		components: {
			flowSelect
		},
		props: {
			modelValue: {
				default: ''
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
			toUserId: {
				type: [String, Array],
				default: ''
			},
			current: {
				type: [String, Number],
				default: ''
			},
		},
		data() {
			return {
				selectShow: false,
				innerValue: '',
				selectedData: [],
				selectedIds: [],
				delegateUser: '',
			}
		},
		watch: {
			modelValue: {
				handler(val) {
					if (!val || !val.length) return this.innerValue = ''
					this.setDefault(val)
				},
				immediate: true
			}
		},
		methods: {
			setDefault(id) {
				if (!id || !id.length) return this.innerValue = ''
				getFlowEngineListByIds(id).then(res => {
					let data = res.data || []
					this.innerValue = data.map(o => o.fullName).join()
					this.selectedData = data.map(o => {
						return {
							...o,
							fullName: o.fullName + '/' + o.enCode,
						};
					});
				})
			},
			openSelect() {
				if (this.disabled) return
				if (!this.toUserId.length) return this.$u.toast('请先选择受委托人');
				this.selectShow = true
				this.$refs.flowSelect.resetData()
				this.setDefault()
			},
			selectConfirm(e) {
				this.selectShow = false
				this.selectedData = e;
				let label = ''
				let value = []
				this.defaultValue = []
				for (let i = 0; i < e.length; i++) {
					label += (i ? ',' : '') + e[i].fullName
					value.push(e[i].id)
				}
				this.defaultValue = value
				this.innerValue = label
				if (!this.multiple) {
					this.$emit('update:modelValue', value)
					this.$emit('change', value.join(), e[0])
					return
				}
				this.$emit('update:modelValue', value)
				this.$emit('change', value, e)
			}
		}
	}
</script>
<style lang="scss" scoped>
	.jnpf-tree-select {
		width: 100%;
	}
</style>