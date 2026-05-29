<template>
	<view class="allApp-v">
		<view class="notice-warp">
			<view class="search-box">
				<u-search :placeholder="$t('app.apply.pleaseKeyword')" v-model="keyword" height="72"
					:show-action="false" @change="search" bg-color="#f0f2f6" shape="square">
				</u-search>
			</view>
		</view>
		<mescroll-uni ref="mescrollRef" @init="mescrollInit" @down="downCallback" @up="upCallback" :down="downOption"
			:up="upOption" :bottombar="false" :sticky="false">
			<view>
				<view class="usualList">
					<view class=" caption u-m-b-20">常用流程</view>
					<view class="u-flex u-flex-wrap">
						<view class="item u-flex-col u-col-center" v-for="(item,i) in usualList" :key="i">
							<text class="u-font-40 item-icon" :class="item.icon"
								:style="{'background':item.iconBackground||'#008cff'}" @click="Jump(item)" />
							<text class="u-font-24 u-line-1 item-text">{{item.fullName}}</text>
						</view>
					</view>
				</view>
				<u-sticky>
					<CommonTabs :list="categoryList" @change="change" :current="current" ref="CommonTabs" isBoxShadow>
					</CommonTabs>
				</u-sticky>
				<view class="allList u-m-t-20" v-if="allList.length">
					<view v-for="(item,i) in allList" :key="i">
						<view class="u-flex childList-item ">
							<text class="u-font-40 item-icon" :class="item.icon"
								:style="{'background':item.iconBackground||'#008cff'}" @click="Jump(item)" />
							<text class="item-text u-m-l-28 u-m-r-28 u-line-2">{{item.fullName}}</text>
							<view class="btnBox">
								<u-button :custom-style="customStyle" @click="handelAdd(item)"
									v-if="!item.isCommonFlow">添加
								</u-button>
								<u-button :custom-style="customStyle" type="error" @click="handelDel(item)" v-else>
									移除
								</u-button>
							</view>
						</view>
					</view>
				</view>
				<NoData v-else paddingTop="100"></NoData>
			</view>
		</mescroll-uni>
	</view>
</template>
<script>
	import MescrollMixin from "@/uni_modules/mescroll-uni/components/mescroll-uni/mescroll-mixins.js";
	import CommonTabs from '@/components/CommonTabs'
	import NoData from '@/components/noData'
	import {
		getFlowList,
		getDataList,
		getFlowUsualList,
		setCommonFlow,
		delUsual
	} from '@/api/apply/apply.js'
	import resources from '@/libs/resources.js'
	export default {
		mixins: [MescrollMixin], // 使用mixin
		components: {
			CommonTabs,
			NoData
		},
		props: {
			categoryList: {
				type: Array,
				default () {
					return [];
				}
			},
		},
		data() {
			return {
				usualList: [],
				current: 0,
				customStyle: {
					width: "128rpx",
					fontSize: "24rpx",
					height: '60rpx'
				},
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
						use: false,
						icon: resources.message.nodata,
						tip: this.$t('common.noData'),
						fixed: true,
						top: "860rpx",
					},
					textNoMore: this.$t('app.apply.noMoreData'),
				},
				category: '',
				allList: [],
				type: '1',
				fullName: '',
				keyword: ''
			}
		},
		methods: {
			search() {
				this.searchTimer && clearTimeout(this.searchTimer);
				this.searchTimer = setTimeout(() => {
					this.list = [];
					this.menuList = [];
					this.mescroll.resetUpScroll();
				}, 300);
			},
			init() {
				this.getFlowUsualList()
			},
			getFlowUsualList() {
				let query = {
					category: 'commonFlow',
					flowType: 0
				}
				getFlowUsualList(query).then(res => {
					this.usualList = res.data.list.map(o => {
						const objectData = o.objectData ? JSON.parse(o.objectData) : {}
						return {
							...o,
							...objectData
						}
					})
				})
			},
			upCallback(page) {
				let query = {
					currentPage: page.num,
					pageSize: page.size,
					category: this.category == 0 ? '' : this.category,
					flowType: 0,
					keyword: this.keyword
				}
				getFlowList(query, {
					load: page.num == 1
				}).then(res => {
					this.mescroll.endSuccess(res.data.list.length);
					if (page.num == 1) this.allList = [];
					const list = res.data.list || [];
					this.allList = this.allList.concat(list);
				}).catch(() => {
					this.mescroll.endSuccess(0);
					this.mescroll.endErr();
				})
			},
			Jump(item) {
				const config = {
					id: "",
					flowId: item.id,
					opType: "-1",
				};
				uni.navigateTo({
					url: "/pages/workFlow/flowBefore/index?config=" +
						this.jnpf.base64.encode(JSON.stringify(config))
				});
			},
			handelAdd(item) {

				setCommonFlow(item.id).then(res => {
					this.usualList.push(item)
					item.isCommonFlow = true
					uni.$emit('updateUsualList')
					uni.showToast({
						title: res.msg
					})
				})
			},
			handelDel(item) {
				setCommonFlow(item.id).then(res => {
					item.isCommonFlow = false
					this.getFlowUsualList()
					uni.$emit('updateUsualList')
					uni.showToast({
						title: res.msg
					})
				})
			},
			change(index) {
				this.current = index;
				this.keyword = ""
				this.category = !this.categoryList[index].id ? '' : this.categoryList[index].id
				this.allList = [];
				this.mescroll.resetUpScroll()
			}
		}
	}
</script>

<style lang="scss" scoped>
	.allApp-v {
		.notice-warp {
			height: 114rpx;

			.search-box {
				padding: 20rpx;
			}
		}

		.caption {
			font-size: 36rpx;
			line-height: 80rpx;
			padding: 0 32rpx;

			.tip {
				margin-left: 20rpx;
				font-size: 24rpx;
				color: #c6c6c6;
			}
		}

		.tabs_box {
			width: 100%;

			.sticky {
				width: 750rpx;
				height: 120rpx;
				color: #fff;
				padding-right: 32rpx;
			}
		}

		.usualList {
			margin-top: 4.2rem;
			margin-bottom: 20rpx;
			background-color: #FFFFFF;

			.item {
				margin-bottom: 32rpx;
				width: 25%;

				.item-icon {
					width: 88rpx;
					height: 88rpx;
					margin-bottom: 8rpx;
					line-height: 88rpx;
					text-align: center;
					border-radius: 30rpx;
					color: #fff;
					font-size: 40rpx;
				}

				.item-text {
					width: 100%;
					text-align: center;
					padding: 0 16rpx;
				}
			}
		}

		.allList {
			padding: 0rpx 32rpx 28rpx;
			background-color: #FFFFFF;

			.childList-item {
				align-items: center;
				padding: 28rpx 0 0 0;

				.item-text {
					width: calc(100% - 216rpx);
				}

				.item-icon {
					width: 88rpx;
					height: 88rpx;
					line-height: 88rpx;
					text-align: center;
					border-radius: 30rpx;
					color: #FFFFFF;
					flex-shrink: 0;
					font-size: 40rpx;
				}
			}
		}
	}
</style>