/**
 * opType
 * -1 - 我发起的新建/编辑
 * 0 - 我发起的详情
 * 1 - 待签事宜
 * 2 - 待办事宜
 * 3 - 在办事宜
 * 4 - 已办事宜
 * 5 - 抄送事宜
 * 6 - 流程监控
 */
const statusMap = {
	1: [{
			name: '全部',
			status: ''
		},
		{
			name: '协办',
			status: '7'
		},
		{
			name: '退回',
			status: '5'
		},
		{
			name: '超时',
			status: '-2'
		}
	],
	2: [{
			name: '全部',
			status: ''
		},
		{
			name: '协办',
			status: '7'
		},
		{
			name: '退回',
			status: '5'
		},
		{
			name: '超时',
			status: '-2'
		}
	],
	3: [{
			name: '全部',
			status: ''
		},
		{
			name: '待提交',
			status: '0'
		},
		{
			name: '进行中',
			status: '1'
		},
		{
			name: '已完成',
			status: '2'
		}
	],
	4: [{
			name: '全部',
			status: ''
		},
		{
			name: '同意',
			status: '1'
		},
		{
			name: '拒绝',
			status: '2'
		},
		{
			name: '转审',
			status: '3'
		},
		{
			name: '加签',
			status: '4'
		},
		{
			name: '退回',
			status: '5'
		}
	],
	5: [{
			name: '全部',
			status: ''
		},
		{
			name: '已读',
			status: '1'
		},
		{
			name: '未读',
			status: '0'
		}
	]
};

import {
	getOperatorList,
	getFlowLaunchList
} from '@/api/workFlow/template'
const sysConfigInfo = uni.getStorageSync('sysConfigInfo')
export default {
	data() {
		return {
			mescrollTop: 206,
			statusList: [],
			tabsList: [{
				fullName: '在办',
				category: '2',
				key: 2
			}, {
				fullName: '发起',
				category: null,
				key: 3
			}, {
				fullName: '已办',
				category: '3',
				key: 4
			}, {
				fullName: '抄送',
				category: '4',
				key: 5
			}],
			current: 0,
			subsectionIndex: 0,
			status: ''
		}
	},
	watch: {
		current: {
			handler(val) {
				if (sysConfigInfo.flowSign == 1 && val == 0) return this.statusList = []
				this.statusList = statusMap[this.tabsList[val].key]
				this.category = this.tabsList[this.current].category
			},
			immediate: true,
			deep: true
		},
	},
	onLoad(e) {
		this.config = e?.data && JSON.parse(decodeURIComponent(e?.data))
		this.addTabList()
		this.getContentHeight()
		if (e?.data) this.change(this.config.tabIndex)
	},
	methods: {
		addTabList() {
			const configToCheck = [{
					key: 'flowTodo',
					tab: {
						fullName: '待办',
						category: '1',
						key: 1
					}
				},
				{
					key: 'flowSign',
					tab: {
						fullName: '待签',
						category: '0',
						key: 0
					}
				}
			];
			configToCheck.forEach(config => {
				if (sysConfigInfo[config.key] === 1) return this.tabsList.unshift(config.tab);
			});
		},
		/* tab1 */
		change(index) {
			let i = this.tabsList.findIndex(o => o.key === index)
			let item = this.tabsList[this.config?.portal ? i : index]
			this.current = Number(this.config?.portal ? i : index);
			this.status = ''
			this.keyword = ''
			this.subsectionIndex = 0
			this.category = item.category
			this.$nextTick(() => {
				this.list = [];
				this.mescroll.resetUpScroll();
			})
		},
		/* tab2 */
		subsection(e) {
			let item = this.statusList[e]
			this.status = item.status
			this.subsectionIndex = e
			this.$nextTick(() => {
				this.list = [];
				this.mescroll.resetUpScroll();
			})
		},
		/* 列表数据 */
		upCallback(page) {
			let methods = this.category ? getOperatorList : getFlowLaunchList;
			let query = {
				currentPage: page.num,
				pageSize: page.size,
				keyword: this.keyword,
				category: this.tabsList[this.current].category,
				status: this.status
			}
			methods(query, {
				load: page.num == 1
			}).then(res => {
				this.mescroll.endSuccess(res.data.list.length);
				if (page.num == 1) this.list = [];
				let flowStatus;
				const list = res.data.list.map(o => ({
					'flowStatus': this.getFlowStatus(o.status),
					'opType': this.setOpType(o.status),
					'swipeAction': this.swipeAction(o.status),
					...o
				}))
				this.list = this.list.concat(list);
			}).catch(() => {
				this.mescroll.endErr();
			})
		},
		swipeAction(status) {
			let swipeAction = true
			if (this.tabsList[this.current].key === 3 && !this.category && (status == '0' || status == '9'))
				swipeAction = false
			return swipeAction
		},
		/* 设置opType */
		setOpType(status) {
			if (this.tabsList[this.current].key == 3) return status == '0' || status == '9' || status == '8' ? '-1' : 0
			if (this.tabsList[this.current].key == 0) return 1
			if (this.tabsList[this.current].key == 1) return 2
			if (this.tabsList[this.current].key == 2) return 3
			if (this.tabsList[this.current].key == 4) return 4
			if (this.tabsList[this.current].key == 5) return 5
		}
	}
}