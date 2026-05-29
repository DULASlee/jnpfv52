<template>
	<uni-popup ref="flowStepPopup" background-color="#fff" border-radius="8rpx 8rpx 0 0" :is-mask-click="false">
		<view class="timeLine-popup-content u-flex-col">
			<view class="u-flex head-title">
				<text class="text">{{popupTitle}}</text>
				<text class="text icon-ym icon-ym-fail" @click="popupClose"></text>
			</view>
			<view class="content" v-for="item,index in recordList" :key="index" v-if="recordList.length">
				<!--头部 -->
				<view class="u-flex u-m-t-20 content-info">
					<view class="u-flex content-info-left">
						<u-avatar :src="baseURL + item.headIcon" size="mini" mode="circle" class="avatar"></u-avatar>
						<view class="u-m-l-10 name-box">
							<p>{{item.userName}}</p>
							<p class="name">
								{{item.handleTime?$u.timeFormat(item.handleTime, 'yyyy-mm-dd hh:MM:ss'):''}}
							</p>
						</view>
					</view>
					<u-tag :text="useDefine.getFlowStateContent(item.handleType)"
						:border-color="useDefine.getHexColor(useDefine.getFlowStateColor(item.handleType))"
						:bg-color="useDefine.getHexColor(useDefine.getFlowStateColor(item.handleType))" color="#fff"
						size="mini" shape="circle" />
					<view class="content-info-right u-line-1" v-if="item.handleUserName">
						<u-icon name="arrow-rightward" color="#1890ff" class="u-m-l-10 u-m-r-10"></u-icon>
						<text class="u-font-24 txt u-line-1">{{item.handleUserName}}</text>
					</view>
				</view>
				<view class="content-info-bottom" v-if="item.signImg || item.fileList?.length || item.handleOpinion">
					<text class="u-line-2" v-if="item.handleOpinion">{{item.handleOpinion}}</text>
					<fileList :fileList="item.fileList" v-if="item?.fileList?.length"></fileList>
					<view class="u-flex sign-box" v-if="item.signImg">
						<view class="sign-title">签名：</view>
						<JnpfSign v-model="item.signImg" align="left" detailed />
					</view>
					<view class="approvalField" v-for="(field,index) in item.approvalField" :key="index">
						<text>{{field.fieldName+'：'}}</text>
						<text>{{field.value}}</text>
					</view>
				</view>
			</view>
			<NoData v-else paddingTop="0" backgroundColor="#fff"></NoData>
		</view>
	</uni-popup>
</template>

<script>
	import fileList from './fileList.vue'
	import NoData from '@/components/noData'
	import {
		useDefineSetting
	} from '@/utils/useDefineSetting';
	export default {
		components: {
			NoData,
			fileList
		},
		props: {
			recordList: {
				type: Array,
				default: () => []
			},
			popupTitle: {
				type: String,
				default: ''
			}
		},
		data() {
			return {
				useDefine: useDefineSetting(),
			}
		},
		computed: {
			baseURL() {
				return this.define.baseURL
			}
		},
		methods: {
			open() {
				this.$nextTick(() => {
					this.$refs.flowStepPopup.open('bottom')
				})
			},
			popupClose() {
				this.$refs.flowStepPopup.close()
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
		}

		.content {
			.content-info {
				height: 100rpx;

				.content-info-left {
					flex: 1;

					.name-box {
						.name {
							color: #606266;
						}
					}
				}

				.content-info-right {
					max-width: 228rpx;

					.txt {
						color: #303133;
					}
				}
			}

			.content-info-bottom {
				background-color: #F5F5F5;
				padding: 20rpx;
				border-radius: 8rpx;
				color: #303133;

				.approvalField {
					padding: 10rpx 0;
				}

				.sign-box {
					height: 88rpx;

					.sign-title {
						width: 120rpx;
					}
				}
			}

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
						}
					}
				}
			}
		}
	}
</style>