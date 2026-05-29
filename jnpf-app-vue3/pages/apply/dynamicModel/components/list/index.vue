<template>
	<view class="dynamicModel-list-v">
		<!-- 批量删除顶部弹窗 -->
		<view class="u-flex top-btn" :class="slide2" v-show="showTop">
			<view class="button-left" @click.stop="cancel">
				<p class="u-m-t-10 u-font-28">{{$t('common.cancelText')}}</p>
			</view>
			<view class="button-center">
				<p class="u-m-t-10 u-font-28">已选中{{selectItems.length}}条</p>
			</view>
			<view class="button-right u-m-t-12" @click.stop="checkAll">
				<p class="icon-ym icon-ym-app-checkAll " :style="{'color':this.checkedAll ? '#0293fc' : '#303133'}">
				</p>
			</view>
		</view>
		<!-- 排序 -->
		<view class="head-warp com-dropdown">
			<u-dropdown class="u-dropdown" ref="uDropdown">
				<u-dropdown-item :title="$t('app.apply.sort')" :options="sortOptions">
					<view class="screen-box">
						<view class="screen-list" v-if="sortOptions.length">
							<view class="u-p-l-20 u-p-r-20 list">
								<scroll-view scroll-y="true" style="height: 100%;">
									<u-cell-group :border="false">
										<u-cell-item @click="cellClick(item)" :arrow="false" :title="item.label"
											v-for="(item, index) in sortOptions" :key="index" :title-style="{
										color: sortValue.includes(item.value) ? '#2979ff' : '#606266' }">
											<u-icon v-if="sortValue.includes(item.value)" name="checkbox-mark"
												color="#2979ff" size="32" />
										</u-cell-item>
									</u-cell-group>
								</scroll-view>
							</view>
						</view>
						<view v-else class="notData-box u-flex-col">
							<view class="u-flex-col notData-inner">
								<image :src="icon" class="iconImg"></image>
								<text class="notData-inner-text">{{$t('common.noData')}}</text>
							</view>
						</view>
						<view class="buttom-actions" v-if="sortOptions.length">
							<u-button class="buttom-btn" @click="handleSortReset">{{$t('common.cleanText')}}</u-button>
							<u-button class="buttom-btn" type="primary" @click="handleSortSearch">
								{{$t('common.okText')}}
							</u-button>
						</view>
					</view>
				</u-dropdown-item>
				<!-- 筛选 -->
				<u-dropdown-item :title="$t('app.apply.screen')">
					<view class="screen-box u-flex-col">
						<view class="screen-list" v-if="showParser && searchFormConf.length">
							<view class="u-p-l-20 u-p-r-20 list">
								<scroll-view scroll-y="true" style="height: 100%;">
									<Parser :formConf="searchFormConf" :searchFormData="searchFormData"
										:webType="config.webType" ref="searchForm" @submit="sumbitSearchForm" />
								</scroll-view>
							</view>
							<view class="u-flex screen-btn" v-if="showParser && searchFormConf.length">
								<text @click="handleReset" class="btn btn1">{{$t('common.resetText')}}</text>
								<text @click="handleSearch" class="btn btn2">{{$t('common.searchText')}}</text>
							</view>
						</view>
						<view v-else class="notData-box u-flex-col">
							<view class="u-flex-col notData-inner">
								<image :src="icon" class="iconImg"></image>
								<text class="notData-inner-text">{{$t('common.noData')}}</text>
							</view>
						</view>
					</view>
				</u-dropdown-item>
			</u-dropdown>
		</view>
		<u-tabs :list="tabList" v-model="tabActiveKey" font-size="28" @change="onTabChange" height="80" name="fullName"
			v-show="showTabs">
		</u-tabs>
		<!-- 列表 -->
		<view class="list-warp">
			<mescroll-uni ref="mescrollRef" @init="mescrollInit" @down="downCallback" @up="upCallback"
				:down="downOption" :up="upOption" :bottombar="false"
				:top="(columnData.tabConfig && columnData.tabConfig.on && tabList.length) ? 170 : 80">
				<list ref="list" :list="list" :columnList="columnList" :config="config" :actionOptions="actionOptions"
					@relationFormClick="relationFormClick" @goDetail="goDetail" @handleMoreClick="handleMoreClick"
					@handleClick="handleClick" :showSelect="showTop" :checkedAll="checkedAll"
					@selectCheckbox="selectCheckbox" :isMoreBtn="isMoreBtn" :customBtnsList="columnData.customBtnsList">
				</list>
			</mescroll-uni>
		</view>
		<view v-if="!showTop">
			<!-- 新增按钮 -->
			<view v-if="config.webType !=4">
				<view class="com-addBtn"
					v-if="isPreview||(permission.btnPermission && permission.btnPermission.includes('btn_add'))"
					@click="addPage()">
					<u-icon name="plus" size="48" color="#fff" />
				</view>
			</view>
			<!-- 批量操作 -->
			<view class="com-batch" @click="openBatchOperate()" v-if="showBatchOperate">
				<u-icon name="grid" size="48" color="#848484" />
			</view>
		</view>
		<u-select :list="listInnerBtn" v-model="showMoreBtn" @confirm="selectBtnconfirm" />
		<u-select :list="bottomCustomBtnsList[1]" v-model="showBottomMoreBtn" @confirm="bottomBtnConfirm" />
		<!-- 批量操作底部弹窗 -->
		<view class="u-flex bottom-btn" :class="slide" v-show="selectItems.length">
			<view class="button-preIcon" @click.stop="handleBottomMoreClick('down')"
				v-if="bottomCustomBtnsList[1].length">
				<u-icon name="more-dot-fill" class="u-m-b-8" size="34"></u-icon>
				<p class="u-font-24">{{$t('common.moreText')}}</p>
			</view>
			<!-- 自定义按钮 -->
			<view class="button-preIcon" v-for="(item,i) in bottomCustomBtnsList[0]" :key="i"
				@click="bottomBtnConfirm(item)">
				<p class="btn-icon u-m-b-8" :class="item.event.btnIcon">
				</p>
				<p class="u-m-t-10 u-font-24 u-line-1">{{item.label}}</p>
			</view>
			<!-- 删除 -->
			<view class="button-preIcon" @click.stop="batchDelete" v-if="isBatchRemove">
				<p class="icon-ym icon-ym-app-delete u-m-b-8"></p>
				<p class="u-m-t-10 u-font-24">{{$t('common.delText')}}</p>
			</view>
		</view>
	</view>
