<template>
	<JnpfText v-if="config.jnpfKey=='text'" :content="item.content" :textStyle="item.textStyle" />
	<JnpfGroupTitle v-else-if="config.jnpfKey=='groupTitle'" :content="item.content"
		:content-position="item.contentPosition" :helpMessage="item.helpMessage" @groupIcon="clickIcon(item)" />
	<JnpfDivider v-else-if="config.jnpfKey=='divider'" :content="item.content" />
	<view class="jnpf-card" v-else-if="config.jnpfKey==='card'||config.jnpfKey==='row'">
		<view class="jnpf-card-cap u-line-1 u-flex" v-if="item.header" @click="clickIcon(item)">
			{{item.header}}
			<u-icon :name="config.tipLabel? 'question-circle-fill':''" class="u-m-l-10" color="#a0acb7" />
		</view>
		<template v-for="(child, index) in config.children" :key="child.__config__.renderKey">
			<item v-if="!child.__config__.noShow&& child.__config__.isVisibility" :itemData="child"
				:ref="child.__vModel__?child.__vModel__: 'ref'+item.__config__.formId" :formConf="formConf"
				:formData="formData" @input="setValue" @clickIcon='clickIcon' @clickFun="onChildClick"
				@collapse-change="onChildCollapseChange" @tab-change='onChildTabChange' />
		</template>
	</view>
	<!-- 步骤条 -->
	<view v-else-if="config.jnpfKey==='steps'">
		<view class="step-container">
			<u-steps :list="config.children" :mode="item.simple ? 'dot' :'number'" name="title"
				@change="onStepChange($event,item)" :current="stepCurrent">
			</u-steps>
		</view>
		<view v-for="(it,i) in config.children" :key='i'>
			<view v-show="i == stepCurrent">
				<template v-for="(child, index) in it.__config__.children" :key="child.__config__.renderKey">
					<item v-if="!child.__config__.noShow&& child.__config__.isVisibility" :itemData="child"
						:formConf="formConf" :formData="formData"
						:ref="child.__vModel__?child.__vModel__: 'ref'+item.__config__.formId" @input="setValue"
						@clickIcon='clickIcon' @clickFun="onChildClick" @collapse-change="onChildCollapseChange" />
				</template>
			</view>
		</view>
	</view>
	<view class="jnpf-tab" v-else-if="config.jnpfKey==='tab'">
		<u-tabs is-scroll :list="config.children" name="title" v-model="tabCurrent" @change="onTabChange"
			:key="tabKey" />
		<view v-for="(it,i) in config.children" :key='i'>
			<view v-show="i == tabCurrent">
				<template v-for="(child, index) in it.__config__.children" :key="child.__config__.renderKey">
					<item v-if="!child.__config__.noShow&& child.__config__.isVisibility" :itemData="child"
						:formConf="formConf" :formData="formData"
						:ref="child.__vModel__?child.__vModel__: 'ref'+item.__config__.formId" @input="setValue"
						@clickIcon='clickIcon' @clickFun="onChildClick" @collapse-change="onChildCollapseChange"
						@tab-change='onChildTabChange' />
				</template>
			</view>
		</view>
	</view>
	<view v-else-if="config.jnpfKey==='collapse'">
		<u-collapse ref="collapseRef" :head-style="{'padding-left':'20rpx'}" :accordion="item.accordion">
			<u-collapse-item v-for="(it, i) in config.children" :key="i" :title="it.title"
				:open="config.active.indexOf(it.name)>-1" @change="onCollapseChange">
				<template v-for="(child, index) in it.__config__.children" :key="child.__config__.renderKey">
					<item v-if="!child.__config__.noShow&& child.__config__.isVisibility" :itemData="child"
						:formConf="formConf" :formData="formData"
						:ref="child.__vModel__?child.__vModel__: 'ref'+item.__config__.formId" @input="setValue"
						@clickIcon='clickIcon' @clickFun="onChildClick" @collapse-change="onChildCollapseChange"
						@tab-change='onChildTabChange' />
				</template>
			</u-collapse-item>
		</u-collapse>
	</view>
	<view v-else-if="config.jnpfKey==='table'">
		<child-table v-if="config.isVisibility" v-model="value" :config="item" :ref="item.__vModel__"
			:formData='formData' @input="setValue" />
	</view>
	<u-form-item v-else-if="config.jnpfKey=='popupSelect' || config.jnpfKey=='relationForm'" :label=" realLabel"
		class="popup-select" :prop="item.__vModel__" :required="config.required" :label-width="labelWidth"
		:left-icon="leftIcon" :left-icon-style="{'color':'#a8aaaf'}" @clickIcon="clickIcon(item)">
		<!-- 弹窗选择 -->
		<JnpfPopupSelect v-if="config.jnpfKey=='popupSelect'" v-model="value" :placeholder="item.placeholder"
			:disabled="item.disabled" :interfaceId="item.interfaceId" :formData="formData"
			:templateJson="item.templateJson" :columnOptions="item.columnOptions" :extraOptions="item.extraOptions"
			:relationField="item.relationField" :propsValue="item.propsValue" :hasPage="item.hasPage"
			:pageSize="item.pageSize"
			:vModel="config.tableName ? item.__vModel__ + '_jnpfTable_' + config.tableName + (config.isSubTable ? '0' : '1') : config.__vModel__"
			:popupTitle="item.popupTitle" @change="onChange" />
		<!-- 关联表单 -->
		<JnpfRelationForm v-if="config.jnpfKey=='relationForm'" v-model="value" :placeholder="item.placeholder"
			:disabled="item.disabled" :modelId="item.modelId" :columnOptions="item.columnOptions"
			:extraOptions="item.extraOptions" :relationField="item.relationField" :hasPage="item.hasPage"
			:pageSize="item.pageSize" :queryType="item.queryType"
			:vModel="config.tableName ? item.__vModel__ + '_jnpfTable_' + config.tableName + (config.isSubTable ? '0' : '1') : item.__vModel__"
			:popupTitle="item.popupTitle" @change="onChange" :propsValue="item.propsValue" />
	</u-form-item>
	<u-form-item v-else :label=" realLabel" :prop="item.__vModel__" :required="config.required"
		:label-width="labelWidth" :left-icon="leftIcon" :left-icon-style="{'color':'#a8aaaf'}"
		@clickIcon="clickIcon(item)">
		<JnpfInput v-if="config.jnpfKey=='input'" v-model="value" :showPassword="item.showPassword"
			:placeholder="item.placeholder" :maxlength="item.maxlength" :showCount="item.showCount"
			:disabled="item.disabled" :clearable='item.clearable' :useScan="item.useScan"
			:addonBefore="item.addonBefore" :addonAfter="item.addonAfter" @change="onChange" @blur="onBlur" />
		<JnpfTextarea v-if="config.jnpfKey=='textarea'" v-model="value" :placeholder="item.placeholder"
			:maxlength="item.maxlength" :showCount="item.showCount" :disabled="item.disabled"
			:clearable='item.clearable' @change="onChange" @blur="onBlur" />
		<!--数字输入步进器-->
		<JnpfInputNumber v-if="config.jnpfKey=='inputNumber'" v-model="value" :step='item.step'
			:max='item.max||999999999999999' :min='item.min||-999999999999999' :disabled="item.disabled"
			:placeholder="item.placeholder" :isAmountChinese="item.isAmountChinese" :thousands="item.thousands"
			:addonAfter="item.addonAfter" :addonBefore="item.addonBefore" :controls="item.controls"
			:precision="item.precision" @change="onChange" @blur="onBlur" />
		<!-- 开关-->
		<JnpfSwitch v-if="config.jnpfKey=='switch'" v-model="value" :disabled="item.disabled" @change="onChange" />
		<!-- 单选框组 -->
		<JnpfRadio v-if="config.jnpfKey=='radio'" v-model="value" :options="item.options" :props="item.props"
			:disabled="item.disabled" :direction='item.direction' @change="onChange" />
		<!-- 多选框组 -->
		<JnpfCheckbox v-if="config.jnpfKey=='checkbox'" v-model="value" :options="item.options" :props="item.props"
			:disabled="item.disabled" :direction='item.direction' @change="onChange" />
		<!-- 下拉选择 -->
		<JnpfSelect v-if="config.jnpfKey=='select'" v-model="value" :placeholder="item.placeholder"
			:options="item.options" :props="item.props" :multiple="item.multiple" :disabled="item.disabled"
			@change="onChange" :filterable="item.filterable" />
		<!-- 级联选择 -->
		<JnpfCascader v-if="config.jnpfKey=='cascader'" v-model="value" :placeholder="item.placeholder"
			:options="item.options" :props="item.props" :disabled="item.disabled" :multiple="item.multiple"
			:filterable='item.filterable' :clearable='item.clearable' :showAllLevels="item.showAllLevels"
			@change="onChange" />
		<JnpfDatePicker v-if="config.jnpfKey=='datePicker'" v-model="value" :placeholder="item.placeholder"
			:disabled="item.disabled" :format="item.format" :startTime="item.startTime" :endTime='item.endTime'
			@change="onChange" />
		<JnpfTimePicker v-if="config.jnpfKey=='timePicker'" v-model="value" :placeholder="item.placeholder"
			:disabled="item.disabled" :format="item.format" :startTime="item.startTime" :endTime='item.endTime'
			@change="onChange" />
		<!-- #ifndef APP-HARMONY -->
		<JnpfUploadFile v-if="config.jnpfKey=='uploadFile'" v-model="value" :disabled="item.disabled"
			:limit="item.limit" :sizeUnit="item.sizeUnit" :fileSize="item.fileSize" :pathType="item.pathType"
			:isAccount="item.isAccount" :folder="item.folder" :accept="item.accept" :tipText="item.tipText"
			@change="onChange" :sortRule="item.sortRule" :timeFormat="item.timeFormat" />
		<!-- #endif -->
		<!-- 图片上传 -->
		<JnpfUploadImg v-if="config.jnpfKey=='uploadImg'" v-model="value" :disabled="item.disabled" :limit="item.limit"
			:sizeUnit="item.sizeUnit" :fileSize="item.fileSize" :pathType="item.pathType" :isAccount="item.isAccount"
			:folder="item.folder" :tipText="item.tipText" @change="onChange" :sortRule="item.sortRule"
			:timeFormat="item.timeFormat" />
		<!-- 颜色选择 -->
		<JnpfColorPicker v-if="config.jnpfKey=='colorPicker'" v-model="value" :colorFormat="item.colorFormat"
			:disabled="item.disabled" @change="onChange" />
		<JnpfRate v-if="config.jnpfKey=='rate'" v-model="value" :max="item.count" :allowHalf="item.allowHalf"
			:disabled="item.disabled" @change="onChange" />
		<JnpfSlider v-if="config.jnpfKey=='slider'" v-model="value" :step="item.step" :min="item.min" :max="item.max"
			:disabled="item.disabled" @change="onChange" />
		<JnpfBarcode v-if="config.jnpfKey=='barcode'" :staticText="item.staticText" :width="item.width"
			:height="item.height" :format="item.format" :dataType="item.dataType" :lineColor="item.lineColor"
			:background="item.background" :relationField="item.relationField" :formData="formData" />
		<JnpfQrcode v-if="config.jnpfKey=='qrcode'" :staticText="item.staticText" :width="item.width"
			:dataType="item.dataType" :colorDark="item.colorDark" :colorLight="item.colorLight"
			:relationField="item.relationField" :formData="formData" />
		<JnpfOrganizeSelect v-if="config.jnpfKey=='organizeSelect'" v-model="value" :multiple="item.multiple"
			:placeholder="item.placeholder" :disabled="item.disabled" :ableIds="item.ableIds"
			:selectType="item.selectType" @change="onChange" />
		<JnpfDepSelect v-if="config.jnpfKey=='depSelect'" v-model="value" :multiple="item.multiple"
			:placeholder="item.placeholder" :disabled="item.disabled" :ableIds="item.ableIds"
			:selectType="item.selectType" @change="onChange" />
		<JnpfPosSelect v-if="config.jnpfKey=='posSelect'" v-model="value" :multiple="item.multiple"
			:placeholder="item.placeholder" :disabled="item.disabled" :ableIds="item.ableIds"
			:selectType="item.selectType" @change="onChange" />
		<JnpfUserSelect v-if="config.jnpfKey=='userSelect'" v-model="value" :multiple="item.multiple"
			:placeholder="item.placeholder" :disabled="item.disabled" :selectType="item.selectType"
			:ableIds="item.ableIds" :clearable="item.clearable" :ableRelationIds="item.ableRelationIds"
			@change="onChange" />
		<JnpfUsersSelect v-if="config.jnpfKey=='usersSelect'" v-model="value" :multiple="item.multiple"
			:placeholder="item.placeholder" :disabled="item.disabled" :selectType="item.selectType"
			:ableIds="item.ableIds" :clearable="item.clearable" @change="onChange" />
		<JnpfRoleSelect v-if="config.jnpfKey=='roleSelect'" v-model="value" :vModel='item.__vModel__'
			:multiple="item.multiple" :disabled="item.disabled" :placeholder="item.placeholder" :ableIds="item.ableIds"
			:selectType="item.selectType" @change="onChange" />
		<JnpfGroupSelect v-if="config.jnpfKey=='groupSelect'" v-model="value" :vModel='item.__vModel__'
			:multiple="item.multiple" :disabled="item.disabled" :ableIds="item.ableIds" :selectType="item.selectType"
			:placeholder="item.placeholder" @change="onChange" />
		<!-- 下拉树形 -->
		<JnpfTreeSelect v-if="config.jnpfKey=='treeSelect'" v-model="value" :options="item.options" :props="item.props"
			:multiple="item.multiple" :placeholder="item.placeholder" :disabled="item.disabled"
			:filterable="item.filterable" @change="onChange" />
		<JnpfAutoComplete v-if="config.jnpfKey=='autoComplete'" v-model="value" :disabled="item.disabled"
			:interfaceName="item.interfaceName" :placeholder="item.placeholder" :interfaceId="item.interfaceId"
			:total="item.total" :templateJson="item.templateJson" :formData='formData'
			:relationField="item.relationField" :propsValue="item.propsValue" :clearable='item.clearable'
			@change="onChange" />
		<JnpfAreaSelect v-if="config.jnpfKey=='areaSelect'" v-model="value" :placeholder="item.placeholder"
			:level="item.level" :disabled="item.disabled" :multiple="item.multiple" @change="onChange" />
		<JnpfRelationFormAttr v-if="config.jnpfKey=='relationFormAttr'" v-model="value" :showField="item.showField"
			:relationField="item.relationField" :isStorage='item.isStorage' @change="onChange" />
		<!-- 下拉表格 -->
		<JnpfPopupSelect v-if="config.jnpfKey=='popupTableSelect'" v-model="value" :placeholder="item.placeholder"
			:disabled="item.disabled" :interfaceId="item.interfaceId" :formData="formData"
			:templateJson="item.templateJson" :columnOptions="item.columnOptions" :relationField="item.relationField"
			:propsValue="item.propsValue" :hasPage="item.hasPage" :pageSize="item.pageSize"
			:vModel="config.tableName ? item.__vModel__ + '_jnpfTable_' + config.tableName + (config.isSubTable ? '0' : '1') : config.__vModel__"
			:popupTitle="item.popupTitle" :multiple="item.multiple" @change="onChange" />
		<JnpfPopupAttr v-if="config.jnpfKey=='popupAttr'" v-model="value" :showField="item.showField"
			:relationField="item.relationField" :isStorage='item.isStorage' @change="onChange" />
		<!-- 计算公式 -->
		<JnpfCalculate v-if="config.jnpfKey=='calculate'" v-model="value" :expression='item.expression'
			:vModel='item.__vModel__' :config='item.__config__' :formData='formData' :precision="item.precision"
			:isAmountChinese="item.isAmountChinese" :thousands="item.thousands" :roundType="item.roundType" />
		<!-- 签名 -->
		<JnpfSign v-if="config.jnpfKey=='sign'" v-model="value" :disabled="item.disabled" :fieldKey="item.__vModel__"
			@change="onChange" :isInvoke="item.isInvoke" />
		<!-- 签章 -->
		<JnpfSignature v-if="config.jnpfKey=='signature'" v-model="value" :disabled="item.disabled" @change="onChange"
			:ableIds="item.ableIds" />
		<JnpfLocation v-if="config.jnpfKey=='location'" v-model="value" :autoLocation="item.autoLocation"
			:adjustmentScope="item.adjustmentScope" :enableLocationScope="item.enableLocationScope"
			:enableDesktopLocation="item.enableDesktopLocation" :locationScope="item.locationScope"
			:disabled="item.disabled" :clearable='item.clearable' @change="onChange" />
		<JnpfOpenData v-if="isSystem" v-model="value" :type="item.type" :showLevel="item.showLevel" />
		<JnpfInput v-if="config.jnpfKey==='modifyUser'||config.jnpfKey==='modifyTime'" v-model="value"
			placeholder="系统自动生成" disabled />
		<!--start labelwidth=0-->
		<JnpfLink v-if="config.jnpfKey=='link'" :content="item.content" :href="item.href" :target='item.target'
			:textStyle="item.textStyle" @click="onClick" />
		<!-- 富文本 -->
		<JnpfEditor v-if="config.jnpfKey=='editor'" v-model="value" :disabled="item.disabled"
			:placeholder="item.placeholder" />
		<JnpfButton v-if="config.jnpfKey=='button'" :buttonText="item.buttonText" :align="item.align" :type="item.type"
			:disabled="item.disabled" @click="onClick($event)" />
		<JnpfAlert v-if="config.jnpfKey=='alert'" :type="item.type" :title="item.title" :tagIcon='item.tagIcon'
			:showIcon="item.showIcon" :closable="item.closable" :description="item.description"
			:closeText="item.closeText" />
		<!--end  labelwidth=0-->
	</u-form-item>
