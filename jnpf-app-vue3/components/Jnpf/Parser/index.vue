<template>
	<u-form class="jnpf-wrap-form" ref="dataForm" :rules="rules" :model="formData" :errorType="['toast']"
		:label-position="formConf.labelPosition==='top'?'top':'left'"
		:label-width="formConf.labelWidth?formConf.labelWidth*1.5:150"
		:label-align="formConf.labelPosition==='right'?'right':'left'" :class="formClass+' '+formConfCopy.className">
		<view v-for="(item, index) in formConfCopy.fields" :key="item.__config__.renderKey">
			<Item v-if="!item.__config__.noShow && item.__config__.isVisibility" :itemData="item" :formConf="formConf"
				:formData="formData" :ref="'ref'+item.__config__.formId" :class="item.__config__.className"
				@clickIcon='clickIcon' @clickFun="onClick" @collapse-change="onCollapseChange" @tab-change="onTabChange"
				@input="setValue" />
		</view>
		<u-modal v-model="showTipsModal" width='70%' border-radius="16" :content-style="contentStyle"
			:content="tipsContent" :titleStyle="titleStyle" :title="tipsTitle" :confirm-style="confirmStyle"
			:confirm-text="$t('common.okText')" />
	</u-form>
</template>
<script>
	import Item from './Item'
	import {
		getDataInterfaceRes,
		getDictionaryDataSelector
	} from '@/api/common'
	const dyOptionsList = ['radio', 'checkbox', 'select', 'cascader', 'treeSelect']
	export default {
		components: {
			Item
		},
		props: {
			formConf: {
				type: Object,
				required: true
			},
			loading: {
				type: Boolean,
				default: false
			},
			isShortLink: {
				type: Boolean,
				default: false
			},
		},
		data() {
			const data = {
				formClass: 'form-' + this.jnpf.idGenerator(),
				formConfCopy: this.$u.deepClone(this.formConf),
				formData: {},
				rules: {},
				options: {},
				tableRefs: {},
				relations: {},
				refList: [],
				contentStyle: {
					fontSize: '28rpx',
					padding: '20rpx',
					lineHeight: '44rpx',
					textAlign: 'left'
				},
				titleStyle: {
					padding: '20rpx'
				},
				confirmStyle: {
					height: '80rpx',
					lineHeight: '80rpx',
				},
				tipsContent: '',
				tipsTitle: this.$t('common.tipTitle'),
				showTipsModal: false
			}
			this.beforeInit(data.formConfCopy.fields)
			this.initRelationForm(data.formConfCopy.fields)
			this.initFormData(data.formConfCopy.fields, data.formData, data.tableRefs)
			this.buildRules(this.$u.deepClone(data.formConfCopy.fields), data.rules)
			this.buildOptions(data.formConfCopy.fields, data.options, data.formData)
			this.buildRelations(data.formConfCopy.fields, data.relations)
			this.$nextTick(() => {
				this.onLoadFunc(data.formConfCopy)
				this.getRefList()
			})
			return data
		},
		provide() {
			return {
				parameter: this.parameter,
				relations: this.relations,
				isShortLink: this.isShortLink
			}
		},
		computed: {
			parameter() {
				const oldFormData = this.formConfCopy.formData ? this.formConfCopy.formData : {}
				this.formData.id = oldFormData.id || null
				this.formData.flowId = oldFormData.flowId || ''
				return {
					formData: this.formData,
					setFormData: this.setFormData,
					setShowOrHide: this.setShowOrHide,
					setRequired: this.setRequired,
					setDisabled: this.setDisabled,
					onlineUtils: this.jnpf.onlineUtils,
				}
			}
		},
		mounted() {
			this.$refs.dataForm.setRules(this.rules);
			this.initRelationData()
			uni.$on('subChange', (field) => {
				this.handleRelation(field.__vModel__)
			})
			this.initCss(this.formConfCopy)
		},
		methods: {
			beforeInit(fields) {
				const loop = (list) => {
					for (var index = 0; index < list.length; index++) {
						const config = list[index].__config__
						if (config.children && config.children.length) loop(config.children)
						if (config.jnpfKey == 'tableGrid') {
							let newList = []
							for (var i = 0; i < config.children.length; i++) {
								let element = config.children[i]
								for (var j = 0; j < element.__config__.children.length; j++) {
									let item = element.__config__.children[j]
									newList.push(...item.__config__.children)
								}
							}
							list.splice(index, 1, ...newList)
						}
					}
				}
				loop(fields)
			},
			initRelationForm(componentList) {
				componentList.forEach(cur => {
					const config = cur.__config__
					if (config.jnpfKey == 'relationFormAttr' || config.jnpfKey == 'popupAttr') {
						const relationKey = cur.relationField.split("_jnpfTable_")[0]
						componentList.forEach(item => {
							const noVisibility = Array.isArray(item.__config__.visibility) && !item
								.__config__.visibility.includes('app')
							if ((relationKey == item.__vModel__) && (noVisibility || !!item.__config__
									.noShow) && !cur.__vModel__) {
								cur.__config__.noShow = true
							}
						})
					}
					if (cur.__config__.children && cur.__config__.children.length) this.initRelationForm(cur
						.__config__.children)
				})
			},
			initFormData(componentList, formData, tableRefs) {
				componentList.forEach(cur => {
					const config = cur.__config__
					if (cur.__vModel__) {
						formData[cur.__vModel__] = config.defaultValue
						if (cur.__config__.jnpfKey == 'table' && !cur.__config__.noShow) {
							tableRefs[cur.__vModel__] = cur
						}
					}
					if (config.children && cur.__config__.jnpfKey !== 'table') {
						this.initFormData(config.children, formData, tableRefs)
					}
				})
			},
			buildOptions(componentList, data, formData) {
				componentList.forEach(cur => {
					const config = cur.__config__
					if (dyOptionsList.indexOf(config.jnpfKey) > -1) {
						if (config.dataType === 'dictionary' && config.dictionaryType) {
							cur.options = []
							getDictionaryDataSelector(config.dictionaryType).then(res => {
								cur.options = res.data.list || []
								data[cur.__vModel__ + 'Options'] = cur.options
								this.setFieldOptions(cur.__vModel__, cur.options)
								this.$nextTick(() => {
									uni.$emit("initCollapse")
								})
							})
						} else if (config.dataType === 'dynamic' && config.propsUrl) {
							cur.options = []
							let query = {
								paramList: this.jnpf.getParamList(config.templateJson, formData),
							}
							getDataInterfaceRes(config.propsUrl, query).then(res => {
								cur.options = Array.isArray(res.data) ? res.data : []
								data[cur.__vModel__ + 'Options'] = cur.options
								this.setFieldOptions(cur.__vModel__, cur.options)
								uni.$emit("initCollapse")
							})
						} else {
							data[cur.__vModel__ + 'Options'] = cur.options
						}
					}
					if (config.children && config.jnpfKey !== 'table') this.buildOptions(config.children, data,
						formData)
				})
			},
			buildRules(componentList, rules) {
				componentList.forEach(cur => {
					const config = cur.__config__
					const jnpfKey = config.jnpfKey
					const useNumList = ['inputNumber', 'switch', 'datePicker', 'rate', 'slider', 'calculate']
					const useArrayList = ['select', 'depSelect', 'posSelect', 'userSelect', 'usersSelect',
						'treeSelect', 'popupTableSelect'
					]
					config.regList = !config.regList ? [] : config.regList
					if (config.required) {
						const label = config.labelI18nCode ?
							this.$t(config.labelI18nCode, config.label) : config.label;
						let requiredItem = {
							required: config.required,
							message: `${label} ${this.$t('sys.validate.textRequiredSuffix')}`
						};
						config.regList.push(requiredItem)
					}
					const rule = config.regList.map(item => {
						if (item.pattern) {
							item.pattern = item.pattern.toString()
							let start = item.pattern.indexOf('/')
							let stop = item.pattern.lastIndexOf('/')
							let str = item.pattern.substring(start + 1, stop)
							let reg = new RegExp(str)
							item.pattern = reg
						}
						item.trigger = config.trigger || 'change, blur'
						if (Array.isArray(config.defaultValue)) item.type = 'array'
						if (useNumList.includes(jnpfKey)) item.type = 'number'
						if (useArrayList.includes(jnpfKey) && cur.multiple) item.type = 'array'
						if (jnpfKey === 'organizeSelect' || jnpfKey === 'areaSelect' || jnpfKey ===
							'checkbox' || jnpfKey === 'cascader') item.type = 'array'

						if (jnpfKey === 'relationForm' || jnpfKey === 'popupSelect') {
							item.type = 'any',
								item.validator = (rule, value, callback) => {
									if (value || value === 0) {
										callback();
									} else {
										callback(new Error(config.label + '不能为空'));
									}
								}
						}
						if (item.messageI18nCode) {
							item.message = this.$t(item.messageI18nCode, item.message);
						}
						return item
					})
					if (rule.length) rules[cur.__vModel__] = rule
					if (config.children && jnpfKey !== 'table') this.buildRules(this.$u.deepClone(config.children),
						rules)
				})
			},
			buildRelations(componentList, relations) {
				componentList.forEach(cur => {
					const config = cur.__config__
					const vModel = cur.__vModel__
					const selectType = ['dep', 'pos', 'role', 'group']
					if (config.jnpfKey === 'userSelect' && selectType.includes(cur.selectType)) {
						if (cur.relationField) {
							let item = {
								...cur,
								realVModel: config.isSubTable ? config.parentVModel + '-' + vModel : vModel,
								opType: 'setUserOptions'
							}
							if (relations.hasOwnProperty(cur.relationField)) {
								let boo = relations[cur.relationField].some(o => o.realVModel === cur.realVModel)
								if (!boo) relations[cur.relationField].push(item)
							} else {
								relations[cur.relationField] = [item]
							}
						}
					}
					if (dyOptionsList.indexOf(config.jnpfKey) > -1 && config.dataType === 'dynamic' && config
						.templateJson && config.templateJson.length) {
						for (let i = 0; i < config.templateJson.length; i++) {
							const e = config.templateJson[i];
							if (e.relationField) {
								let item = {
									...cur,
									realVModel: config.isSubTable ? config.parentVModel + '-' + vModel :
										vModel,
									opType: 'setOptions'
								}
								if (relations.hasOwnProperty(e.relationField)) {
									let boo = relations[e.relationField].some(o => o.realVModel === cur
										.realVModel)
									if (!boo) relations[e.relationField].push(item)
								} else {
									relations[e.relationField] = [item]
								}
							}
						}
					}
					if (config.jnpfKey === 'datePicker') {
						if (config.startTimeRule) {
							let startTimeValue = Number(config.startTimeValue)
							if (config.startTimeType == 1) {
								cur.startTime = startTimeValue
							} else if (config.startTimeType == 3) {
								cur.startTime = new Date().getTime()
							} else {
								if (config.startTimeType == 4) {
									if (config.startTimeTarget == 1) cur.startTime = new Date(new Date()
										.setFullYear((new Date().getFullYear() - startTimeValue))).getTime()
									if (config.startTimeTarget == 2) cur.startTime = new Date(new Date().setMonth((
										new Date().getMonth() - startTimeValue))).getTime()
									if (config.startTimeTarget == 3) cur.startTime = new Date(new Date().setDate((
										new Date().getDate() - startTimeValue))).getTime()
								} else {
									if (config.startTimeTarget == 1) cur.startTime = new Date(new Date()
										.setFullYear((new Date().getFullYear() + startTimeValue))).getTime()
									if (config.startTimeTarget == 2) cur.startTime = new Date(new Date().setMonth((
										new Date().getMonth() + startTimeValue))).getTime()
									if (config.startTimeTarget == 3) cur.startTime = new Date(new Date().setDate((
										new Date().getDate() + startTimeValue))).getTime()
								}
							}
						}
						if (config.endTimeRule) {
							let endTimeValue = Number(config.endTimeValue)
							if (config.endTimeType == 1) {
								cur.endTime = endTimeValue
							} else if (config.endTimeType == 3) {
								cur.endTime = new Date().getTime()
							} else {
								if (config.endTimeType == 4) {
									if (config.endTimeTarget == 1) cur.endTime = new Date(new Date()
										.setFullYear((new Date().getFullYear() - endTimeValue))).getTime()
									if (config.endTimeTarget == 2) cur.endTime = new Date(new Date().setMonth((
										new Date().getMonth() - endTimeValue))).getTime()
									if (config.endTimeTarget == 3) cur.endTime = new Date(new Date().setDate((
										new Date().getDate() - endTimeValue))).getTime()
								} else {
									if (config.endTimeTarget == 1) cur.endTime = new Date(new Date()
										.setFullYear((new Date().getFullYear() + endTimeValue))).getTime()
									if (config.endTimeTarget == 2) cur.endTime = new Date(new Date().setMonth((
										new Date().getMonth() + endTimeValue))).getTime()
									if (config.endTimeTarget == 3) {
										cur.endTime = new Date(new Date().setDate((new Date().getDate() +
											endTimeValue))).getTime()
									}
								}
							}
						}
						if (cur.__config__.startRelationField) {
							let item = {
								...cur,
								realVModel: cur.__config__.isSubTable ? cur.__config__.parentVModel + '-' + cur
									.__vModel__ : cur.__vModel__,
								opType: 'setDate'
							}
							if (relations.hasOwnProperty(cur.__config__.startRelationField)) {
								let boo = relations[cur.__config__.startRelationField].some(o => o.realVModel ===
									cur.realVModel)
								if (!boo) {
									relations[cur.__config__.startRelationField].push(item)
								}
							} else {
								relations[cur.__config__.startRelationField] = [item]
							}
						}
						if (cur.__config__.endRelationField) {
							let item = {
								...cur,
								realVModel: cur.__config__.isSubTable ? cur.__config__.parentVModel + '-' + cur
									.__vModel__ : cur.__vModel__,
								opType: 'setDate'
							}
							if (relations.hasOwnProperty(cur.__config__.endRelationField)) {
								let boo = relations[cur.__config__.endRelationField].some(o => o.realVModel === cur
									.realVModel)
								if (!boo) {
									relations[cur.__config__.endRelationField].push(item)
								}
							} else {
								relations[cur.__config__.endRelationField] = [item]
							}
						}
					}
					if (config.jnpfKey === 'timePicker') {
						let format = cur.format === 'HH:mm' ? 'HH:mm:00' : cur.format
						if (config.startTimeRule) {
							let startTime = ''
							if (config.startTimeType == 1) {
								cur.startTime = config.startTimeValue || '00:00:00'
								if (cur.startTime.split(':').length == 3) {
									cur.startTime = cur.startTime
								} else {
									cur.startTime = cur.startTime + ':00'
								}
							} else if (config.startTimeType == 3) {
								cur.startTime = this.jnpf.toDate(new Date(), format)
							} else {
								let startTimeValue = Number(config.startTimeValue)
								if (config.startTimeType == 4) {
									if (config.startTimeTarget == 1) startTime = new Date(new Date().setHours((
										new Date().getHours() - startTimeValue))).getTime()
									if (config.startTimeTarget == 2) startTime = new Date(new Date()
										.setMinutes((new Date().getMinutes() - startTimeValue))).getTime()
									if (config.startTimeTarget == 3) startTime = new Date(new Date()
										.setSeconds((new Date().getSeconds() - startTimeValue))).getTime()
								} else {
									if (config.startTimeTarget == 1) startTime = new Date(new Date().setHours((
										new Date().getHours() + startTimeValue))).getTime()
									if (config.startTimeTarget == 2) startTime = new Date(new Date()
											.setMinutes((new Date().getMinutes() + startTimeValue)))
										.getTime()
									if (config.startTimeTarget == 3) startTime = new Date(new Date()
										.setSeconds((new Date().getSeconds() + startTimeValue))).getTime()
								}
								cur.startTime = this.$u.timeFormat(startTime, 'hh:MM:ss')
							}
						}
						if (config.endTimeRule) {
							let endTime = ''
							if (config.endTimeType == 1) {
								cur.endTime = config.endTimeValue || '23:59:59'
								if (cur.endTime.split(':').length == 3) {
									cur.endTime = cur.endTime
								} else {
									cur.endTime = cur.endTime + ':00'
								}
							} else if (config.endTimeType == 3) {
								cur.endTime = this.jnpf.toDate(new Date(), format)
							} else {
								let endTimeValue = Number(config.endTimeValue)
								if (config.endTimeType == 4) {
									if (config.endTimeTarget == 1) {
										endTime = new Date(new Date().setHours((new Date().getHours() -
											endTimeValue))).getTime()
									}
									if (config.endTimeTarget == 2) {
										endTime = new Date(new Date().setMinutes((new Date().getMinutes() -
											endTimeValue))).getTime()
									}
									if (config.endTimeTarget == 3) {
										endTime = new Date(new Date().setSeconds((new Date().getSeconds() -
											endTimeValue))).getTime()
									}
								} else {
									if (config.endTimeTarget == 1) {
										endTime = new Date(new Date().setHours((new Date().getHours() +
											endTimeValue))).getTime()
									}
									if (config.endTimeTarget == 2) {
										endTime = new Date(new Date().setMinutes((new Date().getMinutes() +
											endTimeValue))).getTime()
									}
									if (config.endTimeTarget == 3) {
										endTime = new Date(new Date().setSeconds((new Date().getSeconds() +
											endTimeValue))).getTime()
									}
								}
								cur.endTime = this.$u.timeFormat(endTime, 'hh:MM:ss')
							}
						}
						if (cur.__config__.startRelationField) {
							let item = {
								...cur,
								realVModel: cur.__config__.isSubTable ? cur.__config__.parentVModel + '-' + cur
									.__vModel__ : cur.__vModel__,
								opType: 'setTime'
							}
							if (relations.hasOwnProperty(cur.__config__.startRelationField)) {
								let boo = relations[cur.__config__.startRelationField].some(o => o.realVModel ===
									cur.realVModel)
								if (!boo) {
									relations[cur.__config__.startRelationField].push(item)
								}
							} else {
								relations[cur.__config__.startRelationField] = [item]
							}
						}
						if (cur.__config__.endRelationField) {
							let item = {
								...cur,
								realVModel: cur.__config__.isSubTable ? cur.__config__.parentVModel + '-' + cur
									.__vModel__ : cur.__vModel__,
								opType: 'setTime'
							}
							if (relations.hasOwnProperty(cur.__config__.endRelationField)) {
								let boo = relations[cur.__config__.endRelationField].some(o => o.realVModel === cur
									.realVModel)
								if (!boo) {
									relations[cur.__config__.endRelationField].push(item)
								}
							} else {
								relations[cur.__config__.endRelationField] = [item]
							}
						}
					}
					if (config.jnpfKey === 'popupSelect' && cur.templateJson && cur.templateJson.length) {
						for (let i = 0; i < cur.templateJson.length; i++) {
							const e = cur.templateJson[i];
							if (e.relationField) {
								let item = {
									...cur,
									realVModel: cur.__config__.isSubTable ? cur.__config__.parentVModel +
										'-' + cur.__vModel__ : cur.__vModel__,
									opType: 'setPopupOptions'
								}
								if (relations.hasOwnProperty(e.relationField)) {
									let boo = relations[e.relationField].some(o => o.realVModel === cur
										.realVModel)
									if (!boo) {
										relations[e.relationField].push(item)
									}
								} else {
									relations[e.relationField] = [item]
								}
							}
						}
					}
					if (config.children) this.buildRelations(config.children, relations)
				})
			},
			onLoadFunc(formConfCopy) {
				if (!formConfCopy || !formConfCopy.funcs || !formConfCopy.funcs.onLoad) return
				const onLoadFunc = this.jnpf.getScriptFunc(formConfCopy.funcs.onLoad)
				if (!onLoadFunc) return
				onLoadFunc(this.parameter)
			},
			initRelationData() {
				const handleRelationFun = (list) => {
					list.forEach(cur => {
						const config = cur.__config__
						this.handleDefaultRelation(cur.__vModel__)
						if (config.children) handleRelationFun(config.children)
					})
				}
				handleRelationFun(this.formConfCopy.fields)
			},
			initCss(formCopy) {
				// #ifdef H5
				if (!formCopy.classJson) return
				if (document.getElementById('styleId')) document.getElementById('styleId').remove()
				let head = document.getElementsByTagName('head')[0]
				let style = document.createElement('style')
				style.type = 'text/css'
				style.id = 'styleId'
				style.innerText = this.buildCSS(formCopy.classJson)
				head.appendChild(style)
				//#endif
			},
			buildCSS(str) {
				str = str.trim();
				let newStr = '';
				let cut = str.split('}');
				cut.forEach(item => {
					if (item) {
						item = '.' + this.formClass + ' ' + item + '}';
						newStr += item;
					}
				});
				return newStr;
			},
			handleRelation(field) {
				if (!field) return
				const currRelations = this.relations
				for (let key in currRelations) {
					if (key === field) {
						for (let i = 0; i < currRelations[key].length; i++) {
							const e = currRelations[key][i];
							let vModel = e.realVModel || e.__vModel__
							const config = e.__config__
							const jnpfKey = config.jnpfKey
							let defaultValue = ''
							if (['checkbox', 'cascader'].includes(jnpfKey) || (['select', 'treeSelect', 'popupSelect',
									'popupTableSelect', 'userSelect'
								].includes(jnpfKey) && e.multiple)) {
								defaultValue = []
							}
							if (vModel.includes('-')) { // 子表字段
								const tableVModel = vModel.split('-')[0]
								uni.$emit('handleRelation', e, defaultValue, true)
							} else {
								this.setFormData(vModel, defaultValue)
								if (e.opType === 'setOptions') {
									let query = {
										paramList: this.jnpf.getParamList(config.templateJson, this.formData)
									}
									getDataInterfaceRes(config.propsUrl, query).then(res => {
										let realData = res.data || []
										this.setFieldOptions(vModel, realData)
										uni.$emit("initCollapse")
									})
								}
								if (e.opType === 'setUserOptions') {
									let value = this.formData[e.relationField] || []
									this.comSet('ableRelationIds', vModel, Array.isArray(value) ? value : [value])
								}
								if (e.opType === 'setDate' || e.opType === 'setTime') {
									let startTime = ''
									let endTime = ''
									if (config.startTimeType == 2) {
										startTime = this.formData[config.startRelationField] || 0
										if (e.opType === 'setTime') {
											startTime = this.formData[config.startRelationField] ||
												'00:00:00'
											if (startTime && (startTime.split(':').length == 3)) {
												startTime = startTime
											} else {
												startTime = startTime + ':00'
											}
										}
									} else {
										startTime = e.startTime
									}
									if (config.endTimeType == 2) {
										endTime = this.formData[config.endRelationField] || 0
										if (e.opType === 'setTime') {
											endTime = this.formData[config.endRelationField] ||
												'00:00:00'
											if (endTime && (endTime.split(':').length == 3)) {
												endTime = endTime
											} else {
												endTime = endTime + ':00'
											}
										}
									} else {
										endTime = e.endTime
									}
									this.comSet('startTime', vModel, startTime)
									this.comSet('endTime', vModel, endTime)
								}
							}
						}
					}
				}
			},
			handleDefaultRelation(field) {
				if (!field) return
				const currRelations = this.relations
				for (let key in currRelations) {
					if (key === field) {
						for (let i = 0; i < currRelations[key].length; i++) {
							const e = currRelations[key][i];
							let vModel = e.realVModel || e.__vModel__
							const config = e.__config__
							let defaultValue = ''
							if (vModel.includes('-')) { // 子表字段
								const tableVModel = vModel.split('-')[0]
								uni.$emit('handleRelation', e, defaultValue)
							} else {
								if (e.opType === 'setUserOptions') {
									let value = this.formData[e.relationField] || []
									this.comSet('ableRelationIds', e.__vModel__, Array.isArray(value) ? value : [value])
								}
								if (e.opType === 'setDate' || e.opType === 'setTime') {
									let startTime = ''
									let endTime = ''
									if (config.startTimeType == 2) {
										startTime = this.formData[config.startRelationField] || 0
										if (e.opType === 'setTime') {
											startTime = this.formData[config.startRelationField] ||
												'00:00:00'
											if (startTime && (startTime.split(':').length == 3)) {
												startTime = startTime
											} else {
												startTime = startTime + ':00'
											}
										}
									} else {
										startTime = e.startTime
									}
									if (config.endTimeType == 2) {
										endTime = this.formData[config.endRelationField] || 0
										if (e.opType === 'setTime') {
											endTime = this.formData[config.endRelationField] ||
												'23:59:59'
											if (endTime && (endTime.split(':').length == 3)) {
												endTime = endTime
											} else {
												endTime = endTime + ':00'
											}
										}
									} else {
										endTime = e.endTime
									}
									this.comSet('startTime', e.__vModel__, startTime)
									this.comSet('endTime', e.__vModel__, endTime)
								}
							}
						}
					}
				}
			},
			onChange(field) {
				this.handleRelation(field.__vModel__)
			},
			setValue(item) {
				if (item.__vModel__) {
					this.$set(this.formData, item.__vModel__, item.__config__.defaultValue)
				}
			},
			setFormData(prop, value) {
				if (!prop || this.formData[prop] === value) return;
				const isChildTable = prop.indexOf('.') > -1
				if (isChildTable) {
					const list = prop.split('.')
					for (let i = 0; i < this.refList.length; i++) {
						const item = this.refList[i]
						if (item[0] == list[0]) {
							const tableRef = Array.isArray(item[1]) ? item[1][0] : item[1]
							tableRef.setTableFormData(list[1], value)
							break
						}
					}
				} else {
					this.comSet('defaultValue', prop, value)
					this.formData[prop] = value
				}
				this.handleRelation(prop)
			},
			setShowOrHide(prop, value) {
				const newVal = !!value
				const isChildTable = prop.indexOf('.') > -1
				if (!isChildTable) {
					this.comSet('noShow', prop, !newVal)
				}
			},
			setRequired(prop, value) {
				const newVal = !!value
				const isChildTable = prop.indexOf('.') > -1
				if (!isChildTable) {
					this.comSet('required', prop, newVal)
					this.rules = {}
					this.buildRules(this.$u.deepClone(this.formConfCopy.fields), this.rules)
					this.$refs.dataForm.setRules(this.rules);
				}
			},
			setDisabled(prop, value) {
				const newVal = !!value
				const isChildTable = prop.indexOf('.') > -1
				if (!isChildTable) {
					this.comSet('disabled', prop, newVal)
				}
			},
			setFieldOptions(prop, value) {
				const newVal = Array.isArray(value) ? value : []
				const isChildTable = prop.indexOf('.') > -1
				if (!isChildTable) {
					this.comSet('options', prop, newVal)
				}
			},
			comSet(field, prop, value) {
				if (!prop) return
				const loop = list => {
					for (let i = 0; i < list.length; i++) {
						let item = list[i]
						const config = item.__config__
						if (item.__vModel__ && item.__vModel__ === prop) {
							config.defaultValue = this.formData[prop]
							switch (field) {
								case 'disabled':
									item[field] = value
									break;
								case 'ableRelationIds':
									this.$set(item, field, value)
								case 'options':
									if (dyOptionsList.indexOf(config.jnpfKey) > -1) item.options = value
									break;
								case 'startTime':
									this.$set(item, field, value)
									break;
								case 'endTime':
									this.$set(item, field, value)
									break;
								default:
									config[field] = value
									this.setValue(item)
									break;
							}
							config.renderKey = +new Date() + item.__vModel__
							break;
						}
						if (config && config.jnpfKey !== 'table' && config.children && Array.isArray(config
								.children)) loop(config.children)
					}
				}
				loop(this.formConfCopy.fields)
			},
			submitForm() {
				try {
					this.beforeSubmit().then(() => {
						this.submit()
					})
				} catch (e) {
					this.submit()
				}
			},
			submit() {
				this.$refs.dataForm.validate(valid => {
					if (!valid) return
					if (!this.checkTableData()) return
					this.$emit('submit', this.formData, this.afterSubmit)
				});
			},
			beforeSubmit() {
				if (!this.formConfCopy || !this.formConfCopy.funcs || !this.formConfCopy.funcs.beforeSubmit) return Promise
					.resolve()
				const func = this.jnpf.getScriptFunc(this.formConfCopy.funcs.beforeSubmit)
				if (!func) return Promise.resolve()
				return func(this.parameter)
			},
			afterSubmit() {
				if (!this.formConfCopy || !this.formConfCopy.funcs || !this.formConfCopy.funcs.afterSubmit) return
				const func = this.jnpf.getScriptFunc(this.formConfCopy.funcs.afterSubmit)
				if (!func) return
				func(this.parameter)
			},
			// 子表必填验证，子表赋值
			checkTableData() {
				this.getRefList()
				let valid = true
				for (var i = 0; i < Object.keys(this.tableRefs).length; i++) {
					const vModel = Object.keys(this.tableRefs)[i]
					const config = this.tableRefs[vModel].__config__
					if (!config.isVisibility || config.noShow) continue
					let tableRef = null
					for (let i = 0; i < this.refList.length; i++) {
						const item = this.refList[i]
						if (item[0] === vModel) {
							tableRef = Array.isArray(item[1]) ? item[1][0] : item[1]
							break
						}
					}
					if (!tableRef) continue
					const res = (tableRef && tableRef.$refs && tableRef.$refs[vModel] ? tableRef.$refs[vModel] : tableRef)
						.submit() || false;
					res ? (this.formData[vModel] = res) : (valid = false)
				}
				return valid
			},
			//获取子表实例ref 
			getRefList() {
				this.refList = []
				const refList = this.$refs || []
				const loop = (list) => {
					for (var i = 0; i < list.length; i++) {
						const refs = list[i].$refs
						if (Array.isArray(refs)) {
							loop(refs)
						} else {
							if (Object.values(refs).length) {
								for (var k in refs) {
									if (Array.isArray(refs[k])) {
										loop(refs[k])
									} else {
										this.refList.push([k, refs[k]])
									}
								}
							}
						}

					}
				}
				for (var i in refList) {
					const item = refList[i]
					if (Array.isArray(item)) loop(item)
				}
			},
			clickIcon(e) {
				if (!e) return
				if (!e.__config__.tipLabel && !e.helpMessage) return
				this.tipsContent = e.helpMessage || e.__config__.tipLabel
				this.tipsTitle = e.__config__.label
				if (e.__config__.jnpfKey === 'card') this.tipsTitle = e.header
				if (e.__config__.jnpfKey === 'groupTitle') this.tipsTitle = e.content
				this.showTipsModal = true
			},
			onBlur(item, data) {
				this.setValue(item)
				this.setScriptFunc(data, item, 'blur')
			},
			onTabChange(item, data) {
				this.setScriptFunc(data, item, 'tabClick')
			},
			onClick(item, data) {
				this.setScriptFunc(data, item, 'click')
			},
			onCollapseChange(item, data) {
				this.setScriptFunc(data, item)
			},
			setScriptFunc(val, data, type = 'change') {
				if (data && data.on && data.on[type]) {
					const func = this.jnpf.getScriptFunc(data.on[type]);
					if (!func) return
					func.call(this, {
						data: val,
						...this.parameter
					})
				}
			},
		}
	}
</script>