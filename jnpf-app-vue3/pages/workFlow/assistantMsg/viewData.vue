<template>
	<view class="viewData-v">
		<view class="notice-warp">
			<view class="search-box">
				<u-search v-model="keyword" height="72" :show-action="false" @change="search" bg-color="#f0f2f6"
					shape="square">
				</u-search>
			</view>
		</view>
		<mescroll-body ref="mescrollRef" @init="mescrollInit" @down="downCallback" @up="upCallback" :sticky="true"
			:down="downOption" :up="upOption" top="120">
			<view class="u-flex-col tableList">
				<view class="u-flex list-card" v-for="(item,index) in list" :key="index">
					<view class="u-flex-col fieldContent u-m-l-10">
						<view v-for="(column,c) in onLoadData.columnOptions" :key="c" class="fieldList u-line-1 u-flex">
							<view class="val" v-if="column.ifShow">{{column.label+':'}} {{item[column.value]}}</view>
						</view>
					</view>
				</view>
			</view>
		</mescroll-body>
	</view>
</template>
<script>
	import {
		getPopSelect
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
				list: [],
				columnOptions: '',
				onLoadData: {},
				keyword: '',
				listQuery: {
					keyword: ''
				}
			}
		},
		onLoad(e) {
			this.onLoadData = JSON.parse(decodeURIComponent(e.data));
			this.columnOptions = this.onLoadData.columnOptions.map(o => o.value).join(',')
		},
		methods: {
			upCallback(page) {
				const paramList = this.onLoadData.templateJson
				let query = {
					...this.listQuery,
					currentPage: page.num,
					pageSize: 20,
					interfaceId: this.onLoadData.interfaceId,
					columnOptions: this.columnOptions,
					paramList
				}
				getPopSelect(this.onLoadData.interfaceId, query, {
					load: page.num == 1
				}).then(res => {
					this.mescroll.endSuccess(res.data.list.length);
					if (page.num == 1) this.list = [];
					this.list = this.list.concat(res.data.list);
				}).catch(() => {
					this.mescroll.endErr();
				})
			},
			search() {
				// 节流,避免输入过快多次请求
				this.searchTimer && clearTimeout(this.searchTimer)
				this.searchTimer = setTimeout(() => {
					this.list = [];
					this.listQuery.keyword = this.keyword
					this.listQuery.currentPage = 1
					this.mescroll.resetUpScroll();
				}, 300)
			},
		}
	}
</script>

<style lang="scss">
	page {
		background-color: #f0f2f6;
	}

	.viewData-v {
		width: 100%;
		height: 100%;
		padding-bottom: 106rpx;


		.notice-warp {
			height: 3.5rem;
		}

		.tableList {
			padding: 0 20rpx;

			.list-card {
				background-color: #fff;
				width: 100%;
				border-radius: 8rpx;
				margin-top: 20rpx;
				padding: 20rpx 20rpx;

				.fieldContent {
					width: 100%;
					margin-top: -14rpx;

					.fieldList {
						// width: 752rpx;

						.key {
							width: 136rpx;
							margin-right: 10rpx;
							text-align: right;
							overflow: hidden;
							white-space: nowrap;
							text-overflow: ellipsis;
							line-height: 60rpx;
						}

						.val {
							flex: 0.85;
							overflow: hidden;
							white-space: nowrap;
							text-overflow: ellipsis;
						}
					}
				}
			}
		}

		.nodata {
			margin-top: 258rpx;
			justify-content: center;
			align-items: center;

			image {
				width: 280rpx;
				height: 215rpx;
			}
		}
	}
</style>