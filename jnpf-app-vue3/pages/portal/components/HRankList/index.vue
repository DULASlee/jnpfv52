<template>
	<view class="rankList-v">
		<platform v-if="option.styleType==3 || option.styleType==4" :styleType="option.styleType" :option="option"
			:key="key" :props="props" />
		<view class="rankList-list">
			<view class="rankList-list-box">
				<view class="table-tr u-flex table-title">
					<view class="table-th u-flex" v-for="(item,index) in option.columnOptions" :key="index"
						:style="{'width':option.columnOptions.length>1&&option.columnOptions.length<=3?100 / option.columnOptions.length + '%':'208rpx'}">
						<view class="commin-padding u-line-1">
							{{item.label}}
						</view>
					</view>
				</view>
				<view class="table-tr u-flex" v-for="(tr,i) in option.defaultValue" :key="i" @tap="jump(tr)">
					<view class="table-td" v-for="(item,index) in option.columnOptions" :key="index"
						:style="{'width':option.columnOptions.length>1&&option.columnOptions.length<=3?100 / option.columnOptions.length + '%':'208rpx'}">
						<view class="commin-padding"
							v-if="item.value === 'pm'&& i+1 > 0 && i+1<4 && option.styleType!==3 && option.styleType!==4">
							<view class="image-box">
								<u-image :src='tr.imgUrl' width='60rpx' height='60rpx'>
								</u-image>
							</view>
						</view>
						<view class="commin-padding" v-else>
							<view class="order" v-if="item.value === 'pm'" :style="option.styleType == 1?orderSty:''">
								<text>{{tr.index}}</text>
							</view>
							<view v-else class="u-line-1"
								:style="{'color':tr.index == 1?'rgb(206, 124, 31)':tr.index == 2?'rgb(111, 137, 172)':tr.index == 3?'rgb(141, 65, 18)':'#606266'}">
								{{tr[item.value]}}
							</view>
						</view>
					</view>
				</view>
			</view>
		</view>
	</view>
</template>
<script>
	import {
		getDataInterfaceRes
	} from '@/api/common'
	import platform from './platform.vue'
	import ordinary0 from '@/pages/portal/static/image/ordinary0.png'
	import ordinary1 from '@/pages/portal/static/image/ordinary1.png'
	import ordinary2 from '@/pages/portal/static/image/ordinary2.png'
	import medal0 from '@/pages/portal/static/image/medal0.png'
	import medal1 from '@/pages/portal/static/image/medal1.png'
	import medal2 from '@/pages/portal/static/image/medal2.png'

	export default {
		components: {
			platform
		},
		props: {
			config: {
				type: Object,
				required: true
			}
		},
		data() {
			return {
				orderSty: {
					'background': 'rgba(24, 144, 255, 0.39)',
					'border-radius': '50%',
					'opacity': 0.3,
					'color': '#fff'
				},
				option: {},
				props: {},
				ordinary0,
				ordinary1,
				ordinary2,
				medal0,
				medal1,
				medal2,
				key: +new Date()
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
			jump(item) {
				this.jnpf.solveAddressParam(item, this.config)
				this.jnpf.jumpLink(item.urlAddress)
			},
			init() {
				this.initData()
				if (!this.config.allRefresh.autoRefresh && this.config.refresh.autoRefresh) {
					setInterval(this.initData, this.config.refresh.autoRefreshTime * 60000)
				}
			},
			initData() {
				this.props = {
					label: this.config.option.columnOptions[0].value,
					value: this.config.option.columnOptions[1].value
				}
				this.option = JSON.parse(JSON.stringify(this.config.option))
				this.option.columnOptions.unshift({
					label: '排名',
					value: 'pm'
				})
				if (this.config.dataType === "dynamic") {
					if (!this.config.propsApi) return
					const query = {
						paramList: this.config.templateJson
					};
					getDataInterfaceRes(this.config.propsApi, query).then(res => {
						this.option.defaultValue = res.data || []
						this.handleAttrs()
					})
				} else {
					this.handleAttrs()
				}
			},
			handleAttrs() {
				if (!Array.isArray(this.option.defaultValue)) return
				this.option.defaultValue.forEach((o, i) => {
					o.index = i + 1
					if (i <= 2) {
						o.imgUrl = this.option.styleType == 1 || this.option.styleType == 3 ?
							this[`ordinary${i}`] : this[`medal${i}`]
					}
				})
				if (this.option.styleType == 3 || this.option.styleType == 4) {
					this.option.frontValue = this.option.defaultValue.slice(0, 3)
					if (this.option.defaultValue.length <= 3) this.option.columnOptions = []
					let newVal = this.option.defaultValue.slice(3, this.option.defaultValue.length)
					this.option.defaultValue = newVal
				}
				this.key = +new Date()
			},
		}
	}
</script>


<style lang="scss">
	.rankList-v {
		.rankList-list {
			width: 100%;
			height: 100%;
			padding: 0 20rpx;

			.rankList-list-box {
				width: 100%;
				overflow-x: scroll;

				.table-tr {
					width: 100%;
					height: 100%;

					.table-th,
					.table-td {
						height: 80rpx;
						border-bottom: 1px solid #EEF0F4;
						box-sizing: border-box;

						.commin-padding {
							height: 100%;
							width: 200rpx;
							display: flex;
							align-items: center;
							padding: 10rpx;
							justify-content: center;

							.image-box {
								.img {
									width: 100%;
									height: 100%;
								}
							}

							.order {
								width: 46rpx;
								height: 46rpx;
								line-height: 46rpx;
								text-align: center;
								margin-left: 8rpx;
							}
						}

						&:first-child {}
					}

					&:last-child {

						.table-th,
						.table-td {
							border-bottom: 0;
						}
					}
				}

			}
		}
	}
</style>