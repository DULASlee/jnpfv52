<template>
	<view class="HTodo-v" :key="key">
		<view class="HTodo-box">
			<view class="HTodo-list u-flex" :style="{'flex-wrap':option.styleType==1?'nowrap':'wrap'}">
				<view class="u-flex-col HTodo-list-item" v-for="(item,index) in option.defaultValue" :key="index"
					@click="jump(item)" :style="option.style">
					<view class="u-m-b-8" style="position: relative">
						<view :class="item.icon" class="icon"
							:style="{'background-color':item.iconColor||item.iconBgColor || '#008cff'}"></view>
						<u-badge type="error" :offset="offset" :count="item.num" show-zero
							:bgColor="option.valueFontBgColor"
							:style="{'color':option.valueFontColor,'font-weight':option.valueFontWeight?700:400}">
						</u-badge>
					</view>
					<view class="u-line-1 title"
						:style="{'font-size':option.labelFontSize,'color':option.labelFontColor,'font-weight':option.labelFontWeight?700:400}">
						{{item.fullName}}
					</view>
				</view>
			</view>
		</view>
	</view>
</template>
<script>
	import {
		getFlowTodoCount
	} from "@/api/workFlow/flowEngine";
	export default {
		props: {
			config: {
				type: Object,
				default: () => {}
			}
		},
		data() {
			return {
				offset: ['-8', '-14'],
				option: {},
				propsApi: '',
				key: +new Date(),
				flowDone: [],
				toBeReviewed: [],
				flowCirculateType: []
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
			getFlowTodoCount() {
				for (let i = 0; i < this.option.defaultValue.length; i++) {
					let defaultValue = this.option.defaultValue[i]
					if (defaultValue.category && defaultValue.category.length) this[defaultValue.id] = defaultValue
						.category
				}
				const query = {
					flowCirculateType: this.flowCirculateType,
					flowDoneType: this.flowDone,
					toBeReviewedType: this.toBeReviewed,
				}
				getFlowTodoCount(query).then(res => {
					let data = res.data
					this.option.defaultValue.forEach(o => {
						o.num = data[o.id] || 0
					})
					this.key = +new Date()
				})
			},
			init() {
				this.initData()
				if (!this.config.allRefresh.autoRefresh && this.config.refresh.autoRefresh) {
					setInterval(this.initData, this.config.refresh.autoRefreshTime * 60000)
				}
			},
			initData() {
				this.option = JSON.parse(JSON.stringify(this.config.option))
				let style;
				style = {
					'width': '240rpx'
				}
				if (this.option.styleType == 2) {
					style = {
						'width': 100 / this.option.appRowNumber + '%'
					}
					if (this.option.appShowBorder) {
						style['border-right'] = '2rpx solid #f0f2f6'
						style['border-bottom'] = '2rpx solid #f0f2f6'
					}
				}
				this.option.style = style
				this.option.defaultValue = this.option.defaultValue.filter((o) => !o.noShow)
				this.getFlowTodoCount()
				this.option.labelFontSize = this.option.labelFontSize * 2 + 'rpx'
				this.key = +new Date()
			},
			jump(item) {
				if (this.config.platform === 'mp') return
				const sysConfigInfo = uni.getStorageSync('sysConfigInfo')
				if (['flowToSign'].includes(item.id) && sysConfigInfo.flowSign == 0) return this.$u.toast(
					"系统未开启流程待签，无法跳转")
				if (['flowTodo'].includes(item.id) && sysConfigInfo.flowTodo == 0) return this.$u.toast(
					"系统未开启流程待办，无法跳转")
				let url = '/my/entrustAgent'
				let tabIndex = {
					'flowToSign': 0,
					'flowTodo': 1,
					'flowDoing': 2,
					'flowDone': 4,
					'flowCirculate': 5,
				}
				let data = {
					tabIndex: tabIndex[item.id],
					portal: true
				}
				if (['flowToSign', 'flowDoing', 'flowDone', 'flowCirculate', 'flowTodo'].includes(item.id)) url =
					'/workFlow/flowTodo'
				uni.navigateTo({
					url: `/pages${url}/index?data=${encodeURIComponent(JSON.stringify(data))}`
				})
				uni.navigateTo({
					url: url,
					fail: (err) => {
						// this.$u.toast("暂无此页面")
					}
				})
			}
		}
	}
</script>
<style lang="scss">
	.HTodo-v {
		width: 100%;
		height: 100%;

		.HTodo-box {
			width: 100%;
			overflow-x: scroll;

			.HTodo-list {


				.HTodo-list-item {
					align-items: center;
					padding: 16rpx 20rpx;

					.title {
						width: 100%;
						text-align: center;
					}

					.icon {
						width: 90rpx;
						height: 90rpx;
						font-size: 60rpx;
						color: #fff;
						text-align: center;
						line-height: 90rpx;
						border-radius: 50%;
					}
				}
			}
		}
	}
</style>