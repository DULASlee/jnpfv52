<template>
	<view :class="{'item-card':config.jnpfKey==='card'}"
		v-if="!config.noShow && (!config.visibility || (Array.isArray(config.visibility) && config.visibility.includes('app')))">
		<template v-if="config.layout==='colFormItem'">
			<JnpfText v-if="config.jnpfKey=='text'" :content="item.content" :textStyle="item.textStyle" />
			<JnpfDivider v-else-if="config.jnpfKey==='divider'" :content="item.content" />
			<JnpfGroupTitle v-else-if="config.jnpfKey=='groupTitle'" :content="item.content"
				:content-position="item.contentPosition" :helpMessage="item.helpMessage" @groupIcon="clickIcon(item)" />
			<u-form-item v-else-if="config.jnpfKey==='popupSelect' || config.jnpfKey==='relationForm'"
				:label="realLabel" :prop="item.__vModel__" class="popup-select" :label-width="labelWidth"
				:left-icon="leftIcon" :left-icon-style="{'color':'#a8aaaf'}" @clickIcon="clickIcon(item)">
				<view class="detail-text-box" v-if="config.jnpfKey==='popupSelect'">
					<view class="jnpf-detail-text">
						{{formData[item.__vModel__]}}
					</view>
					<DisplayList v-if="Object.keys(extraObj).length" :extraObj="extraObj"
						:extraOptions="item.extraOptions">
					</DisplayList>
				</view>
				<view class="detail-text-box" @click.native="toDetail(item)" v-if="config.jnpfKey==='relationForm'">
					<view class="jnpf-detail-text" style="color:rgb(41, 121, 255)">
						{{formData[item.__vModel__]}}
					</view>
					<DisplayList v-if="Object.keys(extraObj).length" :extraObj="extraObj"
						:extraOptions="item.extraOptions">
					</DisplayList>
				</view>
			</u-form-item>
			<u-form-item v-else :label="realLabel" :prop="item.__vModel__" :label-width="labelWidth"
				:left-icon="leftIcon" :left-icon-style="{'color':'#a8aaaf'}" @clickIcon="clickIcon(item)">
				<!-- #ifndef APP-HARMONY -->
				<JnpfUploadFile v-if="config.jnpfKey=='uploadFile'" v-model="config.defaultValue" detailed />
				<!-- #endif -->
				<view class="preview-image-box" v-if="config.jnpfKey==='uploadImg'">
					<image v-for="(cItem,ci) in item.__config__.defaultValue" :key="ci" class="u-preview-image"
						:src="define.baseURL+cItem.url" mode="aspectFill"
						@tap.stop="doPreviewImage(define.baseURL+cItem.url, item.__config__.defaultValue)"></image>
				</view>
				<JnpfColorPicker v-else-if="config.jnpfKey==='colorPicker'" v-model="config.defaultValue"
					:colorFormat="item.colorFormat" disabled />
				<JnpfRate v-else-if="config.jnpfKey==='rate'" v-model="config.defaultValue" :max="item.count"
					:allowHalf="item.allowHalf" disabled />
				<JnpfEditor v-else-if="config.jnpfKey==='editor'" v-model="config.defaultValue" detailed />
				<JnpfBarcode v-else-if="config.jnpfKey=='barcode'" :staticText="item.staticText" :width="item.width"
					:height="item.height" :format="item.format" :dataType="item.dataType" :lineColor="item.lineColor"
					:background="item.background" :relationField="item.relationField+'_id'" :formData="formData" />
				<JnpfQrcode v-else-if="config.jnpfKey=='qrcode'" :staticText="item.staticText" :width="item.width"
					:dataType="item.dataType" :colorDark="item.colorDark" :colorLight="item.colorLight"
					:relationField="item.relationField+'_id'" :formData="formData" />
				<JnpfInputNumber v-else-if="config.jnpfKey=='inputNumber'" v-model="config.defaultValue"
					:step='item.step' :max='item.max||999999999999999' :min='item.min||-999999999999999'
					:disabled="item.disabled" :placeholder="item.placeholder" :isAmountChinese="item.isAmountChinese"
					:thousands="item.thousands" :addonAfter="item.addonAfter" :addonBefore="item.addonBefore"
					:controls="item.controls" :precision="item.precision" detailed />
				<JnpfCalculate v-else-if="config.jnpfKey==='calculate'&&item.isStorage==0" :expression='item.expression'
					:vModel='item.__vModel__' :config='config' :formData='formData' v-model="config.defaultValue"
					:precision="item.precision" :isAmountChinese="item.isAmountChinese" :thousands="item.thousands"
					:roundType="item.roundType" />
				<!--start labelwidth=0-->
				<JnpfLink v-else-if="config.jnpfKey=='link'" :content="item.content" :href="item.href"
					:target='item.target' :textStyle="item.textStyle" />
				<JnpfAlert v-else-if="config.jnpfKey=='alert'" :type="item.type" :title="item.title"
					:tagIcon='item.tagIcon' :showIcon="item.showIcon" :closable="item.closable"
					:description="item.description" :closeText="item.closeText" />
				<JnpfButton v-else-if="config.jnpfKey=='button'" :buttonText="item.buttonText" :align="item.align"
					:type="item.type" :disabled="item.disabled" />
				<JnpfSlider v-else-if="config.jnpfKey=='slider'" v-model="config.defaultValue" :step="item.step"
					:min="item.min||0" :max="item.max||100" disabled />
				<JnpfSign v-else-if="config.jnpfKey=='sign'" v-model="config.defaultValue" detailed />
				<JnpfSignature v-else-if="config.jnpfKey=='signature'" v-model="config.defaultValue" detailed />
				<JnpfLocation v-else-if="config.jnpfKey=='location'" v-model="config.defaultValue"
					:enableLocationScope="item.enableLocationScope" detailed />
				<!--end  labelwidth=0-->
				<template v-else>
					<view class="jnpf-detail-text" v-if="config.jnpfKey==='calculate'">
						<view>{{ toThousands(config.defaultValue, item) }}</view>
						<view v-if="item.isAmountChinese" style="color: #999;">
							{{jnpf.getAmountChinese(getValue(item))}}
						</view>
					</view>
					<JnpfInput v-else-if="config.jnpfKey=='input'" v-model="config.defaultValue" detailed
						:useMask="item.useMask" :maskConfig="item.maskConfig" :addonBefore="item.addonBefore"
						:addonAfter="item.addonAfter" />
					<view class="jnpf-detail-text" v-else>{{ getValue(item) }}</view>
				</template>
			</u-form-item>
		</template>
		<template v-else>
			<view class="jnpf-card" v-if="config.jnpfKey==='card'||config.jnpfKey==='row'">
				<view class="jnpf-card-cap u-line-1 u-flex" v-if="item.header" @click="clickIcon(item)">
					{{item.header}}
					<u-icon :name="config.tipLabel? 'question-circle-fill':''" class="u-m-l-10" color="#a0acb7" />
				</view>
				<Item v-for="(child, index) in config.children" :key="config.renderKey+index" :itemData="child"
					:formConf="formConf" :formData="formData" @toDetail="toDetail" @clickIcon='clickIcon' />
			</view>
			<template v-if="config.jnpfKey==='table'">
				<view class="jnpf-table">
					<view class="jnpf-table-title u-line-1" @click="clickIcon(item)">
						{{config.label}}
						<u-icon v-if="config.tipLabel" :name="'question-circle-fill'" class="u-m-l-10"
							color="#a0acb7" />
					</view>
					<view v-for="(column,columnIndex) in config.defaultValue" :key="columnIndex">
						<view class="jnpf-table-item-title">
							<view class="jnpf-table-item-title-num">({{columnIndex+1}})</view>
						</view>
						<view class="form-item-box" v-for="(childItem,cIndex) in config.children" :key="cIndex">
							<u-form-item :label="childItem.__config__.showLabel?childItem.__config__.label:''"
								:label-width="childItem.__config__.labelWidth ? childItem.__config__.labelWidth * 1.5 : undefined"
								@clickIcon="clickIcon(childItem)"
								:left-icon='childItem.__config__.tipLabel &&childItem.__config__.showLabel&& childItem.__config__.label? "question-circle-fill":""'
								:left-icon-style="{'color':'#a0acb7'}"
								v-if="!childItem.__config__.noShow&&(!childItem.__config__.visibility|| (Array.isArray(childItem.__config__.visibility) && childItem.__config__.visibility.includes('app')))">
								<template
									v-if="['relationFormAttr','popupAttr'].includes(childItem.__config__.jnpfKey)">
									<view class="jnpf-detail-text" v-if="!childItem.__vModel__">
										{{ column[childItem.relationField.split('_jnpfTable_')[0]+'_'+childItem.showField] }}
									</view>
									<view class="jnpf-detail-text" v-else>
										{{column[childItem.__vModel__]}}
									</view>
								</template>
								<view v-else-if="childItem.__config__.jnpfKey==='relationForm'" class="jnpf-detail-text"
									style="color:rgb(41, 121, 255)"
									@click.native="toTableDetail(childItem,column[childItem.__vModel__+'_id'])">
									{{column[childItem.__vModel__]}}
								</view>
								<JnpfSign v-else-if="childItem.__config__.jnpfKey=='sign'"
									v-model="column[childItem.__vModel__]" detailed />
								<JnpfSignature v-else-if="childItem.__config__.jnpfKey=='signature'"
									v-model="column[childItem.__vModel__]" detailed />
								<JnpfLocation v-else-if="childItem.__config__.jnpfKey=='location'"
									v-model="column[childItem.__vModel__]"
									:enableLocationScope="item.enableLocationScope" detailed />
								<!-- #ifndef APP-HARMONY -->
								<JnpfUploadFile v-else-if="childItem.__config__.jnpfKey==='uploadFile'"
									v-model="column[childItem.__vModel__]" detailed />
								<!-- #endif -->
								<JnpfUploadImg v-else-if="childItem.__config__.jnpfKey==='uploadImg'"
									v-model="column[childItem.__vModel__]" detailed />
								<JnpfInputNumber v-else-if="childItem.__config__.jnpfKey=='inputNumber'"
									v-model="column[childItem.__vModel__]" :step='childItem.step' :max='childItem.max'
									:min='childItem.min' :disabled="childItem.disabled"
									:placeholder="childItem.placeholder" :isAmountChinese="childItem.isAmountChinese"
									:thousands="childItem.thousands" :addonAfter="childItem.addonAfter"
									:addonBefore="childItem.addonBefore" :controls="childItem.controls"
									:precision="childItem.precision" detailed />
								<JnpfCalculate
									v-else-if="childItem.__config__.jnpfKey==='calculate'&&childItem.isStorage==0"
									:expression='childItem.expression' :vModel='childItem.__vModel__'
									:config='childItem.__config__' :formData='formData' :roundType="childItem.roundType"
									v-model="column[childItem.__vModel__]" :precision="childItem.precision"
									:isAmountChinese="childItem.isAmountChinese" :thousands="childItem.thousands" />
								<JnpfRate v-else-if="childItem.__config__.jnpfKey==='rate'" :max="childItem.count"
									v-model="column[childItem.__vModel__]" :allowHalf="childItem.allowHalf" disabled />
								<JnpfSlider v-else-if="childItem.__config__.jnpfKey=='slider'"
									v-model="column[childItem.__vModel__]" :step="childItem.step"
									:min="childItem.min||0" :max="childItem.max||100" disabled />
								<template v-else>
									<view class="jnpf-detail-text" v-if="childItem.__config__.jnpfKey==='calculate'">
										<view>{{toThousands(column[childItem.__vModel__],childItem)}}</view>
										<view v-if="childItem.isAmountChinese" style="color: #999;">
											{{jnpf.getAmountChinese(column[childItem.__vModel__])}}
										</view>
									</view>
									<JnpfInput v-else-if="childItem.__config__.jnpfKey=='input'"
										v-model="column[childItem.__vModel__]" detailed :useMask="childItem.useMask"
										:maskConfig="childItem.maskConfig" :addonBefore="childItem.addonBefore"
										:addonAfter="childItem.addonAfter" />
									<view class="jnpf-detail-text" v-else>{{column[childItem.__vModel__]}}</view>
								</template>
							</u-form-item>
						</view>
					</view>
					<view class="jnpf-table-item" v-if="item.showSummary && summaryField.length">
						<view class="jnpf-table-item-title u-flex u-row-between">
							<text class="jnpf-table-item-title-num">{{item.__config__.label}}合计</text>
						</view>
						<view class=" u-p-l-20 u-p-r-20 form-item-box">
							<u-form-item v-for="(item,index) in summaryField" :label="item.__config__.label"
								:key="item.__vModel__">
								<u-input input-align='right' v-model="item.value" disabled />
							</u-form-item>
						</view>
					</view>
				</view>
			</template>
			<view v-else-if="config.jnpfKey==='steps'" style="background-color: #fff;padding:15px 0">
				<view class="step-container">
					<u-steps :list="config.children" name="title" :mode="item.simple ? 'dot' :'number'"
						@change="onStepChange($event,item)" :current="stepCurrent">
					</u-steps>
				</view>
				<view v-for="(itemSub,i) in config.children" :key='i'>
					<view v-if="i === stepCurrent">
						<Item v-for="(childItem, childIndex) in itemSub.__config__.children" :key="childIndex"
							:itemData="childItem" :formConf="formConf" :formData="formData" @toDetail="toDetail"
							@clickIcon='clickIcon' />
					</view>
				</view>
			</view>
			<view class="jnpf-tab" v-if="config.jnpfKey==='tab'">
				<u-tabs is-scroll :list="config.children" name="title" v-model="tabCurrent" @change="onTabChange" />
				<view v-for="(pane,i) in config.children" :key='i'>
					<view v-show="i == tabCurrent">
						<Item v-for="(childItem, childIndex) in pane.__config__.children" :key="childIndex"
							:itemData="childItem" :formConf="formConf" :formData="formData" @toDetail="toDetail"
							@clickIcon='clickIcon' />
					</view>
				</view>
			</view>
			<template v-if="config.jnpfKey==='collapse'">
				<u-collapse :head-style="{'padding-left':'20rpx'}" :accordion="item.accordion" ref="collapseRef">
					<u-collapse-item :title="pane.title" v-for="(pane, i) in config.children" :key="i"
						:open="config.active.indexOf(pane.name)>-1">
						<Item v-for="(child, j) in pane.__config__.children" :key="child.__config__.renderKey"
							:itemData="child" :formConf="formConf" :formData="formData" @toDetail="toDetail"
							@clickIcon='clickIcon' />
					</u-collapse-item>
				</u-collapse>
			</template>
		</template>
	</view>
