<template>
	<u-form :model="formData" ref="dataForm" :errorType="['toast']" label-position="left" label-width="150">
		<u-form-item :label="item.label" :prop="item.id" v-for="(item, i) in formConfCopy" :key="i">
			<JnpfInput v-if="useInputList.includes(item.__config__.jnpfKey)" input-align='right'
				v-model="formData[item.id]" :placeholder="textPrefix+item.label" clearable />
			<template v-if="['inputNumber','calculate'].includes(item.__config__.jnpfKey)">
				<JnpfInputNumber v-model="formData[item.id]" :precision="item.precision"
					:placeholder="textPrefix+item.__config__.label" v-if="item.__config__.isFromParam" />
				<JnpfNumberRange v-model="formData[item.id]"
					:precision="!item.precision && item.__config__.jnpfKey=='calculate'?0:item.precision" v-else />
			</template>
			<template v-if="['rate', 'slider'].includes(item.__config__.jnpfKey)">
				<JnpfNumberRange v-model="formData[item.id]" :precision="item.allowHalf ? 1 : 0" />
			</template>
			<JnpfSelect v-if="useSelectList.includes(item.__config__.jnpfKey)" v-model="formData[item.id]"
				:placeholder="selectPrefix+item.label" :options="item.options" :props="item.props"
				:multiple="item.searchMultiple" :key="key" filterable />
			<JnpfCascader v-if="item.__config__.jnpfKey==='cascader'" v-model="formData[item.id]"
				:placeholder="selectPrefix+item.label" :options="item.options" :props="item.props" filterable
				:showAllLevels="item.showAllLevels" :multiple="item.searchMultiple" />
			<JnpfAutoComplete v-if="item.__config__.jnpfKey==='autoComplete'" v-model="formData[item.id]"
				:interfaceName="item.interfaceName" :placeholder="selectPrefix+item.label"
				:interfaceId="item.interfaceId" :total="item.total" :templateJson="item.templateJson"
				:formData='formData' :relationField="item.relationField" :propsValue="item.propsValue"
				:clearable='item.clearable' />
			<JnpfGroupSelect v-if="item.__config__.jnpfKey==='groupSelect'" v-model="formData[item.id]"
				:vModel='item.id' :multiple="item.searchMultiple" :disabled="item.disabled"
				:placeholder="selectPrefix+item.label" :ableIds="item.ableIds" :selectType="item.selectType" />
			<JnpfRoleSelect v-if="item.__config__.jnpfKey==='roleSelect'" v-model="formData[item.id]"
				:multiple="item.searchMultiple" :disabled="item.disabled" :placeholder="selectPrefix+item.label"
				:ableIds="item.ableIds" :selectType="item.selectType" />
			<JnpfOrganizeSelect v-if="['organizeSelect','currOrganize'].includes(item.__config__.jnpfKey)"
				v-model="formData[item.id]" :placeholder="selectPrefix+item.label" :multiple="item.searchMultiple"
				:ableIds="item.ableIds" :selectType="item.selectType" />
			<JnpfDepSelect v-if="['depSelect','currDept'].includes(item.__config__.jnpfKey)" v-model="formData[item.id]"
				:placeholder="selectPrefix+item.label" :ableIds="item.ableIds" :selectType="item.selectType"
				:multiple="item.searchMultiple" />
			<JnpfPosSelect v-if="['posSelect','currPosition'].includes(item.__config__.jnpfKey)"
				v-model="formData[item.id]" :placeholder="selectPrefix+item.label" :ableIds="item.ableIds"
				:selectType="item.selectType" :multiple="item.searchMultiple" />
			<JnpfUserSelect v-if="['userSelect','createUser', 'modifyUser'].includes(item.__config__.jnpfKey)"
				v-model="formData[item.id]" :placeholder="selectPrefix+item.label" :ableDepIds="item.ableDepIds"
				:ableIds="item.ableIds" :selectType="item.selectType!='custom'?'all':'custom'"
				:multiple="item.searchMultiple" />
			<JnpfUsersSelect v-if="item.__config__.jnpfKey==='usersSelect'" v-model="formData[item.id]"
				:multiple="item.searchMultiple" :placeholder="selectPrefix+item.label" :selectType="item.selectType"
				:ableIds="item.ableIds" :clearable="item.clearable" />
			<JnpfTreeSelect v-if="item.__config__.jnpfKey==='treeSelect'" v-model="formData[item.id]"
				:options="item.options" :props="item.props" :placeholder="selectPrefix+item.label" filterable
				:multiple="item.searchMultiple" />
			<JnpfAreaSelect v-if="item.__config__.jnpfKey==='areaSelect'" v-model="formData[item.id]"
				:placeholder="selectPrefix+item.label" :level="item.level" :multiple="item.searchMultiple" />
			<template v-if="useDateList.includes(item.__config__.jnpfKey)||item.__config__.jnpfKey==='datePicker'">
				<JnpfDatePicker v-model="formData[item.id]" :format='item.format' v-if="item.__config__.isFromParam" />
				<JnpfDateRange v-model="formData[item.id]" :format='item.format' v-else />
			</template>
			<JnpfTimeRange v-if="item.__config__.jnpfKey==='timePicker'" v-model="formData[item.id]"
				:format='item.format' />
		</u-form-item>
	</u-form>
