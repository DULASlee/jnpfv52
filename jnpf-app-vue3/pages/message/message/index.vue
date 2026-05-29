<template>
	<view class="message-v">
		<view class="nav-bar-box">
			<uni-nav-bar :fixed="true" :statusBar="true" :border="false" @clickLeft="back" left-icon="left"
				height="44px">
				<block #default>
					<view class="u-flex slot-wrap">
						<view class="title">站内消息</view>
						<view class="nav-icon u-m-l-10" @click="readAll">
							<text class="icon-ym icon-ym-clean" />
						</view>
					</view>
				</block>
			</uni-nav-bar>
		</view>
		<view class="notice-warp" :style="{'top':topSearch,'height':noticeWarpH+'px'}">
			<view class="search-box ">
				<u-search :placeholder="$t('app.apply.pleaseKeyword')" v-model="keyword" height="72"
					:show-action="false" @change="search" bg-color="#f0f2f6" shape="square">
				</u-search>
			</view>
			<view class="sticky-box-tabs">
				<view class="tabs-box">
					<u-tabs class="u-tab-box" :list="tablist" :current="current" @change="tabChange" :offset="offset">
					</u-tabs>
				</view>
				<view class="status-box">
					<view class="status-icon" @click="showAction = true">
						<uni-icons type="bottom" size="16" color="#3C3C3C"></uni-icons>
					</view>
				</view>
			</view>
		</view>
		<mescroll-body ref="mescrollRef" @init="mescrollInit" @down="downCallback" @up="upCallback" :down="downOption"
			:up="upOption" :bottombar="false" :top="mescrollTop">
			<view class="message-list">
				<view class="u-flex message-item u-border-bottom " v-for="(item, i) in list" :key="i"
					@click="detail(item)">
					<view class="message-item-img message-item-icon u-flex u-row-center"
						:class="{'message-item-icon-flow':item.type == 2,'message-notice-icon':item.type == 3,'message-schedule':item.type == 4}">
						<text class="icon-ym icon-ym-xitong" v-if="item.type == 1" />
						<text class="icon-ym icon-ym-generator-notice" v-else-if="item.type == 3" />
						<text class="icon-ym icon-ym-portal-schedule" v-else-if="item.type == 4" />
						<text class="icon-ym icon-ym-generator-flow" v-else />
						<text class="redDot" v-if="!item.isRead"></text>
					</view>
					<view class="message-item-txt">
						<view class="message-item-title u-flex">
							<text class="title u-line-1">{{item.title}}</text>
						</view>
						<view class="u-flex u-row-between message-item-cell">
							<text>{{item.releaseUser}}</text>
							<text
								class="u-font-24">{{item.releaseTime?$u.timeFormat(item.releaseTime, 'mm-dd hh:MM'):''}}</text>
						</view>
					</view>
				</view>
			</view>
		</mescroll-body>
		<u-action-sheet :list="statusOptions" v-model="showAction" @click="handleClick"></u-action-sheet>
	</view>
</template>

