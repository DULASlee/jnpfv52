<template>
	<view class="notice-v">
		<template v-if="list.length">
			<view class="item-box u-p-l-20 u-p-r-20" v-for="(item,index) in list" :key="index">
				<view class="u-flex item u-m-t-20 u-p-b-20" v-if="option.appStyleType == 1" @click="readInfo(item)"
					:style="{'border-bottom':index == list.length-1?'none':'2rpx solid #f0f2f6'}">
					<view class="img-box">
						<u-image width="90rpx" height="90rpx" v-if="item.category=='公告'" shape="circle"
							:src="!item.coverImage? gg : define.baseURL+item.coverImage"></u-image>
						<u-image v-else width="90rpx" height="90rpx" shape="circle"
							:src="!item.coverImage?tz:define.baseURL+item.coverImage"></u-image>
					</view>
					<view class="u-p-l-20 item-right">
						<view class="u-flex u-m-b-20">
							<text class="notice-type u-p-l-10 u-p-r-10 u-font-24" v-if="option.appColumnList[0].show"
								:style="{'color':item.category=='公告'?'#9a00f3':'#1448f4','background-color':item.category=='公告'?'#ebe6ff':'#e5ebfe'}">{{item.category}}</text>
							<view class="u-line-1 u-p-l-10 u-p-r-10 name" v-if="option.appColumnList[1].show"
								:style="{'font-size':option.appColumnList[1].fontSize*2+'rpx','font-weight':option.appColumnList[1].fontWeight?'700':'400','color':option.appColumnList[1].fontColor}">
								{{item.fullName}}
							</view>
							<text class="time"
								:style="{'font-size':option.appColumnList[3].fontSize*2+'rpx','font-weight':option.appColumnList[3].fontWeight?'700':'400','color':option.appColumnList[3].fontColor}"
								v-if="option.appColumnList[3].show && option.appColumnList[3].timeClassify ==1">{{$u.timeFormat(item.creatorTime,'mm-dd hh:MM')}}</text>
							<text class="time"
								:style="{'font-size':option.appColumnList[3].fontSize*2+'rpx','font-weight':option.appColumnList[3].fontWeight?'700':'400','color':option.appColumnList[3].fontColor}"
								v-if="option.appColumnList[3].show && option.appColumnList[3].timeClassify == 2">{{$u.timeFormat(item.releaseTime,'mm-dd hh:MM')}}</text>
						</view>
						<view class="u-line-1 u-p-r-10 content2 u-m-t-20" v-if="option.appColumnList[2].show"
							:style="{'font-size':option.appColumnList[2].fontSize*2+'rpx','font-weight':option.appColumnList[1].fontWeight?'700':'400','color':option.appColumnList[2].fontColor}">
							{{item.excerpt}}
						</view>
					</view>
				</view>
				<view class="u-flex item u-m-t-20 u-p-b-20" v-if="option.appStyleType == 2" @click="readInfo(item)"
					:style="{'border-bottom':index == list.length-1?'none':'2rpx solid #f0f2f6'}">
					<view class="img-box">
						<u-image width="90rpx" height="90rpx" v-if="item.category=='公告'" shape="circle"
							:src="!item.coverImage? gg : define.baseURL+item.coverImage"></u-image>
						<u-image v-else width="90rpx" height="90rpx" shape="circle"
							:src="!item.coverImage?tz:define.baseURL+item.coverImage"></u-image>
					</view>
					<view class="u-p-l-20 u-flex-col" style="flex: 1;">
						<view class="u-flex u-m-b-10" style="width: 100%;height: 100%;">
							<view class="notice-type u-p-l-10 u-p-r-10 u-font-24"
								:style="{'color':item.category=='公告'?'#9a00f3':'#1448f4','background-color':item.category=='公告'?'#ebe6ff':'#e5ebfe'}"
								v-if="option.appColumnList[0].show">{{item.category=='公告'?'公告':'通知'}}</view>
							<view class="u-line-1 u-p-l-10 u-p-r-10 name" v-if="option.appColumnList[1].show"
								:style="{'font-size':option.appColumnList[1].fontSize*2+'rpx','font-weight':option.appColumnList[1].fontWeight?'700':'400','color':option.appColumnList[1].fontColor}">
								{{item.fullName}}
							</view>
						</view>
						<view class="u-line-1 u-p-r-10 content2" v-if="option.appColumnList[2].show"
							:style="{'font-size':option.appColumnList[2].fontSize*2+'rpx','font-weight':option.appColumnList[1].fontWeight?'700':'400','color':option.appColumnList[2].fontColor}">
							{{item.excerpt}}
						</view>
						<view class="">
							<text
								:style="{'font-size':option.appColumnList[4].fontSize*2+'rpx','font-weight':option.appColumnList[4].fontWeight?'700':'400','color':option.appColumnList[4].fontColor}">{{option.appColumnList[4].userClassify==2? item.releaseUser : item.creatorUser}}</text>
							<text class="time"
								:style="{'font-size':option.appColumnList[3].fontSize*2+'rpx','font-weight':option.appColumnList[3].fontWeight?'700':'400','color':option.appColumnList[3].fontColor}"
								v-if="option.appColumnList[3].show && option.appColumnList[3].timeClassify == 1">{{$u.timeFormat(item.creatorTime,'mm-dd hh:MM')}}</text>
							<text class="time"
								:style="{'font-size':option.appColumnList[3].fontSize*2+'rpx','font-weight':option.appColumnList[3].fontWeight?'700':'400','color':option.appColumnList[3].fontColor}"
								v-if="option.appColumnList[3].show && option.appColumnList[3].timeClassify == 2">{{$u.timeFormat(item.releaseTime,'mm-dd hh:MM')}}</text>
						</view>
					</view>
				</view>
				<view class="u-flex item u-m-t-20 u-p-b-20" v-if="option.appStyleType == 3" @click="readInfo(item)"
					:style="{'border-bottom':index == list.length-1?'none':'2rpx solid #f0f2f6'}">
					<text class="notice-type u-p-l-10 u-p-r-10 u-font-24"
						:style="{'color':item.category=='公告'?'#9a00f3':'#1448f4','background-color':item.category=='公告'?'#ebe6ff':'#e5ebfe'}"
						v-if="option.appColumnList[0].show">{{item.category=='公告'?'公告':'通知'}}</text>
					<text class="u-line-1 u-p-l-20 u-p-r-10 content" v-if="option.appColumnList[1].show"
						:style="{'font-size':option.appColumnList[1].fontSize*2+'rpx','font-weight':option.appColumnList[1].fontWeight?'700':'400','color':option.appColumnList[1].fontColor}">{{item.fullName}}</text>
					<view v-if="option.appColumnList[3].show">
						<text class="time"
							:style="{'font-size':option.appColumnList[3].fontSize*2+'rpx','font-weight':option.appColumnList[3].fontWeight?'700':'400','color':option.appColumnList[3].fontColor}"
							v-if="option.appColumnList[3].timeClassify == 1">{{$u.timeFormat(item.creatorTime,'mm-dd hh:MM')}}</text>
						<text class="time"
							:style="{'font-size':option.appColumnList[3].fontSize*2+'rpx','font-weight':option.appColumnList[3].fontWeight?'700':'400','color':option.appColumnList[3].fontColor}"
							v-else>{{$u.timeFormat(item.releaseTime,'mm-dd hh:MM')}}</text>
					</view>
				</view>
			</view>
		</template>
		<view v-else class="notData-box u-flex-col">
			<view class="u-flex-col notData-inner">
				<image :src="icon" mode="" class="iconImg"></image>
				<text class="notData-inner-text">{{$t('common.noData')}}</text>
			</view>
		</view>
	</view>
