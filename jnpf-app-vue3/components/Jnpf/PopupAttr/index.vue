<template>
	<JnpfRelationFormAttr v-model="value" :showField="showField" :relationField="relationField" :isStorage='isStorage'
		:type="type" @change="onChange" />
</template>
<script>
	import RelationFormAttr from '../RelationFormAttr/index.vue'
	export default {
		name: 'jnpf-popup-attr',
		inheritAttrs: false,
		props: {
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
				default: 'popupAttr'
			},
			isStorage: {
				type: Number,
				default: 0
			},
		},
		data() {
			return {
				value: '',
			}
		},
		watch: {
			modelValue: {
				immediate: true,
				handler(val) {
					this.value = val
				}
			},
			value(val) {
				this.$emit('update:modelValue', val)
			},
		},
		methods: {
			onChange(val) {
				this.$emit('change', val)
			}
		}
	}
</script>
<style lang="scss" scoped>
	.jnpf-relation-attr {
		width: 100%;
	}
</style>