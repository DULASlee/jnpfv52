<template>
	<view class="flowLaunch-v">
		<view class="notice-warp">
			<view class="search-box">
				<u-search :placeholder="$t('app.apply.pleaseKeyword')" v-model="keyword" height="72"
					:show-action="false" @change="search" bg-color="#f0f2f6" shape="square" />
			</view>
			<u-tabs :list="entrustList" v-model="current" @change="change" :is-scroll='false' name="fullName">
			</u-tabs>
		</view>
		<mescroll-body ref="mescrollRef" @init="mescrollInit" @down="downCallback" @up="upCallback" :down="downOption"
			:up="upOption" :bottombar="false" top="200">
			<view class="flow-list">
				<view class="flow-list-box">
					<uni-swipe-action ref="swipeAction">
						<uni-swipe-action-item v-for="(item, index) in list" :key="item.id" :threshold="0"
							:right-options="options" @click="handleClick(index)" :disabled="actionItemDisabled(item)">
							<view class="item" :id="'item'+index" ref="mydom" @click="goDetail(item)">
								<view class="item-left">
									<text class="title u-line-1 u-font-24 u-m-b-18">
										{{titleList[current]}}：
										<text
											class="titInner">{{current == '0' || current == '2'  ? item.toUserName : item.userName }}</text>
									</text>
									<text class="title u-line-1 u-font-24 u-m-b-18">
										{{current == 2 ? '代理流程：' :'委托流程：' }}
										<text class="titInner">{{item.flowName ? item.flowName : ''}}</text>
									</text>
									<text class="time title u-font-24 u-m-b-18">
										开始时间：
										<text
											class="titInner">{{item.startTime?$u.timeFormat(item.startTime, 'yyyy-mm-dd hh:MM:ss'):''}}</text>
									</text>
									<text class="time title u-font-24 "
										:class="current == 1 || current == 3 ?'u-m-b-18': ''">
										结束时间：
										<text
											class="titInner">{{item.endTime?$u.timeFormat(item.endTime, 'yyyy-mm-dd hh:MM:ss'):''}}</text>
									</text>
									<view class="time title u-font-24" v-if="current == 1 || current == 3 ">
										确认状态：
										<u-tag
											:type="item.confirmStatus == 0 ? 'info' : item.confirmStatus == 1 ? 'success' : 'error'"
											:text="item.confirmStatus == 0 ? '待确认' : item.confirmStatus == 1 ? '已接受' : '已拒绝'"
											size="mini" />
									</view>
								</view>
								<view class="item-right">
									<image :src="item.entrustStatus.flowStatus" mode="widthFix"
										class="item-right-img" />
								</view>
							</view>
						</uni-swipe-action-item>
					</uni-swipe-action>
				</view>
			</view>
		</mescroll-body>
		<view class="com-addBtn" v-if="current == 0 || current == 2" @click="addPage()">
			<u-icon name="plus" size="48" color="#fff" />
		</view>
	</view>
</template>

