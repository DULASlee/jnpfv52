<template>
	<view class="dynamicModel-form-v jnpf-wrap jnpf-wrap-form" v-if="showPage">
		<Parser :formConf="formConf" :formData="formData" ref="dynamicForm" v-if="!loading" :key="key"
			@toDetail="toDetail" />
		<view class="u-m-t-20 dataLog-box u-flex-col u-m-b-20" v-if="formConf.dataLog && !setting.noDataLog">
			<view class="title u-flex">
				<u-icon name=" icon-ym-generator-menu" custom-prefix="icon-ym"></u-icon>
				<text class="u-m-l-10">修改记录</text>
			</view>
			<view class="dataLog-v" v-if="dataLogList.length">
				<dataLog :dataLogList="dataLogList"></dataLog>
			</view>
			<NoData v-else paddingTop="0" backgroundColor="#fff" zIndex="9"></NoData>
		</view>
		<view class="buttom-actions">
			<CustomButton class="u-flex buttom-btn-left-inner" v-if="showMoreBtn" btnText="更多" btnType="more"
				iconName="more-dot-fill" size="28" @handleBtn="showAction = $event" :btnLoading="loading" />
			<template v-if="showEditBtn">
				<CustomButton class="u-flex buttom-btn-left-inner" :btnText="$t('common.cancelText')"
					btnIcon="icon-ym icon-ym-add-cancel" customIcon :btnLoading="loading" />
				<u-button class="buttom-btn" type="primary" @click.stop="handleEdit" :loading="loading">
					{{labelS.btn_edit}}
				</u-button>
			</template>
			<u-button class="cancel" @click.stop="jnpf.goBack()"
				v-if="!showEditBtn && !showMoreBtn">{{$t('common.cancelText')}}</u-button>
		</view>
		<u-select :list="actionList" v-model="showAction" @confirm="selectBtnconfirm" />
	</view>
