<template>
	<view class="workFlow-v">
		<view class="notice-warp">
			<view class="search-box">
				<u-search :placeholder="$t('app.apply.pleaseKeyword')" v-model="keyword" height="72"
					:show-action="false" @change="search" bg-color="#f0f2f6" shape="square" @clear="clear" />
			</view>
			<view class="commonTabs-box">
				<CommonTabs :list="categoryTree" @change="change" :current="current" ref="CommonTabs"
					:isScroll="categoryTree.length >= 4 ? true : false">
				</CommonTabs>
			</view>
		</view>
		<view class="workFlow-list" style="">
			<view class="part" v-if="list.length">
				<view class="caption u-line-1">
					{{fullName }}
				</view>
				<view class="u-flex u-flex-wrap">
					<view class="item u-flex-col u-col-center" v-for="(child, ii) in list" :key="ii"
						@click="Jump(child)">
						<text class="u-font-40 item-icon" :class="child.icon"
							:style="{ background: child.iconBackground || '#008cff' }" />
						<text class="u-font-24 u-line-1 item-text">{{ child.fullName }}</text>
					</view>
				</view>
			</view>
			<NoData v-else></NoData>
		</view>
	</view>
</template>
<script>
	import NoData from '@/components/noData'
	import CommonTabs from '@/components/CommonTabs'
	import {
		getCommonFlowTree
	} from "@/api/apply/apply.js";
	export default {
		components: {
			CommonTabs,
			NoData
		},
		data() {
			return {
				activeFlow: {},
				keyword: "",
				category: "",
				current: 0,
				categoryTree: [],
				fullName: '',
				list: [],
				searchCategoryTree: []
			};
		},
		created() {
			uni.showLoading()
			this.getFlowUsualList();
		},
		methods: {
			getFlowUsualList() {
				this.keyword = ''
				getCommonFlowTree().then((res) => {
					this.categoryTree = res?.data?.list || [];
					this.searchCategoryTree = JSON.parse(JSON.stringify(this.categoryTree))
					this.list = []
					this.$nextTick(() => {
						this.list = this.categoryTree[this.current]?.children || []
						this.fullName = this.categoryTree[this.current]?.fullName;
					})
					uni.hideLoading()
				}).catch(() => {
					this.categoryTree = []
					this.list = []
				})
			},
			search() {
				this.searchTimer && clearTimeout(this.searchTimer);
				this.searchTimer = setTimeout(() => {
					if (!this.keyword) return this.clear()
					let children = this.searchCategoryTree[this.current].children.filter(o => o.fullName.includes(
						this.keyword))
					this.$set(this.categoryTree[this.current], 'children', children)
					this.list = this.categoryTree[this.current].children
				}, 300);
			},
			clear() {
				this.getFlowUsualList();
			},
			change(i) {
				this.list = this.categoryTree[i].children
				this.fullName = this.categoryTree[i].fullName;
				this.keyword = ''
				this.current = i
			},
			Jump(item) {
				const config = {
					id: "",
					flowId: item.id,
					opType: "-1",
				};
				this.current = 0
				this.category = ""
				uni.navigateTo({
					url: "/pages/workFlow/flowBefore/index?config=" +
						this.jnpf.base64.encode(JSON.stringify(config))
				});
			}
		},
	};
</script>

<style lang="scss" scoped>
	page {
		background-color: #f0f2f6;
	}

	.workFlow-v {
		height: 100%;

		.workFlow-list {
			margin-top: 120px;
		}

		.notice-warp {
			height: 115rpx !important;

			.search-box {
				padding: 20rpx;
			}
		}

		.commonTabs-box {
			height: 2.8rem;
		}

	}
</style>