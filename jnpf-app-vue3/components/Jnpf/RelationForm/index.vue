<template>
	<view class="jnpf-relation-form">
		<selectBox v-model="innerValue" :placeholder="placeholder" @openSelect="openSelect"></selectBox>
		<DisplayList :extraObj="extraObj" :extraOptions="extraOptions"
			v-if="Object.keys(extraObj).length && innerValue">
		</DisplayList>
	</view>
</template>
<script>
	import {
		getRelationFormDetail
	} from '@/api/common.js'
	import DisplayList from '@/components/displayList'
	import selectBox from '@/components/selectBox'
	import {
		useBaseStore
	} from '@/store/modules/base'
	const baseStore = useBaseStore()
	export default {
		name: 'jnpf-relation-form',
		components: {
			DisplayList,
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
			formType: {
				type: String,
				default: 'select'
			},
			disabled: {
				type: Boolean,
				default: false
			},
			columnOptions: {
				type: Array,
				default: []
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
			modelId: {
				type: String,
				default: ''
			},
			hasPage: {
				type: Boolean,
				default: false
			},
			pageSize: {
				type: Number,
				default: 10000
			},
			vModel: {
				type: String,
				default: ''
			},
			popupTitle: {
				type: String,
				default: ''
			},
			queryType: {
				type: Number,
				default: 1
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
				extraObj: {}
			}
		},
		watch: {
			modelValue(val) {
				this.setDefault()
			},
		},
		created() {
			uni.$on('relationConfirm', (subVal, innerValue, list, selectRow) => {
				this.confirm(subVal, innerValue, list, selectRow)
			})
			this.setDefault()
		},
		methods: {
			setDefault() {
				if (!this.modelId || !this.vModel) return
				if (!this.modelValue) {
					this.innerValue = ''
					let relationData = baseStore.relationData
					relationData[this.vModel] = {}
					baseStore.updateRelationData(relationData)
					return
				}
				let query = {
					id: this.modelValue,
				};
				if (this.propsValue) query = {
					...query,
					propsValue: this.propsValue
				};
				getRelationFormDetail(this.modelId, query).then(res => {
					if (!res.data || !res.data.data) return
					let data = JSON.parse(res.data.data)
					this.extraObj = data
					this.innerValue = data[this.relationField] ? data[this.relationField] : this
						.modelValue
					let relationData = baseStore.relationData
					relationData[this.vModel] = data
					baseStore.updateRelationData(relationData)
				})
			},
			openSelect() {
				if (this.disabled) {
					if (!this.modelValue) return
					let config = {
						modelId: this.modelId,
						id: this.modelValue,
						formTitle: '详情',
						noShowBtn: 1,
						noDataLog: 1
					}
					this.$nextTick(() => {
						const url =
							'/pages/apply/dynamicModel/detail?config=' +
							this.jnpf.base64.encode(JSON.stringify(config))
						uni.navigateTo({
							url: url
						})
					})
					return
				}
				let data = {
					columnOptions: this.columnOptions,
					relationField: this.relationField,
					type: 'relation',
					propsValue: this.propsValue,
					modelId: this.modelId,
					hasPage: this.hasPage,
					pageSize: this.pageSize,
					id: this.modelValue,
					vModel: this.vModel,
					popupTitle: this.popupTitle || '选择数据',
					innerValue: this.innerValue,
					queryType: this.queryType
				}
				uni.navigateTo({
					url: '/pages/apply/popSelect/index?data=' + encodeURIComponent(JSON.stringify(
						data))
				})
			},
			confirm(subVal, innerValue, vModel, selectRow) {
				if (vModel !== this.vModel) return; // 早期返回，如果条件不满足，则直接退出
				this.firstVal = innerValue;
				this.firstId = subVal;
				// 确定 innerValue 的值
				let innerValueStr = !this.propsValue ? innerValue + '' : selectRow[this.propsValue];
				this.innerValue = selectRow[this.relationField] || "";
				// 触发 update:modelValue 事件
				this.$emit('update:modelValue', selectRow[this.propsValue]);
				// 触发 change 事件
				this.$emit('change', subVal, selectRow);
			}
		}
	}
</script>

<style lang="scss" scoped>
	.jnpf-relation-form {
		width: 100%;
		height: 100%;
	}
</style>