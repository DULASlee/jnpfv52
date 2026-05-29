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
					:hasComment="hasComment">
				</RecordTimeList>
			</view>
		</view>
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
	</view>
</template>
<script>
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
		updateModel
	} from '@/api/apply/visualDev'
	import {
		createComment
	} from '@/api/workFlow/flowEngine'
	import {
		checkInfo
	} from '@/api/message.js'
	import resources from '@/libs/resources.js'
	import childForm from './form.vue'
	import RecordTimeList from '../components/RecordTimeList'
	import ErrorForm from '../components/ErrorForm'
	import MescrollMixin from "@/uni_modules/mescroll-uni/components/mescroll-uni/mescroll-mixins.js";
	import {
		useUserStore
	} from '@/store/modules/user'
	export default {
		mixins: [MescrollMixin],
		components: {
			childForm,
			ErrorForm,
			RecordTimeList,
			NoData
		},
		data() {
			return {
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
				downOption: {
					use: true,
					auto: true
				},
				upOption: {
					page: {
						num: 0,
						size: 20,
						time: null
					},
					empty: {
						use: true,
						icon: resources.message.nodata,
						tip: this.$t('common.noData'),
						fixed: true,
						top: "300rpx",
					},
					textNoMore: this.$t('app.apply.noMoreData'),
				},
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
				flowStatus: '',
				btnLoading: false,
				commentList: [],
				processId: "",
				candidateList: [],
				summaryType: 0,
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
				hasComment: false,
				progressList: []
			};
		},
		computed: {
			baseURL() {
				return this.define.baseURL
			},
			getShowExtraPanel() {
				return this.config.opType != '-1' && !this.loading
			}
		},
		methods: {
			init(data) {
				this.config = data
				this.config.origin = 'scan'
				this.processId = this.config.id
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
					config.formEnCode = this.formInfo.enCode;
					this.nodeList = res.data.nodeList || [];
					this.properties = res.data.nodeProperties || {};
					this.recordList = (res.data.recordList || []).reverse();
					config.formConf = this.formInfo.formData;
					this.hasComment = this.flowInfo.flowNodes.global.hasComment;
					this.loading = false;
					this.formLoding = true;
					uni.setNavigationBarTitle({
						title: this.flowInfo.fullName,
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
					this.config = config;
				});
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