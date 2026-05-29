<template>
	<view class="flowLaunch-v">
		<view class="flow-list" v-if="list.length > 0">
			<view class="flow-list-box">
				<u-swipe-action :show="item.show" :index="index" v-for="(item, index) in list" :key="item.id"
					@click="handleClick(index)" @open="open" :options="options" :btnWidth="160" :vibrateShort="true"
					:disabled="item.swipeAction">
					<view class="item" @click="goDetail(item)" :id="'item'+index" ref="mydom">
						<view class="item-left">
							<view class="item-left-top">
								<view class='common-lable-entrust' v-if="item.delegateUser">
									{{!category ? '委托' : '代理' }}
								</view>
								<view class='common-lable'
									:class="{'urgent-lable':item.flowUrgent==2,'important-lable':item.flowUrgent==3}">
									{{getLableValue(item.flowUrgent)}}
								</view>
								<text class="title u-font-28 u-line-1">{{item.fullName}}</text>
							</view>
							<text class="title u-line-1 u-font-24">审批节点：{{item.currentNodeName}}<text
									class="titInner">{{item.thisStep ? item.thisStep : ''}}</text></text>
							<text class="time title u-font-24">发起时间：<text
									class="titInner">{{item.startTime?$u.timeFormat(item.startTime, 'yyyy-mm-dd hh:MM:ss'):''}}</text></text>
						</view>
						<view class="item-right">
							<image :src="item.flowStatus" mode="widthFix" class="item-right-img">
							</image>
						</view>
					</view>
				</u-swipe-action>
			</view>
		</view>
	</view>
</template>
<script>
	import {
		delFlowLaunch
	} from '@/api/workFlow/template'
	export default {
		name: "FlowList",
		props: {
			list: {
				type: Array,
				default: () => []
			},
			swipeAction: {
				type: Boolean,
				default: false
			},
			category: {
				type: [String, Number],
				default: '0'
			},
		},
		data() {
			return {
				options: [{
					text: '删除',
					style: {
						backgroundColor: '#dd524d'
					}
				}],
				title: '',
			};
		},

		methods: {
			open(index) {
				// 先将正在被操作的swipeAction标记为打开状态，否则由于props的特性限制，
				// 原本为'false'，再次设置为'false'会无效
				this.list[index].show = true;
				this.list.map((val, idx) => {
					if (index != idx) this.list[idx].show = false;
				})
			},
			goDetail(item) {
				const config = {
					opType: item.opType,
					operatorId: item.id,
					category: this.category,
					...item
				}
				uni.navigateTo({
					url: '/pages/workFlow/flowBefore/index?config=' +
						this.jnpf.base64.encode(JSON.stringify(config))
				})
			},
			handleClick(index) {
				const item = this.list[index]
				if ([1, 2, 3, 5].includes(item.status)) {
					this.$u.toast("流程正在审核,请勿删除")
					this.list[index].show = false
					return
				}
				delFlowLaunch(item.id).then(res => {
					this.$u.toast(res.msg)
					this.list.splice(index, 1)
				})
			},
			getLableValue(value) {
				var lableValue = ''
				switch (value) {
					case 1:
						lableValue = '普通'
						break;
					case 2:
						lableValue = '重要'
						break;
					case 3:
						lableValue = '紧急'
						break;
					default:
						lableValue = '普通'
						break;
				}
				return lableValue
			}
		}
	};
</script>
<style scoped lang="scss">
	.flowLaunch-v {
		width: 100%;

		.flow-list-box {
			width: 95%;

			.item-left-top {
				display: flex;
				width: 100%;

				.common-lable {
					font-size: 24rpx;
					padding: 2rpx 8rpx;
					margin-right: 8rpx;
					border-radius: 8rpx;
					color: #409EFF;
					border: 1px solid #409EFF;
					background-color: #e5f3fe;

					&-entrust {
						margin-right: 8rpx;
						font-size: 24rpx;
						padding: 2rpx 8rpx;
						border-radius: 8rpx;
						background-color: #dbf1e1;
						color: #19be6b;
						border: 1px solid #19be6b;
					}
				}

				.urgent-lable {
					color: #E6A23C;
					border: 1px solid #E6A23C;
					background-color: #fef6e5;
				}

				.important-lable {
					color: #F56C6C;
					border: 1px solid #F56C6C;
					background-color: #fee5e5;
				}

				.title {
					width: unset;
					flex: 1;
					min-width: 0;
				}
			}
		}
	}
</style>