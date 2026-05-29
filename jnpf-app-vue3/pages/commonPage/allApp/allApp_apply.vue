<template>
	<view class="allApp-v">
		<view class="notice-warp">
			<view class="search-box">
				<u-search :placeholder="$t('app.apply.pleaseKeyword')" v-model="keyword" height="72"
					:show-action="false" @change="search" bg-color="#f0f2f6" shape="square">
				</u-search>
			</view>
		</view>
		<view class="usualList">
			<view class="caption u-m-b-20">常用菜单</view>
			<view class="u-flex u-flex-wrap">
				<view class="item u-flex-col u-col-center" v-for="(item,i) in usualList" :key="i">
					<text class="u-font-40 item-icon" :class="item.icon"
						:style="{'background':item.iconBackground||'#008cff'}" />
					<text class="u-font-24 u-line-1 item-text">{{item.fullName}}</text>
				</view>
			</view>
		</view>
		<CommonTabs :list="allList" @change="change" :current="current" ref="CommonTabs" isBoxShadow></CommonTabs>
		<view class="allList u-m-t-20" v-if="allList.length">
			<view class="u-m-t-20" v-for="(item,i) in allList" :key="i">
				<template v-if="i==current && item?.children?.length">
					<view v-for="(child,ii) in item.children" class="u-flex childList-item u-p-b-28" :key="ii">
						<text class="u-font-40 item-icon" :class="child.icon"
							:style="{'background':child.iconBackground||'#008cff'}" />
						<text class="u-font-32 item-text u-m-l-28 u-m-r-28 u-line-2">{{child.fullName}}</text>
						<view class="btnBox">
							<u-button :custom-style="customStyle" @click="handelAdd(child)" v-if="!child.isData">添加
							</u-button>
							<u-button :custom-style="customStyle" type="error" @click="handelDel(child)" v-else>移除
							</u-button>
						</view>
					</view>
				</template>
			</view>
		</view>
		<NoData v-else :paddingTop="450"></NoData>
	</view>
</template>
<script>
	import NoData from '@/components/noData'
	import {
		FlowEngineListAll
	} from '@/api/workFlow/flowEngine'
	import CommonTabs from '@/components/CommonTabs'
	import {
		getDataList,
		getUsualList,
		addUsual,
		delUsual
	} from '@/api/apply/apply.js'
	export default {
		components: {
			CommonTabs,
			NoData
		},
		data() {
			return {
				current: 0,
				usualList: [],
				allList: [],
				customStyle: {
					width: "128rpx",
					fontSize: "24rpx",
					height: '60rpx'
				},
				type: '2',
				keyword: ''
			}
		},
		methods: {
			init() {
				uni.showLoading({
					title: '加载中'
				});
				this.getUsualList()
			},
			search() {
				this.searchTimer && clearTimeout(this.searchTimer);
				this.searchTimer = setTimeout(() => {
					this.allList = [];
					this.usualList = [];
					this.current = 0
					this.getUsualList();
				}, 300);
			},
			getUsualList() {
				getUsualList().then(res => {
					this.usualList = res.data.list.map(o => {
						const objectData = o.objectData ? JSON.parse(o.objectData) : {}
						return {
							...o,
							...objectData
						}
					})
				})
				this.getAllList()
			},
			getAllList() {
				getDataList({
					keyword: this.keyword
				}).then(res => {
					uni.hideLoading()
					let list = JSON.parse(JSON.stringify(res.data.list))
					for (let i = 0; i < list.length; i++) {
						let children = list[i].children
						if (Array.isArray(children) && children.length) {
							for (let j = 0; j < children.length; j++) {
								let iconBackground = '',
									moduleId = ''
								if (children[j].propertyJson) {
									let propertyJson = JSON.parse(children[j].propertyJson)
									iconBackground = propertyJson.iconBackgroundColor || ''
									moduleId = propertyJson.moduleId || ''
								}
								this.$set(children[j], 'iconBackground', iconBackground)
								this.$set(children[j], 'moduleId', moduleId)
							}
						}
					}
					this.allList = list.filter(o => o.children)
				})
			},
			handelAdd(item) {
				addUsual(item.id).then(res => {
					this.usualList.push(item)
					item.isData = true
					uni.$emit('updateUsualList')
					uni.showToast({
						title: res.msg
					})
				})
			},
			handelDel(item) {
				delUsual(item.id).then(res => {
					this.usualList = this.usualList.filter(o => o.id !== item.id)
					item.isData = false
					uni.$emit('updateUsualList')
					uni.showToast({
						title: res.msg
					})
				})
			},
			change(index) {
				this.current = index;
			}
		}
	}
</script>

<style lang="scss" scoped>
	.allApp-v {
		.notice-warp {
			height: 114rpx;

			.search-box {
				padding: 20rpx;
			}
		}

		.caption {
			font-size: 36rpx;
			line-height: 80rpx;
			padding: 0 32rpx;

			.tip {
				margin-left: 20rpx;
				font-size: 24rpx;
				color: #c6c6c6;
			}
		}

		.tabs_box {
			width: 100%;

			.sticky {
				width: 750rpx;
				height: 120rpx;
				color: #fff;
				padding-right: 32rpx;
			}
		}

		.usualList {
			margin-top: 4.2rem;
			margin-bottom: 20rpx;
			background-color: #FFFFFF;

			.item {
				margin-bottom: 32rpx;
				width: 25%;

				.item-icon {
					width: 88rpx;
					height: 88rpx;
					margin-bottom: 8rpx;
					line-height: 88rpx;
					text-align: center;
					border-radius: 20rpx;
					color: #fff;
					font-size: 56rpx;
				}

				.item-text {
					width: 100%;
					text-align: center;
					padding: 0 16rpx;
				}
			}
		}

		.allList {
			padding: 20rpx 32rpx 0;
			background-color: #FFFFFF;

			.childList-item {
				align-items: center;

				.item-text {
					width: calc(100% - 216rpx);
				}

				.item-icon {
					width: 88rpx;
					height: 88rpx;
					line-height: 88rpx;
					text-align: center;
					border-radius: 30rpx;
					color: #FFFFFF;
					flex-shrink: 0;
					font-size: 40rpx;
				}
			}
		}
	}
</style>