<template>
	<view class="flowStep u-m-t-20">
		<view class="tabsContent">
			<u-tabs :list="getList" :current="current" @change="change"></u-tabs>
			<view class="time-line-box" v-if="opType != '-1'">
				<TimeLine :progressList="progressList" :taskInfo="taskInfo" v-if="tabId == 0"></TimeLine>
				<ProcessComments ref="ProcessComments" v-if="tabId == 1" :taskId="taskId" @handleReply="handleReply">
				</ProcessComments>
				<view class="u-p-l-20 u-p-r-20" v-if="tabId == 2">
					<dataLog :dataLogList="dataLogList"></dataLog>
				</view>
				<assistantMsg v-if="tabId == 3" :auxiliaryInfo="auxiliaryInfo"></assistantMsg>
			</view>
		</view>
	</view>
</template>
<script>
	import TimeLine from './TimeLine.vue'
	import dataLog from '@/components/dataLog'
	import ProcessComments from './ProcessComments.vue'
	import assistantMsg from '@/components/assistantMsg'
	export default {
		components: {
			ProcessComments,
			TimeLine,
			dataLog,
			assistantMsg
		},
		props: {
			dataLogList: {
				type: Array,
				default: () => []
			},
			progressList: {
				type: Array,
				default: () => []
			},
			commentList: {
				type: Array,
				default: () => []
			},
			taskInfo: {
				type: Object,
				default: () => {}
			},
			auxiliaryInfo: {
				type: Array,
				default: () => []
			},
			taskId: {
				type: [String, Number],
				default: ''
			},
			hasComment: {
				default: false
			},
			dataLog: {
				default: false
			},
			formID: {
				type: [String, Number],
				default: ''
			},
			opType: {
				type: [String, Number],
				default: ''
			},
		},
		data() {
			return {
				current: 0,
				value: '',
				popupTitle: '',
				nodeId: '',
				tabId: 0
			}
		},
		computed: {
			baseURL() {
				return this.define.baseURL
			},
			getList() {
				const list = [];
				const OP_TYPE_INVALID = '-1';
				if (this.opType !== OP_TYPE_INVALID) {
					list.push({
						name: '流转',
						id: 0
					});
					if (this.hasComment) {
						list.push({
							name: '评论',
							id: 1
						});
					};
					if (this.dataLog) {
						list.push({
							name: '修改记录',
							id: 2
						})
					}
				}
				if (this.auxiliaryInfo.length) {
					list.push({
						name: '辅助信息',
						id: 3
					});
				}
				return list
			},
		},
		methods: {
			handleReply(id) {
				this.$emit('handleReply', id)
			},
			popupClose() {
				this.$refs.flowStepPopup.close()
			},
			change(e) {
				this.current = e
				this.tabId = this.getList[e].id
				if (this.current === 1) {
					this.$nextTick(() => {
						this.$refs.ProcessComments.getCommentList()
					})
				}
			}
		}
	}
</script>

<style lang="scss">
	.flowStep {
		width: 100%;

		.tabsContent {
			.time-line-box {
				background-color: #fff;
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