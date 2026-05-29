<template>
	<view class="u-p-l-20 u-p-r-20 u-p-t-20">
		<u-time-line>
			<u-time-line-item nodeTop="2" class="u-p-b-20" v-for="item,index in progressList" :key="index">
				<template v-slot:node>
					<view class="u-node" :style="{ background: getTimeLineTagColor(item.nodeStatus) }"></view>
				</template>
				<template v-slot:content>
					<view class="u-font-24 content">
						<view class="u-order-title u-flex u-m-b-8">
							<view class="u-line-1 name" v-if="item.nodeType === 'start' || item.nodeType === 'end'">
								{{item.nodeName}}
							</view>
							<view class="u-line-1 name" v-else>
								{{item.nodeName + getCounterSignContent(item.counterSign)}}
							</view>
							<view class="u-m-l-10">
								<!-- 流程状态 -->
								<u-tag :text="getNodeStatusContent(item.nodeStatus)" mode="light" size="mini"
									shape="circle" :type="getNodeStatusColor(item.nodeStatus)"
									v-if="item.nodeType !=='end' && getNodeStatusContent(item.nodeStatus)" />
								<text
									class="u-m-l-10">{{item.startTime ? $u.timeFormat(item.startTime, 'mm-dd hh:MM'):''}}</text>
							</view>
						</view>
						<view class="u-flex avatar-box" v-if="item.nodeStatus == 1">
							<u-avatar :src="baseURL + taskInfo.headIcon" size="mini" mode="circle"
								class="avatar"></u-avatar>
							<text class="u-m-l-8">发起人：{{taskInfo.creatorUser}}</text>
						</view>
						<view class="u-flex-col approver-list" v-if="item.nodeStatus != 1 && item.approver?.length">
							<view class="u-flex approver-item" @click="openApprover(item)">
								<view class="u-flex approver-list-l">
									<view class="u-flex-col approver-list-l-box" v-for="child,i in item.approver"
										:key="i">
										<u-avatar :src="baseURL + child.headIcon" size="mini" mode="circle"
											class="avatar"></u-avatar>
										<u-tag :text="useDefine.getFlowStateContent(child.handleType)" mode="light"
											v-if="useDefine.getFlowStateContent(child.handleType)" class="tag"
											size="mini"
											:border-color="useDefine.getHexColor(useDefine.getFlowStateColor(child.handleType))"
											:bg-color="useDefine.getHexColor(useDefine.getFlowStateColor(child.handleType))"
											color="#fff" />
										<text class="u-m-t-20 u-line-1 approver-user-name">
											{{child.userName}}
										</text>
									</view>
								</view>
								<view class="u-m-l-20 approver-list-r u-flex">
									<view class="approver-list-r-box">{{item.approverCount}}</view>
									<text class="icon-ym icon-ym-right u-m-r-12"></text>
								</view>
							</view>
							<view class="bottom-block" @click="openTaskLogModal(item)" v-if="item.showTaskFlow">
								<u-tag text="任务流程" type="info" mode="dark" class="u-m-l-10" />
								<text class="icon-ym icon-ym-right u-m-r-12"></text>
							</view>
						</view>
					</view>
				</template>
			</u-time-line-item>
		</u-time-line>
	</view>
	<TimeLinePopup :popupTitle="popupTitle" :recordList="recordList" ref="TimeLinePopup">
	</TimeLinePopup>
	<TaskLogModal ref="TaskLogModal" :taskLogList="taskLogList" />
</template>
<script>
	import {
		recordList,
		taskList
	} from '@/api/workFlow/flowBefore'
	import fileList from './fileList.vue'
	import TimeLinePopup from './TimeLinePopup.vue'
	import TaskLogModal from './TaskLogModal.vue'
	import NoData from '@/components/noData'
	import {
		useDefineSetting
	} from '@/utils/useDefineSetting';
	export default {
		components: {
			fileList,
			NoData,
			TimeLinePopup,
			TaskLogModal
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
				useDefine: useDefineSetting(),
				recordList: [],
				popupTitle: '',
				taskLogList: []
			}
		},
		computed: {
			baseURL() {
				return this.define.baseURL
			}
		},
		methods: {
			getCounterSignContent(counterSign) {
				return counterSign == 0 ? '(或签)' : counterSign == 1 ? '(会签)' : '(依次审批)';
			},
			getNodeStatusColor(status) {
				return status == 1 || status == 2 ? 'success' : status == 3 ? 'error' : 'primary';
			},
			getNodeStatusContent(status) {
				const list = ['', '已提交', '已通过', '已拒绝', '审批中', '已退回', '已撤回', '等待中', '办理中'];
				return list[status] || '';
			},
			/* 任务流程 */
			openTaskLogModal(item) {
				let data = {
					taskId: this.taskInfo.id,
					nodeCode: item.nodeId
				}
				taskList(data).then(res => {
					this.taskLogList = res.data || []
					this.$nextTick(() => {
						this.$refs.TaskLogModal.open()
					})
				})
			},
			openApprover(item) {
				if (item.nodeType === "start" || item.nodeType === "end") return
				this.popupTitle = item.nodeName + this.getCounterSignContent(item.counterSign)
				this.nodeId = item.nodeId
				this.getRecordList()
			},
			getTimeLineTagColor(status) {
				return status == 1 || status == 2 ? '#08AF28' : status != 3 ? '#0177FF' : '#ed6f6f';
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
					this.$nextTick(() => {
						this.$refs.TimeLinePopup.open()
					})
				})
			}
		}
	}
</script>

<style lang="scss">
	.u-node {
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
				// width: 2rem !important;
				// height: 2rem !important;
			}
		}

		.approver-list {
			border-radius: 8rpx;
			min-height: 140rpx;
			background-color: #F5F5F5;

			.approver-item {
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

			.bottom-block {
				height: 80rpx;
				display: flex;
				align-items: center;
				justify-content: space-between;
				border-top: 1rpx solid #e5e5e5;

				:deep(.u-tag) {
					padding: 8rpx;
					background-color: rgb(165, 168, 172);
				}
			}
		}
	}
</style>