</template>
<script>
	import {
		useBaseStore
	} from '@/store/modules/base'
	const baseStore = useBaseStore()
	import list from './list.vue'
	import resources from '@/libs/resources.js'
	import MescrollMixin from "@/uni_modules/mescroll-uni/components/mescroll-uni/mescroll-mixins.js";
	import bulkOperationMixin from "../../bulkOperationMixin.js";
	import Parser from '../parser/index.vue'
	import {
		getModelList,
		deteleModel,
		getModelInfo,
		launchFlow
	} from '@/api/apply/visualDev'
	import {
		getDataInterfaceRes
	} from '@/api/common'
	import deepClone from '../../../../../uni_modules/vk-uview-ui/libs/function/deepClone';
	export default {
		mixins: [MescrollMixin, bulkOperationMixin],
		props: ['config', 'modelId', 'isPreview', 'title', 'menuId'],
		components: {
			Parser,
			list
		},
		data() {
			return {
				tabActiveKey: 0,
				tabList: [],
				tabQueryJson: {},
				sortValue: [],
				icon: resources.message.nodata,
				downOption: {
					use: true,
					auto: false
				},
				upOption: {
					page: {
						num: 0,
						size: 10,
						time: null
					},
					empty: {
						use: true,
						icon: resources.message.nodata,
						tip: this.$t('common.noData'),
						fixed: true
					},
					textNoMore: this.$t('app.apply.noMoreData'),
				},
				list: [],
				listQuery: {
					sidx: '',
					keyword: '',
					queryJson: ''
				},
				actionOptions: [],
				showParser: false,
				columnData: {},
				columnList: [],
				sortList: [],
				sortOptions: [],
				searchList: [],
				searchFormConf: [],
				permission: {},
				selectListIndex: 0,
				showBottomMoreBtn: false,
				showMoreBtn: false,
				properties: {},
				flowId: '',
				key: +new Date(),
				userInfo: {},
				searchFormData: {},
				enableFunc: {},
				selectItems: [],
				listInnerBtn: [],
				listTopBtn: []
			}
		},
		created() {
			this.init()
		},
		computed: {
			showBatchOperate() {
				return this.list.length && (this.isBatchRemove || this.listTopBtn.length)
			},
			isBatchRemove() {
				return this.columnData.btnsList.find(item => item.value === "batchRemove" && item.show)
			},
			showTabs() {
				return this.columnData?.tabConfig?.on && this.tabList.length
			},
			/* 底部自定义按钮 */
			bottomCustomBtnsList() {
				if (this.listTopBtn.length <= 3) return [this.listTopBtn, []];
				const firstArray = this.listTopBtn.slice(0, 3);
				const secondArray = this.listTopBtn.slice(3);
				return [firstArray, secondArray];
			},
			getRowKey() {
				return this.config.webType == 4 && this.columnData.viewKey ? this.columnData.viewKey : 'id'
			},
			isMoreBtn() {
				return this.columnData?.customBtnsList?.some(item => item.event?.btnType === 2);
			}
		},
		methods: {
			selectCheckbox(data) {
				this.selectItems = data
			},
			init() {
				this.userInfo = uni.getStorageSync('userInfo') || {};
				this.properties = this.config.flowTemplateJson ? JSON.parse(this.config.flowTemplateJson).properties : {};
				let columnDataStr = this.config?.appColumnData || '[]';
				try {
					this.columnData = JSON.parse(columnDataStr);
				} catch (e) {
					this.columnData = [];
				}
				this.permission = this.$permission.getPermission(this.columnData, this.menuId, this.jnpf.getScriptFunc);
				this.enableFunc = this.permission.enableFunc;
				this.upOption.page.size = this.columnData.hasPage ? this.columnData.pageSize : 1000000;
				this.setDefaultQuery();
				this.columnList = this.permission.columnPermission || [];
				this.columnData.customBtnsList = this.permission.customBtnsPermission || [];
				this.columnData.customBtnsList.map((o) => {
					if (o.labelI18nCode) o.label = this.$t(o.labelI18nCode)
				})
				this.setBtns()
				this.columnList = this.transformColumnList(this.columnList)
				this.columnList.map((o) => {
					if (o.labelI18nCode) o.label = this.$t(o.labelI18nCode)
					// if (o.jnpfKey != 'table' && o.label.length > 4) o.label = o.label.substring(0, 4)
				})
				this.sortList = this.columnList.filter(o => o.sortable)
				this.handleSearchList()
				this.handleSortList()
				this.handleDeleteBtn()
				this.key = +new Date()
			},
			setBtns() {
				const buttonsByPosition = this.columnData.customBtnsList.reduce((accumulator, item) => {
					if (item.event.position === 2) {
						accumulator.top.push(item);
					} else {
						accumulator.inner.push(item);
					}
					return accumulator;
				}, {
					inner: [],
					top: []
				});
				this.listInnerBtn = buttonsByPosition.inner;
				this.listTopBtn = buttonsByPosition.top;
			},
			upCallback(page) {
				if (this.isPreview == '1') return this.mescroll.endSuccess(0, false);
				const query = {
					currentPage: page.num,
					pageSize: page.size,
					menuId: this.menuId,
					modelId: this.modelId,
					...this.listQuery
				}
				getModelList(this.modelId, query, {
					load: page.num == 1
				}).then(res => {
					this.selectItems = []
					this.$nextTick(() => {
						this.$refs.list.handleCheckAll()
					})
					this.showParser = true
					if (page.num == 1) this.list = [];
					this.mescroll.endSuccess(res.data.list.length);
					const list = res.data.list.map((o, i) => ({
						checked: false,
						index: i,
						...o
					}));
					this.list = this.list.concat(list);
					this.$nextTick(() => {
						if (this.columnData.funcs && this.columnData.funcs.afterOnload) this
							.setTableLoadFunc()
					})
					if (!this.selectItems.length || !this.list.length) this.cancel()
				}).catch((err) => {
					this.mescroll.endByPage(0, 0);
					this.mescroll.endErr();
				})
			},
			//获取标签面板数据、设置标签面板默认值
			async getTabList() {
				this.tabList = [];
				if (!this.columnData.tabConfig) return;
				const list = this.columnData.columnOptions.filter(o => o.__vModel__ == this.columnData.tabConfig
					.relationField) || [];
				if (list?.length) {
					this.columnData.tabConfig?.hasAllTab && this.tabList.push({
						fullName: '全部',
						id: undefined
					});
					if (list[0].__config__.dataType == 'dictionary' && list[0].__config__.dictionaryType) {
						const data = await baseStore.getDicDataSelector(list[0].__config__.dictionaryType) || [];
						const options = list[0].props.value == 'enCode' ? data.map(o => ({
							...o,
							id: o.enCode
						})) : data;
						this.tabList = [...this.tabList, ...options];
					} else {
						this.tabList = [...this.tabList, ...list[0].options];
					}
				}
				this.tabActiveKey = 0;
				this.onTabChange(this.tabActiveKey)
			},
			onTabChange(val) {
				this.tabActiveKey = val
				this.tabQueryJson = {}
				if (this.columnData.tabConfig.hasAllTab) {
					if (val != 0) {
						this.tabQueryJson = {
							[this.columnData.tabConfig.relationField]: this.tabList[val].id
						};
					}
				} else {
					this.tabQueryJson = {
						[this.columnData.tabConfig.relationField]: this.tabList[val].id
					};
				}
				let search = this.$refs.searchForm && this.$refs.searchForm.allCondition()
				this.listQuery.queryJson = JSON.stringify({
					...search,
					...this.tabQueryJson
				})
				this.initData();
			},
			handleSearchForm(data) {
				let newData = {};
				for (let key in data) {
					if (data.hasOwnProperty(key)) {
						if (typeof data[key] === 'object' && data[key] !== null) {
							for (let innerKey in data[key]) {
								if (data[key].hasOwnProperty(innerKey)) {
									let newKey = `${key}-${innerKey}`;
									newData[newKey] = data[key][innerKey];
								}
							}
						} else {
							newData[key] = data[key];
						}
					}
				}
				return newData
			},
			sumbitSearchForm(data) {
				let queryJson = data || {}
				this.searchFormData = data
				// 标签面板查询
				if (this.columnData.tabConfig && this.columnData.tabConfig.on) {
					this.tabQueryJson = {
						[this.columnData.tabConfig.relationField]: this.tabList[this.tabActiveKey]?.id
					};
					queryJson = {
						...queryJson,
						...this.tabQueryJson
					}
				}
				this.listQuery.queryJson = JSON.stringify(queryJson) !== '{}' ? JSON.stringify(queryJson) : ''
				this.$refs.uDropdown.close();
				this.$nextTick(() => {
					this.list = [];
					this.mescroll.resetUpScroll();
				})
			},
			// 处理启用规则
			customEnableRule(data, funcName) {
				// #ifdef MP-WEIXIN
				return true
				// #endif
				// #ifndef MP-WEIXIN
				let func = this.enableFunc[funcName]
				if (!func) return false
				let res = func.call(this, {
					row: data,
					rowIndex: data.index,
					onlineUtils: this.jnpf.onlineUtils,
				})
				return res
				// #endif
			},
			handleDeleteBtn() {
				if (this.config.webType == 4) return
				const actionOptions = this.columnData.columnBtnsList.filter(o => o.value == 'remove' && o.show)
				this.actionOptions = actionOptions.map(o => ({
					...o,
					//#ifdef MP-WEIXIN
					text: o.labelI18nCode ? this.$t(o.labelI18nCode) : o.label,
					//#endif
					//#ifndef MP-WEIXIN
					text: o.labelI18nCode ? this.$t(o.labelI18nCode, o.label) : o.label,
					//#endif
					style: {
						backgroundColor: '#dd524d'
					}
				}))
			},
			handleSearchList() {
				this.searchList = (this.$u.deepClone(this.columnData.searchList) || []).filter(o => !o.noShow)
				for (let i = 0; i < this.searchList.length; i++) {
					const item = this.searchList[i]
					if (item.labelI18nCode) {
						item.label = this.$t(item.labelI18nCode)
						item.placeholder = this.$t(item.labelI18nCode)
					}
					const config = item.__config__
					if (item.value != null && item.value != '' && item.value != []) {
						this.searchFormData[item.id] = item.value;
					}
					if (this.config.webType == 4) config.label = item.label
				}
				if (Object.keys(this.searchFormData).length) this.listQuery.queryJson = JSON.stringify(this.searchFormData)
				if (this.searchList.some(o => o.isKeyword)) {
					const keywordItem = {
						id: 'jnpfKeyword',
						fullName: '关键词',
						prop: 'jnpfKeyword',
						label: this.$t('common.keyword'),
						jnpfKey: 'input',
						clearable: true,
						placeholder: '请输入',
						value: undefined,
						__config__: {
							jnpfKey: 'input'
						},
					};
					this.searchList.unshift(keywordItem);
				}
				this.searchFormConf = this.$u.deepClone(this.searchList)
			},
			handleSortList() {
				this.sortOptions = [];
				const sortList = this.sortList
				for (let i = 0; i < sortList.length; i++) {
					let ascItem = {
						label: sortList[i].label + ' ' + this.$t('app.apply.ascendingOrder'),
						value: sortList[i].prop,
						sidx: sortList[i].prop,
						sort: 'asc'
					}
					let descItem = {
						label: sortList[i].label + ' ' + this.$t('app.apply.descendingOrder'),
						value: '-' + sortList[i].prop,
						sidx: sortList[i].prop,
						sort: 'desc'
					}
					this.sortOptions.push(ascItem, descItem)
				}
			},
			transformColumnList(columnList) {
				let list = []
				for (let i = 0; i < columnList.length; i++) {
					const e = columnList[i];
					if (!e.prop.includes('-')) {
						e.option = null
						list.push(e)
					} else {
						let prop = e.prop.split('-')[0]
						let vModel = e.prop.split('-')[1]
						let label = e.label.split('-')[0]
						let childLabel = e.label.replace(label + '-', '');
						if (e.fullNameI18nCode && Array.isArray(e.fullNameI18nCode) && e.fullNameI18nCode[0]) {
							label = this.$t(e.fullNameI18nCode[0], label);
						}
						let newItem = {
							align: "center",
							jnpfKey: "table",
							prop,
							label,
							children: []
						}
						e.vModel = vModel
						e.childLabel = e.labelI18nCode ? this.$t(e.labelI18nCode) : childLabel;
						if (!list.some(o => o.prop === prop)) list.push(newItem)
						for (let i = 0; i < list.length; i++) {
							if (list[i].prop === prop) {
								e.option = null
								list[i].children.push(e)
								break
							}
						}
					}
				}
				return list
			},
			setDefaultQuery() {
				const defaultSortConfig = (this.columnData.defaultSortConfig || []).map(o =>
					(o.sort === 'desc' ? '-' : '') + o.field);
				this.listQuery.sidx = defaultSortConfig.join(',')
			},
			setTableLoadFunc() {
				const JNPFTable = this.$refs.tableRef
				const parameter = {
					data: this.list,
					tableRef: JNPFTable,
					onlineUtils: this.jnpf.onlineUtils,
				}
				const func = this.jnpf.getScriptFunc.call(this, this.columnData.funcs.afterOnload)
				if (!func) return
				func.call(this, parameter)
			},
			//删除操作
			handleClick(index) {
				const item = this.list[index]
				if (!this.permission.btnPermission.includes('btn_remove')) return this.$u.toast("未开启删除权限")
				if (!this.customEnableRule(item, 'remove')) return this.$u.toast("没有删除权限")
				let txt = '流程处于暂停状态,不可操作'
				if ([1, 2, 3, 4, 6, 7, 8].includes(item.flowState)) txt = '流程已受理,无法删除'
				uni.showModal({
					title: '提示',
					content: '删除后数据无法恢复',
					success: (res) => {
						if (res.confirm) {
							if (this.config.enableFlow == 1 && ![0, 9].includes(item.flowState)) {
								this.$u.toast(txt)
								return
							}
							let data = {
								flowId: this.config.flowId,
								ids: [item.id]
							}
							deteleModel(data, this.modelId).then(res => {
								this.$u.toast(res.msg)
								this.list.splice(index, 1)
								this.mescroll.resetUpScroll()
							})
						}
					}
				})
			},
			//底部更多按钮
			handleBottomMoreClick(type) {
				this.showBottomMoreBtn = true
			},
			//更多按钮弹窗
			handleMoreClick(index) {
				this.selectListIndex = index
				this.showMoreBtn = true
			},
			//底部按钮操作
			bottomBtnConfirm(e) {
				if (Array.isArray(e) && e.length) {
					const index = this.bottomCustomBtnsList[1].findIndex(item => item.value === e[0].value);
					const item = this.bottomCustomBtnsList[1][index];
					if (item.event && item.event.btnType === 3) this.handleBottomBtnInterface(item.event);
					if (item.event.btnType == 2) this.handleScriptFunc(item.event, this.selectItems)
					if (item.event.btnType == 4) this.handleLaunchFlow(item, this.selectItems)
				} else {
					// 当e是一个对象且包含event属性时
					if (e.event.btnType == 2) this.handleScriptFunc(e.event, this.selectItems)
					if (e.event.btnType === 3) this.handleBottomBtnInterface(e.event);
					if (e.event.btnType == 4) this.handleLaunchFlow(e, this.selectItems)
				}
			},
			//底部自定义按钮接口操作
			handleBottomBtnInterface(item) {
				const selectedItemsCopy = [...this.selectItems];
				const webType = this.config.webType;
				let data = {
					items: selectedItemsCopy,
					webType
				};
				const handlerInterface = (data) => {
					let query = {
						paramList: this.jnpf.getBatchParamList(item.templateJson, data) || [],
					}
					getDataInterfaceRes(item.interfaceId, query).then(res => {
						uni.showToast({
							title: res.msg,
							icon: 'none'
						})
					})
				}
				if (!item.useConfirm) return handlerInterface(data)
				uni.showModal({
					title: this.$t('common.tipTitle'),
					content: item.confirmTitle || '确认执行此操作?',
					showCancel: true,
					confirmText: '确定',
					success: function(res) {
						if (res.confirm) {
							handlerInterface(data)
						}
					}
				});
			},
			// 自定义按钮事件
			selectBtnconfirm(e) {
				var i = this.columnData.customBtnsList.findIndex((item) => {
					return item.value == e[0].value
				})
				const item = this.columnData.customBtnsList[i]
				const row = this.list[this.selectListIndex]
				const index = this.selectListIndex
				// 自定义启用规则判断
				if (!this.customEnableRule(row, item.value)) return this.$u.toast('没有' + item.label + '权限')
				if (item.event.btnType == 1) this.handlePopup(item.event, row)
				if (item.event.btnType == 2) this.handleScriptFunc(item.event, row, index)
				if (item.event.btnType == 3) this.handleInterface(item.event, row)
				if (item.event.btnType == 4) this.handleLaunchFlow(item, [row])
			},
			//自定义按钮发起流程
			handleLaunchFlow(item, records) {
				const data = deepClone(item.event.launchFlow)
				let dataList = [];
				for (let i = 0; i < records.length; i++) {
					dataList.push(this.jnpf.getLaunchFlowParamList(data.transferList, records[i], this.getRowKey));
				}
				const query = {
					template: data.flowId,
					btnCode: item.value,
					currentUser: data.currentUser,
					customUser: data.customUser,
					initiator: data.initiator,
					dataList,
				};
				launchFlow(query, this.modelId).then(res => {});
			},
			//自定义按钮弹窗操作
			handlePopup(item, row) {
				this.handleListen()
				let data = {
					config: item,
					modelId: this.modelId,
					id: this.config.webType == 4 ? '' : row[this.getRowKey],
					isPreview: this.isPreview,
					row: this.config.webType == 4 ? row : '',
				}
				data = encodeURIComponent(JSON.stringify(data))
				uni.navigateTo({
					url: '/pages/apply/customBtn/index?data=' + data
				})
			},
			//自定义按钮JS操作
			handleScriptFunc(item, row, index) {
				const parameter = {
					data: row,
					index,
					refresh: this.initData,
					onlineUtils: this.jnpf.onlineUtils,
				}
				const func = this.jnpf.getScriptFunc.call(this, item.func)
				if (!func) return
				func.call(this, parameter)
			},
			//自定义按钮接口操作
			handleInterface(item, row) {
				const handlerData = () => {
					getModelInfo(this.modelId, row[this.getModelInfo]).then(res => {
						const dataForm = res.data || {};
						if (!dataForm.data) return;
						const data = {
							...JSON.parse(dataForm.data),
							id: row[this.getModelInfo]
						};
						handlerInterface(data);
					})
				}
				const handlerInterface = (data) => {
					let query = {
						paramList: this.jnpf.getParamList(item.templateJson, {
							...data,
							id: row[this.getRowKey]
						}, this.getRowKey) || [],
					}
					getDataInterfaceRes(item.interfaceId, query).then(res => {
						uni.showToast({
							title: res.msg,
							icon: 'none'
						})
						if (item.isRefresh) this.initData();
					})
				}
				const handleFun = () => {
					this.config.webType == '4' ? handlerInterface(row) : handlerData();
				};
				if (!item.useConfirm) return handleFun()
				uni.showModal({
					title: '提示',
					content: item.confirmTitle || '确认执行此操作',
					success: (res) => {
						if (!res.cancel) handleFun()
					}
				})
			},
			initData() {
				this.list = [];
				this.$nextTick(() => {
					this.mescroll.resetUpScroll();
				})
			},
			search() {
				if (this.isPreview == '1') return
				this.searchTimer && clearTimeout(this.searchTimer)
				this.searchTimer = setTimeout(() => {
					this.list = [];
					this.mescroll.resetUpScroll();
				}, 300)
			},
			handleListen() {
				uni.$off('refresh')
				uni.$on('refresh', () => {
					this.list = [];
					this.mescroll.resetUpScroll();
				})
			},
			addPage() {
				this.handleListen()
				this.jumPage({}, '')
			},
			jumPage(item, btnType) {
				if (!item.id && !item.flowState) btnType = 'btn_add'
				if (this.config.enableFlow == 1) {
					if (item.id) {
						if (!this.permission.btnPermission.includes('btn_edit') && item.flowState == 3) return
						if (!this.permission.btnPermission.includes('btn_detail') && ![0, 8, 9].includes(item
								.flowState))
							return
					}
					let opType = '-1'
					if (![0, 8, 9].includes(item.flowState) && btnType != 'btn_add') opType = 0
					const config = {
						id: item.flowTaskId || item.id || '',
						flowId: this.config.flowId,
						opType,
						status: item.flowState || '',
						isPreview: this.isPreview,
						taskId: item.flowTaskId || item.id,
						isFlow: 0,
					}
					uni.navigateTo({
						url: '/pages/workFlow/flowBefore/index?config=' +
							this.jnpf.base64.encode(JSON.stringify(config))
					})
				} else {
					const type = btnType == 'btn_detail' ? 'detail' : 'form'
					const currentMenu = encodeURIComponent(JSON.stringify(this.permission.formPermission))
					let btnType_ = this.permission.btnPermission.includes('btn_edit') ? 'btn_edit' : 'btn_add'
					let enableEdit = this.customEnableRule(item, 'edit')
					let labelS = {}
					for (let i = 0; i < this.columnData.columnBtnsList.length; i++) {
						const item = this.columnData.columnBtnsList[i]
						if (item.value == 'edit') {
							labelS[btnType_] = item.labelI18nCode ? this.$t(item.labelI18nCode) : item.label
						}
					}
					const config = {
						currentMenu,
						btnType: btnType_,
						list: this.list,
						modelId: this.modelId,
						isPreview: this.isPreview,
						id: item.id || '',
						index: item.index,
						enableEdit,
						labelS
					}
					const url = '/pages/apply/dynamicModel/' + type + '?config=' +
						this.jnpf.base64.encode(JSON.stringify(config))
					uni.navigateTo({
						url: url
					})
				}
			},
			goDetail(item) {
				if (this.config.webType == 4) return
				this.handleListen()
				let hasDetail = this.permission.btnPermission.includes('btn_detail')
				let hasEdit = this.permission.btnPermission.includes('btn_edit')
				if (!hasDetail && !hasEdit) return
				if (hasDetail) {
					if (this.customEnableRule(item, 'detail')) {
						return this.jumPage(item, 'btn_detail')
					}
					if (this.customEnableRule(item, 'edit')) {
						return this.jumPage(item, 'btn_edit')
					}
				} else {
					if (this.customEnableRule(item, 'edit')) {
						return this.jumPage(item, 'btn_edit')
					}
				}
			},
			cellClick(item) {
				if (this.isPreview == '1') return this.$u.toast('功能预览不支持排序')
				const findIndex = this.sortValue.findIndex(o => o === item.value);
				if (findIndex < 0) {
					const findLikeIndex = this.sortValue.findIndex(o => o.indexOf(item.sidx) > -1);
					if (findLikeIndex > -1) this.sortValue.splice(findLikeIndex, 1)
					this.sortValue.push(item.value)
				} else {
					this.sortValue.splice(findIndex, 1)
				}
			},
			handleReset() {
				this.searchFormData = {}
				const list = ['datePicker', 'timePicker', 'inputNumber', 'calculate', 'cascader', 'organizeSelect']
				for (let i = 0; i < this.searchList.length; i++) {
					const item = this.searchList[i]
					const config = item.__config__
					let defaultValue = item.searchMultiple || list.includes(config.jnpfKey) ? [] : undefined
					if (config.isFromParam) defaultValue = undefined
					config.defaultValue = defaultValue
					this.searchFormData[item.id] = item.value || defaultValue
				}
				this.searchFormConf = JSON.parse(JSON.stringify(this.searchList))
			},
			handleSearch() {
				if (this.isPreview == '1') return this.$u.toast('功能预览不支持检索')
				this.$refs.searchForm && this.$refs.searchForm.submitForm()
			},
			relationFormClick(item, column) {
				let vModel = column.vModel ? column.vModel : column.__vModel__
				let model_id = column.modelId
				let config = {
					modelId: model_id,
					isPreview: true,
					id: item[vModel + '_id'],
					sourceRelationForm: true,
					noShowBtn: 1,
					noDataLog: 1,
					propsValue: column.propsValue
				}
				const url =
					'/pages/apply/dynamicModel/detail?config=' + this.jnpf.base64.encode(JSON.stringify(config))
				uni.navigateTo({
					url: url
				})
			},
			handleSortReset() {
				this.sortValue = []
			},
			handleSortSearch() {
				if (this.sortValue.length) {
					this.listQuery.sidx = this.sortValue.join(',')
				} else {
					this.setDefaultQuery()
				}
				this.$refs.uDropdown.close();
				this.$nextTick(() => {
					this.list = [];
					this.mescroll.resetUpScroll();
				})
			}
		}
	}
