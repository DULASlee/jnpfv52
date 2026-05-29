<template>
	<view class="workFlow-v">
		<view class="workFlow-nodata" v-show="!workflowEnabled">
			<view class="u-flex-col" style="align-items: center;">
				<u-image width="280rpx" height="280rpx" :src="emptyImg"></u-image>
				<text class="u-m-t-20" style="color: #909399;">该应用协同办公未开启</text>
			</view>
		</view>
		<view class="notice-warp" v-show="workflowEnabled" :style="{'height':noticeWarpH+'px'}">
			<view class="search-box">
				<u-search :placeholder="$t('app.apply.pleaseKeyword')" v-model="keyword" height="72"
					:show-action="false" @change="search" bg-color="#f0f2f6" shape="square" />
			</view>
			<view class="head-tabs u-flex">
				<view class="head-tabs-item" @click="openPage('/pages/workFlow/flowTodo/index')">
					<text class="icon-ym icon-ym-flowTodo-app u-m-r-4 icon-style" />
					<text class="u-font-24 head-tabs-name">审批</text>
					<u-badge type="error" class="badge" :count="count" :absolute="true" :offset="offset">
					</u-badge>
				</view>
				<u-line color="#EEF0F4" style="flex: none;height: 34%;" direction="col" />
				<view class="head-tabs-item" @click="openPage('/pages/workFlow/schedule/index')">
					<text class="icon-ym icon-ym-flowDone-app u-m-r-4 icon-style" />
					<text class="u-font-24 head-tabs-name">日程</text>
				</view>
				<u-line color="#EEF0F4" style="flex: none;height: 34%;" direction="col" />
				<view class="head-tabs-item" @click="openPage('/pages/workFlow/document/index')">
					<text class="icon-ym icon-ym-flowCopy-app u-m-r-4 icon-style" />
					<text class="u-font-24 head-tabs-name">文档</text>
				</view>
			</view>
		</view>
		<mescroll-body ref="mescrollRef" @down="downCallback" :down="downOption" :sticky="false" @up="upCallback"
			:up="upOption" :bottombar="false" @init="mescrollInit" v-show="workflowEnabled" :top='mescrollTop'>
			<view class="common-block">
				<view class="caption u-flex">
					<text class="caption-left">常用流程</text>
					<view class="u-flex" @click="openPage('/pages/commonPage/morePage/index?type=1')">
						<text class="caption-right">{{$t('common.moreText')}}</text>
						<u-icon name="arrow-right" size="24"></u-icon>
					</view>
				</view>
				<view class="u-flex u-flex-wrap">
					<view class="item u-flex-col u-col-center" v-for="(item, i) in usualList" :key="i"
						@click="Jump(item, 1)">
						<text class="u-font-40 item-icon" :class="item.icon"
							:style="{ background: item.iconBackground || '#008cff' }" />
						<text class="u-font-24 u-line-1 item-text">{{ item.fullName }}</text>
					</view>
					<view class="item u-flex-col u-col-center" @click="moreApp">
						<text class="u-font-40 item-icon more">+</text>
						<text class="u-font-24 u-line-1 item-text">添加</text>
					</view>
				</view>
			</view>
			<view>
				<CommonTabs :list="categoryList" @change="change" :current="current" ref="CommonTabs"></CommonTabs>
			</view>
			<view class="workFlow-list u-m-t-20">
				<view class="part">
					<view class="caption u-line-1" v-if="list.length >= 1">
						{{ current === 0 ? "全部流程" : fullName }}
					</view>
					<view class="u-flex u-flex-wrap">
						<view class="item u-flex-col u-col-center" v-for="(child, ii) in list" :key="ii"
							@click="Jump(child)">
							<text class="u-font-40 item-icon" :class="child.icon"
								:style="{ background: child.iconBackground || '#008cff' }" />
							<text class="u-font-24 u-line-1 item-text">{{ child.fullName }}</text>
						</view>
					</view>
				</view>
			</view>
		</mescroll-body>
		<MultSelect :show="show" :list="selector" @confirm="confirm" @close="show = false" isFlow />
	</view>
