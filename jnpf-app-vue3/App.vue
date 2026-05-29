<script>
	import chat from '@/libs/chat.js'
	import {
		getAppVersion
	} from '@/api/common.js'
	import {
		getMessageDetail,
		checkInfo,
	} from '@/api/message.js'
	import {
		useLocale
	} from '@/locale/useLocale';
	import {
		useChatStore
	} from '@/store/modules/chat'
	import {
		useUserStore
	} from '@/store/modules/user'
	const chatStore = useChatStore()

	export default {
		data() {
			return {
				version: 0,
				resVersion: 0,
				modileSystem: 'android',
				Apk: ''
			}
		},
		onLaunch() {
			// #ifndef APP-HARMONY || MP || H5
			/* uniPush */
			// uni.getPushClientId({
			// 	success: (res) => {
			// 		userStore.setCid(res.cid)
			// 		this.handlePush()
			// 	},
			// 	fail(err) {}
			// })
			/* H5+ */
			/* 获取设备信息 */
			uni.getSystemInfo({
				success(res) {
					uni.setStorageSync('systemInfo', res.ua)
				}
			})
			this.handlePush()
			// this.getAppVersion()
			// #endif
			const token = uni.getStorageSync("token");
			if (!token) return
			chat.initSocket()
			const userStore = useUserStore()
			userStore.getCurrentUser().then(res => {
				const {
					initLocale
				} = useLocale();
				initLocale()
			})
		},
		methods: {
			getAppVersion() {
				getAppVersion().then(res => {
					let data = res.data.sysVersion || ''
					const matches = data.match(/(\d+\.\d+\.\d+)/);
					if (matches && matches.length > 0) this.Apk =
						`https://cdn.jnpfsoft.com/apk/Android-java-${matches[0]}.apk`
					data.trim();
					this.version = Number(data.replace(/[^0-9]/ig, ""))
					this.$nextTick(() => {
						this.onUpdate()
					})
				}).catch((err) => {})
			},
			onUpdate() {
				plus.runtime.getProperty(plus.runtime.appid, (wgtinfo) => {
					let resVersion = this.define.sysVersion;
					resVersion.trim();
					this.resVersion = Number(resVersion.replace(/[^0-9]/ig, ""))
					if (this.version != this.resVersion) {
						process.env.NODE_ENV === "production" ?
							uni.setStorageSync('isUpdate', 1) : uni.removeStorageSync('isUpdate')
						uni.showModal({ //提醒用户更新
							title: "立即更新版本",
							success: (res) => {
								if (res.confirm) {
									uni.removeStorageSync('isUpdate')
									let system = plus.os.name;
									if (system === 'Android') {
										// let url = devLanguage ? javaApk : dotNetApk;
										if (!this.Apk) return this.$u.toast('下载链接为空')
										plus.runtime.openURL(this.Apk)
										// uni.downloadFile({
										// 	//下载地址
										// 	url: url,
										// 	success: data => {
										// 		if (data.statusCode === 200) {
										// 			plus.runtime.install(data
										// 				.tempFilePath, {
										// 					force: false
										// 				},
										// 				function() {
										// 					plus.runtime
										// 						.restart();
										// 				});
										// 		}
										// 	}
										// })
									} else {
										plus.runtime.launchApplication({
											action: `itms-apps://itunes.apple.com/cn/app/id${'appleId自行配置'}`
										}, function(e) {});
									}
								} else if (res.cancel) {
									if (this.modileSystem == 'ios') {
										plus.ios.import("UIApplication")
											.sharedApplication()
											.performSelector("exit")
									} else if (this.modileSystem == 'android') {
										plus.runtime.quit();
									}
								}
							}
						})
					}
				})
			},
			// 打开聊天页面
			toIm(item) {
				chatStore.reduceBadgeNum(0)
				uni.navigateTo({
					url: '/pages/message/im/index?name=' + item.realName + '/' + item.account + '&formUserId=' +
						item.formUserId +
						'&headIcon=' +
						item
						.headIcon
				})
			},
			// 打开日程页面
			toSchedule(id) {
				getMessageDetail(id).then(res => {
					chatStore.setMsgInfoNum()
					let bodyText = res.data.bodyText ? JSON.parse(res.data.bodyText) : {};
					if (bodyText.type == 3) return
					let groupId = bodyText.groupId || ''
					uni.navigateTo({
						url: '/pages/workFlow/schedule/detail?groupId=' + groupId +
							'&id=' + bodyText.id
					});
				})
			},
			// 打开流程页面
			toFlow(id) {
				getMessageDetail(id).then(res => {
					chatStore.setMsgInfoNum()
					let bodyText = res.data.bodyText ? JSON.parse(res.data.bodyText) : {};
					if (res.data.flowType == 2) {
						let url = '/pages/my/entrustAgent/index'
						url = bodyText.type == 1 ? url + '?index=1' : url + '?index=2'
						uni.navigateTo({
							url: url
						});
						return
					}
					let config = {
						fullName: res.data.title,
						id: bodyText.operatorId,
						...bodyText
					}
					checkInfo(bodyText.operatorId || config.taskId, config.opType).then(res => {
						config.opType = res.data.opType;
						setTimeout(() => {
							uni.navigateTo({
								url: '/pages/workFlow/flowBefore/index?config=' +
									this.jnpf.base64.encode(JSON.stringify(config))
							});
						}, 300)
					})
				})
			},
			// 处理推送消息
			handlePush() {
				// #ifndef APP-HARMONY || MP || H5
				/* H5+ */
				plus.push.addEventListener(
					'click',
					msg => {
						const messageType = msg.payload.messageType
						// 公告跟系统消息
						if (messageType == 1 || messageType == 3) {
							uni.navigateTo({
								url: '/pages/message/messageDetail/index?id=' + msg.payload.id
							});
							return
						}
						// 流程消息（包括委托）
						if (messageType == 2) {
							return this.toFlow(msg.payload.id)
						}
						// 日程消息
						if (messageType == 4) {
							return this.toSchedule(msg.payload.id)
						}
						// 聊天
						if (messageType == 100) {
							return this.toIm(msg.payload)
						}
					})

				/* uniPush */
				// uni.onPushMessage((res) => {
				// 	// const pages = getCurrentPages();
				// 	// const currentRoute = pages[pages.length - 1].$page.fullPath
				// 	let payload = res.data.payload
				// 	let text = JSON.parse(this.jnpf.base64.decode(payload.text))
				// 	let content = text.type == 1 ? '公告' : text.type == 2 ? '流程' : '聊天'
				// 	let title = text.type == 3 ? text.name : text.title
				// 	if (res.type === 'receive') {
				// 		uni.createPushMessage({
				// 			title,
				// 			content: `你有一条${content}消息`,
				// 			payload,
				// 			icon: './static/logo.png',
				// 			success: (res) => {},
				// 			fail: (err) => {}
				// 		})
				// 	} else {
				// 		if (text.type == 1) {
				// 			uni.navigateTo({
				// 				url: '/pages/message/messageDetail/index?id=' + text.id
				// 			});
				// 		} else if (text.type == 2) {
				// 			this.toFlow(text)
				// 		} else {
				// 			this.toIm(text)
				// 		}
				// 	}
				// })
				// #endif
			},
		}
	}
</script>

<style lang="scss">
	/*每个页面公共css */
	@import "@/uni_modules/vk-uview-ui/index.scss";
	@import "@/assets/iconfont/ym/iconfont.css";
	@import "@/assets/iconfont/custom/iconfont.css";
	@import "@/assets/scss/common.scss";
	@import "@/assets/scss/components.scss";
</style>