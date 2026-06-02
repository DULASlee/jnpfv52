<template>
	<view class="index-v">
		<!-- #ifndef MP -->
		<uni-nav-bar class='nav' :fixed="true" :statusBar="true" :border="false" :right-icon="rightIcon"
			@clickRight="scan">
			<!-- 左边插槽 -->
			<template #left>
				<view class="">
					<uni-icons class='icon-ym icon-ym-app-role-toggle' color="#222222" size="20"
						@click="showSelectBox(0)" />
				</view>
			</template>
			<template #default>
				<view class="nav-left">
					<view class="nav-left-text">
						Baobab快速开发平台
					</view>
				</view>
			</template>
		</uni-nav-bar>
		<template v-if="userInfo.appPortalId">
			<view class="portal-select" v-if="columnList.length">
				<view class="u-flex portal-select-inner" @click.stop="showSelectBox(1)">
					<text class="portal-select-text u-line-1">
						{{portalTitle}}
					</text>
					<uni-icons class='right-icons' type="down" color="#000000" size="14"
						v-if="portalList.length > 0 && userInfo.appPortalId"
						:class="{'select-right-icons':showSelect && type ==1}" />
				</view>
			</view>
			<mescroll-body ref="mescrollRef" @init="mescrollInit" @down="downCallback" :down="downOption"
				@up="upCallback" :up="upOption" :bottombar="false" top='10'>
				<view class="portal-v" v-if="authConfig.type==0">
					<template v-if="formData.length">
						<view class="portal-box" v-for="(item,index) in formData" :key="index">
							<portalItem :item='item' ref="portalItem" :key="key" v-if="item.show" />
						</view>
					</template>
					<view v-else class="portal-v portal-nodata">
						<view class="u-flex-col" style="align-items: center;">
							<u-image width="280rpx" height="280rpx" :src="emptyImg1"></u-image>
							<text class="u-m-t-20" style="color: #909399;">{{$t('common.noData')}}</text>
						</view>
					</view>
				</view>
				<template v-if="authConfig.type==1">
					<!-- #ifdef APP -->
					<view v-if="authConfig.linkType==1 && showWebView">
						<web-view :src="authConfig.customUrl"></web-view>
					</view>
					<!-- #endif -->
					<!-- #ifdef H5 -->
					<view v-if="authConfig.linkType==1 && showWebView">
						<web-view :src="authConfig.customUrl" :fullscreen="false"
							style="width: 100%;height: calc(100vh - 300rpx);"></web-view>
					</view>
					<!-- #endif -->
					<view v-else class="portal-v portal-nodata">
						<view class="u-flex-col" style="align-items: center;">
							<u-image width="280rpx" height="280rpx" :src="emptyImg"></u-image>
							<text class="u-m-t-20" style="color: #909399;">当前内容无法在APP上显示，请前往PC门户查看～～</text>
						</view>
					</view>
				</template>
			</mescroll-body>
		</template>
		<view class="portal-v" style="padding-top: 20rpx;" v-else>
			<view class="portal-box">
				<defaultPortal></defaultPortal>
			</view>
		</view>
		<u-popup v-model="showSelect" mode="bottom" class="select-box" height="600rpx" @close="closePopup">
			<view class="search-box" v-if="type == 1">
				<u-search :placeholder="$t('app.apply.pleaseKeyword')" v-model="keyword" height="72"
					:show-action="false" bg-color="#f0f2f6" shape="square" search-icon-color="#909399" />
			</view>
			<view v-for="(item,index) in columnList" :key="index" class="select-item" @click="selectItem(item,index)">
				<text class="u-m-r-12 u-font-30"
					:class="[item.icon,{'currentItem':item.isDefault || item.id === item.appPortalId }]" />
				<text class="item-text sysName"
					:class="{'currentItem':item.isDefault || item.id === item.appPortalId}">{{item.fullName}}</text>
				<u-icon name="checkbox-mark " class="currentItem"
					v-if="item.isDefault || item.id === item.appPortalId"></u-icon>
			</view>

		</u-popup>
		<PasswordPopup @submit="dataFormSubmit" :passwordShow="passwordShow" :formData="baseForm"></PasswordPopup>
		<!-- #endif -->
		<!-- #ifdef MP -->
		<view>
			<web-view :src="mpPortalUrl"></web-view>
		</view>
		<!-- #endif -->
	</view>
