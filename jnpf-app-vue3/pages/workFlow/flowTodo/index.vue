<template>
	<view class="flow-v">
		<view class="notice-warp" :style="{height:noticeWarpH + 'px'}">
			<view class="search-box">
				<u-search :placeholder="$t('app.apply.pleaseKeyword')" v-model="keyword" height="72"
					:show-action="false" @change="search" bg-color="#f0f2f6" shape="square">
				</u-search>
			</view>
			<view class="flow-tabs">
				<u-tabs ref="tabs" :list="tabsList" active-color="#0177FF" inactive-color="#303133" font-size="30"
					v-model="current" name="fullName" @change="change" height="80" :is-scroll="false"></u-tabs>
			</view>
			<view class="flow-status-tabs" v-if="statusList.length">
				<u-subsection :list="statusList" :current="subsectionIndex" name="name" active-color="#2979FF"
					inactive-color="#999999" bg-color="#F2F3F7" font-size="24" :bold="false"
					@change="subsection"></u-subsection>
			</view>
		</view>
		<mescroll-body ref="mescrollRef" @init="mescrollInit" @down="downCallback" @up="upCallback" :down="downOption"
			:up="upOption" :top="mescrollTop">
			<flowlist :list='list' :swipeAction='current != 3' :category='category' />
		</mescroll-body>
	</view>
</template>

<script>
	import resources from '@/libs/resources.js'
	import MescrollMixin from "@/uni_modules/mescroll-uni/components/mescroll-uni/mescroll-mixins.js";
	import FlowMixin from "./FlowMixin.js";
	import flowlist from './flowList.vue'
	export default {
		components: {
			flowlist
		},
		mixins: [MescrollMixin, FlowMixin],
		data() {
			return {
				noticeWarpH: 0,
				searchBox: 0,
				flowTabs: 0,
				flowStatusTabs: 0,
				activeItemStyle: {
					backgroundColor: '#fff'
				},
				keyword: '',
				category: '0',
				list: [],
				downOption: {
					use: true,
					auto: true
				},
				upOption: {
					page: {
						num: 0,
						size: 20,
						time: null
					},
					empty: {
						use: true,
						icon: resources.message.nodata,
						tip: this.$t('common.noData'),
						fixed: true,
						top: "300rpx",
					},
					textNoMore: this.$t('app.apply.noMoreData')
				},
			}
		},
		onShow() {
			uni.$off('operate')
			uni.$on('refresh', () => {
				this.list = [];
				this.mescroll.resetUpScroll();
			})
		},
		onUnload() {
			uni.$off('refresh')
		},
		mounted() {
			this.getContentHeight()
		},
		methods: {
			async getContentHeight() {
				const windowHeight = this.$u.sys().windowHeight;
				// 获取元素尺寸
				const [flowStatusTabs, flowTabs, searchBox] = await Promise.all([
					this.$uGetRect('.search-box'),
					this.$uGetRect('.flow-tabs'),
					this.$uGetRect('.flow-status-tabs')
				]);
				this.flowStatusTabs = flowStatusTabs.height
				this.flowTabs = flowTabs.height
				this.searchBox = searchBox.height
				this.mescrollTop = this.flowStatusTabs + this.flowTabs + this.searchBox
				this.noticeWarpH = this.mescrollTop
			},
			getFlowStatus(status) {
				let flowStatus;
				//待签收
				if (this.category == '0') flowStatus = resources.status.signfor
				// 待办，在办
				if (this.category == '1' || this.category == '2') {
					//流转中
					if (status == '1') flowStatus = resources.status.circulation
					//已退回
					if (status == '5') flowStatus = resources.status.back
					//协办
					if (status == '7') flowStatus = resources.status.assist
					//转审
					if (status == '3') flowStatus = resources.status.transfer
					//撤回
					if (status == '6') flowStatus = resources.status.recall
					//撤销中
					if (status == '8') flowStatus = resources.status.revoking
					//加签
					if (status == '2') flowStatus = resources.status.addSign
					//指派
					if (status == '4') flowStatus = resources.status.assign
					//转办
					if (status == '9') flowStatus = resources.status.transfer2
				}
				//发起
				if (!this.category) {
					//待提交
					if (status == '0') flowStatus = resources.status.draft
					//进行中
					if (status == '1') flowStatus = resources.status.doing
					//已通过
					if (status == '2') flowStatus = resources.status.adopt
					//已拒绝
					if (status == '3') flowStatus = resources.status.reject
					//已终止
					if (status == '4') flowStatus = resources.status.cancel
					//已暂停
					if (status == '5') flowStatus = resources.status.pause
					//撤销中
					if (status == '6') flowStatus = resources.status.revoking
					//已撤销
					if (status == '7') flowStatus = resources.status.revoke
					//退回
					if (status == '8') flowStatus = resources.status.back
					//撤回
					if (status == '9') flowStatus = resources.status.recall
				}
				//已办
				if (this.category == '3') {
					//转审
					if (status == '7') flowStatus = resources.status.transfer
					//同意
					if (status == '1') flowStatus = resources.status.agree
					//拒绝
					if (status == '0') flowStatus = resources.status.refuse
					//加签
					if (status == '5') flowStatus = resources.status.addSign
					//退回
					if (status == '3') flowStatus = resources.status.return
					//转办
					if (status == '18') flowStatus = resources.status.transfer2
				}
				//抄送
				if (this.category == '4') {
					//进行中
					if (status == '1') flowStatus = resources.status.doing
					//已通过
					if (status == '2') flowStatus = resources.status.adopt
					//已拒绝
					if (status == '3') flowStatus = resources.status.reject
					//已退回
					if (status == '8') flowStatus = resources.status.back
				}
				return flowStatus
			},
			search() {
				// 节流,避免输入过快多次请求
				this.searchTimer && clearTimeout(this.searchTimer)
				this.searchTimer = setTimeout(() => {
					this.list = [];
					this.mescroll.resetUpScroll();
				}, 300)
			}
		}
	}
</script>

<style lang="scss">
	page {
		background-color: #f0f2f6;
	}

	.u-tabs {
		padding-bottom: 4rpx;
	}
</style>