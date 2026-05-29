<template>
	<view class="jnpf-relation-form-attr">
		<u-input v-model="innerValue" input-align='right' disabled :placeholder="placeholder" />
	</view>
</template>

<script>
	import {
		useBaseStore
	} from '@/store/modules/base'
	const baseStore = useBaseStore()
	export default {
		name: 'jnpf-relation-form-attr',
		props: {
			modelValue: {
				type: [String, Number],
				default: ''
			},
			showField: {
				type: String,
				default: ''
			},
			relationField: {
				type: String,
				default: ''
			},
			type: {
				type: String,
				default: 'relationFormAttr'
			},
			isStorage: {
				type: Number,
				default: 0
			},
		},
		data() {
			return {
				innerValue: '',
				placeholder: ''
			}
		},
		computed: {
			relationData() {
				return baseStore.relationData
			}
		},
		watch: {
			relationData: {
				handler(val) {
					if (!this.showField || !this.relationField) return
					let obj = val[this.relationField] || {}
					this.innerValue = obj[this.showField] ? obj[this.showField] : ''
					this.$emit('change', this.innerValue)
				},
				deep: true
			},
			innerValue(val) {
				this.$emit('update:modelValue', val)
			}
		},
		created() {
			const placeholder = this.type === 'relationFormAttr' ? this.isStorage ? this.$t(
					'component.jnpf.relationFormAttr.storage') : this.$t('component.jnpf.relationFormAttr.unStorage') :
				this.isStorage ? this.$t('component.jnpf.popupAttr.storage') : this.$t(
					'component.jnpf.popupAttr.unStorage')
			this.placeholder = placeholder
		}
	}
</script>

<style lang="scss" scoped>
	.jnpf-relation-form-attr {
		width: 100%;
		text-align: right;
	}
</style>