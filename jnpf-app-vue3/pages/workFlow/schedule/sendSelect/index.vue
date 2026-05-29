<template>
	<view class="jnpf-tree-select">
		<u-input input-align='right' type="select" :select-open="selectShow" v-model="innerValue"
			:placeholder="placeholder" @click="openSelect" />
		<Select v-model="selectShow" :selectedId="selectedId" ref="userTree" @close="handleClose()"
			@confirm="handleConfirm" />
	</view>
</template>

<script>
	import Select from './Select.vue';
	import {
		getMsgTemplate
	} from '@/api/portal/portal.js'
	import {
		login
	} from '@/api/common';
	export default {
		components: {
			Select
		},
		props: {
			modelValue: {
				default: ''
			},
			send: {
				type: String,
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
			sendName: {
				type: String,
				default: '请选择'
			},
		},
		data() {
			return {
				selectShow: false,
				innerValue: '',
				selectedId: '',
			}
		},
		watch: {
			send: {
				handler(val) {
					if (!val) return this.innerValue = ''
					this.setDefault(val)
				},
				immediate: true
			}
		},
		methods: {
			setDefault(id) {
				this.innerValue = this.modelValue
				this.selectedId = id
			},
			openSelect() {
				if (this.disabled) return
				this.selectShow = true
				this.$refs.userTree.resetData()
			},
			handleConfirm(e) {
				this.selectedId = e.id;
				this.defaultValue = e.id
				this.innerValue = e.fullName
				this.$emit('update:modelValue', e.id)
				this.$emit('change', e.id, e.fullName)
			},
			handleClose() {
				this.selectShow = false
			}
		}
	}
</script>
<style lang="scss" scoped>
	.jnpf-tree-select {
		width: 100%;
	}
</style>