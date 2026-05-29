<template>
	<uni-popup ref="taskLogPoup" background-color="#fff" border-radius="8rpx 8rpx 0 0" :is-mask-click="false">
		<view class="timeLine-popup-content u-flex-col">
			<view class="u-flex head-title">
				<text class="text">任务流程</text>
				<text class="text icon-ym icon-ym-fail" @click="popupClose"></text>
			</view>
			<view class="content" v-for="item,index in taskLogList" :key="index" v-if="taskLogList.length">
				<view class="u-flex-col content-info-process">
					<view class="info-process-item">
						<view class="u-flex u-m-t-20 u-m-b-20 process-item-head">
							<view class="left">
								<text>触发时间:</text>
								<text>{{item.startTime?$u.timeFormat(item.startTime, 'yyyy-mm-dd hh:MM:ss'):''}}</text>
							</view>
							<u-tag :text="item.isAsync == 1 ? '异步' : '同步'" size="mini"
								:type="item.isAsync == 1 ? 'error' : 'primary'" />
						</view>
						<view class="process-list u-m-t-14">
							<view class="u-flex list u-p-l-12 u-p-r-12" v-for="(child,c) in item.recordList" :key="c">
								<view class="list-left">
									<u-icon :name="child.status === 0 ? 'checkmark-circle-fill' : 'close-circle-fill'"
										:color="child.status === 0 ? '#52c41a' : '#f4420a' "></u-icon>
									<text class="u-m-l-8">{{child.nodeName}}</text>
								</view>
								<text class="u-m-l-8 r-txt" v-if="child.status !== 0"
									@click="jumpErrorPage(child)">查看异常</text>
							</view>
						</view>
					</view>
				</view>
			</view>
			<NoData v-else paddingTop="0" backgroundColor="#fff"></NoData>
		</view>
	</uni-popup>
</template>
<script>
	import NoData from '@/components/noData'
	export default {
		components: {
			NoData
		},
		props: {
			taskLogList: {
				type: Array,
				default: () => []
			}
		},
		data() {
			return {}
		},
		computed: {
			baseURL() {
				return this.define.baseURL
			}
		},
		methods: {
			jumpErrorPage(e) {
				let data = {
					errorData: e.errorData,
					errorTip: e.errorTip
				}
				uni.navigateTo({
					url: '/pages/workFlow/flowBefore/logError?data=' + this.jnpf.base64.encode(JSON.stringify(
							data),
						"UTF-8"),
				})
			},
			open() {
				this.$nextTick(() => {
					this.$refs.taskLogPoup.open('bottom')
				})
			},
			popupClose() {
				this.$refs.taskLogPoup.close()
			}
		}
	}
</script>

<style lang="scss" scoped>
	.timeLine-popup-content {
		padding: 20rpx;
		height: 1200rpx;
		overflow-y: scroll;

		.head-title {
			justify-content: space-between;
			color: #333333;
			/* #ifdef APP-PLUS */
			padding: 20rpx 0;
			/* #endif */
			height: 80rpx;
		}

		.content {
			.content-info-process {
				.info-process-item {
					border-bottom: 1rpx solid #d7d7d7;
					padding-bottom: 26rpx;

					.process-item-head {
						justify-content: space-between;
					}

					.process-list {
						background-color: #f1f4f8;
						border-radius: 8rpx;

						.list {
							height: 68rpx;
							align-items: center;
							border-bottom: 1rpx solid #e7e9ec;
							justify-content: space-between;

							.list-left {}

							.r-txt {
								color: #0000ff;
							}

							&:last-child {
								border-bottom: none
							}
						}
					}
				}
			}
		}
	}
</style>