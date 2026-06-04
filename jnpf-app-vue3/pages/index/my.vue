<template>
	<view class="my-v" v-if="loading">
		<view class="u-flex user-box u-p-l-20 u-p-r-10 u-p-b-20">
			<view class="u-m-r-10">
				<u-avatar size="127" @click='chooseAvatar' :src='avatarSrc'></u-avatar>
			</view>
			<view class="u-flex-1 f-right" @click="personalPage('/pages/my/personalData/index')">
				<view class="u-font-36 u-m-l-16">{{baseInfo.realName}}</view>
				<view class="u-m-l-10 u-p-10">
					<u-icon name="arrow-right" color="#969799" size="28"></u-icon>
				</view>
			</view>
		</view>
		<view class="u-m-t-20 my-group-box">
			<view class="my-group-box-inner">
				<u-cell-group :border="false" class="cell-group">
					<u-cell-item :title="$t('app.my.organization')"
						@click="openPage('/pages/my/business/index','Organize')" :title-style="titleStyle">
						<template #icon>
							<text class="icon-ym icon-ym-zuzhi u-m-r-16 u-font-32 my-list" />
						</template>
					</u-cell-item>
					<u-cell-item :title="$t('app.my.position')" @click="openPage('/pages/my/business/index','Position')"
						:title-style="titleStyle">
						<template #icon>
							<text class="icon-ym icon-ym-position1 u-m-r-16 u-font-32 my-list" />
						</template>
					</u-cell-item>
					<u-cell-item :title="$t('app.my.subordinates')" @click="openPage('/pages/my/subordinate/index')"
						:title-style="titleStyle">
						<template #icon>
							<text class="icon-ym icon-ym-generator-section u-m-r-16 u-font-32 my-list" />
						</template>
					</u-cell-item>
					<u-cell-item :title="$t('app.my.entrustedAgency')" @click="openPage('/pages/my/entrustAgent/index')"
						:title-style="titleStyle">
						<template #icon>
							<text class="icon-ym icon-ym-header-role-toggle u-m-r-16 u-font-32 my-list" />
						</template>
					</u-cell-item>
					<u-cell-item :title="$t('app.my.switchIdentity')" @click="selectShow = true"
						v-if="userInfo.standingList?.length > 1" :title-style="titleStyle">
						<template #icon>
							<text class="icon-ym icon-ym-header-role-toggle u-m-r-16 u-font-32 my-list" />
						</template>
					</u-cell-item>
					<!-- #ifndef H5 -->
					<u-cell-item :title="$t('app.my.scanCode')" @click="scanCode()" :title-style="titleStyle">
						<template #icon>
							<text class="icon-ym icon-ym-scanCode1 u-m-r-16 u-font-32 my-list" />
						</template>
					</u-cell-item>
					<!-- #endif -->
					<!-- #ifdef APP-PLUS -->
					<u-cell-item :title="$t('app.my.accountSecurity')"
						@click="openPage('/pages/my/accountSecurity/index')" :title-style="titleStyle">
						<template #icon>
							<text class="icon-ym icon-ym-zhanghao u-m-r-16 u-font-32 my-list" />
						</template>
					</u-cell-item>
					<!-- #endif -->
					<u-cell-item :title="$t('app.my.setting')" @click="openPage('/pages/my/settings/index')"
						:title-style="titleStyle" :border-bottom="false">
						<template #icon>
							<text class="icon-ym icon-ym-shezhi u-m-r-16 u-font-32 my-list" />
						</template>
					</u-cell-item>
				</u-cell-group>
			</view>
		</view>
		<view class="u-p-t-20">
			<view class="logout-cell" hover-class="u-cell-hover" @click="logout">{{$t('app.my.logOut')}}</view>
		</view>
		<u-select v-model="selectShow" :list="selectList" mode="single-column" value-name='id' label-name='name'
			:default-value="defaultValue" @confirm="confirm"></u-select>
	</view>
