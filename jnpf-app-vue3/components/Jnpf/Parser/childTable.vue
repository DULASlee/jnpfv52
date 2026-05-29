<template>
	<view class="jnpf-table">
		<view class="jnpf-table-title u-line-1" @click="clickIcon(config)">
			{{config.__config__.label}}
			<u-icon v-if="config.__config__.tipLabel" :name="'question-circle-fill'" class="u-m-l-10" color="#a0acb7" />
		</view>
		<view class="jnpf-table-item" v-for="(item,i) in tableFormData" :key="i">
			<view class="jnpf-table-item-title">
				<u-checkbox @change="checkboxChange($event,item,index)" v-model="item.checked" @tap.stop
					shape="circle"></u-checkbox>
				<view class="jnpf-table-item-title-num">({{i+1}})</view>
				<template v-if="!disabled">
					<template v-for="(it, index) in config.columnBtnsList">
						<view v-if="it.show&&!disabled"
							:class="it.value=='remove'?'jnpf-table-delete-btn':'jnpf-table-copy-btn'"
							@click="columnBtnsHandel(it, i)" :key="index">
							{{ it.labelI18nCode ? $t(it.labelI18nCode) : it.label }}
						</view>
					</template>
				</template>
			</view>
			<view class="form-item-box" v-for="(child,cIndex) in item" :key="cIndex">
				<u-form-item :label="child.__config__.showLabel?child.__config__.label:''"
					:required="child.__config__.required"
					:left-icon='child.__config__.showLabel &&child.__config__.tipLabel && child.__config__.label? "question-circle-fill":""'
					@clickIcon="clickIcon(child)" :left-icon-style="{'color':'#a8aaaf'}"
					:label-width="child.__config__.labelWidth ? child.__config__.labelWidth * 1.5 : undefined"
					v-if="!child.__config__.noShow && child.__config__.isVisibility">
					<JnpfInput v-if="child.__config__.jnpfKey=='input'" v-model="tableFormData[i][cIndex].value"
						:showPassword="child.showPassword" :placeholder="child.placeholder" :maxlength="child.maxlength"
						:showCount="child.showCount" :disabled="disabled||child.disabled" :clearable='child.clearable'
						:useScan="child.useScan" :addonBefore="child.addonBefore" :addonAfter="child.addonAfter"
						@change="onChange($event,child,i)" @blur="onBlur($event,child,i)" />
					<JnpfTextarea v-if="child.__config__.jnpfKey=='textarea'" v-model="tableFormData[i][cIndex].value"
						:placeholder="child.placeholder" :maxlength="child.maxlength" :showCount="child.showCount"
						:disabled="disabled||child.disabled" :clearable='child.clearable'
						@change="onChange($event,child,i)" @blur="onBlur($event,child,i)" />
					<JnpfInputNumber v-if="child.__config__.jnpfKey=='inputNumber'"
						v-model="tableFormData[i][cIndex].value" :step='child.step' :max='child.max' :min='child.min'
						:disabled="disabled||child.disabled" :placeholder="child.placeholder"
						:isAmountChinese="child.isAmountChinese" :thousands="child.thousands"
						:addonAfter="child.addonAfter" :addonBefore="child.addonBefore" :controls="child.controls"
						:precision="child.precision" @change="onChange($event,child,i)"
						@blur="onBlur($event,child,i)" />
					<JnpfSwitch v-if="child.__config__.jnpfKey=='switch'" v-model="tableFormData[i][cIndex].value"
						:disabled="disabled||child.disabled" @change="onChange($event,child,i)" />
					<JnpfSelect v-if="child.__config__.jnpfKey=='select'" v-model="tableFormData[i][cIndex].value"
						:placeholder="child.placeholder" :options="child.options" :props="child.props"
						:multiple="child.multiple" :disabled="disabled||child.disabled"
						@change="(val,value)=>{onChange(value,child,i)}" :filterable="child.filterable" />
					<JnpfCascader v-if="child.__config__.jnpfKey=='cascader'" v-model="tableFormData[i][cIndex].value"
						:placeholder="child.placeholder" :options="child.options" :props="child.props"
						:disabled="disabled||child.disabled" :multiple="child.multiple" :filterable='child.filterable'
						:clearable='child.clearable' :showAllLevels="child.showAllLevels"
						@change="(val,value)=>{onChange(value,child,i)}" />
					<JnpfDatePicker v-if="child.__config__.jnpfKey=='datePicker'"
						v-model="tableFormData[i][cIndex].value" :placeholder="child.placeholder"
						:disabled="disabled||child.disabled" :format="child.format" :startTime="child.startTime"
						:endTime='child.endTime' @change="onChange($event,child,i)" />
					<JnpfTimePicker v-if="child.__config__.jnpfKey=='timePicker'"
						v-model="tableFormData[i][cIndex].value" :placeholder="child.placeholder"
						:disabled="disabled||child.disabled" :format="child.format" :startTime="child.startTime"
						:endTime='child.endTime' @change="onChange($event,child,i)" />
					<!-- #ifndef APP-HARMONY -->
					<JnpfUploadFile v-if="child.__config__.jnpfKey=='uploadFile'"
						v-model="tableFormData[i][cIndex].value" :disabled="disabled||child.disabled"
						:limit="child.limit" :sizeUnit="child.sizeUnit" :fileSize="child.fileSize"
						:pathType="child.pathType" :isAccount="child.isAccount" :folder="child.folder"
						:accept="child.accept" :tipText="child.tipText" @change="onChange($event,child,i)"
						:sortRule="child.sortRule" :timeFormat="child.timeFormat" />
					<!-- #endif -->
					<JnpfUploadImg v-if="child.__config__.jnpfKey=='uploadImg'" v-model="tableFormData[i][cIndex].value"
						:disabled="disabled||child.disabled" :limit="child.limit" :sizeUnit="child.sizeUnit"
						:fileSize="child.fileSize" :pathType="child.pathType" :isAccount="child.isAccount"
						:folder="child.folder" :tipText="child.tipText" @change="onChange($event,child,i)"
						:sortRule="child.sortRule" :timeFormat="child.timeFormat" />
					<JnpfRate v-if="child.__config__.jnpfKey=='rate'" v-model="tableFormData[i][cIndex].value"
						:max="child.count" :allowHalf="child.allowHalf" :disabled="disabled||child.disabled"
						@change="onChange($event,child,i)" />
					<JnpfSlider v-if="child.__config__.jnpfKey=='slider'" v-model="tableFormData[i][cIndex].value"
						:step="child.step" :min="child.min" :max="child.max" :disabled="disabled||child.disabled"
						@change="onChange($event,child,i)" />
					<JnpfOrganizeSelect v-if="child.__config__.jnpfKey=='organizeSelect'"
						v-model="tableFormData[i][cIndex].value" :multiple="child.multiple"
						:placeholder="child.placeholder" :disabled="disabled||child.disabled"
						:selectType="child.selectType" :ableIds="child.ableIds"
						@change="(val,value)=>{onChange(value,child,i)}" />
					<JnpfDepSelect v-if="child.__config__.jnpfKey=='depSelect'" v-model="tableFormData[i][cIndex].value"
						:multiple="child.multiple" :placeholder="child.placeholder" :disabled="disabled||child.disabled"
						:ableIds="child.ableIds" :selectType="child.selectType"
						@change="(val,value)=>{onChange(value,child,i)}" />
					<JnpfPosSelect v-if="child.__config__.jnpfKey=='posSelect'" v-model="tableFormData[i][cIndex].value"
						:multiple="child.multiple" :placeholder="child.placeholder" :disabled="disabled||child.disabled"
						:ableIds="child.ableIds" :selectType="child.selectType"
						@change="(val,value)=>{onChange(value,child,i)}" />
					<JnpfUserSelect v-if="child.__config__.jnpfKey=='userSelect'"
						v-model="tableFormData[i][cIndex].value" :multiple="child.multiple"
						:placeholder="child.placeholder" :disabled="disabled||child.disabled"
						:selectType="child.selectType" :ableIds="child.ableIds" :clearable="child.clearable"
						:ableRelationIds="child.ableRelationIds" @change="(val,value)=>{onChange(value,child,i)}" />
					<JnpfUsersSelect v-if="child.__config__.jnpfKey=='usersSelect'"
						v-model="tableFormData[i][cIndex].value" :multiple="child.multiple"
						:placeholder="child.placeholder" :disabled="disabled||child.disabled"
						:selectType="child.selectType" :ableIds="child.ableIds" :clearable="child.clearable"
						@change="(val,value)=>{onChange(value,child,i)}" />
					<JnpfRoleSelect v-if="child.__config__.jnpfKey=='roleSelect'"
						v-model="tableFormData[i][cIndex].value" :vModel='child.__vModel__' :multiple="child.multiple"
						:disabled="disabled||child.disabled" :placeholder="child.placeholder"
						:selectType="child.selectType" :ableIds="child.ableIds"
						@change="(val,value)=>{onChange(value,child,i)}" />
					<JnpfGroupSelect v-if="child.__config__.jnpfKey=='groupSelect'"
						v-model="tableFormData[i][cIndex].value" :vModel='child.__vModel__' :multiple="child.multiple"
						:disabled="disabled||child.disabled" :placeholder="child.placeholder"
						:selectType="child.selectType" :ableIds="child.ableIds"
						@change="(val,value)=>{onChange(value,child,i)}" />
					<JnpfTreeSelect v-if="child.__config__.jnpfKey=='treeSelect'"
						v-model="tableFormData[i][cIndex].value" :options="child.options" :props="child.props"
						:multiple="child.multiple" :placeholder="child.placeholder" :disabled="disabled||child.disabled"
						:filterable="child.filterable" @change="(val,value)=>{onChange(value,child,i)}" />
					<JnpfAutoComplete v-if="child.__config__.jnpfKey=='autoComplete'"
						v-model="tableFormData[i][cIndex].value" :disabled="disabled||child.disabled"
						:interfaceName="child.interfaceName" :placeholder="child.placeholder"
						:interfaceId="child.interfaceId" :total="child.total" :templateJson="child.templateJson"
						:formData='formData' :relationField="child.relationField" :propsValue="child.propsValue"
						:clearable='child.clearable' @change="onChange($event,child,i)" />
					<JnpfAreaSelect v-if="child.__config__.jnpfKey=='areaSelect'"
						v-model="tableFormData[i][cIndex].value" :placeholder="child.placeholder" :level="child.level"
						:disabled="disabled||child.disabled" :multiple="child.multiple"
						@change="(val,value)=>{onChange(value,child,i)}" />
					<JnpfRelationForm v-if="child.__config__.jnpfKey=='relationForm'"
						v-model="tableFormData[i][cIndex].value" :placeholder="child.placeholder"
						:disabled="disabled||child.disabled" :modelId="child.modelId"
						:columnOptions="child.columnOptions" :relationField="child.relationField"
						:hasPage="child.hasPage" :pageSize="child.pageSize" :queryType="child.queryType"
						:vModel="child.__config__.tableName ? child.__vModel__ + '_jnpfTable_' + child.__config__.tableName + (child.__config__.isSubTable ? '0' : '1')+'_jnpfRelation_'+i : child.__vModel__+'_jnpfRelation_'+i"
						:popupTitle="child.popupTitle" @change="(val,value)=>{onChange(value,child,i)}"
						:propsValue="child.propsValue" />
					<JnpfRelationFormAttr v-if="child.__config__.jnpfKey=='relationFormAttr'"
						v-model="tableFormData[i][cIndex].value" :showField="child.showField"
						:relationField="child.relationField+'_jnpfRelation_'+i" :isStorage='child.isStorage' />
					<JnpfPopupSelect
						v-if="child.__config__.jnpfKey=='popupSelect'||child.__config__.jnpfKey=='popupTableSelect'"
						v-model="tableFormData[i][cIndex].value" :placeholder="child.placeholder" :rowIndex="i"
						:disabled="disabled||child.disabled" :interfaceId="child.interfaceId" :formData="formData"
						:templateJson="child.templateJson" :columnOptions="child.columnOptions"
						:relationField="child.relationField" :propsValue="child.propsValue" :hasPage="child.hasPage"
						:pageSize="child.pageSize"
						:vModel="child.__config__.tableName ? child.__vModel__ + '_jnpfTable_' + child.__config__.tableName + (child.__config__.isSubTable ? '0' : '1')+'_jnpfRelation_'+i : child.__vModel__+'_jnpfRelation_'+i"
						:popupTitle="child.popupTitle" @change="(val,value)=>{onChange(value,child,i)}" />
					<JnpfPopupAttr v-if="child.__config__.jnpfKey=='popupAttr'" v-model="tableFormData[i][cIndex].value"
						:showField="child.showField" :relationField="child.relationField+'_jnpfRelation_'+i"
						:isStorage='child.isStorage' />
					<JnpfCalculate v-if="child.__config__.jnpfKey=='calculate'" v-model="tableFormData[i][cIndex].value"
						:expression='child.expression' :config='child.__config__' :formData='formData'
						:precision="child.precision" :isAmountChinese="child.isAmountChinese"
						:thousands="child.thousands" :rowIndex="i" :roundType="child.roundType" />
					<JnpfSign v-if="child.__config__.jnpfKey=='sign'" v-model="tableFormData[i][cIndex].value"
						:fieldKey="child.__vModel__" :disabled="disabled||child.disabled"
						@change="onChange($event,child,i)" :isInvoke="child.isInvoke" />
					<JnpfSignature v-if="child.__config__.jnpfKey=='signature'" v-model="tableFormData[i][cIndex].value"
						:disabled="disabled||child.disabled" @change="onChange($event,child,i)"
						:ableIds="child.ableIds" />
					<JnpfLocation v-if="child.__config__.jnpfKey=='location'" v-model="tableFormData[i][cIndex].value"
						:autoLocation="child.autoLocation" :adjustmentScope="child.adjustmentScope"
						:enableLocationScope="child.enableLocationScope"
						:enableDesktopLocation="child.enableDesktopLocation" :locationScope="child.locationScope"
						:disabled="disabled||child.disabled" :clearable='child.clearable'
						@change="onChange($event,child,i)" />
					<JnpfOpenData v-if="systemList.includes(child.__config__.jnpfKey)"
						v-model="tableFormData[i][cIndex].value" />
				</u-form-item>
			</view>
		</view>
		<view class="jnpf-table-footer-btn" v-if="!disabled && getFooterBtnsList.length">
			<template v-for="item in getFooterBtnsList">
				<view class="jnpf-table-btn" :class="'jnpf-table-'+item.btnType+'-btn'" @click="footerBtnsHandle(item)">
					<text class="jnpf-table-btn-icon" :class="item.btnIcon" />
					<text class="jnpf-table-btn-text">
						{{ item.labelI18nCode ? $t(item.labelI18nCode, item.label) : item.label }}
					</text>
				</view>
			</template>
		</view>
		<view class="jnpf-table-item" v-if="config.showSummary && summaryField.length">
			<view class="jnpf-table-item-title">
				<text class="jnpf-table-item-title-num">
					{{config.__config__.label}}{{$t('component.table.summary')}}
				</text>
			</view>
			<view class="form-item-box">
				<u-form-item v-for="(item,index) in summaryField" :label="item.__config__.label" :key="item.__vModel__"
					:label-width="item.__config__.labelWidth ? item.__config__.labelWidth * 1.5 : undefined">
					<JnpfInput v-model="item.value" disabled placeholder="" />
				</u-form-item>
			</view>
		</view>
		<u-modal v-model="showTipsModal" width='70%' border-radius="16" :content-style="contentStyle"
			:titleStyle="titleStyle" :confirm-style="confirmStyle" :title="tipsTitle" :content="tipsContent"
			:confirm-text="$t('common.okText')" />
	</view>
