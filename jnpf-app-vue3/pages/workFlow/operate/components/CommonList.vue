<template>
	<view class="common-list-v" v-if="show">
		<view class="common-list-contain">
			<view class="common-list-main u-p-20 u-flex-col">
				<view class="common-list-search" style=" ">
					<uni-search-bar radius="100" placeholder="请输入" clearButton="always" cancelButton="always"
						@cancel="cancel" v-model="searchValue" focus />
				</view>
				<view class="" style="flex: 1;margin-top: 10rpx; overflow-y: scroll;" v-if="columnList.length">
					<view class="u-line-1" style="width: 100%;height:68rpx;line-height: 68rpx;"
						v-for="(item,index) in columnList" :key="index" @click.stop="selectConfirm(item)">
						{{item.commonWordsText}}
					</view>
				</view>
				<NoData v-else backgroundColor="#fff" paddingTop="0"></NoData>
			</view>
		</view>
	</view>
</template>

<script>
	import NoData from '@/components/noData'
	import {
		getSelector
	} from "@/api/commonWords.js";
	export default {
		components: {
			NoData
		},
		data() {
			return {
				show: false,
				commonWordsList: [],
				searchValue: ''
			}
		},
		computed: {
			columnList() {
				return this.commonWordsList.filter((o) => (o.commonWordsText && o.commonWordsText.match(this.searchValue)))
			}
		},
		methods: {
			open() {
				this.show = true
				this.getCommonList()
			},
			cancel() {
				this.close()
			},
			close() {
				this.searchValue = ""
				this.show = false
			},
			selectConfirm(item) {
				this.$emit('confirm', item)

				this.close()
			},
			getCommonList() {
				getSelector().then((res) => {
					let list = JSON.parse(JSON.stringify(res.data.list)) || []
					this.commonWordsList = list.filter((o, i) => i > 2)
				});
			}
		}
	}
</script>

<style lang="scss">
	.common-list-v {
		z-index: 9000;
		position: fixed;
		left: 0;
		top: 0;
		width: 100%;
		height: 100vh !important;




		.common-list-contain {
			height: 100%;
			width: 100%;

			.common-list-main {
				background: white;

				height: 100%;


				.common-list-search {
					width: 100%;
					height: 70rpx;

					.uni-searchbar {
						width: 100%;
						padding: 0;

						.uni-searchbar__box {
							justify-content: flex-start !important;
						}
					}
				}
			}
		}
	}
</style>