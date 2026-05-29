import {
	defineStore
} from 'pinia';
import store from '../index'

export const useChatStore = defineStore({
	id: ' chat',
	state: () => ({
		socket: null,
		badgeNum: 0,
		msgInfo: {},
		formUserId: ''
	}),
	getters: {
		getSocket() {
			return this.socket
		},
		getBadgeNum() {
			return this.badgeNum
		},
		getMsgInfo() {
			return this.msgInfo
		}
	},
	actions: {
		setSocket(socket) {
			this.socket = socket
		},
		setBadgeNum(badgeNum) {
			this.badgeNum = badgeNum
		},
		addBadgeNum(num) {
			this.badgeNum += num
		},
		reduceBadgeNum(num) {
			let badgeNum = this.badgeNum - num
			if (badgeNum < 0) badgeNum = 0
			this.badgeNum = badgeNum
		},
		setMsgInfo(msgInfo) {
			this.msgInfo = msgInfo
		},
		setMsgInfoNum(num) {
			if (num || num === 0) {
				this.msgInfo.messageCount = num
				this.msgInfo.count = num
				this.badgeNum = num
				return
			}
			this.msgInfo.messageCount -= 1
			this.msgInfo.count = this.msgInfo.messageCount
			let badgeNum = this.badgeNum - 1
			if (badgeNum < 0) badgeNum = 0
			this.badgeNum = badgeNum
		},
		setFormUserId(formUserId) {
			this.formUserId = formUserId
		},
		sendMessage(data) {
			const item = {
				account: data.toAccount,
				headIcon: data.toHeadIcon,
				id: data.toUserId,
				latestDate: data.latestDate,
				latestMessage: data.toMessage,
				messageType: data.messageType,
				realName: data.toRealName,
				unreadMessage: 0
			}
			const addItem = {
				sendUserId: data.UserId,
				contentType: data.messageType,
				content: data.toMessage,
				sendTime: data.dateTime,
				method: data.method
			}
			uni.$emit('addMsg', addItem)
			uni.$emit('updateList', item)
		},
		receiveMessage(data) {
			if (this.formUserId === data.formUserId) {
				data.unreadMessage = 0
				const item = {
					sendUserId: data.formUserId,
					contentType: data.messageType,
					content: data.formMessage,
					sendTime: data.dateTime,
					method: data.method
				}
				uni.$emit('addMsg', item)
			} else {
				data.unreadMessage = 1
				this.addBadgeNum(1)
			}
			data.id = data.formUserId
			data.latestMessage = data.formMessage
			uni.$emit('updateList', data)
		},
		getMessageList(data) {
			uni.$emit('getMessageList', data)
		},
		messagePush(data) {
			this.msgInfo.messageText = data.title;
			this.msgInfo.messageCount += data.unreadNoticeCount;
			this.msgInfo.messageDate = data.messageDefaultTime
			this.addBadgeNum(data.unreadNoticeCount || 1)
		}
	},
});

export function useChatStoreWithOut() {
	return useChatStore(store);
}