</template>
<script>
	import {
		getFlowSelector,
		getFlowTodoCount
	} from "@/api/workFlow/flowEngine";
	import MultSelect from '@/components/MultSelect'
	import CommonTabs from '@/components/CommonTabs'
	import {
		getFlowUsualList
	} from "@/api/apply/apply.js";
	import resources from "@/libs/resources.js";
	import MescrollMixin from "@/uni_modules/mescroll-uni/components/mescroll-uni/mescroll-mixins.js";
	import IndexMixin from "./mixin.js";
	import {
		useUserStore
	} from '@/store/modules/user'
	import {
		useBaseStore
	} from '@/store/modules/base'
	const baseStore = useBaseStore()
	export default {
		mixins: [MescrollMixin, IndexMixin],
		components: {
			MultSelect,
			CommonTabs
		},
		data() {
			return {
				selector: [],
				show: false,
				activeFlow: {},
				templateList: [],
				count: 0,
				offset: [-25, 60],
				usualList: [],
				downOption: {
					use: true,
					auto: true,
				},
				className: "",
				emptyImg: resources.message.nodata,
				upOption: {
					page: {
						num: 0,
						size: 50,
						time: null,
					},
					empty: {
						use: true,
						icon: resources.message.nodata,
						tip: this.$t('common.noData'),
						fixed: false,
						top: "560rpx",
					},
					textNoMore: this.$t('app.apply.noMoreData'),
				},
				keyword: "",
				category: "",
				current: 0,
				categoryList: [],
				list: [],
				fullName: "",
				loading: false,
				selectFlowValue: 0,
				enCode: "",
				workflowEnabled: false,
				noticeWarpH: 120,
				mescrollTop: 120
			};
		},
		onLoad() {
			uni.showLoading()
			const userStore = useUserStore()
			userStore.getCurrentUser().then((res) => {
				const userInfo = uni.getStorageSync('userInfo') || {}
				uni.hideLoading()
				this.workflowEnabled = !!userInfo.workflowEnabled
				if (!this.workflowEnabled) return
				uni.$on("updateUsualList", (data) => {
					this.getFlowUsualList();
				});
				this.getPaymentMethodOptions();
				this.getContentHeight()
			})
		},
		onUnload() {
			uni.$off("updateUsualList");
		},
		onShow() {
			this.keyword = ""
			const userInfo = uni.getStorageSync('userInfo') || {}
			this.workflowEnabled = !!userInfo.workflowEnabled
			this.$nextTick(() => {
				this.list = [];
				this.mescroll.resetUpScroll();
				if (!this.workflowEnabled) return
				this.setFlowTodoCount()
				this.getPaymentMethodOptions();
			})
		},
		onHide() {
			this.restTabs()
		},
		methods: {
			async getContentHeight() {
				const windowHeight = this.$u.sys().windowHeight;

				// 获取元素尺寸
				const [headTabs, searchBox] = await Promise.all([
					this.$uGetRect('.head-tabs'),
					this.$uGetRect('.search-box')

				]);
				this.noticeWarpH = headTabs.height + searchBox.height
				this.mescrollTop = this.noticeWarpH + 10
			},
			restTabs() {
				this.current = 0;
				this.category = ""
			},
			change(i) {
				this.list = [];
				this.current = i
				this.category = this.categoryList[i].id || "";
				this.mescroll.resetUpScroll();
			},
			setFlowTodoCount() {
				const query = {
					flowCirculateType: [],
					flowDoneType: [],
					toBeReviewedType: [],
				}
				getFlowTodoCount(query).then((res) => {
					this.count = res.data.toBeReviewed || 0;
				})
			},
			openPage(path) {
				if (!path) return;
				this.restTabs()
				uni.navigateTo({
					url: path,
				});
			},
			upCallback(page) {
				this.$nextTick(() => {
					this.getFlowUsualList();
				});
				let query = {
					currentPage: page.num,
					pageSize: page.size,
					keyword: this.keyword,
					category: this.category == 0 ? "" : this.category,
					flowType: 0,
				};
				this.loading = false;
				getFlowSelector(query, {
					load: page.num == 1,
				}).then((res) => {
					let resData = res.data.list || [];
					this.mescroll.endSuccess(resData.length);
					if (page.num == 1) this.list = [];
					const list = resData.map((o) => ({
						show: false,
						...o,
					}));
					this.list = this.list.concat(list);
					this.loading = true;
				}).catch(() => {
					this.mescroll.endErr();
				});
			},
			search() {
				this.searchTimer && clearTimeout(this.searchTimer);
				this.searchTimer = setTimeout(() => {
					this.list = [];
					this.mescroll.resetUpScroll();
				}, 300);
			},
			//获取常用
			getFlowUsualList() {
				let query = {
					category: 'commonFlow',
					flowType: 0
				}
				getFlowUsualList(query).then((res) => {
					let list = res.data.list.map((o) => {
						const objectData = o.objectData ? JSON.parse(o.objectData) : {};
						return {
							...o,
							...objectData,
						};
					});
					this.usualList = [...list].slice(0, 11)
				});
			},
			getPaymentMethodOptions() {
				baseStore.getDictionaryData({
					sort: "businessType",
				}).then((res) => {
					const firstItem = {
						fullName: "全部流程",
						id: ''
					}
					this.categoryList = [firstItem, ...(res || [])]
				});
			},
			moreApp() {
				uni.navigateTo({
					url: "/pages/commonPage/allApp/index?categoryList=" +
						encodeURIComponent(JSON.stringify(this.categoryList)),
				});
			},
			confirm(e) {
				this.activeFlow = e[0];
				this.Jump();
			},
			Jump(item) {
				const config = {
					id: "",
					flowId: item.id,
					opType: "-1",
					isFlow: 1
				};
				uni.navigateTo({
					url: "/pages/workFlow/flowBefore/index?config=" +
						this.jnpf.base64.encode(JSON.stringify(config))
				});
			}
		},
	};