</script>

<style lang="scss">
	page {
		background-color: #f0f2f6;
		height: 100%;
		/* #ifdef MP-ALIPAY */
		position: absolute;
		top: 0;
		left: 0;
		width: 100%;
		/* #endif */
	}

	.top-btn {
		height: 80rpx;
		position: fixed;
		width: 100%;
		top: 0;
		left: 0;
		background-color: #fff;
		/* #ifdef MP-WEIXIN */
		z-index: 99;
		/* #endif */
		/* #ifndef MP-WEIXIN */
		z-index: 999;
		/* #endif */
		justify-content: space-between;
		padding: 0 20rpx;

		.button-left {
			color: #0293fc;
		}

		.button-right {
			width: 30rpx;
			height: 30rpx;
		}
	}

	.slide-down2 {
		animation: slide-down2 0.5s forwards;
		opacity: 1;
		transform: translateY(0);
	}

	.slide-up2 {
		animation: slide-up2 0.5s forwards;
		opacity: 0;
		transform: translateY(-100%);
	}

	.slide-down {
		animation: slide-down 0.5s forwards;
		opacity: 1;
		transform: translateY(0);
	}

	.slide-up {
		animation: slide-up 0.5s forwards;
		opacity: 0;
		transform: translateY(100%);
	}

	.bottom-btn {
		height: 100rpx;
		position: fixed;
		width: 100%;
		bottom: 0;
		left: 0;
		background-color: #0293fc;
		z-index: 9;
		justify-content: space-around;

		.button-preIcon {
			color: #fff;
			text-align: center;
			width: 20%;

			.btn-icon {
				height: 32rpx;
			}
		}
	}

	@keyframes slide-up {
		to {
			transform: translateY(0);
			opacity: 1;
		}
	}

	@keyframes slide-down {
		to {
			transform: translateY(100%);
			opacity: 0;
		}
	}

	@keyframes slide-up2 {
		to {
			transform: translateY(0);
			opacity: 1;
		}
	}

	@keyframes slide-down2 {
		to {
			transform: translateY(-100%);
			opacity: 0;
		}
	}

	:deep(.u-cell) {
		padding: 0rpx;
		height: 112rpx;
	}

	.buttom-actions {
		z-index: 1;
	}

	.screen-box {
		background-color: #fff;
		height: 100%;

		.screen-btn {
			width: 100%;
			height: 2.75rem;

			.btn {
				width: 50%;
				height: 2.75rem;
				text-align: center;
				line-height: 2.75rem;
				box-shadow: 0px -4rpx 20rpx #F8F8F8
			}

			.btn1 {
				color: #606266;
			}

			.btn2 {
				background-color: #2979ff;
				color: #fff;
			}
		}

		.screen-list {
			width: 100%;
			height: 100%;

			.list {
				height: calc(100% - 88rpx);
				overflow-y: scroll;
			}
		}
	}

	.notData-box {
		width: 100%;
		height: 100%;
		justify-content: center;
		align-items: center;
		padding-bottom: 200rpx;

		.notData-inner {
			width: 280rpx;
			height: 308rpx;
			align-items: center;

			.iconImg {
				width: 100%;
				height: 100%;
			}

			.notData-inner-text {
				padding: 30rpx 0;
				color: #909399;
			}
		}
	}
</style>