<script>
	import {
		getMessageList,
		getMessageDetail,
		checkInfo,
		getUnReadMsgNum,
		MessageAllRead
	} from '@/api/message.js'
	import resources from '@/libs/resources.js'
	import MescrollMixin from "@/uni_modules/mescroll-uni/components/mescroll-uni/mescroll-mixins.js";
	import {
		useChatStore
	} from '@/store/modules/chat'
	const chatStore = useChatStore()
	export default {
		mixins: [MescrollMixin],
		data() {
			return {
				mescrollTop: 326,
				topSearch: '80px',
				appTopHeight: 0,
				offset: [5, 8],
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
						fixed: false,
						top: "640rpx",
					},
					textNoMore: this.$t('app.apply.noMoreData'),
				},
				keyword: '',
				type: "",
				list: [],
				current: 0,
				tablist: [{
					name: '全部',
					count: 0
				}, {
					name: '系统',
					count: 0
				}, {
					name: '流程',
					count: 0
				}, {
					name: '公告',
					count: 0
				}, {
					name: '日程',
					count: 0
				}],
				status: '未读',
				isRead: 0,
				statusOptions: [{
					text: '全部'
				}, {
					text: '未读'
				}, {
					text: '已读'
				}],
				showAction: false,
				noticeWarpH: 0
			}
		},
		onLoad(option) {
			this.getUnReadMsgNum()
		},
		mounted() {
			this.getContentHeight()
		},
		methods: {
			back() {
				uni.navigateBack()
			},
			upCallback(page) {
				let query = {
					currentPage: page.num,
					pageSize: page.size,
					keyword: this.keyword,
					type: this.type,
					isRead: this.isRead
				}
				getMessageList(query, {
					load: page.num == 1
				}).then(res => {
					this.mescroll.endSuccess(res.data.list.length);
					if (page.num == 1) this.list = [];
					const list = res.data.list;
					this.list = this.list.concat(list);
				}).catch(() => {
					this.mescroll.endErr();
				})
			},
			getUnReadMsgNum() {
				getUnReadMsgNum().then(res => {
					const data = res.data
					for (var i = 0; i < this.tablist.length; i++) {
						const item = this.tablist[i]
						if (item.name == '全部') item.count = data.unReadNum
						if (item.name == '系统') item.count = data.unReadSystemMsg
						if (item.name == '流程') item.count = data.unReadMsg
						if (item.name == '公告') item.count = data.unReadNotice
						if (item.name == '日程') item.count = data.unReadSchedule
					}
					chatStore.setMsgInfoNum(Number(data.unReadNum))
				})
			},
			search() {
				this.searchTimer && clearTimeout(this.searchTimer)
				this.searchTimer = setTimeout(() => {
					this.list = [];
					this.mescroll.resetUpScroll();
				}, 300)
			},
			tabChange(e) {
				this.current = e
				if (e == 0) this.type = ''
				if (e == 1) this.type = 3
				if (e == 2) this.type = 2
				if (e == 3) this.type = 1
				if (e == 4) this.type = 4
				this.list = [];
				this.mescroll.resetUpScroll();
			},
			handleClick(index) {
				if (index == 0) {
					this.status = '全部'
					this.isRead = ''
				} else if (index == 1) {
					this.status = '未读'
					this.isRead = 0
				} else {
					this.status = '已读'
					this.isRead = 1
				}
				this.list = [];
				this.mescroll.resetUpScroll();
			},
			async getContentHeight() {
				const windowHeight = this.$u.sys().windowHeight;

				// 获取元素尺寸
				const [navBarHeight, noticeWarpRect, searchBoxRect, stickyBoxTabsRect] = await Promise.all([
					this.$uGetRect('.nav-bar-box'),
					this.$uGetRect('.notice-warp'),
					this.$uGetRect('.search-box'),
					this.$uGetRect('.sticky-box-tabs')
				]);

				// 计算高度
				const appTopHeight = navBarHeight.height;
				const searchBoxHeight = searchBoxRect.height;
				const stickyBoxTabsHeight = stickyBoxTabsRect.height;

				// 设置组件数据
				this.topSearch = `${appTopHeight}px`;
				this.appTopHeight = appTopHeight;
				this.noticeWarpH = searchBoxHeight + stickyBoxTabsHeight;
				this.mescrollTop = appTopHeight + searchBoxHeight + stickyBoxTabsHeight + 10;
			},
			readAll() {
				const query = {
					keyword: this.keyword,
					type: this.type,
					isRead: this.isRead
				}
				MessageAllRead(query).then(res => {
					if (this.isRead === 0) {
						this.list = [];
						this.mescroll.resetUpScroll();
					} else {
						for (let i = 0; i < this.list.length; i++) {
							this.$set(this.list[i], 'isRead', '1')
						}
					}
					this.getUnReadMsgNum()
					uni.showToast({
						title: res.msg,
						icon: 'none'
					});
				})
			},
			detail(item) {
				if (item.type == '1' || item.type == '3') {
					if (!item.isRead) {
						item.isRead = 1
						chatStore.setMsgInfoNum()
						uni.$on('initUnReadMsgNum', () => {
							this.getUnReadMsgNum()
						})
					}
					uni.navigateTo({
						url: '/pages/message/messageDetail/index?id=' + item.id
					});
				} else {
					getMessageDetail(item.id).then(res => {
						if (!item.isRead) {
							item.isRead = 1
							chatStore.setMsgInfoNum()
							this.$nextTick(() => {
								this.getUnReadMsgNum()
							})
						}
						let data = res.data || {};
						let bodyText = data.bodyText ? JSON.parse(data.bodyText) : {};
						if (item.type == 4) {
							if (bodyText.type == 3) return
							let groupId = bodyText.groupId || ''
							uni.navigateTo({
								url: `/pages/workFlow/schedule/detail?groupId=${groupId}&id=${bodyText.id}`
							});
							return
						}
						let config = {
							id: data.id,
							flowId: bodyText.flowId,
							opType: bodyText.opType,
							taskId: bodyText.taskId,
							operatorId: bodyText.operatorId,
						}
						if (item.flowType == 1) {
							checkInfo(config.operatorId || config.taskId, config.opType).then(res => {
								config.opType = res.data.opType;
								setTimeout(() => {
									uni.navigateTo({
										url: '/pages/workFlow/flowBefore/index?config=' +
											this.jnpf.base64.encode(JSON.stringify(config))
									});
								}, 300)
							})
						} else {
							if (bodyText.type == 0) return
							let url = '/pages/my/entrustAgent/index'
							let i = bodyText.type == 1 ? 0 : bodyText.type == 2 ? 1 : bodyText.type == 3 ? 2 : 3
							setTimeout(() => {
								uni.navigateTo({
									url: url + '?index=' + i
								});
							}, 300)
						}
					})
				}
			}
		}
	}
