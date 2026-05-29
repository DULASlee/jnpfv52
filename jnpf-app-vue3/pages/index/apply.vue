<template>
	<view class="apply-v">
		<view class="nav-bar-box">
			<uni-nav-bar :fixed="true" :statusBar="true" :border="false" left-icon="bars"
				@clickLeft="showSelectBox('left')" height="44px">
				<block #default>
					<view class="nav-left" @click="showSelectBox('center')">
						<view class="nav-left-text">{{ selectData.name }}</view>
						<uni-icons class="right-icons" type="down" color="#000000" size="14"
							v-if="userInfo.systemIds.length > 1" :class="{ 'select-right-icons': showSelect }" />
					</view>
				</block>
			</uni-nav-bar>
		</view>
		<view class="notice-warp" :style="{'top':topSearch,'height':noticeWarpH+'px'}">
			<view class="search-box">
				<u-search :placeholder="$t('app.apply.pleaseKeyword')" v-model="keyword" height="72"
					:show-action="false" @change="search" bg-color="#f0f2f6" shape="square">
				</u-search>
			</view>
		</view>
		<mescroll-body ref="mescrollRef" @down="downCallback" :down="downOption" :sticky="false" @up="upCallback"
			:up="upOption" :bottombar="false" @init="mescrollInit" :top="mescrollTop">
			<view class="common-block">
				<view class="caption u-flex">
					<text class="caption-left">常用菜单</text>
					<view class="u-flex" @click="openPage">
						<text class="caption-right">{{$t('common.moreText')}}</text>
						<u-icon name="arrow-right" size="24"></u-icon>
					</view>
				</view>
				<view class="u-flex u-flex-wrap">
					<view class="item u-flex-col u-col-center" v-for="(item, i) in usualList" :key="i"
						@click="handelClick(item)">
						<text class="u-font-40 item-icon" :class="item.icon"
							:style="{ background: item.iconBackground || '#008cff' }" />
						<text class="u-font-24 u-line-1 item-text">{{ item.fullName}}</text>
					</view>
					<view class="item u-flex-col u-col-center" @click="moreApp">
						<text class="u-font-40 item-icon more">+</text>
						<text class="u-font-24 u-line-1 item-text">添加</text>
					</view>
				</view>
			</view>
			<view class="workFlow-list">
				<view class="part" v-for="(item, i) in menuList" :key="i">
					<view class="caption u-line-1" v-if="item?.children?.length">
						{{ item.fullName }}
					</view>
					<view class="u-flex u-flex-wrap">
						<view class="item u-flex-col u-col-center" v-for="(child, ii) in item.children" :key="ii"
							@click="handelClick(child)">
							<text class="u-font-40 item-icon" :class="child.icon"
								:style="{ background: child.iconBackground || '#008cff' }" />
							<text class="u-font-24 u-line-1 item-text">{{child.fullName}}</text>
						</view>
					</view>
				</view>
			</view>
		</mescroll-body>
		<uni-popup ref="popup" background-color="#fff" @change="popupChange">
			<!-- #ifdef MP -->
			<view :style="{ 'margin-top': statusBarHeight + 226 + 'rpx' }"></view>
			<!-- #endif -->
			<!-- #ifndef MP -->
			<view :style="{ 'margin-top': statusBarHeight + 220+ 'rpx' }"></view>
			<!-- #endif -->
			<view class="notice-warp" :style="{'top':topSearch,'height':noticeWarpH+'px'}">
				<view class="search-box">
					<u-search :placeholder="$t('app.apply.pleaseKeyword')" v-model="systemKeyword" height="72"
						:show-action="false" @change="onSearchInput" bg-color="#f0f2f6" shape="square">
					</u-search>
				</view>
			</view>
			<view class="item popupItem">
				<view v-for="(item, index) in filteredList" :key="index" class="select-item"
					@click="selectItem(item, index)" v-if="filteredList.length">
					<text class="u-m-r-12 u-font-40"
						:class="[item.icon, { currentItem: item.id === userInfo.appSystemId }]" />
					<text class="item-text sysName"
						:class="{ currentItem: item.id === userInfo.appSystemId }">{{ item.name }}</text>
					<u-icon name="checkbox-mark " class="currentItem" v-if="item.id === userInfo.appSystemId"></u-icon>
				</view>
			</view>
		</uni-popup>
		<treeCollapse :show="showApply" v-if="showApply" :treeData="menuList" @change="handelClick">
			<!-- #ifdef APP -->
			<view :style="{ 'margin-top': statusBarHeight + 40 + 'rpx' }"></view>
			<!-- #endif -->
		</treeCollapse>
	</view>
