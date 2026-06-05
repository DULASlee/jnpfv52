import define from '@/utils/define'
import jnpf from '@/utils/jnpf'
import {
	useChatStoreWithOut
} from '@/store/modules/chat'
import {
	useUserStore
} from '@/store/modules/user'

const chatStore = useChatStoreWithOut()
const userStore = useUserStore()
let socketTask = false
const Socket = {
	conTime: 0,
	initSocket() {
		try {
			const token = uni.getStorageSync('token') || ''
			const sys = uni.getStorageSync('systemInfo') || ''
			const userInfo = uni.getStorageSync('userInfo') || {}
			socketTask = uni.connectSocket({
				url: define.webSocketUrl + '/' + encodeURIComponent(token),
				// #ifdef APP
				header: {
					'User-Agent': sys
				},
				// #endif
				success() {
					setTimeout(() => chatStore.setSocket(socketTask), 0)
				},
			});
			socketTask.onMessage((res) => {
				let dataStr = res.data;
				const data = JSON.parse(dataStr)
				let options = {
					cover: false,
					sound: 'system',
					title: data.title
				};
				switch (data.method) {
					case "initMessage": //初始化
						const msgInfo = {
							messageText: data.messageDefaultText || '暂无数据',
							messageCount: data.unreadMessageCount + data.unreadSystemMessageCount < 0 ?
								0 : data.unreadMessageCount + data.unreadSystemMessageCount + data
								.unreadScheduleCount,
							messageDate: data.messageDefaultTime || 0,
						}
						let badgeNum = data.unreadTotalCount
						for (let i = 0; i < data.unreadNums.length; i++) {
							badgeNum = badgeNum + data.unreadNums[i].unreadNum
						}
						chatStore.setBadgeNum(badgeNum)
						chatStore.setMsgInfo(msgInfo)
						break;
					case "Online": //在线用户

						break;
					case "Offline": //离线用户

						break;
					case "sendMessage": //发送消息
						chatStore.sendMessage(data)
						break;
					case "receiveMessage": //接收消息
						// #ifdef APP
						plus.push.createMessage('你有一条聊天消息', {
							...data,
							messageType: 100
						}, options);
						// #endif
						chatStore.receiveMessage(data)
						break;
					case "messageList": //消息列表
						chatStore.getMessageList(data)
						break;
					case "messagePush": //消息推送
						// #ifdef APP
						let content = "公告"
						if (data.messageType == 2) content = '流程'
						if (data.messageType == 3) content = '系统'
						if (data.messageType == 4) content = '日程'
						plus.push.createMessage(`你有一条${content}消息`, data, options);
						// #endif
						chatStore.messagePush(data)
						break;
					case "closeSocket": //断开websocket连接
						Socket.close()
						break;
					case "logout":
						uni.showToast({
							title: data.msg || '登录已过期',
							icon: 'none',
							complete: () => {
								setTimeout(() => {
									userStore.resetToken()
									setTimeout(() => {
										Socket.close()
										uni.reLaunch({
											url: '/pages/login/index'
										})
									}, 500)
								}, 1000)
							}
						})
						break;
					default:
						break;
				}
			})

			socketTask.onOpen((data) => {
				Socket.conTime = 0
				const msg = JSON.stringify({
					method: "OnConnection",
					token,
					mobileDevice: true,
					systemId: userInfo.appSystemId
				});
				Socket.sendMsg(msg)
			})

			socketTask.onClose((data) => {
				socketTask = false
				chatStore.setSocket(null)
			})

			socketTask.onError((data) => {
				chatStore.setSocket(null)
				setTimeout(() => {
					Socket.conTime += 1
					if (Socket.conTime <= 10) {
						if (Socket.conTime >= 3) {
							uni.showToast({
								title: 'IM通讯正在连接:' + '连接第' + Socket.conTime + '次！稍后...',
								icon: 'none'
							})
						}
						Socket.reConnect();
					} else {
						uni.showToast({
							title: 'IM通讯连接失败，联系服务器管理员',
							icon: 'none'
						})
					}
				}, 10)
			})
		} catch (e) {}
	},
	sendMsg(data) {
		if (socketTask === false) return Socket.reConnect()
		let content = data;
		socketTask.send({
			data: content,
			complete(e) {}
		})
	},
	//重连
	reConnect() {
		Socket.initSocket()
	},
	close() {
		socketTask.close({
			complete(e) {
				socketTask = false
				chatStore.setSocket(null)
			}
		})
	}
};

export default Socket