</script>

<style lang="scss">
	page {
		background-color: #f0f2f6;
	}

	.workFlow-v {


		.common-block {
			background: #fff;
			margin-bottom: 20rpx;

			.caption {
				padding: 0 32rpx;
				line-height: 100rpx;
				justify-content: space-between;

				.caption-left {
					font-size: 36rpx;
					font-weight: bold;
				}

				.caption-right {}
			}

			.item {
				// margin-bottom: 32rpx;
				padding: 1rem 0;
				width: 25%;

				.item-icon {
					width: 88rpx;
					height: 88rpx;
					margin-bottom: 8rpx;
					line-height: 88rpx;
					text-align: center;
					border-radius: 20rpx;
					color: #fff;
					font-size: 56rpx;

					&.more {
						background: #ececec;
						color: #666666;
						font-size: 50rpx;
					}
				}

				.item-text {
					width: 100%;
					text-align: center;
					padding: 0 16rpx;
				}
			}
		}

		.notice-warp {
			height: 210rpx;

			.search-box {
				padding: 20rpx;
			}
		}

		.head-tabs {
			width: 100%;
			padding: 0 32rpx;
			height: 100rpx;
			justify-content: space-between;

			.head-tabs-item {
				width: 25%;
				display: flex;
				justify-content: center;
				font-size: 28rpx;
				color: #303133;
				flex-shrink: 0;
				position: relative;
				align-items: center;

				.icon-style {
					font-size: 46rpx;
					color: #666666;
				}

				.head-tabs-name {
					color: #303133;
					font-family: PingFang SC;
				}
			}
		}

		.workFlow-nodata {
			position: absolute;
			top: 450rpx;
			width: 100%;
			text-align: center;
			z-index: 100;
			background-color: #f0f2f6;
		}
	}
</style>