</script>

<style lang="scss">
	page {
		background-color: #f0f2f6;
	}

	.message-v {

		height: 100%;

		:deep(.u-border-bottom):after {
			border-bottom-width: 0px
		}

		.slot-wrap {
			.title {
				font-size: 32rpx;
				font-weight: bold;
			}

			.nav-icon {
				width: 44rpx;
				height: 44rpx;
				background: rgb(240, 242, 246);
				border-radius: 50%;
				display: flex;
				align-items: center;
				justify-content: center;

				text {
					font-size: 32rpx;
				}
			}
		}

		.message-schedule {
			background-color: #77f !important;
		}

		.sticky-box {
			height: 100%;
			position: sticky;
			z-index: 100;
		}

		.sticky-box-tabs {
			width: 100%;
			display: flex;
			flex-direction: row;
			background-color: #fff;
			align-items: center;

			.tabs-box {
				width: 90%;
			}

			.status-box {
				width: 10%;
				text-align: center;
				padding: 28rpx 18rpx;

				.status-title {
					flex-shrink: 0;
					color: #999;
					font-size: 28rpx;
				}

				.status-icon {
					width: 100%;
					align-items: center;
					font-size: 24rpx;
				}

				.status-input {
					flex: 1;
				}
			}
		}

		.u-tab-box {}

		.message-list {
			padding: 0 20rpx;
			background-color: #fff;

			.message-item {
				height: 132rpx;

				.message-item-icon-flow {
					background-color: #33CC51 !important;
				}

				.message-notice-icon {
					background-color: #e09f0c !important;
				}

				.message-item-img {
					width: 96rpx;
					height: 96rpx;
					margin-right: 16rpx;
					flex-shrink: 0;
					border-radius: 50%;
					background-color: #3B87F7;
					position: relative;

					.icon-ym {
						color: #fff;
						font-size: 50rpx;
					}

					.redDot {
						height: 16rpx;
						width: 16rpx;
						border-radius: 50%;
						background: #FE5146;
						display: inline-block;
						// margin-right: 6rpx;
						flex-shrink: 0;
						position: absolute;
						right: 2rpx;
						top: 2rpx;
					}
				}

				.message-item-txt {
					width: calc(100% - 112rpx);

					.message-item-title {
						line-height: 46rpx;
						margin-bottom: 6rpx;

						.title {
							font-size: 28rpx;
						}
					}

					.message-item-cell {
						color: #909399;
						font-size: 24rpx;
					}
				}
			}
		}
	}
</style>