</template>
<script>
	import {
		getDictionaryDataSelector,
		getDataInterfaceRes
	} from '@/api/common'
	const dyOptionsList = ['radio', 'checkbox', 'select', 'cascader', 'treeSelect'];
	const useSelectList = ['radio', 'checkbox', 'select'];
	const useInputList = ['input', 'textarea', 'text', 'link', 'billRule', 'location'];
	const useDateList = ['createTime', 'modifyTime'];
	const useArrList = ['cascader', 'address', 'numInput', 'calculate', ...useDateList]
	export default {
		props: ['formConf', 'webType', 'searchFormData'],
		data() {
			const data = {
				useInputList,
				useDateList,
				useSelectList,
				formConfCopy: this.$u.deepClone(this.formConf),
				formData: this.$u.deepClone(this.searchFormData),
				key: +new Date(),
				textPrefix: this.$t('common.inputTextPrefix') + ' ',
				selectPrefix: this.$t('common.chooseTextPrefix') + ' ',
			}
			this.initRelationForm(data.formConfCopy)
			this.initFormData(data.formConfCopy, data.formData)
			return data
		},
		watch: {
			searchFormData(val) {
				this.formData = val
			}
		},
		methods: {
			initFormData(componentList, formData) {
				componentList.forEach(cur => {
					const config = cur.__config__
					if (dyOptionsList.indexOf(config.jnpfKey) > -1) {
						if (config.dataType === 'dictionary' && config.dictionaryType) {
							getDictionaryDataSelector(config.dictionaryType).then(res => {
								cur.options = res.data.list || []
								this.key = +new Date()
								this.resetForm()
							})
						}
						if (config.dataType === 'dynamic' && config.propsUrl) {
							const query = {
								paramList: this.jnpf.getParamList(config.templateJson) || []
							};
							getDataInterfaceRes(config.propsUrl, query).then(res => {
								let list = res.data || []
								cur.options = Array.isArray(list) ? list : [];
								this.key = +new Date()
								this.resetForm()
							})
						}
					}
				})
			},
			initRelationForm(componentList) {
				componentList.forEach(cur => {
					const config = cur.__config__
					if (config.jnpfKey == 'relationFormAttr' || config.jnpfKey == 'popupAttr') {
						const relationKey = cur.relationField.split("_jnpfTable_")[0]
						componentList.forEach(item => {
							const noVisibility = Array.isArray(item.__config__.visibility) && !item
								.__config__.visibility.includes('app')
							if ((relationKey == item.id) && (noVisibility || !!item.__config__
									.noShow)) {
								cur.__config__.noShow = true
							}
						})
					}
					if (cur.__config__.children && cur.__config__.children.length) this.initRelationForm(cur
						.__config__.children)
				})
			},
			allCondition() {
				for (let key in this.formData) {
					if (!this.formData[key]) this.formData[key] = undefined
					if (this.formData[key] && Array.isArray(this.formData[key]) && !this.formData[key]
						.length) {
						this.formData[key] = undefined
					}
				}
				return this.formData
			},
			submitForm() {
				this.$refs.dataForm.validate(valid => {
					if (!valid) return
					for (let key in this.formData) {
						if (!this.formData[key]) this.formData[key] = undefined
						if (this.formData[key] && Array.isArray(this.formData[key]) && !this.formData[key]
							.length) {
							this.formData[key] = undefined
						}
					}
					this.$emit('submit', this.formData)
				})
			},
			resetForm() {
				this.$refs.dataForm.resetFields()
			}
		}
	}
</script>