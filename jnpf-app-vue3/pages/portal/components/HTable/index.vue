<template>
	<view class="tabs-v">
		<template v-if="option.defaultValue && option.defaultValue.length">
			<scroll-view scroll-y="true" :style="option.defaultValue<5?'' :'height: 620rpx'">
				<div v-for="(item, i) in option.defaultValue" :key="i">
					<div class="app-title">
						<div class='name' v-for="(it, ii) in option.appColumnList" :key="ii" style="" @tap="jump(item)">
							<text v-if="option.showName" :style="{'font-weight': option.nameFontWeight?'bolder':'normal',
          'font-size':option.nameFontSize+'px',color:option.nameFontColor}">
								{{it.fullName}}:</text>
							<text :style="{'font-weight': option.dataFontWeight?'bolder':'normal','font-size':option.dataFontSize+'px',
          color:option.dataFontColor}">{{item[it.filedName]}}</text>
						</div>
						<u-line v-if="i<option.defaultValue.length-1"></u-line>
					</div>
				</div>
			</scroll-view>
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
		getDataInterfaceRes
	} from '@/api/common'
	import resources from '@/libs/resources.js'
	export default {

		props: {
			config: {
				type: Object,
				required: true
			}
		},
		data() {
			return {
				icon: resources.message.nodata,
				option: {},
				propsApi: ""
			}
		},
		created() {
			this.init()
		},
		methods: {
			jump(item) {
				this.jnpf.solveAddressParam(item, this.config)
				this.jnpf.jumpLink(item.urlAddress)
			},
			init() {
				this.initData()
				if (this.config.refresh.autoRefresh) {
					setInterval(this.initData, this.config.refresh.autoRefreshTime * 60000)
				}
			},
			initData() {
				if (this.config.dataType === "dynamic") {
					if (!this.config.propsApi) return
					const query = {
						paramList: this.config.templateJson
					};
					getDataInterfaceRes(this.config.propsApi, query).then(res => {
						this.config.option.defaultValue = res.data || []
						this.handleAttrs()
					})
				} else {
					this.handleAttrs()
				}
			},
			handleAttrs() {
				this.option = this.config.option
				this.option.appColumnList.map((o) => {
					if (o.fullNameI18nCode) o.fullName = this.$t(o.fullNameI18nCode)
				})
				this.option.defaultValue = this.option.defaultValue.slice(0, this.option.appCount || 50)
			}
		}
	}
</script>


<style lang="scss">
	.tabs-v {
		padding: 16rpx 10rpx 10rpx;
		height: 100%;
		overflow: hidden;

	}

	.name {
		font-size: 28rpx;
		display: inline-block;
		width: calc(100% - 100rpx);
		white-space: nowrap;
		text-overflow: ellipsis;
		overflow: hidden;
		word-break: break-all;
		vertical-align: top;
		margin-bottom: 10rpx;
	}

	.app-title {
		padding: 8rpx 0px 10rpx 20px;
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