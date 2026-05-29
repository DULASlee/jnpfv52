<template>
	<view class="all-apply-v">
		<view class="notice-warp">
			<view class="search-box">
				<u-search :placeholder="$t('app.apply.pleaseKeyword')" v-model="keyword" height="72"
					:show-action="false" @change="search" bg-color="#f0f2f6" shape="square" />
			</view>
			<view>
				<CommonTabs :list="tabsMenuList" @change="change" :current="current" ref="CommonTabs">
				</CommonTabs>
			</view>
		</view>
		<mescroll-uni @init="mescrollInit" @down="downCallback" @up="upCallback" :down="downOption" :up="upOption"
			top='220'>
			<view class="workFlow-list">
				<view class="part" v-for="(item, i) in menuList" :key="i" v-if=" menuList.length && hasChildren">
					<view class="caption u-line-1">
						{{ item.fullName }}
					</view>
					<view class="u-flex u-flex-wrap">
						<view class="item u-flex-col u-col-center" v-for="(child, ii) in item.children" :key="ii"
							@click="handleClick(child)">
							<text class="u-font-40 item-icon" :class="child.icon"
								:style="{ background: child.iconBackground || '#008cff' }" />
							<text class="u-font-24 u-line-1 item-text">{{child.fullName}}</text>
						</view>
					</view>
				</view>
				<NoData v-else :paddingTop="400"></NoData>
			</view>
		</mescroll-uni>
	</view>
</template>
<script>
	import {
		getAppDataList
	} from "@/api/apply/apply.js";
	import NoData from '@/components/noData'
	import resources from "@/libs/resources.js";
	import CommonTabs from '@/components/CommonTabs'
	import MescrollMixin from "@/uni_modules/mescroll-uni/components/mescroll-uni/mescroll-mixins.js";
	export default {
		mixins: [MescrollMixin],
		components: {
			NoData,
			CommonTabs
		},
		data() {
			return {
				showPopup: false,
				topSearch: 80,
				current: 0,
				usualList: [],
				tabsMenuList: [],
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
						use: false,
						icon: resources.message.nodata,
						tip: "暂无数据",
						fixed: true,
						top: "560rpx",
					},
					textNoMore: "",
				},
				keyword: "",
				userInfo: {
					systemIds: [],
				}, //CurrentUser接口中的userInfo数据
				modelId: "",
				config: {},
				key: +new Date()
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
			}
		},
		created() {
			uni.$on('refresh', () => {
				this.menuList = [];
				this.current = 0;
				this.mescroll.resetUpScroll();
			});
		},
		methods: {
			classifyItem(index) {
				this.showPopup = false
				this.change(index)
			},
			upCallback(keyword) {
				let query = {
					keyword: this.keyword,
					type: 2
				};
				uni.showLoading({
					title: '正在加载',
					mask: true
				})
				getAppDataList(query).then(res => {
					let list = res.data.list || [];
					if (!list.length) this.current = 0
					this.tabsMenuList = [{
						fullName: "全部功能"
					}];
					this.mescroll.endSuccess(list.length);
					for (let i = 0; i < list.length; i++) {
						let children = list[i].children;
						let tabsMenuList = {
							fullName: list[i].fullName,
						};
						this.tabsMenuList.push(tabsMenuList);
						if (Array.isArray(children) && children.length) {
							for (let j = 0; j < children.length; j++) {
								let iconBackground = "",
									moduleId = "";
								if (children[j].propertyJson) {
									let propertyJson = JSON.parse(children[j].propertyJson);
									iconBackground = propertyJson.iconBackgroundColor || "";
									moduleId = propertyJson.moduleId || "";
								}
								this.$set(children[j], "iconBackground", iconBackground);
								this.$set(children[j], "moduleId", moduleId);
							}
						}
					}
					this.list = JSON.parse(JSON.stringify(list));
					if (this.current === 0) {
						let allApp = [{
							fullName: '全部功能',
							children: this.jnpf.treeToArray(this.list).filter(o => !o.hasChildren),
							id: 0
						}]
						this.menuList = allApp
					} else {
						this.menuList = this.list
					}
					uni.hideLoading()
					this.key = +new Date();
					this.mescroll.endSuccess(this.menuList.length, false);
				}).catch(() => {
					this.mescroll.endSuccess(0);
					this.mescroll.endErr();
				});
			},
			change(index) {
				this.current = index;
				this.menuList = this.list;
				if (this.current === 0) {
					let allApp = [{
						fullName: '全部功能',
						children: this.jnpf.treeToArray(this.list).filter(o => !o.hasChildren),
						id: 0
					}]
					this.menuList = allApp
				} else {
					this.menuList = [this.list[index - 1]] || [];
				}
			},
			search() {
				this.searchTimer && clearTimeout(this.searchTimer);
				this.searchTimer = setTimeout(() => {
					this.list = [];
					this.menuList = [];
					this.tabsMenuList = [];
					this.current = 0
					this.mescroll.resetUpScroll();
				}, 300);
			},
			handleClick(item) {
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
				if (item.type == 3) {
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
			}
		},
	};
</script>

<style lang="scss" scoped>
	page {
		background-color: #f0f2f6;
	}

	.all-apply-v {
		height: 100%;

		.notice-warp {
			height: 3.59rem !important;
			text-align: left;

			.search-box {
				padding: 20rpx;
			}
		}

		.commonTabs-box {
			height: 2.8rem;
		}
	}
</style>