</template>

<script>
	import {
		getModelInfo
	} from '@/api/apply/visualDev'
	import {
		getDictionaryDataSelector,
		getDataInterfaceRes
	} from '@/api/common'
	import {
		useBaseStore
	} from '@/store/modules/base'
	const baseStore = useBaseStore()
	const dyOptionsList = ['select', 'cascader', 'treeSelect']
	const systemList = ['createUser', 'createTime', 'modifyUser', 'modifyTime', 'currOrganize', 'currDept', 'currPosition',
		'billRule'
	]
	import childTableMixin from './mixin'
	export default {
		name: 'jnpf-child-table',
		inject: ["parameter", "relations", "isShortLink"],
		mixins: [childTableMixin],
		props: {
			config: {
				type: Object,
				default: () => {}
			},
			formData: {
				type: Object,
				required: true
			},
			modelValue: {
				type: [Array, String],
				default: () => ([])
			}
		},
		data() {
			return {
				systemList,
				dataInterfaceInfo: [],
				activeRowIndex: 0,
				tableData: [],
				tableFormData: [],
				summaryField: [],
				isIgnore: false,
				tableVmodel: '',
				childRelations: {},
				userInfo: {},
				showTipsModal: false,
				tipsContent: '',
				tipsTitle: this.$t('common.tipTitle'),
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
			}
		},
		watch: {
			tableFormData: {
				handler(val, oldVal) {
					//获取子表数据并更新
					const data = this.submit(true)
					this.config.__config__.defaultValue = data
					this.$emit('input', this.config)
					// 获取合计数据
					this.getTableSummaries()
				},
				deep: true
			},

		},
		computed: {
			disabled() {
				return this.config.disabled
			},
			getFooterBtnsList() {
				if (!this.config?.footerBtnsList?.length) return [];
				let list = this.config.footerBtnsList.filter(o => o.show);
				if (this.isShortLink) list = list.filter(o => ['add'].includes(o.value));
				//移除批量删除
				list = list.filter(item => item.value != 'batchRemove')
				return list;
			},
		},
		created() {
			this.init()
		},
		methods: {
			init() {
				this.userInfo = uni.getStorageSync('userInfo') || {}
				this.tableData = this.config.__config__.children || []
				this.handleSummary()
				this.buildOptions()
				this.handleListen()
				this.buildRelation()
			},
			handleSummary() {
				this.summaryField = []
				let summaryField = this.config.summaryField || []
				for (let i = 0; i < summaryField.length; i++) {
					for (let o = 0; o < this.tableData.length; o++) {
						const item = this.tableData[o]
						if (item.__vModel__ === summaryField[i] && !item.__config__.noShow) {
							this.summaryField.push({
								value: '0.00',
								...item
							})
						}
					}
				}
			},
			handleListen() {
				uni.$on('linkPageConfirm', (subVal, Vmodel) => {
					if (this.config.__vModel__ === Vmodel) {
						subVal.forEach(t => {
							this.tableFormData.push(this.getEmptyItem(t))
							this.buildRowAttr(this.tableFormData.length - 1, t)
						})
						setTimeout(() => {
							uni.$emit('initCollapse')
						}, 50)
					}

				})
				uni.$on('handleRelation', this.handleRelationForParent)
			},
			buildOptions() {
				for (let i = 0; i < this.tableData.length; i++) {
					const config = this.tableData[i].__config__
					if (dyOptionsList.indexOf(config.jnpfKey) > -1) {
						if (config.dataType === 'dictionary' && config.dictionaryType) {
							baseStore.getDicDataSelector(config.dictionaryType).then(res => {
								this.tableData[i].options = res || []
								uni.$emit('initCollapse')
							})
						}
						if (config.dataType === 'dynamic' && config.propsUrl) {
							let query = {
								paramList: this.getDefaultParamList(config.templateJson, this.formData)
							}
							const matchInfo = JSON.stringify({
								id: config.propsUrl,
								query
							});
							const item = {
								matchInfo,
								rowIndex: -1,
								colIndex: i
							};
							this.dataInterfaceInfo.push(item);
							getDataInterfaceRes(config.propsUrl, query).then(res => {
								this.tableData[i].options = Array.isArray(res.data) ? res.data : []
							})
						}
					}
				}
				this.initData()
			},
			initData() {
				if (Array.isArray(this.modelValue) && this.modelValue.length) {
					this.modelValue.forEach((t, index) => {
						this.tableFormData.push(this.getEmptyItem(t))
						this.buildAttr(index, t)
					})
					this.initRelationData()
					this.$nextTick(() => {
						uni.$emit('initCollapse')
					})
				}
			},
			buildAttr(rowIndex, val) {
				let row = this.tableFormData[rowIndex];
				for (let i = 0; i < row.length; i++) {
					let item = row[i];
					const config = item.__config__
					if (dyOptionsList.indexOf(config.jnpfKey) > -1) {
						if (config.dataType === 'dictionary' && config.dictionaryType) {
							baseStore.getDicDataSelector(config.dictionaryType).then(res => {
								item.options = res || []
								uni.$emit('initCollapse')
							})
						}
						if (config.dataType === 'dynamic' && config.propsUrl) {
							this.handleRelation(item, rowIndex)
							if (item.options && item.options.length && (!config.templateJson || !config.templateJson
									.length || !this.hasTemplateJsonRelation(config.templateJson))) continue
							let query = {
								paramList: this.getParamList(config.templateJson, this.formData, rowIndex)
							}
							const matchInfo = JSON.stringify({
								id: config.propsUrl,
								query
							});
							const itemInfo = {
								matchInfo,
								rowIndex,
								colIndex: i
							};
							const infoIndex = this.dataInterfaceInfo.findIndex(o => o.matchInfo === matchInfo);
							let useCacheOptions = false;
							if (infoIndex === -1) {
								this.dataInterfaceInfo.push(itemInfo);
							} else {
								const cacheOptions = this.getCacheOptions(infoIndex);
								if (cacheOptions.length) {
									item.options = cacheOptions;
									useCacheOptions = true;
									uni.$emit('initCollapse')
								}
							}
							if (!useCacheOptions) {
								getDataInterfaceRes(config.propsUrl, query).then(res => {
									item.options = Array.isArray(res.data) ? res.data : []
									uni.$emit('initCollapse')
								})
							}
						}
					}
				}
			},
			buildRelation() {
				for (let key in this.relations) {
					if (key.includes('-')) {
						let tableVModel = key.split('-')[0]
						if (tableVModel === this.config.__vModel__) {
							let newKey = key.split('-')[1]
							this.childRelations[newKey] = this.relations[key]
						}
					}
				}
			},
			/* 合计 */
			getTableSummaries() {
				if (!this.config.showSummary) return
				if (!this.tableFormData.length) return this.handleSummary()
				const list = this.tableFormData.map((row, i) => {
					return row.reduce((p, c) => {
						p[c.__vModel__] = c.value
						return p
					}, {})
				})
				for (let i = 0; i < this.summaryField.length; i++) {
					let val = 0
					for (let j = 0; j < list.length; j++) {
						const value = list[j][this.summaryField[i].__vModel__]
						if (value) {
							let data = isNaN(value) ? 0 : Number(value)
							val += data
						}
					}
					let realVal = val && !Number.isInteger(val) ? Number(val).toFixed(2) : val;
					if (this.summaryField[i].thousands) realVal = Number(realVal).toLocaleString('zh')
					this.summaryField[i].value = realVal
				}
			},
			handleRelationForParent(e, defaultValue, st) {
				if (!this.tableFormData.length) return
				for (let i = 0; i < this.tableFormData.length; i++) {
					let row = this.tableFormData[i];
					for (let j = 0; j < row.length; j++) {
						let item = row[j];
						const vModel = item.jnpfKey === 'popupSelect' ? item.__vModel__.substring(0, item.__vModel__
							.indexOf('_jnpfRelation_')) : item.__vModel__
						if (e.__vModel__ === vModel) {
							if (e.opType === 'setOptions') {
								item.options = []
								let query = {
									paramList: this.getParamList(e.__config__.templateJson, this.formData, i)
								}
								getDataInterfaceRes(e.__config__.propsUrl, query).then(res => {
									item.options = Array.isArray(res.data) ? res.data : []
									uni.$emit('initCollapse')
								})
							}
							if (e.opType === 'setUserOptions') {
								if (e.relationField.includes('-')) {
									const [attr1, attr2] = e.relationField.split('-')
									this.$nextTick(() => {
										let value = this.formData[attr1][i][attr2] || []
										this.$set(this.tableFormData[i][j], 'ableRelationIds', Array.isArray(
											value) ? value : [value])
									})
								} else {
									let value = this.formData[e.relationField] || []
									this.$set(this.tableFormData[i][j], 'ableRelationIds', Array.isArray(value) ? value : [
										value
									])
								}
							}
							this.$nextTick(() => {
								if (e.opType === 'setDate') {
									let startTime = 0
									let endTime = 0
									if (e.__config__.startRelationField && e.__config__.startTimeType == 2) {
										if (e.__config__.startRelationField.includes('-')) {
											const [attr0, attr5] = e.__config__.startRelationField.split('-')
											startTime = this.formData[attr0][i][attr5] || 0
										} else {
											startTime = this.formData[e.__config__.startRelationField] || 0
										}
									} else {
										startTime = e.startTime
									}
									if (e.__config__.endRelationField && e.__config__.endTimeType == 2) {
										if (e.__config__.endRelationField.includes('-')) {
											const [attr3, attr4] = e.__config__.endRelationField.split('-')
											endTime = this.formData[attr3][i][attr4] || 0
										} else {
											endTime = this.formData[e.__config__.endRelationField] || 0
										}
									} else {
										endTime = e.endTime
									}
									item.startTime = startTime
									item.endTime = endTime
								}
								if (e.opType === 'setTime') {
									let format = e.format
									let startTime = ''
									let endTime = ''
									if (e.__config__.startRelationField && e.__config__.startTimeType == 2) {
										if (e.__config__.startRelationField.includes('-')) {
											const [attr0, attr5] = e.__config__.startRelationField.split('-')
											startTime = this.formData[attr0][i][attr5] || '00:00:00'
										} else {
											startTime = this.formData[e.__config__.startRelationField] ||
												'00:00:00'
										}
										startTime = startTime && startTime.split(':').length == 3 ? startTime :
											startTime + ':00'
									} else {
										startTime = e.startTime
									}
									if (e.__config__.endRelationField && e.__config__.endTimeType == 2) {
										if (e.__config__.endRelationField.includes('-')) {
											const [attr3, attr4] = e.__config__.endRelationField.split('-')
											endTime = this.formData[attr3][i][attr4] || '23:59:59'
										} else {
											endTime = this.formData[e.__config__.endRelationField] || '23:59:59'
										}
										endTime = endTime && endTime.split(':').length == 3 ? endTime : endTime +
											':00'
									} else {
										endTime = e.endTime
									}
									item.startTime = startTime
									item.endTime = endTime
								}
							})
							if (item.value != defaultValue) {
								if (st || !item.value) item.value = defaultValue
							}
						}
					}
				}
			},
			handleRelation(data, rowIndex) {
				const currRelations = this.childRelations
				for (let key in currRelations) {
					if (key === data.__vModel__) {
						for (let i = 0; i < currRelations[key].length; i++) {
							const e = currRelations[key][i];
							const config = e.__config__
							const jnpfKey = config.jnpfKey
							let defaultValue = ''
							if (['checkbox', 'cascader'].includes(jnpfKey) || (['select', 'treeSelect',
									'popupSelect',
									'popupTableSelect', 'userSelect'
								].includes(jnpfKey) && e.multiple)) {
								defaultValue = []
							}
							let row = this.tableFormData[rowIndex];
							for (let j = 0; j < row.length; j++) {
								let item = row[j];
								const vModel = item.jnpfKey === 'popupSelect' ? item.__vModel__.substring(0, item
									.__vModel__.indexOf('_jnpfRelation_')) : item.__vModel__
								if (e.__vModel__ === vModel) {
									if (e.opType === 'setOptions') {
										item.options = []
										let query = {
											paramList: this.getParamList(config.templateJson, this.formData, rowIndex)
										}
										getDataInterfaceRes(config.propsUrl, query).then(res => {
											item.options = Array.isArray(res.data) ? res.data : []
											uni.$emit('initCollapse')
										})
									}
									if (e.opType === 'setUserOptions') {
										let value = this.getFieldVal(e.relationField, rowIndex) || []
										item.ableRelationIds = Array.isArray(value) ? value : [value]
									}
									if (e.opType === 'setDate') {
										let startTime = 0
										let endTime = 0
										if (config.startRelationField && config.startTimeType == 2) {
											startTime = this.getFieldVal(config.startRelationField, rowIndex) || 0
										} else {
											startTime = e.startTime
										}
										if (config.endRelationField && config.endTimeType == 2) {
											endTime = this.getFieldVal(config.endRelationField, rowIndex) || 0
										} else {
											endTime = e.endTime
										}
										item.startTime = startTime
										item.endTime = endTime
									}
									if (e.opType === 'setTime') {
										let startTime = 0
										let endTime = 0
										if (config.startRelationField && config.startTimeType == 2) {
											startTime = this.getFieldVal(config.startRelationField, rowIndex) || '00:00:00'
											startTime = startTime.split(':').length == 3 ? startTime : startTime + ':00'
										} else {
											startTime = e.startTime
										}
										if (config.endRelationField && config.endTimeType == 2) {
											endTime = this.getFieldVal(config.endRelationField, rowIndex) || '23:59:59'
											endTime = endTime.split(':').length == 3 ? endTime : endTime + ':00'
										} else {
											endTime = e.endTime
										}
										item.startTime = startTime
										item.endTime = endTime
									}
									if (item.value != defaultValue) {
										item.value = defaultValue
										this.$nextTick(() => this.handleRelation(item, rowIndex));
									}
								}
							}
						}
					}
				}
			},
			handleDefaultRelation(data, rowIndex = 0) {
				const currRelations = this.childRelations
				for (let key in currRelations) {
					if (key === data) {
						for (let i = 0; i < currRelations[key].length; i++) {
							const e = currRelations[key][i];
							const config = e.__config__
							let defaultValue = ''
							let row = this.tableFormData[rowIndex];
							for (let j = 0; j < row.length; j++) {
								let item = row[j];
								const vModel = item.jnpfKey === 'popupSelect' ? item.__vModel__.substring(0, item
									.__vModel__.indexOf('_jnpfRelation_')) : item.__vModel__
								if (e.__vModel__ === vModel) {
									if (e.opType === 'setUserOptions') {
										let value = this.getFieldVal(e.relationField, rowIndex) || []
										item.ableRelationIds = Array.isArray(value) ? value : [value]
									}
									if (e.opType === 'setDate') {
										let startTime = 0
										let endTime = 0
										if (config.startRelationField && config.startTimeType == 2) {
											startTime = this.getFieldVal(config.startRelationField, rowIndex) || 0
										} else {
											startTime = e.startTime
										}
										if (config.endRelationField && config.endTimeType == 2) {
											endTime = this.getFieldVal(config.endRelationField, rowIndex) || 0
										} else {
											endTime = e.endTime
										}
										item.startTime = startTime
										item.endTime = endTime
									}
									if (e.opType === 'setTime') {
										let startTime = 0
										let endTime = 0
										if (config.startRelationField && config.startTimeType == 2) {
											startTime = this.getFieldVal(config.startRelationField, rowIndex) || '00:00:00'
											if (startTime.split(':').length == 3) {
												startTime = startTime
											} else {
												startTime = startTime + ':00'
											}
										} else {
											startTime = e.startTime
										}
										if (config.endRelationField && config.endTimeType == 2) {
											endTime = this.getFieldVal(config.endRelationField, rowIndex) ||
												'23:59:59'
											if (endTime.split(':').length == 3) {
												endTime = endTime
											} else {
												endTime = endTime + ':00'
											}
										} else {
											endTime = e.endTime
										}
										item.startTime = startTime
										item.endTime = endTime
									}
								}
							}
						}
					}
				}
			},
			getFieldVal(field, rowIndex) {
				let val = ''
				if (field.includes('-')) {
					let childVModel = field.split('-')[1]
					let list = this.tableFormData[rowIndex].filter(o => o.__vModel__ === childVModel)
					val = list.length ? list[0].value : ''
				} else {
					val = this.formData[field] || ''
				}
				return val
			},
			buildRowAttr(rowIndex, val) {
				let row = this.tableFormData[rowIndex];
				for (let i = 0; i < row.length; i++) {
					let item = row[i];
					const config = item.__config__
					for (let key in this.modelValue[rowIndex]) {
						if (key === item.__vModel__) item.value = this.modelValue[rowIndex][key]
					}
					if (dyOptionsList.indexOf(config.jnpfKey) > -1) {
						if (config.dataType === 'dictionary' && config.dictionaryType) {
							baseStore.getDicDataSelector(config.dictionaryType).then(res => {
								item.options = res || []
								uni.$emit('initCollapse')
							})
						}
						if (config.dataType === 'dynamic' && config.propsUrl) {
							this.handleRelation(item, rowIndex)
							if (item.options && item.options.length && (!config.templateJson || !config.templateJson
									.length || !this.hasTemplateJsonRelation(config.templateJson))) continue
							let query = {
								paramList: this.getParamList(config.templateJson, this.formData, rowIndex)
							}
							const matchInfo = JSON.stringify({
								id: config.propsUrl,
								query
							});
							const itemInfo = {
								matchInfo,
								rowIndex,
								colIndex: i
							};
							const infoIndex = this.dataInterfaceInfo.findIndex(o => o.matchInfo === matchInfo);
							let useCacheOptions = false;
							if (infoIndex === -1) {
								this.dataInterfaceInfo.push(itemInfo);
							} else {
								const cacheOptions = this.getCacheOptions(infoIndex);
								if (cacheOptions.length) {
									item.options = cacheOptions;
									uni.$emit('initCollapse')
									useCacheOptions = true;
								}
							}
							if (!useCacheOptions) {
								getDataInterfaceRes(config.propsUrl, query).then(res => {
									item.options = Array.isArray(res.data) ? res.data : []
									uni.$emit('initCollapse')
								})
							}
						}
					}
					if (config.jnpfKey === 'userSelect' && item.relationField && item.selectType !== 'all' && item
						.selectType !== 'custom') {
						let value = this.getFieldVal(item.relationField, rowIndex) || []
						item.ableRelationIds = Array.isArray(value) ? value : [value]
					}
					if (config.jnpfKey === 'datePicker') {
						let startTime = 0
						let endTime = 0
						if (config.startRelationField && config.startTimeType == 2) {
							startTime = this.getFieldVal(config.startRelationField, rowIndex) || 0
						} else {
							startTime = item.startTime
						}
						if (config.endRelationField && config.endTimeType == 2) {
							endTime = this.getFieldVal(config.endRelationField, rowIndex) || 0
						} else {
							endTime = item.endTime
						}
						item.startTime = startTime
						item.endTime = endTime
					}
					if (config.jnpfKey === 'timePicker') {
						let startTime = 0
						let endTime = 0
						if (config.startRelationField && config.startTimeType == 2) {
							startTime = this.getFieldVal(config.startRelationField, rowIndex) || '00:00:00'
							startTime = startTime && (startTime.split(':').length == 3) ? startTime : startTime + ':00'
						} else {
							startTime = item.startTime
						}
						if (config.endRelationField && config.endTimeType == 2) {
							endTime = this.getFieldVal(config.endRelationField, rowIndex) || '23:59:59'
							endTime = endTime.split(':').length == 3 ? endTime : endTime + ':00'
						} else {
							endTime = item.endTime
						}
						item.startTime = startTime
						item.endTime = endTime
					}
				}
			},
			// 获取缓存options数据
			getCacheOptions(index) {
				const item = this.dataInterfaceInfo[index];
				if (item.rowIndex === -1) return this.tableData[item.colIndex].options || [];
				return this.tableFormData[item.rowIndex][item.colIndex].options || [];
			},
			// 判断templateJson里是否有关联字段
			hasTemplateJsonRelation(templateJson) {
				return templateJson.some(o => o.relationField);
			},
			getParamList(templateJson, formData, index) {
				if (!templateJson) return []
				for (let i = 0; i < templateJson.length; i++) {
					if (templateJson[i].relationField && templateJson[i].sourceType == 1) {
						if (templateJson[i].relationField.includes('-')) {
							let childVModel = templateJson[i].relationField.split('-')[1]
							let list = this.tableFormData[index].filter(o => o.__vModel__ === childVModel)
							templateJson[i].defaultValue = list.length ? list[0].value : ''
						} else {
							templateJson[i].defaultValue = formData[templateJson[i].relationField] || ''
						}
					}
				}
				return templateJson
			},
			getDefaultParamList(templateJson, formData) {
				if (!templateJson) return []
				for (let i = 0; i < templateJson.length; i++) {
					if (templateJson[i].relationField && templateJson[i].sourceType == 1) {
						if (templateJson[i].relationField.includes('-')) {
							let childVModel = templateJson[i].relationField.split('-')[1]
							let list = this.tableData.filter(o => o.__vModel__ === childVModel)
							templateJson[i].defaultValue = ''
							if (list.length) templateJson[i].defaultValue = list[0].__config__.defaultValue || ''
						} else {
							templateJson[i].defaultValue = formData[templateJson[i].relationField] || ''
						}
					}
				}
				return templateJson
			},
			initRelationData() {
				const handleRelationFun = (list) => {
					list.forEach(cur => {
						this.handleDefaultRelation(cur.__vModel__)
						if (cur.__config__.children) handleRelationFun(cur.__config__.children)
					})
				}
				handleRelationFun(this.config.__config__.children)
			},
			getEmptyItem(val) {
				return this.tableData.map(o => {
					const config = o.__config__
					if (config.jnpfKey === 'datePicker' && config.defaultCurrent) {
						let format = this.jnpf.handelFormat(o.format)
						let dateStr = this.jnpf.toDate(new Date().getTime(), format)
						let time = format === 'yyyy' ? '-01-01 00:00:00' : format === 'yyyy-MM' ? '-01 00:00:00' :
							format === 'yyyy-MM-dd' ? ' 00:00:00' : ''
						config.defaultValue = new Date(dateStr + time).getTime()
					}
					if (config.jnpfKey === 'timePicker' && config.defaultCurrent) {
						config.defaultValue = this.jnpf.toDate(new Date(), o.format)
					}
					const res = {
						...o,
						value: val ? val[o.__vModel__] : config.defaultValue,
						options: config.dataType == "dynamic" ? [] : o.options,
						rowData: val || {}
					}
					return res
				})
			},
			formatData() {
				const organizeIdList = this.userInfo.organizeIdList
				for (let i = 0; i < this.tableFormData.length; i++) {
					const item = this.tableFormData[i]
					for (let j = 0; j < item.length; j++) {
						const it = item[j]
						const config = item[j].__config__
						if (config.jnpfKey === 'datePicker' && config.defaultCurrent &&
							i === this.tableFormData.length - 1) {
							let format = this.jnpf.handelFormat(it.format)
							let dateStr = this.jnpf.toDate(new Date().getTime(), format)
							let time = format === 'yyyy' ? '-01-01 00:00:00' : format === 'yyyy-MM' ?
								'-01 00:00:00' : format === 'yyyy-MM-dd' ?
								' 00:00:00' : ''
							it.value = new Date(dateStr + time).getTime()
						}
						if (config.jnpfKey === 'organizeSelect' && config.defaultCurrent && Array.isArray(
								organizeIdList) && organizeIdList.length && i === this.tableFormData.length - 1) {
							it.value = it.multiple ? [organizeIdList] : organizeIdList
						}
					}
				}
			},
			checkData(item) {
				if ([null, undefined, ''].includes(item.value)) return false
				if (Array.isArray(item.value)) return item.value.length > 0
				return true
			},
			submit(noShowToast) {
				let res = true
				outer: for (let i = 0; i < this.tableFormData.length; i++) {
					const row = this.tableFormData[i]
					for (let j = 0; j < row.length; j++) {
						const cur = row[j]
						const config = cur.__config__
						if (config.required && !this.checkData(cur) && config.isVisibility && !config.noShow) {
							res = false
							if (!noShowToast) this.$u.toast(
								`${this.config.__config__.label}(${i+1})${config.label}${this.$t('sys.validate.textRequiredSuffix')}`
							)
							break outer
						}
						if (config.regList && config.regList.length && config.isVisibility) {
							let regList = config.regList
							for (let ii = 0; ii < regList.length; ii++) {
								const item = regList[ii];
								if (item.pattern) {
									item.pattern = item.pattern.toString()
									let start = item.pattern.indexOf('/')
									let stop = item.pattern.lastIndexOf('/')
									let str = item.pattern.substring(start + 1, stop)
									let reg = new RegExp(str)
									item.pattern = reg
								}
								if (cur.value && item.pattern && !item.pattern.test(cur.value)) {
									if (item.messageI18nCode) {
										item.message = this.$t(item.messageI18nCode, item.message);
									}
									if (!noShowToast) this.$u.toast(
										`${this.config.__config__.label}(${i+1})${config.label}${item.message}`
									)
									res = false
									break outer
								}
							}
						}
					}
				}
				const data = this.getTableValue() || []
				return noShowToast ? data : res ? data : false
			},
			getTableValue() {
				return this.tableFormData.map(row => row.reduce((p, c) => {
					let str = c.__vModel__
					if (c.__vModel__ && c.__vModel__.indexOf('_jnpfRelation_') >= 0) {
						str = c.__vModel__.substring(0, c.__vModel__.indexOf('_jnpfRelation_'))
					}
					p[str] = c.value
					if (c.rowData) p = {
						...c.rowData,
						...p
					}
					return p
				}, {}))
			},
			setTableFormData(prop, value) {
				let activeRow = this.tableFormData[this.activeRowIndex] || []
				for (let i = 0; i < activeRow.length; i++) {
					if (activeRow[i].__vModel__ === prop) {
						activeRow[i].value = value
						break
					}
				}
			},
			getTableFieldOptions(prop) {
				let res = []
				for (let i = 0; i < this.tableData.length; i++) {
					if (this.tableData[i].__vModel__ === prop) {
						res = this.tableData[i].options || []
						break
					}
				}
				return res
			},
			onChange(val, data, rowIndex) {
				this.activeRowIndex = rowIndex;
				this.setScriptFunc(val, data, 'change', rowIndex)
				if (['popupSelect', 'relationForm'].includes(data.__config__.jnpfKey)) {
					this.setTransferFormData(val, data.__config__, data.__config__.jnpfKey)
				}
				this.$nextTick(() => this.handleRelation(data, rowIndex))
			},
			setScriptFunc(val, data, type = 'change', rowIndex) {
				if (data && data.on && data.on[type]) {
					const func = this.jnpf.getScriptFunc(data.on[type]);
					if (!func) return
					func.call(this, {
						data: val,
						rowIndex,
						...this.parameter
					})
				}
			},
			setTransferFormData(data, config, jnpfKey) {
				if (!config.transferList.length) return;
				let row = this.tableFormData[this.activeRowIndex];
				for (let index = 0; index < config.transferList.length; index++) {
					const element = config.transferList[index];
					if (element.sourceValue.includes('-')) element.sourceValue = element.sourceValue.split('-')[1];
					for (let index = 0; index < row.length; index++) {
						const e = row[index];
						if (e.__vModel__ == element.sourceValue) e.value = data[element.targetField];
					}
				}
			},
			clickIcon(e) {
				if (!e.__config__.tipLabel) return
				this.tipsContent = e.__config__.tipLabel
				this.tipsTitle = e.__config__.label
				this.showTipsModal = true
			},
			onBlur(val, data, rowIndex) {
				this.activeRowIndex = rowIndex
				this.setScriptFunc(val, data, 'blur', rowIndex)
			},
			columnBtnsHandel(item, index) {
				if (item.value == 'remove') return this.removeRow(index, item.showConfirm);
				if (item.value == 'copy') return this.copyRow(index);
			},
			/* 子表删除 */
			removeRow(index, showConfirm = 0) {
				const handleRemove = () => {
					this.tableFormData.splice(index, 1);
					this.modelValue.splice(index, 1)
					this.$nextTick(() => uni.$emit('initCollapse'))
				};
				if (!showConfirm) return handleRemove();
				uni.showModal({
					title: this.$t('common.tipTitle'),
					content: this.$t('common.delTip'),
					success: (res) => {
						if (res.confirm) handleRemove()
					}
				})
			},
			/* 子表复制 */
			copyRow(index) {
				let item = JSON.parse(JSON.stringify(this.tableFormData[index]));
				item.forEach(item => {
					if (systemList.includes(item.__config__.jnpfKey)) {
						item.value = ''
						item.placeholder = '系统自动生成'
					}
				})
				item.length && item.map(o => delete o.rowData);
				this.tableFormData.push(item);
			},
			footerBtnsHandle(item) {
				item.value == 'add' ? this.addRow() : this.openSelectDialog(item)
			},
			/* 子表删除 */
			addRow(val) {
				this.tableFormData.push(this.getEmptyItem(val))
				if (this.tableFormData.length) this.formatData()
				const rowIndex = this.tableFormData.length - 1
				this.buildRowAttr(rowIndex, val)
				this.$nextTick(() => uni.$emit('initCollapse'))
			}
		}
	}
</script>