</template>
<script>
	import IndexMixin from './mixin.js'
	import {
		UpdateAvatar,
		UserSettingInfo,
		setMajor
	} from '@/api/common'
	import chat from '@/libs/chat.js'
	import {
		useUserStore
	} from '@/store/modules/user'
	import {
		useChatStore
	} from '@/store/modules/chat'

	export default {
		mixins: [IndexMixin],
		data() {
			return {
				defaultValue: [],
				selectList: [],
				selectShow: false,
				titleStyle: {
					color: '#606266'
				},
				userInfo: '',
				avatarSrc: '',
				baseInfo: {},
				loading: false,
				cellItemColor: ['#6071F5', '#F4A02F', '#2B7FF0', '#4CBF2A']
			}
		},
		computed: {
			baseURL() {
				return this.define.comUploadUrl
			},
			baseURL2() {
				return this.define.baseURL
			},
			token() {
				return uni.getStorageSync('token')
			},
			report() {
				return this.define.report
			}
		},
		onLoad() {
			const chatStore = useChatStore()
			if (!chatStore.getSocket) chat.initSocket()
		},
		onShow() {
			UserSettingInfo().then(res => {
				this.baseInfo = res.data || {}
				this.avatarSrc = this.baseURL2 + this.baseInfo.avatar
				this.loading = true
			})
			this.setStanding()
		},
		methods: {
			confirm(e) {
				if (e[0].index == this.defaultValue[0]) return
				let data = {
					majorId: e[0].value,
					majorType: "Standing",
					menuType: 1,
				}
				setMajor(data).then(res => {
					this.$u.toast(res.msg)
					this.getCurrentUser()
				})
			},
			setStanding() {
				this.selectShow = false
				const raw = uni.getStorageSync('userInfo')
				this.userInfo = (raw && typeof raw === 'object') ? raw : {}
				this.selectList = []
				const standingList = this.userInfo.standingList
				if (!standingList || !Array.isArray(standingList)) return
				try {
					this.selectList = JSON.parse(JSON.stringify(standingList))
				} catch (e) {
					return
				}
				this.selectList.forEach((o, i) => {
					o.id = Number(this.selectList[i].id)
				})
				this.defaultValue = [this.selectList.findIndex(o => o.currentStanding)];
			},
			getCurrentUser() {
				const userStore = useUserStore()
				userStore.getCurrentUser().then(() => {
					this.setStanding()
				})
			},
			chooseAvatar() {
				uni.chooseImage({
					count: 1,
					sizeType: ['original', 'compressed'],
					success: (res) => {
						// #ifdef H5
						let isAccept = new RegExp('image/*').test(res.tempFiles[0].type)
						if (!isAccept) return this.$u.toast(`请上传图片`)
						// #endif
						let tempFilePaths = res.tempFilePaths[0]
						uni.uploadFile({
							url: this.baseURL + 'userAvatar',
							filePath: tempFilePaths,
							name: 'file',
							header: {
								'Authorization': this.token
							},
							success: (uploadFileRes) => {
								let data = JSON.parse(uploadFileRes.data)
								if (data.code === 200) {
									UpdateAvatar(data.data.name).then(res => {
										this.$u.toast('头像更换成功')
										this.avatarSrc = this.baseURL2 + data.data.url
									})
								} else {
									this.$u.toast(data.msg)
								}
							},
							fail: (err) => {
								this.$u.toast('头像更换失败')
							}
						});
					}
				});
			},
			openPage(path, type) {
				if (!path) return
				let url = !!type ? path + '?majorType=' + type : path
				uni.navigateTo({
					url: url
				})
			},
			personalPage(path) {
				if (!path) return
				uni.navigateTo({
					url: path + '?baseInfo=' + encodeURIComponent(JSON
						.stringify(this.baseInfo))
				})
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
			logout() {
				uni.showModal({
					title: '提示',
					content: '确定退出当前账号吗？',
					success: res => {
						if (res.confirm) {
							const userStore = useUserStore()
							userStore.logout().then(() => {
								uni.closeSocket()
								uni.reLaunch({
									url: '/pages/login/index'
								})
							})
							this.removeAccount()
						}
					}
				})
			},
			removeAccount() {
				let model = uni.getStorageSync('rememberAccount')
				if (!model.remember) {
					model.account = ''
					model.password = ''
					model.remember = false
					uni.setStorageSync('rememberAccount', model)
				}
			},
			scanCode() {
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
			}
		}
	}
</script>

<style lang="scss">
	page {
		background-color: #f0f2f6;
	}

	.my-v {
		:deep(.u-cell) {
			height: 112rpx;
			padding: 0;
		}

		.my-group-box {
			.my-group-box-inner {
				background-color: #fff;

				.cell-group {
					padding: 0 20rpx;

					.icon-ym-zuzhi {
						color: #6071F5;
					}

					.icon-ym-position1 {
						color: #2B7FF0;
					}

					.icon-ym-generator-section {
						color: #F4A02F;
					}

					.icon-ym-shezhi {
						color: #4CBF2A;
					}
				}
			}
		}

		.user-box {
			background-color: #fff;
		}

		.logout-cell {
			text-align: center;
			font-size: 32rpx;
			height: 108rpx;
			background-color: #fff;
			color: #D82828;
			line-height: 98rpx;
			font-family: PingFang SC;
		}

		.f-right {
			display: flex;
			flex-direction: row;
			justify-content: space-between;
			align-items: center;
			text-align: center;
		}
	}
</style>