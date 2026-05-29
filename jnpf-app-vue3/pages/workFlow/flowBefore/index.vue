<template>
	<view class="flowBefore-v">
		<div class="flow-urgent-value" :style="{'background-color':flowUrgentList[selectflowUrgent.extra].bgColor}"
			@click="handleShowSelect">
			<span :style="{'color':flowUrgentList[selectflowUrgent.extra].color}">{{selectflowUrgent.label}}</span>
		</div>
		<view class="flowBefore-box">
			<view class="scroll-v" scroll-y>
				<childForm ref="child" :config="config" @eventReceiver="eventReceiver" v-if="!loading"
					@setBtnLoad="setBtnLoad" :key="childFormKey" />
				<ErrorForm @submitErrorForm="submitErrorForm" ref="ErrorForm" />
				<RecordTimeList :progressList="progressList" :taskInfo="taskInfo" v-if="getShowExtraPanel"
					:commentList="commentList" :taskId="config.opType==0 ? config.id:config.taskId" ref="RecordTimeList"
					@handleReply="goWriteComment" :hasComment="hasComment" :dataLog="dataLog" :opType="config.opType"
					:formID="config?.formData?.id" :dataLogList="dataLogList" :auxiliaryInfo="auxiliaryInfo">
				</RecordTimeList>
			</view>
		</view>
		<flowBtn :actionList="actionList" :btnInfo="btnInfo" :opType="config.opType" :hideSaveBtn="config.hideSaveBtn"
			@handleBtn="handleBtn" @handlePress="handlePress" :btnLoading="btnLoading" v-if="formLoding"
			:rightBtnList="rightBtnList" :saveBtnText="properties.saveBtnText" :hasComment="hasComment"
			:hasSignFor="flowInfo?.flowNodes?.global?.hasSignFor" :todoBtnList="todoBtnList"
			:isProcessing="config.isProcessing">
		</flowBtn>
		<u-select :list="flowUrgentList" v-model="showFlowUrgent" @confirm="seltConfirm"
			:default-value="defaultValue" />
		<uni-popup mode="bottom" ref="reduceApprover" background-color='#fff'>
			<view class="approverContent">
				<view class="notice-warp">
					<view class="u-flex close-icon">
						<u-icon name="close" size="32" @click="hideReduceApprover" color="#93969c"></u-icon>
					</view>
					<view class="search-box">
						<u-search :placeholder="$t('app.apply.pleaseKeyword')" v-model="keyword" height="72"
							:show-action="false" @change="getAddSignUserList" bg-color="#f0f2f6" shape="square" />
					</view>
				</view>
				<view class="popup">
					<view v-for="(item, index) in signUserIdList" :key="index" v-if="signUserIdList.length"
						class="list-box">
						<view class="u-flex item">
							<view class="u-flex" style="flex: 1;">
								<u-avatar :src="define.baseURL+item.headIcon"></u-avatar>
								<view class="u-m-l-10">
									<view>{{item.fullName}}</view>
									<view>{{item.organize}}</view>
								</view>
							</view>
							<view class="" @click="deleteReduce(item.id)">
								<u-icon name="trash" size="32" color="#93969c"></u-icon>
							</view>
						</view>
					</view>
					<NoData v-else backgroundColor="#fff"></NoData>
				</view>
			</view>
		</uni-popup>
		<MultSelect :list="userList" :show="showSelectModal" @confirm="selectUser" @close="showSelectClose"
			v-if="userList.length" />
	</view>
