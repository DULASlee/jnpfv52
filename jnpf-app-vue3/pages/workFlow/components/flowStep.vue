<template>
	<view class="flowStep u-m-t-20">
		<view class="tabsContent">
			<u-tabs :list="list" :current="current" @change="change"></u-tabs>
			<view class="u-p-l-20 u-p-r-20 time-line-box">
				<view class="u-p-l-20 u-p-r-20 u-p-t-20" v-if="current == 0">
					<TimeLine :progressList="progressList" :taskInfo="taskInfo"></TimeLine>
					<!-- <u-time-line>
						<u-time-line-item nodeTop="2" class="u-p-b-20" v-for="item,index in progressList" :key="index">
							<template v-slot:node>
								<view class="u-node"></view>
							</template>
							<template v-slot:content>
								<view class="u-font-24 content">
									<view class="u-order-title u-flex u-m-b-8">
										<view class="u-line-1 name">
											{{item.nodeName + getCounterSignContent(item.counterSign)}}
										</view>
										<view class="u-m-l-10">
											<u-tag :text="getNodeStatusContent(item.nodeStatus)" mode="light"
												size="mini" shape="circle"
												:type="getNodeStatusColor(item.nodeStatus)" />
											<text
												class="u-m-l-10">{{item.startTime?$u.timeFormat(item.startTime, 'mm-dd hh:MM'):''}}</text>
										</view>
									</view>
									<view class="u-flex avatar-box" v-if="item.nodeStatus == 1">
										<u-avatar :src="baseURL + taskInfo.headIcon" size="mini" mode="circle"
											class="avatar"></u-avatar>
										<text class="u-m-l-8">发起人：{{taskInfo.creatorUser}}</text>
									</view>
									<view class="u-flex approver-list" v-else>
										<view class="u-flex approver-list-l">
											<view class="u-flex-col approver-list-l-box"
												v-for="child,i in item.approver" :key="i">
												<u-avatar :src="baseURL + taskInfo.headIcon" size="mini" mode="circle"
													class="avatar"></u-avatar>
												<u-tag :text="useDefine.getFlowStateContent(child.handleType)"
													mode="light" v-if="useDefine.getFlowStateContent(child.handleType)"
													class="tag" size="mini"
													:border-color="useDefine.getHexColor(useDefine.getFlowStateColor(child.handleType))"
													:bg-color="useDefine.getHexColor(useDefine.getFlowStateColor(child.handleType))"
													color="#fff" />
												<text class="u-m-t-20 u-line-1 approver-user-name">
													{{child.userName}}
												</text>
											</view>
										</view>
										<view class="u-m-l-20 approver-list-r u-flex" @click="openApprover(item)">
											<view class="approver-list-r-box">{{item.approverCount}}</view>
											<text class="icon-ym icon-ym-right u-m-r-12"></text>
										</view>
									</view>
								</view>
							</template>
						</u-time-line-item>
					</u-time-line> -->
				</view>
				<view class="u-p-l-20 u-p-r-20" v-if="current == 1">
					2
				</view>
			</view>
		</view>
	</view>
	<uni-popup ref="flowStepPopup" background-color="#fff" border-radius="8rpx 8rpx 0 0" :is-mask-click="false">
		<view class="flow-popup-content" style="height: 1200rpx;">
			<view class="u-flex head-title">
				<text class="text">{{popupTitle}}</text>
				<text class="text icon-ym icon-ym-fail" @click="popupClose"></text>
			</view>
			<view class="" v-for="item,index in recordList" :key="index" v-if="recordList.length">
				<view class="u-flex u-m-t-20" style="height: 100rpx;">
					<view class="u-flex" style="flex: 1;">
						<u-avatar :src="baseURL + taskInfo.headIcon" size="mini" mode="circle"
							class="avatar"></u-avatar>
						<view class="u-m-l-10">
							<p>{{item.userName}}</p>
							<p style="color: #606266;">
								{{item.handleTime?$u.timeFormat(item.handleTime, 'yyyy-mm-dd hh:MM:ss'):''}}
							</p>
						</view>
					</view>
					<u-tag :text="useDefine.getFlowStateContent(item.handleType)"
						:border-color="useDefine.getHexColor(useDefine.getFlowStateColor(item.handleType))"
						:bg-color="useDefine.getHexColor(useDefine.getFlowStateColor(item.handleType))" color="#fff"
						size="mini" shape="circle" />
					<view class="" v-if="item.handleUserName">
						<u-icon name="arrow-rightward" color="#1890ff" class="u-m-l-10 u-m-r-10"></u-icon>
						<text class="u-font-24" style="color: #303133;">{{item.handleUserName}}</text>
					</view>
				</view>
				<view class="" style="background-color: #F5F5F5;padding: 20rpx;border-radius: 8rpx;color: #303133;"
					v-if="item.signImg || item.fileList.length || item.handleOpinion">
					<text class="u-line-2" v-if="item.handleOpinion">{{item.handleOpinion}}</text>
					<fileList :fileList="item.fileList" v-if="item.fileList.length"></fileList>
					<view class="u-flex" style="height: 88rpx;" v-if="item.signImg">
						<view style="width: 60px;">签名：</view>
						<JnpfSign v-model="item.signImg" align="left" detailed />
					</view>
				</view>
			</view>
			<NoData v-else paddingTop="0" backgroundColor="#fff"></NoData>
		</view>
	</uni-popup>
</template>

