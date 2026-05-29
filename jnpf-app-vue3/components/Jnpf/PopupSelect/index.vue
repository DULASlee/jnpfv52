<template>
	<view class="jnpf-popup-select">
		<selectBox v-model="innerValue" :placeholder="placeholder" @openSelect="openSelect"></selectBox>
		<DisplayList :extraObj="extraObj" :extraOptions="extraOptions"
			v-if="Object.keys(extraObj).length && innerValue">
		</DisplayList>
	</view>
</template>
<script>
	import DisplayList from '@/components/displayList'
	import selectBox from '@/components/selectBox'
	import {
		getDataInterfaceDataInfoByIds
	} from '@/api/common.js'
	import {
		useBaseStore
	} from '@/store/modules/base'
	const baseStore = useBaseStore()
	export default {
		name: 'jnpf-popup-select',
		components: {
			DisplayList,
			selectBox
		},
		props: {
			modelValue: {
				default: ''
			},
			formType: {
				type: String,
				default: 'select'
			},
			placeholder: {
				type: String,
				default: '请选择'
			},
			disabled: {
				type: Boolean,
				default: false
			},
			columnOptions: {
				type: Array,
				default: () => []
			},
			extraOptions: {
				type: Array,
				default: () => []
			},
			relationField: {
				type: String,
				default: ''
			},
			propsValue: {
				type: String,
				default: ''
			},
			popupTitle: {
				type: String,
				default: ''
			},
			interfaceId: {
				type: String,
				default: ''
			},
			hasPage: {
				type: Boolean,
				default: false
			},
			pageSize: {
				type: Number,
				default: 100000
			},
			vModel: {
				type: String,
				default: ''
			},
			rowIndex: {
				default: null
			},
			formData: {
				type: Object
			},
			templateJson: {
				type: Array,
				default: () => []
			}
		},
		data() {
			return {
				selectShow: false,
				innerValue: '',
				defaultValue: '',
				current: null,
				defaultOptions: [],
				firstVal: '',
				firstId: 0,
				selectData: [],
				extraObj: {}
			}
		},
		watch: {
			modelValue(val) {
				this.setDefault()
			},
		},
		created() {
			uni.$on('popConfirm', (subVal, innerValue, list, selectData) => {
				this.confirm(subVal, innerValue, list, selectData)
			})
			this.setDefault()
		},
		methods: {
			setDefault() {
				if (this.modelValue) {
					if (!this.interfaceId) return
					let query = {
						ids: [this.modelValue],
						interfaceId: this.interfaceId,
						propsValue: this.propsValue,
						relationField: this.relationField,
						paramList: this.getParamList()
					}
					getDataInterfaceDataInfoByIds(this.interfaceId, query).then(res => {
						const data = res.data && res.data.length ? res.data[0] : {};
						this.extraObj = data
						this.innerValue = data[this.relationField]
						if (!this.vModel) return
						let relationData = baseStore.relationData
						relationData[this.vModel] = data
						baseStore.updateRelationData(relationData)
					})
				} else {
					this.innerValue = ''
					if (!this.vModel) return
					let relationData = baseStore.relationData
					relationData[this.vModel] = {}
					baseStore.updateRelationData(relationData)
				}
			},
			getParamList() {
				let templateJson = this.templateJson
				if (!this.formData) return templateJson
				for (let i = 0; i < templateJson.length; i++) {
					if (templateJson[i].relationField && templateJson[i].sourceType == 1) {
						if (templateJson[i].relationField.includes('-')) {
							let tableVModel = templateJson[i].relationField.split('-')[0]
							let childVModel = templateJson[i].relationField.split('-')[1]
							templateJson[i].defaultValue = this.formData[tableVModel] && this.formData[tableVModel][this
								.rowIndex
							] && this.formData[tableVModel][this.rowIndex][childVModel] || ''
						} else {
							templateJson[i].defaultValue = this.formData[templateJson[i].relationField] || ''
						}
					}
				}
				return templateJson
			},
			openSelect() {
				if (this.disabled) return
				const pageSize = this.hasPage ? this.pageSize : 100000
				let data = {
					columnOptions: this.columnOptions,
					relationField: this.relationField,
					type: 'popup',
					propsValue: this.propsValue,
					modelId: this.interfaceId,
					hasPage: this.hasPage,
					pageSize,
					id: [this.modelValue],
					vModel: this.vModel,
					popupTitle: this.popupTitle || '选择数据',
					innerValue: this.innerValue,
					paramList: this.getParamList(),
					selectData: this.selectData
				}
				uni.navigateTo({
					url: '/pages/apply/popSelect/index?data=' + encodeURIComponent(JSON.stringify(data))
				})
			},
			confirm(subVal, innerValue, vModel, selectData) {
				if (vModel === this.vModel) {
					this.firstVal = innerValue;
					this.firstId = subVal;
					this.innerValue = innerValue;
					this.extraObj = selectData
					this.$emit('update:modelValue', subVal)
					this.$emit('change', subVal, selectData)
				}
			},
		}
	}
</script>

<style lang="scss">
	.jnpf-popup-select {
		width: 100%;
		height: 100%;
	}
</style>