</template>
<script>
	import {
		getRelationFormDetail,
		getDataInterfaceDataInfoByIds
	} from '@/api/common.js'
	// #ifdef MP
	import Item from './Item.vue' //兼容小程序
	// #endif
	import DisplayList from '@/components/displayList'
	const specialList = ['link', 'editor', 'button', 'alert']
	export default {
		name: 'Item',
		components: {
			// #ifdef MP
			Item,
			// #endif
			DisplayList
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
		data() {
			return {
				tabCurrent: 0,
				tableData: [],
				summaryField: [],
				stepCurrent: 0,
				extraObj: {}
			}
		},
		created() {
			this.handleSummary()
			this.handleTab()
		},
		mounted() {
			if (this.config.jnpfKey === 'collapse') {
				this.$refs.collapseRef && this.$refs.collapseRef.init()
			}
			uni.$on('initCollapse', () => {
				this.$refs.collapseRef && this.$refs.collapseRef.init()
			})
			this.getDataChange()
			this.getDataInterfaceDataInfoByIds()
		},
		methods: {
			onStepChange(index, item) {
				if (this.stepCurrent === index) return
				item.__config__.active = index
				this.stepCurrent = index
				this.$nextTick(() => {
					uni.$emit('updateCode')
					uni.$emit('initCollapse')
				})
			},
			initI18n(item) {
				const config = item.__config__
				if (item.placeholderI18nCode) {
					//#ifdef MP-WEIXIN
					item.placeholder = this.$t(item.placeholderI18nCode);
					//#endif
					//#ifndef MP-WEIXIN
					item.placeholder = this.$t(item.placeholderI18nCode, item.placeholder);
					//#endif
				}
				if (item.__config__.label && item.__config__.labelI18nCode) {
					//#ifdef MP-WEIXIN
					item.__config__.label = this.$t(item.__config__.labelI18nCode);
					//#endif
					//#ifndef MP-WEIXIN
					item.__config__.label = this.$t(item.__config__.labelI18nCode, item.__config__.label);
					//#endif
				}
				if (item.__config__.tipLabel && item.__config__.tipLabelI18nCode) {
					//#ifdef MP-WEIXIN
					item.__config__.tipLabel = this.$t(item.__config__.tipLabelI18nCode);
					//#endif
					//#ifndef MP-WEIXIN
					item.__config__.tipLabel = this.$t(item.__config__.tipLabelI18nCode, item.__config__.tipLabel);
					//#endif
				}
				if (['groupTitle', 'divider', 'link', 'text'].includes(config.jnpfKey)) {
					if (item.contentI18nCode) {
						//#ifdef MP-WEIXIN
						item.content = this.$t(item.contentI18nCode);
						//#endif
						//#ifndef MP-WEIXIN
						item.content = this.$t(item.contentI18nCode, item.content);
						//#endif
					}
					if (item.helpMessageI18nCode) {
						//#ifdef MP-WEIXIN
						item.helpMessage = this.$t(item.helpMessageI18nCode);
						//#endif
						//#ifndef MP-WEIXIN
						item.helpMessage = this.$t(item.helpMessageI18nCode, item.helpMessage);
						//#endif
					}
				}
				if (config.jnpfKey === 'button') {
					if (item.buttonTextI18nCode) {
						//#ifdef MP-WEIXIN
						item.buttonText = this.$t(item.buttonTextI18nCode);
						//#endif
						//#ifndef MP-WEIXIN
						item.buttonText = this.$t(item.buttonTextI18nCode.item.buttonText);
						//#endif
					}
				}
				if (config.jnpfKey === 'alert') {
					if (item.titleI18nCode) {
						//#ifdef MP-WEIXIN
						item.title = this.$t(item.titleI18nCode);
						//#endif
						//#ifndef MP-WEIXIN
						item.title = this.$t(item.titleI18nCode, item.title);
						//#endif
					}
					if (item.descriptionI18nCode) {
						//#ifdef MP-WEIXIN
						item.description = this.$t(item.descriptionI18nCode);
						//#endif
						//#ifndef MP-WEIXIN
						item.description = this.$t(item.descriptionI18nCode, item.description);
						//#endif
					}
					if (item.closeTextI18nCode) {
						//#ifdef MP-WEIXIN
						item.closeText = this.$t(item.closeTextI18nCode);
						//#endif
						//#ifndef MP-WEIXIN
						item.closeText = this.$t(item.closeTextI18nCode, item.closeText);
						//#endif
					}
				}
				if (config.jnpfKey === 'card') {
					if (item.headerI18nCode) {
						//#ifdef MP-WEIXIN
						item.header = this.$t(item.headerI18nCode);
						//#endif
						//#ifndef MP-WEIXIN
						item.header = this.$t(item.headerI18nCode, item.header);
						//#endif
					}
				}
				if (['tab', 'collapse', 'steps'].includes(config.jnpfKey)) {
					if (config.children && config.children.length) {
						for (let i = 0; i < config.children.length; i++) {
							if (config.children[i].titleI18nCode) {
								//#ifdef MP-WEIXIN
								config.children[i].title =
									this.$t(config.children[i].titleI18nCode);
								//#endif
								//#ifndef MP-WEIXIN
								config.children[i].title =
									this.$t(config.children[i].titleI18nCode, config.children[i].title);
								//#endif
							}
						}
					}
					if (item.headerI18nCode) {
						//#ifdef MP-WEIXIN
						item.header = this.$t(item.headerI18nCode);
						//#endif
						//#ifndef MP-WEIXIN
						item.header = this.$t(item.headerI18nCode, item.header);
						//#endif
					}
				}
				if (config.jnpfKey === 'table') {
					if (config.children && config.children.length) {
						for (let i = 0; i < config.children.length; i++) {
							this.initI18n(config.children[i])
						}
					}
				}
			},
			handleTab() {
				if (this.config.jnpfKey === 'steps') return this.stepCurrent = this.config.active
				if (this.config.jnpfKey !== 'tab') return
				for (var i = 0; i < this.config.children.length; i++) {
					if (this.config.active == this.config.children[i].name) {
						this.tabCurrent = i
						break
					}
				}
			},
			getDataChange() {
				if (this.config.jnpfKey === 'relationForm' && this.config.defaultValue) {
					let query = {
						id: this.formData[this.item.__vModel__ + '_id'],
					};
					if (this.item.propsValue) query = {
						...query,
						propsValue: this.item.propsValue
					};
					getRelationFormDetail(this.item.modelId, query).then(res => {
						if ((!res.data || !res.data.data) || res.data.data === "undefined") return
						let data = JSON.parse(res.data?.data)
						this.extraObj = data
					})
				}
			},
			getDataInterfaceDataInfoByIds() {
				if (this.config.jnpfKey === 'popupSelect' && this.config.defaultValue) {
					let query = {
						ids: [this.config.defaultValue],
						interfaceId: this.item.interfaceId,
						propsValue: this.item.propsValue,
						relationField: this.item.relationField,
						paramList: this.getParamList()
					}
					getDataInterfaceDataInfoByIds(this.item.interfaceId, query).then(res => {
						const data = res.data && res.data.length ? res.data[0] : {};
						this.extraObj = data
					})
				}
			},
			getParamList() {
				let templateJson = this.item.templateJson
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
			handleSummary() {
				if (this.item.__config__.jnpfKey !== 'table') return
				const val = this.item.__config__.defaultValue
				let summaryField = this.item.summaryField || []
				this.summaryField = []
				this.tableData = this.item.__config__.children || []
				for (let i = 0; i < summaryField.length; i++) {
					for (let o = 0; o < this.tableData.length; o++) {
						const item = this.tableData[o]
						if (this.tableData[o].__vModel__ === summaryField[i] && !item.__config__.noShow) {
							this.summaryField.push({
								value: '',
								...item
							})
						}
					}
				}
				this.$nextTick(() => this.getTableSummaries(val, this.item))
			},
			toThousands(val, column) {
				if (val) {
					let valList = val.toString().split('.')
					let num = Number(valList[0])
					let newVal = column.thousands ? num.toLocaleString() : num
					return valList[1] ? newVal + '.' + valList[1] : newVal
				} else {
					return val
				}
			},
			getTableSummaries(newVal, config) {
				for (let i = 0; i < this.summaryField.length; i++) {
					let val = 0
					for (let j = 0; j < newVal.length; j++) {
						if (newVal[j][this.summaryField[i].__vModel__]) {
							let data = isNaN(newVal[j][this.summaryField[i].__vModel__]) ? 0 :
								Number(newVal[j][this.summaryField[i].__vModel__])
							val += data
						}
					}
					let realVal = val && !Number.isInteger(val) ? Number(val).toFixed(2) : val;
					if (this.summaryField[i].thousands) realVal = Number(realVal).toLocaleString('zh')
					this.summaryField[i].value = realVal
				}
			},
			clickIcon(e) {
				this.$emit('clickIcon', e)
			},
			onTabChange(index) {
				if (this.tabCurrent === index) return
				this.tabCurrent = index;
				this.$emit('tab-change', this.item, index)
				this.$nextTick(() => {
					uni.$emit('initCollapse')
					uni.$emit('updateCode')
				})
			},
			doPreviewImage(current, imageList) {
				const images = imageList.map(item => this.define.baseURL + item.url);
				uni.previewImage({
					urls: images,
					current: current,
					success: () => {},
					fail: () => {
						uni.showToast({
							title: '预览图片失败',
							icon: 'none'
						});
					}
				});
			},
			toDetail(item) {
				const data = {
					...item,
					...(item.__config__.jnpfKey === 'relationForm' ? {
						sourceRelationForm: true,
						propsValue: item.propsValue
					} : {})
				};
				this.$emit('toDetail', data)
			},
			toTableDetail(item, value) {
				item.__config__.defaultValue = value
				this.$emit('toDetail', item)
			},
			getValue(item) {
				if (Array.isArray(item.__config__.defaultValue)) {
					if (['timeRange', 'dateRange'].includes(item.__config__.jnpfKey)) {
						return item.__config__.defaultValue.join('')
					}
					return item.__config__.defaultValue.join()
				}
				return item.__config__.defaultValue
			},
		}
	}
</script>
<style lang="scss">
	.detail-text-box {
		width: 100%;
	}
</style>