</template>
<script>
	var wv; //计划创建的webview
	import {
		PortalList,
		SetPortal
	} from '@/api/portal/portal.js'
	// #ifndef MP
	import MescrollMixin from "@/uni_modules/mescroll-uni/components/mescroll-uni/mescroll-mixins.js"
	import IndexMixin from './mixin.js'
	import portalItem from '@/pages/portal/components/index.vue'
	import defaultPortal from '@/pages/portal/components/defaultPortal.vue'
	import {
		auth
	} from '@/api/portal/portal.js'
	// #endif
	import resources from '@/libs/resources.js'
	import emptyImg from '@/static/image/defPortal.png'
	import PasswordPopup from './components/PasswordPopup'
	import chat from '@/libs/chat.js'
	import {
		useChatStore
	} from '@/store/modules/chat'
	import {
		useUserStore
	} from '@/store/modules/user'
	import {
		getUserOrganizes,
		updatePassword,
		getSystemConfig,
		updatePasswordMessage,
		setMajor
	} from '@/api/common.js'
	import {
		refreshCurrentPage
	} from '@/utils/refreshCurrent.js'
	export default {
		// #ifndef MP
		mixins: [MescrollMixin, IndexMixin],
		// #endif
		components: {
			// #ifndef MP
			portalItem,
			defaultPortal,
			PasswordPopup
			// #endif
		},
		data() {
			return {
				passwordShow: false,
				keyword: '',
				showWebView: true,
				emptyImg: emptyImg,
				emptyImg1: resources.message.nodata,
				rightIcon: '',
				key: +new Date(),
				formData: [],
				portalTitle: '门户',
				statusBarHeight: '',
				showSelect: false,
				selectData: {
					name: '',
					id: ''
				},
				upOption: {
					page: {
						num: 0,
						size: 50,
						time: null
					},
					empty: {
						use: false,
					},
					textNoMore: this.$t('app.apply.noMoreData'),
				},
				portalList: [],
				id: '',
				userInfo: {},
				downOption: {
					use: true,
					auto: true
				},
				authConfig: {},
				token: '',
				mpPortalUrl: '',
				timer: null,
				type: 0,
				baseForm: {
					passwordStrengthLimit: 0,
					passwordLengthMin: false,
					passwordLengthMinNumber: 0,
					containsNumbers: false,
					includeLowercaseLetters: false,
					includeUppercaseLetters: false,
					containsCharacters: false,
					mandatoryModificationOfInitialPassword: 0,
				},
				userOrganizes: '',
				oldUserOrganizes: ''

			};
		},
		onShow() {
			this.token = uni.getStorageSync('token')
			this.mpPortalUrl = this.define.baseURL + '/pages/portal/mpPortal/index?token=' + this.token
			this.userInfo = uni.getStorageSync('userInfo') || {}
			this.showSelect = false
			if (!this.userInfo.appPortalId) return
			// #ifndef MP
			this.getPortalList()
			this.$nextTick(() => {
				this.mescroll.resetUpScroll();
				this.portalList = []
			})
			// #endif
			// #ifdef APP
			this.rightIcon = 'scan'
			// #endif
		},
		onReady() {
			// #ifdef APP
			this.setWebview()
			// #endif
		},
		onLoad(e) {
			// #ifndef MP
			this.token = uni.getStorageSync('token')
			// #endif
			const chatStore = useChatStore()
			if (!chatStore.getSocket) chat && chat.initSocket()
		},
		computed: {
			isPortalListValid() {
				return Array.isArray(this.portalList) && this.portalList.length;
			},
			columnList() {
				return this.portalList.filter((o) => (o.fullName && o.fullName.match(this.keyword)))
			}
		},
		methods: {
			dataFormSubmit(query) {
				updatePassword(query).then((res) => {
					const userStore = useUserStore()
					userStore.logout().then(() => {
						uni.reLaunch({
							url: "/pages/login/index",
						});
					});
				})
			},
			setWebview() {
				if (this.authConfig.linkType == 1) {
					var currentWebview = this.$scope
						.$getAppWebview() //此对象相当于html5plus里的plus.webview.currentWebview()。在uni-app里vue页面直接使用plus.webview.currentWebview()无效
					let height = 0;
					uni.getSystemInfo({
						//成功获取的回调函数，返回值为系统信息
						success: (sysinfo) => {
							height = sysinfo.windowHeight - 50; //自行修改，自己需要的高度 此处如底部有其他内容，可以直接---(-50)这种
						},
						complete: () => {}
					});
					this.$nextTick(() => {
						setTimeout(() => {
							wv = currentWebview.children()[0]
							wv.setStyle({
								top: 80,
								height,
								scalable: true
							})
						}, 500); //如果是页面初始化调用时，需要延时一下
					})
				}
			},
			getSystemConfig() {
				updatePasswordMessage();
				getSystemConfig().then((res) => {
					if (this.userInfo.changePasswordDate == null && res.data
						.mandatoryModificationOfInitialPassword == 1) this.passwordShow = true;
					this.baseForm = res.data;
				});
			},
			upCallback(keyword) {
				auth(this.userInfo.appPortalId).then(res => {
					this.authConfig = res.data || {}
					let data = res.data.formData ? JSON.parse(res.data.formData) : {};
					this.formData = data.layout ? JSON.parse(JSON.stringify(data.layout)) : [];
					this.handelFormData(data);
					this.getSystemConfig()
					if (data.refresh.autoRefresh) {
						this.timer && clearInterval(this.timer);
						this.timer = setInterval(() => {
							uni.$emit('proRefresh')
						}, data.refresh.autoRefreshTime * 60000)
					}
					this.mescroll.endSuccess(this.formData.length);
					this.key = +new Date()
					// #ifdef APP
					this.setWebview()
					// #endif
				}).catch(() => {
					this.formData = []
					this.mescroll.endSuccess(0);
					this.mescroll.endErr();
					this.key = +new Date()
				})
			},
			handelFormData(data) {
				const loop = (list) => {
					list.forEach(o => {
						o.allRefresh = data.refresh
						o.show = false
						if (o.visibility && o.visibility.length && o.visibility.includes('app')) o.show =
							true
						if (o.children && o.children.length) loop(o.children)
					})
					this.key = +new Date()
				}
				loop(this.formData)
				this.dataList = this.formData.filter(o => o.show)
				if (this.dataList.length < 1) {
					this.formData = this.dataList
					this.mescroll.endSuccess(this.dataList.length);
				}
			},
			isJSON(str) {
				try {
					var obj = JSON.parse(str);
					if (typeof obj == 'object' && obj) {
						return true;
					} else {
						return false;
					}
				} catch (e) {
					return false;
				}
			},
			scan() {
				uni.scanCode({
					success: res => {
						let url = ""
						if (this.isJSON(res.result.trim())) {
							const result = JSON.parse(res.result.trim())
							if (result.t === 'ADP') {
								let config = {
									isPreview: 1,
									moduleId: result.id,
									previewType: result.previewType
								}
								url = '/pages/apply/dynamicModel/index?config=' + this.jnpf.base64.encode(JSON
									.stringify(config))
							}
							if (result.t === 'DFD') {
								url = '/pages/apply/dynamicModel/scanForm?config=' + JSON.stringify(result)
							}
							if (result.t === 'WFP') {
								url = '/pages/workFlow/scanForm/index?config=' + JSON.stringify(result)
							}
							if (result.t === 'report') {
								let url_ =
									`${this.report}/preview.html?id=${result.id}&token=${this.token}&page=1&from=menu`
								url = '/pages/apply/externalLink/index?url=' + encodeURIComponent(url_) +
									'&fullName= ' + result.fullName
							}
							if (result.t === 'portal') {
								url = '/pages/portal/scanPortal/index?id=' + result.id
							}
							if (result.t === 'login') {
								url = '/pages/login/scanLogin?id=' + result.id
							}
						} else {
							url = '/pages/my/scanResult/index?result=' + res.result
						}
						uni.navigateTo({
							url,
							fail: (err) => {
								this.$u.toast("暂无此页面")
							}
						})
					}
				});
			},
			getPortalList() {
				PortalList().then(res => {
					let list = res.data.list || [];
					this.portalList = []
					list.map(o => {
						o.children && this.portalList.push(...o.children)
						this.portalList.forEach(o => {
							o.appPortalId = this.userInfo.appPortalId
							if (o.id === o.appPortalId) this.portalTitle = o.fullName
						})
					})
				})
			},
			closePopup() {
				// #ifdef APP
				this.setWebview()
				uni.$emit('showVideo', true)
				this.showWebView = true
				// #endif
			},
			showSelectBox(type) {
				this.type = type;
				if (type === 0) {
					getUserOrganizes().then(res => {
						this.portalList = res.data;
						this.portalList.forEach(o => {
							o.icon = 'icon-ym icon-ym-flow-node-condition';
						});
						if (this.isPortalListValid) this.showSelect = !this.showSelect;
					});
				} else {
					if (this.isPortalListValid) this.showSelect = !this.showSelect;
					// #ifndef MP
					this.getPortalList();
					// #endif
					// #ifdef APP
					uni.$emit('showVideo', false);
					this.showWebView = false;
					this.setWebview();
					// #endif
				}
			},
			getStatusBarHeight() {
				let that = this;
				wx.getSystemInfo({
					success: function(res) {
						that.statusBarHeight = res.statusBarHeight;
					},
				});
			},
			/* 组织切换 */
			selectItem(item, index) {
				if (this.type === 1) {
					this.handleTypeOneSelection(item, index);
				} else {
					this.handleOtherTypeSelection(item);
				}
			},
			handleTypeOneSelection(item, index) {
				SetPortal(item.id).then(res => {
					this.portalTitle = this.portalList[index].fullName;
					this.userInfo.appPortalId = item.id;
					// #ifndef MP
					this.mescroll.triggerDownScroll();
					// #endif
					this.showSelectBox(false);
					uni.setStorageSync('userInfo', this.userInfo);
				});
			},
			handleOtherTypeSelection(item) {
				this.userOrganizes = item.id;
				this.portalList = this.portalList.map(o => {
					o.isDefault = o.id === item.id;
					return o;
				});
				this.changeMajor(item.id, this.majorType);
			},
			changeMajor(majorId, majorType) {
				let query = {
					majorId,
					majorType: 'Organize'
				}
				setMajor(query).then(res => {
					return this.getCurrentUser()
				}).catch(() => {})
			},
			getCurrentUser() {
				const userStore = useUserStore()
				userStore.getCurrentUser().then(() => {
					/* 刷新当前页 */
					refreshCurrentPage();
				})
			}
		}
	}