</template>
<script>
	import {
		getDelegateUser
	} from '@/api/workFlow/entrust.js'
	import NoData from '@/components/noData'
	import {
		Create,
		Update
	} from '@/api/workFlow/workFlowForm'
	import {
		FlowTask,
		Audit,
		Reject,
		Transfer,
		saveAudit,
		saveAssist,
		AddSignUserIdList,
		ReduceApprover,
		Candidates,
		RejectList,
		FreeApprover,
		launchRecall,
		auditRecall,
		cancel,
		SignFor,
		Transact,
		sendBack,
		back,
		Assist
	} from '@/api/workFlow/flowBefore'
	import {
		Revoke,
		Press
	} from '@/api/workFlow/flowLaunch'
	import {
		createModel,
		updateModel,
		getOnlineLog
	} from '@/api/apply/visualDev'
	import {
		createComment
	} from '@/api/workFlow/flowEngine'
	import {
		checkInfo
	} from '@/api/message.js'
	import resources from '@/libs/resources.js'
	import childForm from './form.vue'
	import flowBtn from '../components/flowBtn'
	import RecordTimeList from '../components/RecordTimeList'
	import ErrorForm from '../components/ErrorForm'
	import {
		useUserStore
	} from '@/store/modules/user'
	const sysConfigInfo = uni.getStorageSync('sysConfigInfo')
	import MultSelect from '@/components/MultSelect'
	export default {
		components: {
			childForm,
			ErrorForm,
			flowBtn,
			RecordTimeList,
			NoData,
			MultSelect
		},
		data() {
			return {
				dataLogList: [],
				dataLog: false,
				childFormKey: +new Date(),
				todoBtnList: [],
				signUserIdList: [],
				keyword: '',
				formLoding: false,
				loading: false,
				taskInfo: {},
				backNodeList: [],
				btnInfo: [],
				nodeList: [],
				handleStatus: 1,
				isComment: false,
				isSummary: false,
				show: false,
				config: {},
				currentView: '',
				formData: {},
				flowFormInfo: {},
				flowTemplateInfo: {},
				flowTaskNodeList: [],
				flowTemplateJson: [],
				recordList: [],
				properties: {},
				tablist: [],
				flowStatus: '',
				stepIndex: 0,
				btnLoading: false,
				eventType: '',
				commentList: [],
				processId: "",
				candidateList: [],
				summaryType: 0,
				title: '',
				branchList: [],
				candidateType: 3,
				selectflowUrgent: {
					extra: '0',
					label: '普通',
					value: 1,
				},
				showFlowUrgent: false,
				defaultValue: [0],
				flowUrgent: 1,
				flowUrgentList: [{
						label: '普通',
						color: '#409EFF',
						bgColor: '#e5f3fe',
						value: 1,
						extra: '0'
					},
					{
						label: '重要',
						color: '#E6A23C',
						bgColor: '#fef6e5',
						value: 2,
						extra: '1'
					},
					{
						label: '紧急',
						color: '#F56C6C',
						bgColor: '#fee5e5',
						value: 3,
						extra: '2'
					},
				],
				showAction: false,
				actionList: [],
				rejectList: [],
				rejectStep: '',
				hasComment: false,
				progressList: [],
				signValue: '',
				rightBtnList: [],
				approvalField: [],
				delegateUser: '',
				showSelectModal: false,
				userList: [],
				auxiliaryInfo: []
			};
		},
		computed: {
			baseURL() {
				return this.define.baseURL
			},
			getShowExtraPanel() {
				return this.dataLog && this.config?.formData?.id || (this.config.opType != '-1' && !this.loading)
			}
		},
		onLoad(option) {
			if (!option.config) return this.jnpf.goBack()
			this.config = JSON.parse(this.jnpf.base64.decode(option.config))
			uni.$on('operate', (data) => {
				this.btnLoading = true
				this[data.eventType + 'Handle'](data)
			})
			this.loading = true
			// 短链接
			if (option.token) {
				const userStore = useUserStore()
				userStore.setToken('')
				userStore.setToken(option.token)
				let dataConfig = {
					id: this.config.taskId,
					flowId: this.config.flowId,
					opType: this.config.opType,
					taskId: this.config.taskId,
					operatorId: this.config.operatorId,
					hideCancelBtn: true,
				}
				checkInfo(dataConfig.operatorId || dataConfig.taskId, dataConfig.opType).then(res => {
					dataConfig.opType = res.data.opType;
					this.config = dataConfig
					this.init()
				}).catch((err) => {})
				return
			}
			this.init()
			this.processId = this.config.id
		},
		onShow() {
			uni.$off('comment')
			uni.$on('comment', data => {
				this.commentList = [];
				this.current = 0;
				this.addComment(data)
			})
			uni.$on('openRevokeFlow', data => {
				this.config = data
				this.$nextTick(() => {
					this.getBeforeInfo(this.config)
				})
			})
			uni.$emit('initCollapse')
		},
		onUnload() {
			uni.$off('operate')
			uni.$off('refresh')
			uni.$off('comment')
			uni.$off('openRevokeFlow')
		},
		methods: {
			handlePreviewImage(url) {
				// #ifdef H5
				uni.previewImage({
					urls: [url],
					current: url,
					success: () => {},
					fail: () => {
						uni.showToast({
							title: '预览图片失败',
							icon: 'none'
						});
					}
				});
				// #endif
			},
			handleCodeGeneration(config) {
				this.config = config
				this.init(this.config)
				uni.$on('operate', (data) => {
					this[data.eventType + 'Handle'](data)
				})
				uni.$on('comment', data => {
					this.commentList = [];
					this.current = 0;
					this.addComment(data)
				})
				this.processId = this.config.id
				uni.setNavigationBarTitle({
					title: this.config.fullName
				})
				this.key = +new Date()
			},
			//流程按钮事件
			handleBtn(type, dataForm = {}) {
				// 定义事件类型列表
				const eventTypeList1 = ['submit', 'audit', 'reject', 'save', 'saveAudit', 'saveAssist',
					'freeApprover', 'approvalButton', 'delegateSubmit'
				];
				const eventTypeList2 = ['transfer', 'back', 'launchRecall', 'assist', 'auditRecall', 'cancelSign',
					'revoke', 'approvalCancel', 'sign', 'startHandle'
				];
				// 根据事件类型执行相应逻辑
				this.eventType = type;
				switch (true) {
					case type.includes('initiationForm'):
						const config = {
							previewType: 'initiationForm',
							taskId: this.config.taskId,
							title: this.config.fullName
						};
						uni.navigateTo({
							url: `/pages/workFlow/scanForm/index?config=${JSON.stringify(config)}`
						});
						break;
					case type.includes('press'):
						this.handlePress();
						break;
					case type.includes('reduceApprover'):
						this.getAddSignUserList();
						break;
					case type.includes('comment'):
						this.goWriteComment();
						break;
					case eventTypeList1.includes(type):
						this.eventLauncher(type);
						break;
					case eventTypeList2.includes(type):
						this.eventReceiver({}, type);
						break;
					case type === 'customBtns':
						this.handleCustomBtns(dataForm.value)
						break;
					default:
						// 可以添加默认处理逻辑，或者什么也不做
						break;
				}
			},
			// 自定义按钮
			handleCustomBtns(key) {
				this.actionList.length &&
					this.actionList.map(item => {
						if (item.value === key) {
							const parameter = {
								flowInfo: this.flowInfo,
								formData: this.formData,
								taskInfo: this.taskInfo,
								onlineUtils: this.jnpf.onlineUtils
							};
							const func = this.jnpf.getScriptFunc(item.jsJson);
							if (!func) return
							func.call(this, {
								...parameter
							})
						}
					});
				return null;
			},
			// 委托用户详情
			getDelegateUser() {
				getDelegateUser(this.config.flowId).then(res => {
					this.userList = res.data.list || [];
					if (this.userList.length == 1) return this.selectUser([{
						id: this.userList[0].id
					}]);
					if (this.userList.length) return this.openUserListModal();
				});
			},
			showSelectClose() {
				this.showSelectModal = false
			},
			openUserListModal() {
				this.showSelectModal = true
			},
			selectUser(item) {
				this.delegateUser = item[0].id;
				this.getCandidates(this.config.operatorId);
			},
			delegateSubmitHandle(data) {
				this.handleRequest(data)
			},
			//催办
			handlePress() {
				uni.showModal({
					title: '提示',
					content: '此操作将提示该节点尽快处理',
					success: res => {
						if (res.confirm) {
							Press(this.config.id).then(res => {
								this.$u.toast(res.msg)
							})
						}
					}
				})
			},
			//关闭减签弹窗
			hideReduceApprover() {
				this.keyword = ''
				this.$refs.reduceApprover.close()
			},
			//获取减签加签列表
			getAddSignUserList() {
				let data = {
					currentPage: 1,
					enabledMark: 1,
					id: this.config.operatorId,
					keyword: this.keyword,
					organizeId: '',
					pageSize: 10000,
					sidx: "",
					sort: "desc"
				}
				AddSignUserIdList(data, this.config.operatorId).then(res => {
					this.signUserIdList = res.data.list || []
					this.$refs.reduceApprover.open('bottom')
				})
			},
			//减签
			deleteReduce(id) {
				let data = {
					ids: [id]
				}
				ReduceApprover(data, this.config.operatorId).then(res => {
					let signUserIdList = this.signUserIdList.filter(item => id !== item.id)
					this.signUserIdList = signUserIdList
				})
			},
			goWriteComment(replyId) {
				let data = {
					taskId: this.config.opType == 0 ? this.config.id : this.config.taskId
				};
				if (replyId) {
					data.replyId = replyId;
				}
				data = encodeURIComponent(JSON.stringify(data));
				uni.navigateTo({
					url: '/pages/workFlow/comment/index?data=' + data
				})
			},
			addComment(query) {
				query.taskId = this.config.opType == 0 ? this.config.id : this.config.taskId
				createComment(query).then(res => {
					this.$nextTick(() => {
						this.$refs.RecordTimeList.change(1)
					})
				})
			},
			init() {
				this.processId = this.config.id
				if (this.config.id) {
					let extra = {
						modelId: this.config.flowId,
						id: this.config.id,
						type: 1,
						flowId: this.config.flowId,
						processId: this.config.id,
						taskId: this.config.taskId,
						opType: this.config.opType,
					}
					uni.setStorageSync('dynamicModelExtra', extra)
				}
				/**
				 * opType
				 * -1 - 我发起的新建/编辑 
				 * 0 - 我发起的详情
				 * 1 - 待办事宜
				 * 2 - 已办事宜
				 * 3 - 抄送事宜
				 */
				this.getBeforeInfo(this.config)
			},
			getBeforeInfo() {
				let config = this.config;
				this.formData.flowId = config.flowId;
				this.loading = true;
				const query = {
					flowId: config.flowId, // 流程大Id
					opType: config.opType == 2 ? 3 : config.opType, //特殊：待办点击开始办理需要跳到在办页面
				};
				if (config.opType != "-1" && config.opType != '0') query.operatorId = config.operatorId;
				FlowTask(config?.taskId || config?.id || 0, query).then((res) => {
					this.flowInfo = res.data.flowInfo || {};
					this.properties = res.data.nodeProperties || {};
					this.auxiliaryInfo = this.properties.auxiliaryInfo
					this.$nextTick(() => {
						this.initApprovalField()
					})
					this.formInfo = res.data.formInfo || {};
					this.taskInfo = res.data.taskInfo || {};
					this.btnInfo = res.data.btnInfo || [];
					this.progressList = res.data.progressList || [];
					config.formOperates = res.data.formOperates || [];
					config.formType = this.formInfo.type
					const fullName =
						config.opType == "-1" ?
						this.flowInfo.fullName :
						this.taskInfo.fullName;
					config.fullName = fullName;
					this.title = this.flowInfo.fullName;
					this.thisStep = this.taskInfo.thisStep || "";
					if (config.status !== 0 && config.status !== 3) {
						this.title = this.thisStep ?
							config.fullName + "/" + this.thisStep :
							config.fullName;
					}
					config.type = this.flowInfo.type;
					config.draftData = res.data.draftData || null;
					config.formData = res.data.formData || {};
					let dataId = config.formData.id
					config.formEnCode = this.formInfo.enCode;
					this.nodeList = res.data.nodeList || [];
					this.recordList = (res.data.recordList || []).reverse();
					config.formConf = this.formInfo.formData;
					if (config.formConf) {
						this.dataLog = JSON.parse(config.formConf).dataLog
						if (this.dataLog) this.getOnlineLog(dataId)
					}
					this.hasComment = this.flowInfo.flowNodes.global.hasComment;
					this.loading = false;
					this.formLoding = true;
					uni.setNavigationBarTitle({
						title: this.config.formEnCode === "revoke" ? `${this.flowInfo.fullName}撤销申请` : this
							.flowInfo.fullName,
					});
					if (config.formRecords && config.title) {
						uni.setNavigationBarTitle({
							title: config.title,
						});
					}
					this.flowUrgent = this.taskInfo.flowUrgent || 1;
					const getSelectInfo = () => {
						var obj = {
							value: this.flowUrgent,
							extra: "0",
							label: "普通",
						};
						this.flowUrgentList.forEach((e, i) => {
							if (e.value == this.flowUrgent) {
								obj.extra = i;
								obj.label = e.label;
							}
						});
						return obj;
					};
					this.selectflowUrgent = getSelectInfo();
					this.initRightBtnList();
					if (config.opType != "-1" && config.opType != "3") config.readonly =
						true;
					config.formOperates = [];
					if (config.opType == 0) {
						if (this.properties && this.properties && this.properties.formOperates) config
							.formOperates = this.properties.formOperates || [];
					} else {
						config.formOperates = res.data.formOperates || [];
					}
					this.getFlowStatus()
					setTimeout(() => {
						this.$nextTick(() => {
							if (!this.$refs.child || !this.$refs.child.$refs.form) {
								uni.showToast({
									title: "暂无此流程表单",
									icon: "none",
									complete: () => {
										setTimeout(() => {
											uni.navigateBack();
										}, 1500);
									},
								});
								return;
							}
							this.$refs.child.$refs.form.init(config)
						});
					}, 100);
					this.config = config;
				});
			},
			//获取修改记录
			getOnlineLog(dataId) {
				let modelId = this.formInfo.id
				getOnlineLog(modelId, dataId).then(res => {
					let list = res.data.list || []
					//倒序转正
					// this.dataLogList = [...list].reverse()
					this.dataLogList = list
				})
			},
			//设置拓展自带
			initApprovalField() {
				this.approvalField = []
				if (this.config.opType == 3 && this.properties.hasApprovalField) {
					const approvalFieldList = this.flowInfo?.flowNodes?.global?.approvalFieldList || [];
					this.properties.approvalField.map(o => {
						const list = approvalFieldList.filter(item => item.id == o);
						list?.length && this.approvalField.push({
							...list[0],
							value: null
						});
					});
				}
			},
			getFlowStatus() {
				//待签收
				if (this.config.category == '0') this.flowStatus = resources.status.signfor
				// 待办，在办
				if (this.config.category == '1' || this.config.category == '2') {
					//流转中
					if (this.config.status == '1') this.flowStatus = resources.status.circulation
					//已退回
					if (this.config.status == '5') this.flowStatus = resources.status.back
				}
				//发起
				if (!this.config.category) {
					//待提交
					if (this.config.status == '0') this.flowStatus = resources.status.draft
					//进行中
					if (this.config.status == '1') this.flowStatus = resources.status.doing
					//已通过
					if (this.config.status == '2') this.flowStatus = resources.status.adopt
				}
				//已办
				if (this.config.category == '3') {
					//转审
					if (this.config.status == '7') this.flowStatus = resources.status.transfer
					//同意
					if (this.config.status == '1') this.flowStatus = resources.status.agree
					//拒绝
					if (this.config.status == '0') this.flowStatus = resources.status.refuse
					//加签
					if (this.config.status == '4') this.flowStatus = resources.status.addSign
					//减签
					if (this.config.status == '5') this.flowStatus = resources.status.return
				}
				//抄送
				if (this.config.category == '4') {
					//进行中
					if (this.config.status == '1') this.flowStatus = resources.status.doing
					//已退回
					if (this.config.status == '8') this.flowStatus = resources.status.back
				}
			},
			initRightBtnList() {
				const list = [];
				if (this.opType != 6) {
					// 右侧按钮
					// 委托发起
					if (this.btnInfo?.hasDelegateSubmitBtn) list.push({
						id: 'delegateSubmit',
						fullName: '委托发起',
						type: 'primary'
					});
					// 提交
					if (this.btnInfo?.hasSubmitBtn) list.push({
						id: 'submit',
						fullName: this.properties.submitBtnText || '提交',
						type: 'primary'
					});
					// 待签
					if (sysConfigInfo.flowSign == 1 && this.btnInfo?.hasSignBtn) list.push({
						id: 'sign',
						fullName: this.btnInfo?.proxyMark ? '代签' : '签收',
						type: 'primary',
						width: '100%'
					});
					//特殊：待办点击开始办理需要跳到在办页面
					if (this.config.opType == 2) {
						if (sysConfigInfo.flowSign == 1 && this.flowInfo?.flowNodes?.global?.hasSignFor) this.todoBtnList
							.push({
								id: 'cancelSign',
								fullName: '退签',
								type: 'warning'
							});
						this.todoBtnList.push({
							id: 'startHandle',
							fullName: this.btnInfo?.proxyMark ? '代办' : '开始办理',
							type: 'primary',
							width: '100%'
						});
					}
					// 在办
					if (this.btnInfo?.hasRecallLaunchBtn) list.push({
						id: 'launchRecall',
						fullName: '撤回',
						type: 'warning',
						width: '100%'
					});
					if (this.btnInfo?.hasRecallAuditBtn) list.push({
						id: 'auditRecall',
						fullName: '撤回',
						type: 'warning',
						width: '100%'
					});
					if (this.btnInfo?.hasAssistSaveBtn) list.push({
						id: 'saveAssist',
						fullName: '保存'
					});
					if (this.btnInfo?.hasAuditBtn) list.push({
						id: 'audit',
						fullName: this.properties.auditBtnText || '同意',
						type: 'primary'
					});
					if (this.btnInfo?.hasReduceApproverBtn) {
						list.push({
							id: 'reduceApprover',
							fullName: this.properties.reduceApproverBtnText || '减签',
							type: 'primary',
							width: '100%'
						});
					}
					this.rightBtnList = list
					this.initBtnList()
				}
			},
			initBtnList() {
				const list = [];
				const properties = this.properties;
				const btnInfo = this.btnInfo;
				// 自定义按钮
				if (properties?.customBtns?.length) {
					properties?.customBtns.map((item) => {
						list.push({
							id: "customBtns",
							text: item.label,
							jsJson: item.jsJson,
							value: item.value
						});
					});
				}
				// 流程审批
				if (this.hasComment && this.config.opType != '-1' && this.rightBtnList.length) list.push({
					id: 'comment',
					text: '评论'
				});
				if (btnInfo?.hasViewStartFormBtn) list.push({
					id: 'initiationForm',
					text: '查看发起表单'
				});
				if (this.config.opType != 2) {
					if (btnInfo?.hasPressBtn) list.push({
						id: 'press',
						text: '催办'
					});
					if (btnInfo?.hasSaveAuditBtn) list.push({
						id: 'saveAudit',
						text: '暂存'
					});
					if (btnInfo?.hasBackBtn) list.push({
						id: 'back',
						text: properties.backBtnText || '退回'
					});
					if (btnInfo?.hasFreeApproverBtn) list.push({
						id: 'freeApprover',
						text: properties.freeApproverBtnText || '加签'
					});
					if (btnInfo?.hasTransferBtn) list.push({
						id: 'transfer',
						text: properties.transferBtnText || '转审'
					});
					if (btnInfo?.hasAssistBtn) list.push({
						id: 'assist',
						text: properties.assistBtnText || '协办'
					});
					if (btnInfo?.hasRevokeBtn) list.push({
						id: 'revoke',
						text: '撤销'
					});
				}
				this.actionList = list;
			},
			handleAction(index) {
				switch (this.actionList[index].id) {
					case 'assist':
						this.eventLauncher('assist')
						break;
					case 'back':
						this.eventLauncher('back')
						break;
					case 'save':
						this.eventLauncher('save')
						break;
					case 'transfer':
						this.eventReceiver({}, 'transfer')
						break;
					case 'reject':
						this.eventReceiver({}, 'reject')
						break;
					case 'launchRecall':
						this.eventReceiver({}, 'launchRecall')
						break;
					case 'auditRecall':
						this.eventReceiver({}, 'auditRecall')
						break;
					case 'cancelSign':
						this.eventReceiver({}, 'cancelSign')
						break;
					case 'saveAudit':
						this.eventLauncher('saveAudit')
						break;
					case 'revoke':
						this.eventReceiver({}, 'revoke')
						break;
					case 'freeApprover':
						this.eventLauncher('freeApprover')
						break;
					case 'approvalCancel':
						this.eventReceiver({}, 'approvalCancel')
					default:
						break;
				}
			},
			eventLauncher(eventType) {
				this.$refs.child && this.$refs.child.$refs.form && this.$refs.child
					.$refs.form.submit(eventType, this.selectflowUrgent.value)
			},
			eventReceiver(formData, eventType) {
				this.formData = {
					...formData,
					flowId: this.flowInfo.flowId
				};
				this.eventType = eventType
				if (eventType === "sign" || eventType === "cancelSign") {
					let data = {
						ids: [this.config.operatorId],
						type: eventType === "sign" ? 0 : 1,
					};
					let signContent = "确定签收，签收后进入待办。";
					if (eventType === "cancelSign")
						signContent = "确定退签，确定后进入我的待签。";
					uni.showModal({
						title: "提示",
						content: signContent,
						success: (res) => {
							if (res.confirm) this.signFor(data);
						},
					});
				}
				if (eventType === "startHandle") {
					let data = {
						ids: [this.config.operatorId],
					};
					uni.showModal({
						title: "提示",
						content: "确定开始办理流程。",
						success: (res) => {
							if (res.confirm) this.Transact(data);
						},
					});
				}
				if (['save', 'submit', 'delegateSubmit'].includes(eventType)) return this.submitOrSave(eventType)
				if (['saveAudit', 'saveAssist'].includes(eventType)) return this.saveAudit()
				if (eventType === "reject" || eventType === 'audit') {
					if (this.properties.type === 'processing') {
						uni.showModal({
							title: "提示",
							content: "是否确认办理该审批单?",
							success: (res) => {
								if (res.confirm) return this.getCandidates(this.config.operatorId)
							},
						});
					} else {
						this.getCandidates(this.config.operatorId)
					}
				}
				if (eventType === 'back') return this.back()
				if (eventType === 'revoke') return this.jumRevokePage()
				if (eventType === 'assist') return this.operate()
				if (eventType === 'transfer') return this.operate(this.properties.transferBtnText)
				if (eventType === 'freeApprover') return this.getCandidates(this.config.operatorId)
				if (eventType === 'launchRecall' || eventType === 'auditRecall') this.operate()
				if (eventType === 'approvalCancel') {
					let data = {
						formData: this.formData,
						eventType: this.eventType
					}
					if (!this.properties.hasOpinion && !this.properties.hasSign) {
						return uni.showModal({
							title: '提示',
							content: `此操作将审批驳回终止流程，是否继续?`,
							success: res => {
								if (res.confirm) this.approvalCancelHandle(data)
							}
						})
					}
					return this.operate(this.properties.cancelBtnText || '驳回')
				}
			},
			// 撤销操作
			jumRevokePage() {
				let config = {
					type: this.eventType
				}
				uni.navigateTo({
					url: '/pages/workFlow/operate/revoke?config=' + encodeURIComponent(JSON.stringify(config))
				})
			},
			/* 退回 */
			back(data) {
				back(this.config.id).then(res => {
					this.backNodeList = res.data.list || []
					this.$nextTick(() => {
						this.operate(this.properties.backBtnText)
					})
				}).catch(() => {
					this.btnLoading = false
				});
			},
			/* 退回处理 */
			backHandle(data) {
				delete data.addSignParameter
				const query = {
					...data,
					...this.formData,
				};
				sendBack(query, this.config.id).then(res => {
					const errorData = res.data?.errorCodeList;
					if (errorData && Array.isArray(errorData) && errorData.length) return this.$refs.ErrorForm
						.init(errorData, query)
					this.toastAndBack(res.msg)
				}).catch(() => {
					this.btnLoading = false
				});
			},
			/* 开始办理 */
			Transact(data) {
				this.btnLoading = true
				// #ifdef MP-WEIXIN
				this.loading = true
				// #endif
				Transact(data).then(res => {
					this.config.opType = 3
					this.btnLoading = false
					// #ifdef MP-WEIXIN
					this.loading = false
					// #endif
					if (this.config.readonly) this.$set(this.config, 'readonly', false)
					this.initRightBtnList();
					this.initApprovalField()
					this.childFormKey = +new Date()
					uni.$emit('refresh')
				}).catch(err => {
					this.btnLoading = false
				})
			},
			/* 签收 */
			signFor(data) {
				this.btnLoading = true
				SignFor(data).then(res => {
					this.toastAndBack(res.msg)
				}).catch(err => {
					this.btnLoading = false
				})
			},
			/* 保存草稿 */
			saveAudit() {
				this.btnLoading = true
				const save = this.eventType == 'saveAssist' ? saveAssist : saveAudit;
				this.formData.id = this.config.taskId
				let data = {
					...this.formData,
					id: this.config.taskId,
					flowId: this.flowInfo.flowId
				}
				save(this.config.operatorId, data).then(res => {
					uni.showToast({
						title: res.msg,
						icon: 'none',
						complete: () => {
							setTimeout(() => {
								uni.navigateBack()
							}, 1500)
						}
					});
				}).catch(() => {
					this.btnLoading = false
				})
			},
			//异常处理
			submitErrorForm(data) {
				let formData = {
					...this.formData,
					...data
				}
				if (this.eventType === "submit") {
					this.handleRequest(formData)
				} else {
					this.handleApproval(formData)
				}
			},
			getCandidates(id) {
				this.formData.flowId = this.flowInfo.flowId
				this.formData.f_id = this.config.taskId
				let data = {
					flowId: this.flowInfo.flowId,
					flowUrgent: this.flowUrgent,
					...this.formData,
					f_id: this.config.taskId
				}
				if (this.eventType === 'delegateSubmit') data.delegateUser = this.delegateUser
				if (['audit', 'reject'].includes(this.eventType)) data.handleStatus = this.eventType === 'audit' ? 1 : 0
				Candidates(id || 0, data).then(res => {
					const data = res.data || {}
					this.candidateType = data.type || 3
					this.candidateList = data.list.filter(o => !o.isBranchFlow && o.isCandidates);
					this.branchList = data.list || []
					if (['audit', 'reject'].includes(this.eventType)) return this.operate()
					if (this.eventType === 'freeApprover') return this.operate(this.properties.freeApproverBtnText)
					if (this.eventType === 'save') return handleRequest();
					if (this.candidateList.length || this.candidateType == 1) return this.operate('submit')
					if (!this.candidateList.length && !this.properties.isCustomCopy && this.candidateType == 3) {
						this.branchList = []
						uni.showModal({
							title: '提示',
							content: '您确定要提交当前流程吗?',
							success: res => {
								if (res.confirm) {
									this.btnLoading = true
									this.handleRequest()
								}
							}
						})
					}
				}).catch(() => {})
			},
			// getCandidates(id) {
			// 	this.formData.flowId = this.flowInfo.flowId
			// 	this.formData.f_id = this.config.taskId
			// 	let data = {
			// 		flowId: this.flowInfo.flowId,
			// 		flowUrgent: this.flowUrgent,
			// 		...this.formData,
			// 		f_id: this.config.taskId
			// 	}
			// 	if (this.eventType === 'delegateSubmit') data.delegateUser = this.delegateUser
			// 	if (['audit', 'reject'].includes(this.eventType)) data.handleStatus = this.eventType === 'audit' ? 1 : 0
			// 	Candidates(id || 0, data).then(res => {
			// 		const data = res.data || {}
			// 		this.candidateType = data.type || 3
			// 		this.branchList = data.list || []
			// 		this.candidateList = res.data.list.filter(o => !o.isBranchFlow && o.isCandidates)
			// 		if (['audit', 'reject'].includes(this.eventType)) return this.operate()
			// 		if (this.eventType === 'freeApprover') return this.operate(this.properties.freeApproverBtnText)
			// 		if (['save', 'submit', 'delegateSubmit'].includes(this.eventType)) {
			// 			if (this.eventType === 'save') return handleRequest();
			// 			if (!this.candidateList.length && this.candidateType != 3) return this.operate('submit')
			// 			this.branchList = []
			// 			uni.showModal({
			// 				title: '提示',
			// 				content: '您确定要提交当前流程吗?',
			// 				success: res => {
			// 					if (res.confirm) {
			// 						this.btnLoading = true
			// 						this.handleRequest()
			// 					}
			// 				}
			// 			})
			// 		}
			// 	}).catch(() => {})
			// },
			submitOrSave(eventType) {
				this.formData.status = eventType === 'submit' ? 0 : 1
				if (eventType === 'save') return this.handleRequest()
				if (eventType === 'delegateSubmit') return this.getDelegateUser()
				let id = this.config.opType === '-1' ? 0 : this.config.operatorId
				this.getCandidates(id)
			},
			submitHandle(data) {
				this.handleRequest(data)
			},
			selfHandleRequest() {
				const formMethod = this.formData.id ? updateModel : createModel
				formMethod(this.flowInfo.flowId, this.formData).then(res => {
					uni.showToast({
						title: res.msg,
						icon: 'none',
						complete: () => {
							setTimeout(() => {
								uni.navigateBack()
							}, 1500)
						}
					})
				}).catch(() => {})
			},
			//暂存提交
			handleRequest(data) {
				this.formData = {
					...this.formData,
					...data,
					flowId: this.flowInfo.flowId,
					candidateType: this.candidateType,
					status: this.eventType === 'save' ? 0 : 1,
					delegateUser: this.config.delegateUser || '',
					id: this.config.id,
					isFlow: this.config.isFlow || 1,
					flowUrgent: this.flowUrgent
				}
				if (this.eventType === 'save') this.btnLoading = true
				if (this.eventType === 'delegateSubmit') this.formData.delegateUser = this.delegateUser
				let formMethod = this.formData.id ? Update : Create
				// 流程
				formMethod(this.formData).then(res => {
					//异常处理
					const errorData = res.data?.errorCodeList;
					if (errorData && Array.isArray(errorData) && errorData.length) return this.$refs.ErrorForm
						.init(errorData)
					uni.showToast({
						title: res.msg,
						icon: 'none',
						complete: () => {
							setTimeout(() => {
								uni.$emit('refresh')
								uni.navigateBack()
							}, 1500)
						}
					})
				}).catch(() => {
					this.btnLoading = false
				})
			},
			//催办
			handlePress() {
				uni.showModal({
					title: '提示',
					content: '此操作将提示该节点尽快处理',
					success: res => {
						if (res.confirm) {
							Press(this.config.id).then(res => {
								this.$u.toast(res.msg)
							})
						}
					}
				})
			},
			operate() {
				const candidateList = this.candidateList.filter(o => !o.isBranchFlow && o.isCandidates);
				const branchList = this.branchList.filter(o => o.isBranchFlow);
				let properties = this.properties;
				let eventType = this.eventType;
				let config = {
					// 基本属性
					type: eventType,
					operatorId: this.config.operatorId,
					formData: this.formData,
					candidateType: this.candidateType,
					branchList: branchList,
					candidateList: candidateList,
					rejectList: this.rejectList,
					// 根据事件类型设置的属性
					propertiesType: properties.type === 'processing' ? properties.type : '',
					taskId: eventType === 'submit' ? 0 : this.config.taskId,
					hasFreeApprover: eventType === 'freeApprover' ? properties.hasFreeApproverBtn : false,
					// 流程相关属性
					hasSign: properties.hasSign,
					isCustomCopy: properties.isCustomCopy,
					hasOpinion: properties.hasOpinion,
					hasReduceApproverBtn: properties.hasReduceApproverBtn,
					hasFile: properties.hasFile,
					hasBackBtn: properties.hasBackBtn,
					backNodeList: this.backNodeList,
					// 回退相关属性
					backNodeCode: properties.backNodeCode,
					backMsgConfig: properties.backMsgConfig,
					backType: properties.backType,
					nodeName: properties.nodeName,
					// 审批相关属性
					approvalField: this.approvalField,
					delegateUser: this.delegateUser,
					// UI组件配置
					props: {
						label: 'nodeName',
						value: 'nodeId'
					}
				};
				uni.navigateTo({
					url: '/pages/workFlow/operate/index?config=' + encodeURIComponent(JSON.stringify(config))
				})
			},
			//撤销
			revokeHandle(data) {
				Revoke(this.config.id, {
					handleOpinion: data.handleOpinion
				}).then(res => {
					this.toastAndBack(res.msg, true)
				}).catch(() => {
					this.btnLoading = false
				})
			},
			//协办
			assistHandle(data) {
				Assist(this.config.id, data).then(res => {
					this.toastAndBack(res.msg, true)
				}).catch(() => {
					this.btnLoading = false
				})
			},
			//撤回
			recallHandle(data) {
				let id = this.eventType === 'auditRecall' ? this.config.operatorId : this.config.id
				let recallMethod = this.eventType === 'auditRecall' ? auditRecall : launchRecall;
				recallMethod(id, {
					handleOpinion: data.handleOpinion,
					signImg: data.signImg,
					fileList: data.fileList
				}).then(res => {
					this.toastAndBack(res.msg)
				}).catch(() => {
					this.btnLoading = false
				})
			},
			//同意
			auditHandle(data) {
				this.handleApproval(data)
			},
			//加签
			freeApproverHandle(data) {
				this.freeApprover(data)
			},
			//转审
			transferHandle(data) {
				Transfer(this.config.id, data).then(res => {
					this.toastAndBack(res.msg, true)
				}).catch(() => {
					this.btnLoading = false
				})
			},
			// 驳回审核
			approvalCancelHandle(data) {
				cancel(this.config.id, {
					handleOpinion: data.handleOpinion,
					signImg: data.signImg,
					fileList: data.fileList,
					eventType: 'approvalCancel',
				}).then(res => {
					this.toastAndBack(res.msg, true)
				}).catch(err => {
					this.btnLoading = false
				})
			},
			//拒绝
			rejectHandle(data) {
				this.handleApproval(data)
			},
			//加签
			freeApprover(data) {
				const query = {
					...data,
					...this.formData,
					id: this.config.taskId
				}
				FreeApprover(this.config.id, query).then(res => {
					this.toastAndBack(res.msg, true)
				}).catch(err => {
					this.btnLoading = false
				})
			},
			setBtnLoad(val) {
				this.btnLoading = !!val
			},
			//通过，拒绝
			handleApproval(data) {
				delete data.addSignParameter
				if (data.eventType === 'audit') this.handleStatus = 1;
				if (data.eventType === 'reject') this.handleStatus = 0;
				const query = {
					...data,
					...this.formData,
					id: this.config.taskId,
					flowId: this.flowInfo.flowId,
					handleStatus: this.handleStatus
				}
				const approvalMethod = this.eventType === 'audit' ? Audit : Reject
				approvalMethod(this.config.operatorId, query).then(res => {
					//异常处理
					const errorData = res.data?.errorCodeList;
					if (errorData && Array.isArray(errorData) && errorData.length) return this.$refs.ErrorForm
						.init(errorData, query)
					this.toastAndBack(res.msg, true)
				}).catch(err => {
					this.btnLoading = false
				})
			},
			//消息弹窗
			toastAndBack(title, refresh) {
				uni.showToast({
					title: title,
					icon: 'none',
					mask: true,
					complete: () => {
						setTimeout(() => {
							uni.$emit('refresh')
							uni.navigateBack()
						}, 1500)
					}
				})
			},
			handleShowSelect() {
				if (this.config.opType == '-1') this.showFlowUrgent = true
			},
			seltConfirm(e) {
				this.flowUrgent = e[0].value
				this.selectflowUrgent = e[0]
				this.defaultValue = [this.flowUrgentList.findIndex(item => item.value === e[0].value)] || [0]
			}
		}
	}
