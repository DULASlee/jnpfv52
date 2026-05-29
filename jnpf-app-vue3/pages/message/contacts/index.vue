<template>
	<view class="contacts-v">
		<mescroll-body ref="mescrollRef" @init="mescrollInit" @down="downCallback" @up="upCallback" :sticky="true"
			:down="downOption" :up="upOption" :bottombar="false">
			<view class="search-box search-box_sticky">
				<u-search :placeholder="$t('app.apply.pleaseKeyword')" v-model="keyword" height="72"
					:show-action="false" @change="search" bg-color="#f0f2f6" shape="square">
				</u-search>
			</view>
			<view class="list-cell u-p-l-20 u-p-r-20" v-for="(item, i) in list" :key="i" @click="detail(item.id)">
				<view class="u-border-bottom list-item u-font-28 u-flex">
					<u-avatar :src="baseURL+item.headIcon"></u-avatar>
					<view class="list-cell-txt">
						<view class="u-font-30 u-m-b-4" style="color: #303133;font-size: 28rpx;">
							{{item.realName}}/{{item.account}}
						</view>
						<view class="u-font-24 department u-m-t-4">{{item.department}}</view>
					</view>
				</view>
			</view>
		</mescroll-body>
	</view>
</template>

<script>
	import {
		getImUser
	} from '@/api/common.js'
	import resources from '@/libs/resources.js'
	import MescrollMixin from "@/uni_modules/mescroll-uni/components/mescroll-uni/mescroll-mixins.js";
	export default {
		mixins: [MescrollMixin],
		data() {
			return {
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
					textNoMore: this.$t('app.apply.noMoreData'),
				},
				keyword: '',
				list: []
			}
		},
		computed: {
			baseURL() {
				return this.define.baseURL
			}
		},
		methods: {
			upCallback(page) {
				let query = {
					currentPage: page.num,
					pageSize: page.size,
					keyword: this.keyword
				}
				getImUser(query, {
					load: page.num == 1
				}).then(res => {
					this.mescroll.endSuccess(res.data.list.length);
					if (page.num == 1) this.list = [];
					const list = res.data.list;
					this.list = this.list.concat(list);
				}).catch(() => {
					this.mescroll.endErr();
				})
			},
			search() {
				// 节流,避免输入过快多次请求
				this.searchTimer && clearTimeout(this.searchTimer)
				this.searchTimer = setTimeout(() => {
					this.list = [];
					this.mescroll.resetUpScroll();
				}, 300)
			},
			detail(id) {
				uni.navigateTo({
					url: '/pages/message/userDetail/index?userId=' + id,
				})
			}
		}
	}
</script>

<style lang="scss">
	page {
		background-color: #eef0f4;
	}

	.contacts-v {

		.list-cell {
			width: 100%;
			background-color: #fff;

			.list-item {
				box-sizing: border-box;
				overflow: hidden;
				color: $u-content-color;
				height: 136rpx;

				.list-cell-txt {
					margin-left: 20rpx;

					.department {
						color: #909399;
						font-size: 24rpx;
					}
				}
			}
		}
	}
</style>