</script>

<style lang="scss">
	page {
		background-color: #f0f2f6;
	}

	.index-v {
		.portal-v {
			padding: 0 20rpx 20rpx 20rpx;

			.portal-box {
				width: 100%;
				height: 100%;

				.htabs {
					.u-scroll-box {
						height: 80rpx;

						.u-tab-item {
							border-right: 1px solid #e4e7ed;

							&::before {
								content: "";
							}
						}
					}
				}

				.card-v {
					&.u-card {
						margin: 0rpx !important;
						padding: 0rpx !important;
					}
				}
			}
		}

		.nav {
			z-index: 99999;

			::v-deep .uni-navbar__content {
				z-index: 99999;
			}

			::v-deep .uni-navbar__header-container {
				justify-content: center;
			}

			.nav-left {
				max-width: 100%;
				display: flex;
				align-items: center;

				.nav-left-text {
					font-weight: 700;
					font-size: 32rpx;
					flex: 1;
					min-width: 0;
					white-space: nowrap;
					overflow: hidden;
					text-overflow: ellipsis;
				}
			}
		}

		.portal-select {
			background-color: #fff;
			height: 80rpx;
			padding-left: 20rpx;
			line-height: 80rpx;

			.portal-select-inner {
				width: 200rpx;
				height: 100%;

				.portal-select-text {
					color: #303133;
				}
			}

			.right-icons {
				font-weight: 700;
				margin-top: 2px;
				margin-left: 4px;
				transition-duration: 0.3s;
				color: #606266 !important;
			}

			.select-right-icons {
				transform: rotate(-180deg);
			}
		}

		.select-box {
			overflow-y: scroll;

			.search-box {
				height: 112rpx;
				width: 100%;
				padding: 20rpx 20rpx;
				z-index: 10000;
				background: #fff;

				&::after {
					content: " ";
					position: absolute;
					left: 2%;
					top: 62px;
					box-sizing: border-box;
					width: 96%;
					height: 1px;
					transform: scale(1, .3);
					border: 0 solid #e4e7ed;
					z-index: 2;
					border-bottom-width: 1px;
				}
			}

			.currentItem {
				color: #02a7f0;
			}

			.select-item {
				height: 88rpx;
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
					bottom: 0;
					box-sizing: border-box;
					width: 96%;
					height: 1px;
					transform: scale(1, .3);
					border: 0 solid #e4e7ed;
					z-index: 2;
					border-bottom-width: 1px;
				}

				.sysName {
					flex: 1;
					overflow: auto;
					min-width: 0;
					font-size: 28rpx;
				}
			}
		}

		.popup {
			position: absolute;
			top: 244rpx;
			z-index: 99999;
			width: 70%;
			height: 200px;
			border: 1px solid #ccc;
			background-color: #fff;
			left: 283rpx;
			border-radius: 4rpx;
			transform: translate(-50%, -50%) scale(0);
			animation: popup-animation 0.4s ease-in-out forwards;
		}

		.uni-select--mask {
			position: fixed;
			top: 0;
			bottom: 0;
			right: 0;
			left: 0;
			z-index: 2;
		}

		@keyframes popup-animation {
			from {
				transform: translate(-50%, -50%) scale(0);
				opacity: 0;
			}

			to {
				transform: translate(-50%, -50%) scale(1);
				opacity: 1;
			}
		}

		.nav {
			z-index: 99999;

			::v-deep .uni-navbar__content {
				z-index: 99999;
			}

			::v-deep .uni-navbar__header-container {
				justify-content: center;
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
		}
	}

	.portal-nodata {
		position: absolute;
		top: 450rpx;
		width: 100%;
		text-align: center;
		z-index: 100;
		background-color: #f0f2f6;
	}
</style>