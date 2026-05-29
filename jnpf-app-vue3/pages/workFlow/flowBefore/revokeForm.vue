<template>
	<view class="flowBefore-v">
		<div class="flow-urgent-value" :style="{'background-color':flowUrgentList[selectflowUrgent.extra].bgColor}"
			@click="handleShowSelect">
			<span :style="{'color':flowUrgentList[selectflowUrgent.extra].color}">{{selectflowUrgent.label}}</span>
		</div>
		<view class="flowBefore-box">
			<view class="scroll-v" scroll-y>
				<childForm ref="child" :config="config" v-if="!loading" :key="childFormKey" />
				<RecordTimeList :progressList="progressList" :taskInfo="taskInfo" v-if="!loading"
					:commentList="commentList" :taskId="config.id" ref="RecordTimeList" @handleReply="goWriteComment"
					:hasComment="hasComment" :dataLog="dataLog" :opType="config.opType" :formID="config?.formData?.id"
					:dataLogList="dataLogList">
				</RecordTimeList>
			</view>
		</view>
		<flowBtn :actionList="actionList" :btnInfo="btnInfo" :opType="config.opType" :hideSaveBtn="config.hideSaveBtn"
			@handleBtn="handleBtn" :btnLoading="btnLoading" v-if="formLoding" :rightBtnList="rightBtnList"
			:saveBtnText="properties.saveBtnText" :hasComment="hasComment"
			:hasSignFor="flowInfo?.flowNodes?.global?.hasSignFor">
		</flowBtn>
		<u-select :list="flowUrgentList" v-model="showFlowUrgent" @confirm="seltConfirm"
			:default-value="defaultValue" />
	</view>
</template>
<script>
	import {
		getDelegateUser
	} from '@/api/workFlow/entrust.js'
	import {
		Create,
		Update
	} from '@/api/workFlow/workFlowForm'
	import {
		FlowTask,
	} from '@/api/workFlow/flowBefore'
	import {
		Revoke,
		Press
	} from '@/api/workFlow/flowLaunch'
	import {
		getOnlineLog
	} from '@/api/apply/visualDev'
	import {
		createComment
	} from '@/api/workFlow/flowEngine'
	import resources from '@/libs/resources.js'
	import childForm from './form.vue'
	import flowBtn from '../components/flowBtn'
	import RecordTimeList from '../components/RecordTimeList'
	import {
		useUserStore
	} from '@/store/modules/user'
	const sysConfigInfo = uni.getStorageSync('sysConfigInfo')
	export default {
		components: {
			childForm,
			flowBtn,
			RecordTimeList
		},
		data() {
			return {
				dataLogList: [],
				dataLog: false,
				childFormKey: +new Date(),
				formLoding: false,
				loading: false,
				taskInfo: {},
				btnInfo: [],
				show: false,
				config: {},
				formData: {},
				properties: {},
				btnLoading: false,
				commentList: [],
				tabsName: '表单信息',
				title: '',
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
				hasComment: false,
				progressList: [],
				rightBtnList: [],
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
		onUnload() {
			uni.$off('comment')
		},
		onShow() {
			uni.$on('comment', data => {
				this.commentList = [];
				this.current = 0;
				this.addComment(data)
			})
		},
		onLoad(e) {
			this.config = JSON.parse(this.jnpf.base64.decode(e.config))
			this.$nextTick(() => {
				this.getBeforeInfo(this.config)
			})
		},
		methods: {
			addComment(query) {
				query.taskId = this.config.id
				createComment(query).then(res => {
					this.$nextTick(() => {
						this.$refs.RecordTimeList.change(1)
					})
				})
			},
			//流程按钮事件
			handleBtn(type, dataForm = {}) {
				if ('comment'.includes(type)) this.goWriteComment();
			},
			goWriteComment(replyId) {
				let data = {
					taskId: this.config.id
				};
				if (replyId) data.replyId = replyId;
				data = encodeURIComponent(JSON.stringify(data));
				uni.navigateTo({
					url: '/pages/workFlow/comment/index?data=' + data
				})
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
					this.recordList = (res.data.recordList || []).reverse();
					config.formConf = this.formInfo.formData;
					if (config.formConf) {
						this.dataLog = JSON.parse(config.formConf).dataLog
						if (this.dataLog) this.getOnlineLog(dataId)
					}
					this.hasComment = this.flowInfo.flowNodes.global.hasComment;
					this.formLoding = true;
					// 设置表单标题
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
					if (config.opType != "-1" && config.opType != "3") config.readonly =
						true;
					config.formOperates = [];
					if (config.opType == 0) {
						if (this.properties && this.properties && this.properties.formOperates) config
							.formOperates = this.properties.formOperates || [];
					} else {
						config.formOperates = res.data.formOperates || [];
					}
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
					this.loading = false;
					this.config = config;
				});
			},
			//获取修改记录
			getOnlineLog(dataId) {
				let modelId = this.formInfo.id
				getOnlineLog(modelId, dataId).then(res => {
					let list = res.data.list || []
					//倒序转正
					this.dataLogList = [...list].reverse()
				})
			},
			initBtnList() {
				const list = [];
				const properties = this.properties;
				const btnInfo = this.btnInfo;
				// 流程审批
				if (this.hasComment && this.config.opType != '-1' && this.rightBtnList.length) list.push({
					id: 'comment',
					text: '评论'
				});
				this.actionList = list;
			},
			// 撤销操作

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