</template>
<script>
	import {
		getNotice
	} from '@/api/home'
	import resources from '@/libs/resources.js'
	import gg from '@/pages/portal/static/image/gg.png'
	import tz from '@/pages/portal/static/image/tz.png'

	export default {
		components: {},
		props: {
			config: {
				type: Object,
				default: () => {}
			}
		},
		data() {
			return {
				list: [],
				typeList: [],
				icon: resources.message.nodata,
				gg,
				tz
			}
		},
		created() {
			this.init()
			uni.$off('proRefresh')
			uni.$on('proRefresh', () => {
				this.initData()
			})
		},
		methods: {
			init() {
				this.option = JSON.parse(JSON.stringify(this.config.option))
				this.option.appColumnList.map((o) => {
					if (o.fullNameI18nCode) o.fullName = this.$t(o.fullNameI18nCode)
				})
				this.initData()
				if (!this.config.allRefresh.autoRefresh && this.config.refresh.autoRefresh) {
					setInterval(this.getData, this.config.refresh.autoRefreshTime * 60000)
				}
			},
			readInfo(item) {
				if (this.config.platform === 'mp') return
				uni.navigateTo({
					url: '/pages/message/messageDetail/index?id=' + item.id,
					fail: (err) => {
						this.$u.toast("暂无此页面")
					}
				})
			},
			initData() {
				this.getData()
			},
			getData() {
				this.option.appColumnList.forEach((o, i) => {
					if (o.classify && o.classify.length) {
						this.typeList = o.classify
					}
				});
				let data = {
					typeList: this.typeList
				}
				getNotice(data).then(res => {
					let list = JSON.parse(JSON.stringify(res.data.list)) || []
					this.list = list.slice(0, this.option.appCount || 50)
				})
			}
		}
	}
</script>
<style lang="scss">
	.notice-v {
		max-height: 472rpx;
		overflow-y: scroll;

		.item-box {

			.item {
				width: 100%;
				height: 100%;

				.img-box {
					flex-shrink: 0;
				}

				.item-right {
					flex: 1;
					height: 100rpx;
				}

				.notice-type {
					border-radius: 8rpx;
					flex-shrink: 0;
				}

				.content {
					flex: 1;
				}

				.name {
					flex: 1;
					max-width: 366rpx;
				}

				.content2 {
					width: 540rpx;
				}

				.time {
					float: right;
					flex-shrink: 0;
				}
			}
		}
	}

	.notData-box {
		width: 100%;
		height: 100%;
		justify-content: center;
		align-items: center;

		.notData-inner {
			width: 280rpx;
			height: 308rpx;
			align-items: center;

			.iconImg {
				width: 100%;
				height: 100%;
			}

			.notData-inner-text {
				padding: 30rpx 0;
				color: #909399;
			}
		}
	}
</style>