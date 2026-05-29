import {
	useChatStore
} from '@/store/modules/chat'
export default {
	onShow() {
		this.setTabBarBadge()
	},
	methods: {
		setTabBarBadge() {
			const chatStore = useChatStore()
			const badgeNum = chatStore.getBadgeNum || 0
			if (badgeNum) {
				uni.setTabBarBadge({
					index: 3,
					text: badgeNum > 99 ? '99+' : badgeNum.toString()
				});
			} else {
				uni.removeTabBarBadge({
					index: 3
				});
			}
		}
	}
}