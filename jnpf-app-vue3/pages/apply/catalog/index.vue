<template>
	<view class="catalog-v">
		<uni-nav-bar class="nav" :fixed="true" :statusBar="true" :border="false" height="44" right-icon="bars"
			@clickRight="clickRight" left-icon="left" @clickLeft="back">
			<!-- 左边插槽 -->
			<block #default>
				<view class="" @click="close"
					style="position: absolute;top: 0;left: 40px;bottom: 0;text-align: center;line-height: 82rpx;height: 100%;width: 40rpx;">
					<u-icon name="close"></u-icon>
				</view>
				<view class="nav-left">
					<view class="nav-left-text">{{ config.fullName }}</view>
				</view>
			</block>
		</uni-nav-bar>
		<view class="search-box_sticky" :style="{'top':topSearch+'rpx'}">
			<view class="search-box">
				<u-search :placeholder="$t('app.apply.pleaseKeyword')" v-model="keyword" height="72"
					:show-action="false" @change="search" bg-color="#f0f2f6" shape="square">
				</u-search>
			</view>
		</view>
		<view class="workFlow-list">
			<view class="part" v-if="childrenData.length">
				<view class="u-flex u-flex-wrap">
					<view class="item u-flex-col u-col-center" v-for="(child, ii) in childrenData" :key="ii"
						@click="handelClick(child)">
						<text class="u-font-40 item-icon" :class="child.icon"
							:style="{ background: child.iconBackground || '#008cff' }" />
						<text class="u-font-24 u-line-1 item-text">{{child.fullName}}</text>
					</view>
				</view>
			</view>
			<NoData v-else></NoData>
		</view>
		<treeCollapse :show="showApply" v-if="showApply" :treeData="menuList" @change="handelClick"></treeCollapse>
	</view>