</template>

<script>
	import childTable from './childTable.vue'
	// #ifdef MP
	import Item from './Item.vue' //兼容小程序
	// #endif
	const systemList = ['createUser', 'createTime', 'currOrganize', 'currDept', 'currPosition', 'billRule']
	const specialList = ['link', 'editor', 'button', 'alert']
	export default {
		name: 'Item',
		inject: ["parameter"],
		emits: ['input', 'clickIcon', 'clickFun', 'collapseChange', 'tab-change'],
		components: {
			childTable,
			// #ifdef MP
			Item
			// #endif
		},
		data() {
			return {
				value: undefined,
				tabCurrent: 0,
				stepCurrent: 0,
				tabKey: +new Date(),
				extraObj: {}
			}
		},
		props: {
			itemData: {
				type: Object,
				required: true
			},
			formConf: {
				type: Object,
				required: true
			},
			formData: {
				type: Object,
				required: true
			},
		},
		computed: {
			item() {
				const item = uni.$u.deepClone(this.itemData)
				this.initI18n(item)
				return item
			},
			config() {
				return this.item.__config__
			},
			isSystem() {
				return systemList.indexOf(this.config.jnpfKey) > -1
			},
			labelWidth() {
				if (specialList.indexOf(this.config.jnpfKey) > -1) return 0
				return this.config.labelWidth ? this.config.labelWidth * 1.5 : undefined
			},
			label() {
				return this.config.showLabel && specialList.indexOf(this.config.jnpfKey) < 0 ? this.config.label : ''
			},
			realLabel() {
				return this.label ? (this.label + (this.formConf.labelSuffix || '')) : ''
			},
			leftIcon() {
				return this.config.tipLabel && this.label && this.config.showLabel ? "question-circle-fill" : ""
			}
		},
		watch: {
			value(val) {
				this.item.__config__.defaultValue = val
				this.$emit('input', this.item)
			}
		},
		created() {
			this.initData()
		},
		mounted() {
			if (this.config.jnpfKey === 'collapse') {
				uni.$on('initCollapse', () => {
					this.$refs.collapseRef && this.$refs.collapseRef.init()
				})
			}
		},
		methods: {
			initI18n(item) {
				const config = item.__config__
				if (item.placeholderI18nCode) item.placeholder = this.$t(item.placeholderI18nCode);
				if (config.tipLabelI18nCode) config.tipLabel = this.$t(config.tipLabelI18nCode)
				if (item.__config__.label && item.__config__.labelI18nCode) item.__config__.label = this.$t(item.__config__
					.labelI18nCode);
				if (item.__config__.tipLabel && item.__config__.tipLabelI18nCode) item.__config__.tipLabel = this.$t(item
					.__config__.tipLabelI18nCode);
				if (['groupTitle', 'divider', 'link', 'text'].includes(config.jnpfKey)) {
					if (item.contentI18nCode) item.content = this.$t(item.contentI18nCode);
					if (item.helpMessageI18nCode) item.helpMessage = this.$t(item.helpMessageI18nCode);
				}
				if (config.jnpfKey === 'button' && item.buttonTextI18nCode) item.buttonText = this.$t(item
					.buttonTextI18nCode);
				if (config.jnpfKey === 'alert') {
					if (item.titleI18nCode) item.title = this.$t(item.titleI18nCode);
					if (item.descriptionI18nCode) item.description = this.$t(item.descriptionI18nCode);
					if (item.closeTextI18nCode) item.closeText = this.$t(item.closeTextI18nCode);
				}
				if (config.jnpfKey === 'card') {
					if (item.headerI18nCode) item.header = this.$t(item.headerI18nCode);
				}
				if (['tab', 'collapse', 'steps'].includes(config.jnpfKey)) {
					if (config.children && config.children.length) {
						for (let i = 0; i < config.children.length; i++) {
							if (config.children[i].titleI18nCode) config.children[i].title =
								this.$t(config.children[i].titleI18nCode);
						}
					}
					if (item.headerI18nCode) item.header = this.$t(item.headerI18nCode);
				}
				if (config.jnpfKey === 'table') {
					if (config.children && config.children.length) {
						for (let i = 0; i < config.children.length; i++) {
							this.initI18n(config.children[i])
						}
					}
				}
			},
			onStepChange(index, item) {
				if (this.stepCurrent === index) return
				item.__config__.active = index
				this.stepCurrent = index
				this.$nextTick(() => {
					uni.$emit('updateCode')
					uni.$emit('initCollapse')
				})
			},
			initData() {
				if (this.config.jnpfKey === 'steps') this.stepCurrent = this.config.active
				if (this.config.jnpfKey != 'tab') return this.value = this.config.defaultValue
				for (var i = 0; i < this.config.children.length; i++) {
					if (this.config.active == this.config.children[i].name) {
						this.tabCurrent = i
						break
					}
				}
			},
			onBlur(val) {
				this.setScriptFunc(val, this.item, 'blur')
			},
			onChange(val, data) {
				this.extraObj = data
				this.$nextTick(() => {
					this.setScriptFunc(data || val, this.item)
				})
				if (['popupSelect', 'relationForm'].includes(this.item.__config__.jnpfKey)) {
					this.setTransferFormData(data || val, this.item.__config__, this.item.__config__.jnpfKey)
				}
				this.$nextTick(() => {
					uni.$emit('subChange', this.item, data || val)
				})
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
			setTransferFormData(data, config, jnpfKey) {
				if (!config.transferList.length) return;
				for (let index = 0; index < config.transferList.length; index++) {
					const element = config.transferList[index];
					this.parameter.setFormData(element.sourceValue, data[element.targetField]);
				}
			},
			onTabChange(index) {
				if (this.tabCurrent === index) return
				this.tabCurrent = index;
				const id = this.item.__config__.children[index].name
				this.$emit('tab-change', this.item, id)
				this.$nextTick(() => {
					uni.$emit('updateCode')
					uni.$emit('initCollapse')
				})
			},
			onChildTabChange(item, id) {
				this.$emit('tab-change', item, id)
			},
			onCollapseChange(data) {
				this.$emit('collapse-change', this.item, data)
				this.$nextTick(() => {
					uni.$emit('initCollapse')
				})
			},
			onChildCollapseChange(item, id) {
				this.$emit('collapse-change', item, id)
			},
			setValue(item, data) {
				this.$emit('input', item, data)
			},
			onClick(e) {
				this.$emit('clickFun', this.item, e || '')
			},
			onChildClick(e, item) {
				this.$emit('clickFun', item, e || '')
			},
			clickIcon(e) {
				this.$emit('clickIcon', e)
			}
		}
	}
</script>
<style lang="scss" scoped>
	.form-item-box {
		padding: 0 20rpx;
	}

	.popup-select {
		::v-deep .u-form-item--left {
			align-items: flex-start !important;
		}
	}
</style>