<script>
	import NoData from '@/components/noData'
	import fileList from './fileList.vue'
	import TimeLine from './TimeLine.vue'
	import {
		useDefineSetting
	} from '@/utils/useDefineSetting';
	import {
		recordList
	} from '@/api/workFlow/flowBefore'
	const imgTypeList = ['png', 'jpg', 'jpeg', 'bmp', 'gif']
	export default {
		components: {
			fileList,
			NoData,
			TimeLine
		},
		props: {
			progressList: {
				type: Array,
				default: () => []
			},
			taskInfo: {
				type: Object,
				default: () => {}
			}
		},
		data() {
			return {
				list: [{
					name: '流转'
				}, {
					name: '评论'
				}],
				current: 0,
				value: '',
				popupTitle: '',
				recordList: [],
				nodeId: '',
				useDefine: useDefineSetting()
			}
		},
		computed: {
			baseURL() {
				return this.define.baseURL
			}
		},
		methods: {
			openApprover(item) {
				this.popupTitle = item.nodeName + this.getCounterSignContent(item.counterSign)
				this.nodeId = item.nodeId
				this.$nextTick(() => {
					this.$refs.flowStepPopup.open('bottom')
					this.getRecordList()
				})
			},
			getRecordList() {
				let data = {
					taskId: this.taskInfo.id,
					nodeId: this.nodeId
				}
				recordList(data).then(res => {
					let list = res.data || []
					list.map(o => {
						o.fileList = JSON.parse(o.fileList)
					})
					this.recordList = list
				})
			},
			getCounterSignContent(counterSign) {
				return counterSign == 0 ? '(或签)' : counterSign == 1 ? '(会签)' : '(依次审批)';
			},
			popupClose() {
				this.$refs.flowStepPopup.close()
			},
			getNodeStatusContent(status) {
				const list = ['', '已提交', '已通过', '已拒绝', '审批中'];
				return list[status] || '';
			},

			getNodeStatusColor(status) {
				return status == 1 || status == 2 ? 'success' : status == 3 ? 'error' : 'primary';
			},
			change(e) {
				this.current = e
			}
		}
	}
</script>

<style lang="scss">
	.flow-popup-content {
		padding: 20rpx;

		.head-title {

			justify-content: space-between;
			color: #333333;
			// border-bottom: 1rpx solid #f0f0f0;
		}
	}


	.flowStep {
		width: 100%;

		.tabsContent {
			.time-line-box {
				background-color: #fff;

				.u-node {
					background: #19be6b;
					width: 20rpx;
					height: 20rpx;
					border-radius: 50%;
				}

				.active {
					background: #02a7f0;
				}

				.content {
					.u-order-title {
						.name {
							flex: 1;
						}
					}

					.avatar-box {
						::v-deep .u-avatar {
							width: 2rem !important;
							height: 2rem !important;
						}
					}

					.approver-list {
						border-radius: 8rpx;
						height: 140rpx;
						background-color: #F5F5F5;

						.approver-list-l {
							flex: 1;

							.approver-list-l-box {
								width: 120rpx;
								align-items: center;
								position: relative;

								.tag {
									position: absolute;
									top: 50rpx;
								}

								.approver-user-name {
									color: #303133;
									width: 120rpx;
									text-align: center;
								}
							}
						}

						.approver-list-r {
							height: 4.375rem;

							.approver-list-r-box {
								width: 24px;
								height: 24px;
								margin-right: 10px;
								display: flex;
								align-items: center;
								justify-content: center;
								border-radius: 12px;
								background-color: #fff;
							}
						}
					}
				}
			}

			.bottom-btn {
				background-color: #fff;
				width: 100%;
				height: 88rpx;
				padding: 0 20rpx;
				color: #7f7f7f;

				.btn {
					background-color: #f2f2f2;
					border-radius: 8rpx;
					height: 60rpx;
					padding-left: 10rpx;
					width: 100%;
					line-height: 60rpx;
				}
			}
		}
	}

	::v-deep .u-time-axis-item {
		margin-bottom: 0 !important;
	}

	.timeLine {
		width: 100%;
		height: 100%;
		padding: 20rpx;

		.u-time-axis-item {
			.u-time-axis-node {
				top: 0 !important;
			}
		}

		.timeLine-right {
			padding-left: 0;
			padding-right: 40rpx !important;

			&::before {
				left: 670rpx !important;
			}

			.u-time-axis-item {
				.u-time-axis-node {
					left: 670rpx !important;
				}
			}
		}

		.timeLine-dot {
			width: 28rpx;
			height: 28rpx;
			border: 4rpx solid #FFFFFF;
			box-shadow: 0 6rpx 12rpx rgba(2, 7, 28, 0.16);
			border-radius: 50%;
		}

		.timeLine-content {
			padding: 0 10rpx;

			.timeLine-title {
				font-size: 30rpx;
				line-height: 36rpx;

				.name {
					margin-bottom: 6rpx;
				}
			}

			.timeLine-title2 {
				margin-top: 6rpx;
				background: rgba(255, 255, 255, 0.39);
				box-shadow: 0px 3px 10px rgba(0, 0, 0, 0.1);
				padding: 10rpx 20rpx;
				border-radius: 4px;
			}
		}


		.timeLine-desc {
			margin-top: 10rpx;
			font-size: 26rpx;
			line-height: 36rpx;
			color: #909399;
			margin-bottom: 10rpx;
		}
	}
</style>