</template>
<script>
	import NoData from '@/components/noData'
	import treeCollapse from '@/components/treeCollapse'
	import resources from "@/libs/resources.js";
	export default {
		components: {
			NoData,
			treeCollapse
		},
		data() {
			return {
				showApply: false,
				topSearch: 80,
				passwordShow: false,
				keyword: "",
				statusBarHeight: "",
				userInfo: {
					systemIds: [],
				}, //CurrentUser接口中的userInfo数据
				modelId: "",
				config: {},
				fullName: "",
				childrenData: []
			};
		},
		computed: {
			baseURL() {
				return this.define.baseURL;
			},
			token() {
				return uni.getStorageSync('token')
			},
			report() {
				return this.define.report;
			},
			menuList() {
				let list = uni.getStorageSync('menuList').filter(o => o.enCode !== 'workFlow')
				return list
			}
		},
		onLoad(e) {
			this.config = JSON.parse(this.jnpf.base64.decode(e.config)) || {};
			if (Array.isArray(this.config.children) && this.config.children.length) this.childrenData = JSON.parse(JSON
				.stringify(this.config.children))
			this.keyword = ""
			this.handleData(this.childrenData)
			uni.setNavigationBarTitle({
				title: this.config.fullName
			})
			this.getStatusBarHeight();
		},
		methods: {
			close() {
				uni.switchTab({
					url: '/pages/index/apply',
				});
			},
			back() {
				uni.navigateBack({
					delta: 1
				})
			},
			clickRight() {
				this.handleData(this.menuList)
				this.$nextTick(() => {
					this.showApply = !this.showApply
				})
			},
			handleData(childrenData) {
				const loop = (list, parent) => {
					for (let i = 0; i < list.length; i++) {
						let propertyJson = JSON.parse(list[i].propertyJson)
						this.$set(list[i], 'moduleId', propertyJson.moduleId)
						if (list[i].children && Array.isArray(list[i].children)) {
							loop(list[i].children, list[i])
						}
					}
				}
				loop(childrenData)
			},
			getStatusBarHeight() {
				let that = this
				wx.getSystemInfo({
					success(res) {
						that.statusBarHeight = res.statusBarHeight;
					},
				});
				// #ifdef APP-PLUS
				uni.getSystemInfo({
					success(res) {
						that.statusBarHeight = res.statusBarHeight;
						let topSearch = 75 + that.statusBarHeight * 2
						that.topSearch = topSearch
					}
				})
				// #endif
			},
			search() {
				this.searchTimer && clearTimeout(this.searchTimer);
				this.searchTimer = setTimeout(() => {
					if (!this.keyword) return this.childrenData = JSON.parse(JSON.stringify(this.config.children))
					const regex = new RegExp(this.keyword, 'i');
					this.childrenData = this.childrenData.filter(item => regex.test(item.fullName))
				}, 300);
			},
			handelClick(item) {
				this.showApply = false
				this.modelId = item.moduleId
				if (item.type == 1) {
					uni.navigateTo({
						url: "/pages/apply/catalog/index?config=" +
							this.jnpf.base64.encode(JSON.stringify(item)),
						fail: (err) => {
							this.$u.toast("暂无此页面");
						},
					});
					return;
				}
				if (item.type == 2) {
					uni.navigateTo({
						url: item.urlAddress +
							"?menuId=" +
							item.id +
							"&fullName=" +
							item.fullName,
						fail: (err) => {
							this.$u.toast("暂无此页面");
						},
					});
					return;
				}
				if (item.type == 3 || item.type == 9) {
					if (!this.modelId) {
						this.$u.toast("暂无此页面");
						return;
					}
					uni.navigateTo({
						url: "/pages/apply/dynamicModel/index?config=" +
							this.jnpf.base64.encode(JSON.stringify(item)),
						fail: (err) => {
							this.$u.toast("暂无此页面");
						},
					});
				}
				if (item.type == 7 || item.type == 5) {
					let url =
						encodeURIComponent(item.urlAddress) + "&fullName=" + item.fullName;
					if (item.type == 5) {
						url = encodeURIComponent(
							`${this.report}/preview.html?id=${item.moduleId}&token=${this.token}&page=1&from=menu`
						);
					}
					if (!item.urlAddress && item.type == 7) {
						this.$u.toast("暂无此页面");
						return;
					}
					uni.navigateTo({
						url: "/pages/apply/externalLink/index?url=" +
							url +
							"&fullName=" +
							item.fullName +
							"&type=" +
							item.type,
						fail: (err) => {
							this.$u.toast("暂无此页面");
						},
					});
					return;
				}
				if (item.type == 8) {
					if (!item.urlAddress) {
						this.$u.toast("暂无此页面");
						return;
					}
					uni.navigateTo({
						url: "/pages/portal/scanPortal/index?id=" + item.moduleId + "&portalType=1&fullName=" +
							item.fullName,
						fail: (err) => {
							this.$u.toast("暂无此页面");
						},
					});
					return
				}
			}
		},
	};
</script>

<style lang="scss">
	page {
		background-color: #f0f2f6;
	}

	.catalog-v {
		.search-box_sticky {
			margin-bottom: 20rpx;

			.search-box {
				padding: 20rpx;
			}
		}

		.nav {
			z-index: 99999;

			:deep(.uni-navbar__content) {
				z-index: 99999;
			}

			:deep(.uni-navbar__header-container) {
				justify-content: center;
			}
		}

		.nav-left {
			max-width: 100%;
			display: flex;
			align-items: center;

			.nav-left-text {
				font-weight: 700;
				font-size: 29rpx;
				flex: 1;
				min-width: 0;
				white-space: nowrap;
				overflow: hidden;
				text-overflow: ellipsis;
			}
		}

		.select-box,
		.u-drawer {
			max-height: 600rpx;

			:deep(.u-drawer-content) {
				height: 100% !important;
			}

			.currentItem {
				color: #2979ff;
			}
		}

		.search-box {
			overflow-y: overlay;
			height: 112rpx;
			width: 100%;
			padding: 20rpx 20rpx;
			z-index: 10000;
			background: #fff;
		}

		.workFlow-list {
			.part {
				.item {
					margin-bottom: 0;
					padding: 1rem 0;
				}
			}
		}



	}
</style>