<script>
	import resources from '@/libs/resources.js'
	import {
		FlowDelegateList,
		DeleteDelagate,
		getDelegateUser
	} from '@/api/workFlow/entrust.js'
	import {
		getFlowLaunchList,
		delFlowLaunch
	} from '@/api/workFlow/template'
	import MescrollMixin from "@/uni_modules/mescroll-uni/components/mescroll-uni/mescroll-mixins.js";
	import flowlist from '../../workFlow/flowTodo/flowList.vue'
	export default {
		components: {
			flowlist
		},
		mixins: [MescrollMixin],
		data() {
			return {
				keyword: '',
				list: [],
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
						top: "450rpx",
					},
					textNoMore: this.$t('app.apply.noMoreData'),
				},
				entrustList: [{
						fullName: this.$t('app.my.myEntrust')
					},
					{
						fullName: this.$t('app.my.entrustMe')
					},
					{
						fullName: this.$t('app.my.myAgency')
					},
					{
						fullName: this.$t('app.my.agencyMe')
					}
				],
				titleList: ['受委托人', '委托人', '代理人', '被代理人'],
				current: 0,
				options: [{
					text: '删除',
					style: {
						backgroundColor: '#dd524d'
					}
				}]
			}
		},
		onLoad(e) {
			uni.$on('refresh', () => {
				this.list = [];
				this.mescroll.resetUpScroll();
			})
			if (e.index) this.current = Number(e.index)
		},
		onShow() {
			uni.$on('refreshDetail', () => {
				this.list = [];
				this.mescroll.resetUpScroll();
			})
		},
		methods: {
			actionItemDisabled(item) {
				if (this.current == 1 || this.current == 3) return true
				return !(item.status == 0 || item.status == 2)
			},
			addPage() {
				let url = '/entrustAgent/form'
				const data = {
					current: this.current,
					type: this.current == 0 ? 0 : 1
				}
				uni.navigateTo({
					url: `/pages/my${url}?data=${encodeURIComponent(JSON.stringify(data))}`
				})
			},
			handleClick(index) {
				const item = this.list[index]
				DeleteDelagate(item.id).then(res => {
					this.$u.toast(res.msg)
					this.mescroll.resetUpScroll()
				})
			},
			upCallback(page) {
				let query = {
					currentPage: page.num,
					pageSize: page.size,
					keyword: this.keyword,
					type: this.current + 1
				}
				FlowDelegateList(query, {
					load: page.num == 1
				}).then(res => {
					this.mescroll.endSuccess(res.data.list.length);
					if (page.num == 1) this.list = [];
					// #ifndef APP-HARMONY
					const list = res.data.list.map(o => ({
						'entrustStatus': this.getEntrustStatus(o),
						...o
					}))
					// #endif
					// #ifdef APP-HARMONY
					const list = res.data.list.map(o =>
						Object.assign({}, {
							'entrustStatus': this.getEntrustStatus(o)
						}, o)
					);
					// #endif

					this.list = this.list.concat(list);
				}).catch(() => {
					this.mescroll.endErr();
				})
			},
			// 状态
			getEntrustStatus(o) {
				let status = o.status;
				let flowStatus;
				switch (status) {
					case 0: //未生效
						flowStatus = resources.status.notEffective
						break;
					case 1: //生效中
						flowStatus = resources.status.effectuate
						break;
					default: //已失效
						flowStatus = resources.status.expired
						break;
				}
				return {
					flowStatus,
					status
				}
			},
			search() {
				// 节流,避免输入过快多次请求
				this.searchTimer && clearTimeout(this.searchTimer)
				this.searchTimer = setTimeout(() => {
					this.list = [];
					this.mescroll.resetUpScroll();
				}, 300)
			},
			change(index) {
				this.keyword = ''
				this.current = index;
				this.list = [];
				this.search()
			},
			// 详情
			goDetail(item) {
				const data = {
					...item,
					current: this.current,
					type: this.current == 0 ? 0 : 1
				};
				uni.navigateTo({
					url: `/pages/my/entrustAgent/detail?data=${encodeURIComponent(JSON.stringify(data))}`
				});
			}
		}
	}
</script>

<style lang="scss">
	page {
		background-color: #f0f2f6;
	}

	.search_sticky {
		z-index: 990;
		position: sticky;
		background-color: #fff;
	}

	.flowLaunch-v {
		width: 100%;

		.flow-list-box {
			width: 95%;

			.item {
				display: flex;
				width: 100%;
				height: 100%;

				.common-lable {
					font-size: 24rpx;
					border-radius: 8rpx;
					color: #409EFF;
					border: 1px solid #409EFF;
					background-color: #e5f3fe;

				}

				.urgent-lable {
					color: #E6A23C;
					border: 1px solid #E6A23C;
					background-color: #fef6e5;
				}

				.important-lable {
					color: #F56C6C;
					border: 1px solid #F56C6C;
					background-color: #fee5e5;
				}

				.item-right {
					display: flex;
					justify-content: flex-end;
					height: 88rpx;

					.item-right-img {
						width: 102rpx;

					}
				}
			}


		}

		.item-left-top {
			display: flex;
			width: 100%;
			align-items: baseline;

			.common-lable {
				font-size: 24rpx;
				padding: 2rpx 8rpx;

				border-radius: 8rpx;
				color: #409EFF;
				border: 1px solid #409EFF;
				background-color: #e5f3fe;
				// margin-bottom: 16rpx;
			}

			.urgent-lable {
				color: #E6A23C;
				border: 1px solid #E6A23C;
				background-color: #fef6e5;
			}

			.important-lable {
				color: #F56C6C;
				border: 1px solid #F56C6C;
				background-color: #fee5e5;
			}

			.title {
				width: unset;
				flex: 1;
				min-width: 0;
			}
		}
	}
</style>