</script>

<style lang="scss">
	page {
		background-color: #f0f2f6;
		height: 100%;
	}

	.flow-urgent-value {
		position: fixed;
		top: var(--window-top);
		width: 100%;
		z-index: 99;
		display: flex;
		align-items: center;
		justify-content: center;
		height: 60rpx;
		font-size: 28rpx;
	}

	.flowBefore-v {
		display: flex;
		flex-direction: column;
		margin-top: 60rpx;



		.workFlowTitle {
			width: 100%;
			padding: 0 32rpx 32rpx 32rpx;
			background-color: #FFFFFF;
			font-size: 32rpx;
			font-weight: 700;
			white-space: pre-wrap;
			text-align: left;
		}

		.flowBefore-box {
			height: 100%;
			flex: 1;
			display: flex;
			flex-direction: column;
			overflow: hidden;
			padding-bottom: 3.3rem;

			.sticky-box {
				z-index: 500;
			}

		}

		.swiper-box {
			height: 100vh;
		}

		.swiper-item {
			flex: 1;
			flex-direction: row;
		}

		.scroll-v {
			flex: 1;
			/* #ifndef MP-ALIPAY */
			flex-direction: column;
			/* #endif */
			width: 100%;
			height: 100%;
		}

		.flowStatus {
			position: absolute;
			top: 90rpx;
			right: 0;
			border: 0;
			margin: 20rpx;
			opacity: 0.7;
			z-index: 999;

			image {
				width: 200rpx;
			}
		}

		.discuss_btn {
			background-color: #fff;
			position: fixed;
			bottom: 0;
			display: flex;
			width: 100%;
			// height: 88rpx;
			// box-shadow: 0 -2rpx 8rpx #e1e5ec;
			z-index: 20;

			.custom-style {
				background-color: #2979ff;
				color: #FFFFFF;
				width: 100%;
				border-radius: 0 !important;

				&::after {
					border: none !important;
				}
			}

			.content {
				padding: 24rpx;
				text-align: center;

				.confrim-btn {
					display: flex;
					flex-direction: row;

					.send {
						flex: 1;
						background-color: #2979ff;
						color: #FFFFFF;
					}

					.cancel {
						flex: 1;
					}
				}
			}
		}
	}

	.approverContent {
		height: 1000rpx;
		overflow-y: scroll;
		padding: 0 20rpx;

		.notice-warp {
			top: 0;
			height: 5.6rem;

			.close-icon {
				height: 60rpx;
				padding: 10rpx 20rpx;
				justify-content: flex-end;
			}
		}

		.popup {
			margin-top: 5.65rem;

			.list-box {
				.item {
					border-bottom: 1rpx solid #f0f0f0;
					padding: 20rpx 0;
				}
			}
		}
	}

	.nodeList-v {
		background-color: #fff;
	}

	.record-v {
		padding: 32rpx 32rpx 10rpx;
		background-color: #fff;
	}


	.discuss_box {
		.discuss_list {

			.time_button {
				padding-left: 110rpx;
				margin-top: 20rpx;
				padding-right: 10rpx;
			}

			.discuss_txt {
				width: 100%;
				justify-content: space-between;

				.discuss_txt_left {
					.uName {
						margin-left: 8px;
						font-size: 14px;
						font-family: PingFang SC;
						line-height: 17rpx;

						.replyText {
							margin-left: 8px;
						}
					}
				}

				.del {
					color: red;
					margin-left: 20px;
				}

				.reply {
					margin-left: 20px;
				}

			}

			.discuss_content {
				font-size: 12px;
				padding-left: 70rpx;

				.img_box {
					margin: 40rpx 0;
				}
			}
		}
	}



	.comment-creator-time {
		font-size: 12px;
		color: #999999;
		font-family: PingFang SC;
		line-height: 17rpx;
	}

	.comment-content-color {
		color: #606266;
	}

	.comment-header-color {
		color: #303133;
	}
</style>