</template>
<script>
	import NoData from '@/components/noData'
	import CustomButton from '@/components/CustomButton'
	import {
		getConfigData,
		getOnlineLog,
		getModelInfo,
		getDataChange,
		launchFlow
	} from "@/api/apply/visualDev";
	import {
		getRelationFormDetail,
		getDataInterfaceRes
	} from "@/api/common.js";
	import Parser from "./components/detail/Parser";
	import dataLog from '@/components/dataLog'
	import deepClone from '../../../uni_modules/vk-uview-ui/libs/function/deepClone';
	export default {
		components: {
			Parser,
			dataLog,
			NoData,
			CustomButton
		},
		data() {
			return {
				dataLogList: [],
				actionList: [],
				showAction: false,
				showPage: false,
				loading: true,
				isPreview: "0",
				modelId: "",
				formConf: {},
				formData: {},
				dataForm: {
					id: "",
					data: "",
				},
				btnType: "",
				formPermissionList: {},
				formList: [],
				labelS: {}
			};
		},
		onLoad(option) {
			uni.setNavigationBarTitle({
				title: this.$t('common.detailText')
			})
			let config = JSON.parse(this.jnpf.base64.decode(option.config));
			this.formPermissionList = !config.currentMenu ? [] :
				JSON.parse(decodeURIComponent(config.currentMenu));
			this.formList = this.formPermissionList.formList;
			this.btnType = config.btnType || "";
			this.labelS = config.labelS || {
				btn_edit: this.$t('common.editText')
			}
			this.modelId = config.modelId;
			this.isPreview = config.isPreview || "0";
			this.dataForm.id = config.id || "";
			this.setting = config;
			this.getConfigData();
			uni.$on("refresh", () => {
				this.getConfigData();
			});
		},
		computed: {
			showMoreBtn() {
				if (this.actionList.length && !this.setting?.noShowBtn || 0 && this.setting?.noDataLog) return true
				return false
			},
			showEditBtn() {
				if (this.btnType === 'btn_edit' && !this.setting.noShowBtn && this.setting.enableEdit) return true
				return false
			}
		},
		onShow() {
			setTimeout(() => {
				uni.$emit('initCollapse')
			}, 100)
		},
		onUnload() {
			uni.$off("refresh");
		},
		methods: {
			// 自定义按钮事件
			selectBtnconfirm(e) {
				var i = this.actionList.findIndex((item) => {
					return item.value == e[0].value
				})
				const item = this.actionList[i].actionConfig
				const row = this.formData
				// 自定义启用规则判断
				if (item.btnType == 1) this.handlePopup(item, row)
				if (item.btnType == 2) this.handleScriptFunc(item, row)
				if (item.btnType == 3) this.handleInterface(item, row)
				if (item.btnType == 4) this.handleLaunchFlow(item, [row])
			},
			//自定义按钮发起流程
			handleLaunchFlow(item, records) {
				const data = deepClone(item.launchFlow)
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
					dataList
				};
				launchFlow(query, this.modelId).then(res => {
					this.$u.toast(res.msg)
				});
			},
			//自定义按钮弹窗操作
			handlePopup(item, row) {
				let data = {
					config: item,
					modelId: this.modelId,
					id: row.id,
					row,
				}
				data = encodeURIComponent(JSON.stringify(data))
				uni.navigateTo({
					url: '/pages/apply/customBtn/index?data=' + data
				})
			},
			//自定义按钮JS操作
			handleScriptFunc(item, row) {
				const parameter = {
					data: row,
					refresh: this.initData,
					onlineUtils: this.jnpf.onlineUtils,
				}
				const func = this.jnpf.getScriptFunc.call(this, item.func)
				if (!func) return
				func.call(this, parameter)
			},
			//自定义按钮接口操作
			handleInterface(item, row, index) {
				const handlerData = () => {
					getModelInfo(this.modelId, row.id).then(res => {
						const dataForm = res.data || {};
						if (!dataForm.data) return;
						const data = {
							...JSON.parse(dataForm.data),
							id: row.id
						};
						handlerInterface(data);
					})
				}
				const handlerInterface = (data) => {
					let query = {
						paramList: this.jnpf.getParamList(item.templateJson, data) || [],
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
					handlerData();
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
			getOnlineLog() {
				getOnlineLog(this.setting.modelId, this.setting.id).then(res => {
					this.dataLogList = res.data.list || []
				})
			},
			getConfigData() {
				this.loading = true;
				getConfigData(this.modelId).then((res) => {
					if (res.code !== 200 || !res.data) {
						uni.showToast({
							title: "暂无此页面",
							icon: "none",
							complete: () => {
								setTimeout(() => {
									uni.navigateBack();
								}, 1500);
							},
						});
						return;
					}
					this.formConf = res.data.formData ? JSON.parse(res.data.formData) : {};
					this.actionList = this.formConf?.customBtns || []
					this.actionList.map((o) => {
						if (o.labelI18nCode) o.label = this.$t(o.labelI18nCode, o.label)
					})
					this.beforeInit(this.formConf.fields || []);
					this.showPage = true;
					this.key = +new Date();
					this.initData();
				});
			},
			beforeInit(fields) {
				const loop = (list) => {
					for (var index = 0; index < list.length; index++) {
						const config = list[index].__config__;
						if (config.children && config.children.length) loop(config.children);
						if (config.jnpfKey == "tableGrid") {
							let newList = [];
							for (var i = 0; i < config.children.length; i++) {
								let element = config.children[i];
								for (var j = 0; j < element.__config__.children.length; j++) {
									let item = element.__config__.children[j];
									newList.push(...item.__config__.children);
								}
							}
							list.splice(index, 1, ...newList);
						}
					}
				};
				loop(fields);
			},
			initData() {
				this.$nextTick(() => {
					if (this.dataForm.id) {
						let extra = {
							modelId: this.modelId,
							id: this.dataForm.id,
							type: 2,
						};
						uni.setStorageSync('dynamicModelExtra', extra)
						this.getRelationFormDetail()
					} else {
						this.loading = false;
					}
					this.$nextTick(() => {
						this.getOnlineLog()
					})
					this.key = +new Date();
				});
			},
			getRelationFormDetail() {
				const processResponse = (res) => {
					this.dataForm = res.data;
					this.loading = false;
					if (!this.dataForm.data) return;
					this.formData = {
						...JSON.parse(this.dataForm.data),
						id: this.dataForm.id,
					};
					this.fillFormData(this.formConf, this.formData);
					this.initRelationForm(this.formConf.fields);
				};
				let requestParams = {
					id: this.dataForm.id
				};
				if (this.setting?.sourceRelationForm) {
					if (this.setting.propsValue) requestParams.propsValue = this.setting.propsValue;
				}
				getDataChange(requestParams, this.modelId).then(res => {
					processResponse(res)
				}).catch(err => {
					this.loading = false;
				})
			},
			fillFormData(form, data) {
				const loop = (list, parent) => {
					for (let i = 0; i < list.length; i++) {
						let item = list[i];
						if (item.__vModel__) {
							if (
								item.__config__.jnpfKey === "relationForm" ||
								item.__config__.jnpfKey === "popupSelect"
							) {
								item.__config__.defaultValue = data[item.__vModel__ + "_id"];
								this.$set(item, "name", item.__config__.defaultValue || "");
							} else {
								let val = data.hasOwnProperty(item.__vModel__) ?
									data[item.__vModel__] :
									item.__config__.defaultValue;
								item.__config__.defaultValue = val;
							}
							if (this.formPermissionList.useFormPermission) {
								let id = item.__config__.isSubTable ?
									parent.__vModel__ + "-" + item.__vModel__ :
									item.__vModel__;
								let noShow = true;
								if (this.formList && this.formList.length) {
									noShow = !this.formList.some((o) => o.enCode === id);
								}
								noShow = item.__config__.noShow ? item.__config__.noShow : noShow;
								this.$set(item.__config__, "noShow", noShow);
							}
						} else {
							if (['relationFormAttr', 'popupAttr'].includes(item.__config__.jnpfKey)) {
								item.__config__.defaultValue =
									data[item.relationField.split('_jnpfTable_')[0] + '_' + item.showField];
							}
						}
						if (
							item.__config__ &&
							item.__config__.children &&
							Array.isArray(item.__config__.children)
						) {
							loop(item.__config__.children, item);
						}
					}
				};
				loop(form.fields);
				this.loading = false;
			},
			initRelationForm(componentList) {
				componentList.forEach((cur) => {
					const config = cur.__config__;
					if (
						config.jnpfKey == "relationFormAttr" ||
						config.jnpfKey == "popupAttr"
					) {
						const relationKey = cur.relationField.split("_jnpfTable_")[0];
						componentList.forEach((item) => {
							const noVisibility =
								Array.isArray(item.__config__.visibility) &&
								!item.__config__.visibility.includes("app");
							if (
								relationKey == item.__vModel__ &&
								(noVisibility || !!item.__config__.noShow) && !cur.__vModel__
							) {
								cur.__config__.noShow = true;
							}
						});
					}
					if (cur.__config__.children && cur.__config__.children.length)
						this.initRelationForm(cur.__config__.children);
				});
			},
			toDetail(item) {
				const id = item.__config__.defaultValue;
				if (!id) return;
				let config = {
					modelId: item.modelId,
					id: id,
					formTitle: "详情",
					noShowBtn: 1,
					noDataLog: 1,
					sourceRelationForm: item?.sourceRelationForm || false,
					propsValue: item?.propsValue || ''
				};
				this.$nextTick(() => {
					const url =
						"/pages/apply/dynamicModel/detail?config=" +
						this.jnpf.base64.encode(JSON.stringify(config));
					uni.navigateTo({
						url: url,
					});
				});
			},
			handleEdit() {
				if (this.setting.disableEdit) return;
				const currentMenu = encodeURIComponent(JSON.stringify(this.formPermissionList));
				let config = {
					modelId: this.modelId,
					isPreview: this.isPreview,
					id: this.setting.id,
					btnType: "btn_edit",
					currentMenu,
					list: this.setting.list,
					index: this.setting.index,
				};
				const url =
					"/pages/apply/dynamicModel/form?config=" +
					this.jnpf.base64.encode(JSON.stringify(config));
				uni.navigateTo({
					url: url,
				});
			},
		},
	};
</script>
<style lang="scss" scoped>
	page {
		background-color: #f0f2f6;
	}
</style>