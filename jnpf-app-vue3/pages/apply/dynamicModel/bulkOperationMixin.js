import {
	deteleModel
} from '@/api/apply/visualDev'
export default {
	data() {
		return {
			slide: '',
			slide2: '',
			checkedAll: false,
			ids: [],
			showTop: false,
		}
	},
	methods: {
		/* 批量删除 */
		batchDelete() {
			uni.showModal({
				title: '提示',
				content: '删除后数据无法恢复',
				success: (res) => {
					if (res.confirm) {
						const uniqueIds = new Set();
						this.selectItems.forEach(item => {
							uniqueIds.add(item.id);
						});
						const ids = [...uniqueIds];
						let data = {
							flowId: this.config.flowId,
							ids
						};
						deteleModel(data, this.modelId).then(res => {
							this.selectItems = [];
							this.$u.toast(res.msg)
							this.mescroll.resetUpScroll()
						})
					}
				}
			})
		},
		openBatchOperate() {
			this.showTop = !this.showTop
			if (this.showTop) {
				this.slide = 'slide-up'
				this.slide2 = 'slide-up2'
			}
		},
		checkAll() {
			this.checkedAll = !this.checkedAll
			this.list = this.list.map(o => ({
				...o,
				checked: false
			}))
			if (this.checkedAll) {
				this.list = this.list.map(o => ({
					...o,
					checked: true
				}))
			}
		},
		cancel() {
			this.list = this.list.map(o => ({
				...o,
				checked: false
			}))
			this.showTop = false
			this.checkedAll = false
			this.$nextTick(() => {
				this.$refs.list.handleCheckAll()
			})
		}
	}
}