</template>
<script>
	import {
		getMenuList,
		getUsualList,
		getChildList
	} from "@/api/apply/apply.js";
	import treeCollapse from '@/components/treeCollapse'
	import {
		setMajor,
		updatePassword,
		getSystemConfig,
	} from "@/api/common";
	import resources from "@/libs/resources.js";
	import MescrollMixin from "@/uni_modules/mescroll-uni/components/mescroll-uni/mescroll-mixins.js";
	import IndexMixin from "./mixin.js";
	import {
		useUserStore
	} from '@/store/modules/user'
	import {
		useChatStore
	} from '@/store/modules/chat'
	export default {
		mixins: [MescrollMixin, IndexMixin],
		components: {
			treeCollapse
		},
		data() {
			return {
				mescrollTop: 198,
				systemKeyword: '',
				showApply: false,
				topSearch: '80px',
				appTopHeight: 0,
				usualList: [],
				menuList: [],
				downOption: {
					use: true,
					auto: true,
				},
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
				statusBarHeight: "",
				userInfo: {
					systemIds: [],
				},
				showSelect: false,
				selectData: {
					name: "",
					id: "",
				},
				modelId: "",
				fullName: "",
				key: +new Date(),
				listChild: [],
				height: 0,
				noticeWarpH: 0
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
			hasChildren() {
				let hasChildren = false
				for (let i = 0; i < this.menuList.length; i++) {
					if (this.menuList[i].children && this.menuList[i].children.length) {
						hasChildren = true
						break
					}
				}
				return hasChildren
			},
			filteredList() {
				return this.userInfo.systemIds.filter(item => {
					return item.name.includes(this.systemKeyword);
				});
			}
		},
		onShow() {
			this.keyword = "";
			this.systemKeyword = ''
			this.$nextTick(() => {
				this.mescroll.resetUpScroll();
			})
		},
		mounted() {
			this.getContentHeight()
		},
		onLoad() {
			uni.$on('updateUsualList', data => {
				this.getUsualList()
			})
			uni.$on('refresh', () => {
				this.menuList = [];
				this.mescroll.resetUpScroll();
			});
		},
		onUnload() {
			uni.$off("updateUsualList");
		},
		methods: {
			async getContentHeight() {
				const windowHeight = this.$u.sys().windowHeight;

				// 获取元素尺寸
				const [navBarHeight, noticeWarpRect, searchBoxRect, stickyBoxTabsRect] = await Promise.all([
					this.$uGetRect('.nav-bar-box'),
					this.$uGetRect('.notice-warp'),
					this.$uGetRect('.search-box')
				]);

				// 计算高度
				const appTopHeight = navBarHeight.height;
				const searchBoxHeight = searchBoxRect.height;

				// 设置组件数据
				this.topSearch = `${appTopHeight}px`;
				this.appTopHeight = appTopHeight;
				this.noticeWarpH = `${searchBoxHeight}`;
				this.mescrollTop = appTopHeight + searchBoxHeight;

				// #ifdef MP
				this.mescrollTop = 54 + searchBoxHeight;
				// #endif
			},
			popupChange(e) {
				this.showSelect = e.show
			},
			showSelectBox(type) {
				if (type === 'center' && this.userInfo.systemIds.length > 1) {
					if (this.showApply) this.showApply = false
					if (this.showSelect) this.$refs.popup.close();
					this.$refs.popup.open('top')
				}
				if (type === 'left' && this.menuList.length) {
					if (this.showSelect) this.$refs.popup.close()
					this.showApply = !this.showApply;
				}
			},
			handelClick(item) {
				this.showApply = false
				if (item.type == 1) {
					getChildList(item.id).then(res => {
						this.listChild = res.data || []
						this.handleProperty(this.listChild)
						this.$nextTick(() => {
							uni.navigateTo({
								url: "/pages/apply/catalog/index?config=" +
									this.jnpf.base64.encode(JSON.stringify(this.listChild[0])),
								fail: (err) => {
									this.$u.toast("暂无此页面");
								},
							});
						})
					})
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
					this.modelId = item.moduleId;
					if (!item.moduleId) {
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
			},
			openPage() {
				uni.navigateTo({
					url: '/pages/commonPage/morePage/index?type=2',
				});
			},
			onSearchInput(value) {
				this.systemKeyword = value;
			},
			initSysList(res) {
				this.userInfo = res;
				if (this.userInfo.systemIds && this.userInfo.systemIds.length) {
					this.userInfo.systemIds.forEach((item) => {
						if (item.id == this.userInfo.appSystemId) this.selectData = item
					});
				}
				getSystemConfig().then((res) => {
					this.baseForm = res.data;
				});
			},
			processObject(obj) {
				const objectDataParsed = obj.objectData ? JSON.parse(obj.objectData) : {};
				const propertyJsonParsed = JSON.parse(obj.propertyJson);
				const moduleId = propertyJsonParsed.moduleId || "";
				this.$set(obj, "moduleId", moduleId);
				return {
					...obj,
					...objectDataParsed,
				};
			},
			getUsualList() {
				getUsualList(2).then((res) => {
					// 使用 map 方法处理 res.data.list 中的每个对象
					let list = res.data.list.map(o => (this.processObject(o))).slice(0, 11);
					// 更新 usualList
					this.usualList = [...list];
				});
			},
			upCallback(keyword) {
				let query = {
					keyword: this.keyword,
				};
				uni.showLoading({
					title: '正在加载',
					mask: true
				})
				const userStore = useUserStore()
				userStore.getCurrentUser().then((res) => {
					this.initSysList(res);
					this.getUsualList();
					getMenuList(query)
						.then((res) => {
							let list = res.data.list || [];
							this.mescroll.endSuccess(list.length);
							this.list = list.filter(o => o.children && o.children.length)
							this.menuList = this.list;
							this.handleProperty(this.list)
							uni.hideLoading()
							this.key = +new Date();
							this.mescroll.endSuccess(this.menuList.length, false);
						}).catch(() => {
							this.mescroll.endSuccess(0);
							this.mescroll.endErr();
						});
				});
			},
			handleProperty(list) {
				const loop = (par) => {
					par.map(o => {
						if (o?.propertyJson) {
							let propertyJson = JSON.parse(o.propertyJson);
							this.$set(o, "iconBackground", propertyJson.iconBackgroundColor || "");
							this.$set(o, "moduleId", propertyJson.moduleId || "");
						}
						if (o?.children && o?.children?.length) loop(o.children)
					})
				}
				loop(list)
			},
			search() {
				this.searchTimer && clearTimeout(this.searchTimer);
				this.searchTimer = setTimeout(() => {
					this.list = [];
					this.menuList = [];
					this.mescroll.resetUpScroll();
				}, 300);
			},
			moreApp() {
				uni.navigateTo({
					url: "/pages/commonPage/allApp/index?type=2",
				});
			},
			selectItem(item, index) {
				if (item.id === this.userInfo.appSystemId) return
				let query = {
					majorId: item.id,
					majorType: "System",
					menuType: 1,
				};
				setMajor(query).then((res) => {
					if (res.code == 200) {
						this.changeSelData(item, index);
						this.keyword = "";
						this.systemKeyword = "";
						this.$u.toast(res.msg);
						this.$nextTick(() => {
							this.mescroll.resetUpScroll();
						})
					}
				}).catch((err) => {
					this.$u.toast(err);
					setTimeout(() => {
						this.$nextTick(() => {
							this.mescroll.resetUpScroll();
						})
					}, 1200)
				});
			},
			changeSelData(item, index) {
				this.selectData = item
				this.userInfo.appSystemId = item.id
				this.$refs.popup.close()
			},
			getFullName(item) {
				if (!item.enCode) return item.name;
				return this.$t('routes.' + item.enCode.replace(/\./g, '-'));
			}
		},
	};
</script>

<style lang="scss">
	page {
		background-color: #f0f2f6;
	}

	.apply-v {
		.notice-warp {
			width: 100%;

			.search-box {
				height: 120rpx;
				padding: 0rpx 20rpx;
				display: flex;
				align-items: center;
			}
		}

		.common-block {
			background-color: #fff;
			margin: 20rpx 0;

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
				margin-bottom: 32rpx;
				width: 25%;

				.item-icon {
					width: 88rpx;
					height: 88rpx;
					margin-bottom: 8rpx;
					line-height: 82rpx;
					text-align: center;
					border-radius: 30rpx;
					color: #fff;
					font-size: 40rpx;

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

		.select-box {
			max-height: 600rpx;

			:deep(.u-drawer-content) {
				height: 100% !important;
			}
		}

		.popupItem {
			height: 400rpx;
			overflow-y: scroll;
			/* #ifdef APP-HARMONY */
			padding-top: 40rpx;
			/* #endif */
		}

		.item {
			.currentItem {
				color: #2979ff;
			}

			.select-item {
				height: 100rpx;
				display: flex;
				align-items: center;
				padding: 0 20rpx;
				font-size: 30rpx;
				color: #303133;
				text-align: left;
				position: relative;

				&::after {
					content: " ";
					position: absolute;
					left: 2%;
					top: 0;
					box-sizing: border-box;
					width: 96%;
					height: 1px;
					transform: scale(1, 0.3);
					border: 0 solid #e4e7ed;
					z-index: 2;
					border-bottom-width: 1px;
				}

				.sysName {
					flex: 1;
					overflow: auto;
					min-width: 0;
				}
			}
		}
	}
</style>