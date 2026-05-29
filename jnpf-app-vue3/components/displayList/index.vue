<template>
	<view class="u-flex extra-val" v-for="(extra,e) in getExtraList" :key="e" v-if="getExtraList.length">
		<view class="u-flex-col extra-child" v-if="extra.jnpfKey === 'table'">
			<view class="title" v-if="!extraObj[extra.value]?.length">{{extra.label}}</view>
			<view class="extra-child-item" v-for="(item,i) in extraObj[extra.value]" :key="i"
				v-if="extraObj[extra.value]?.length">
				<view class="u-flex-col extra-child-item-hd">
					<view class="title">{{extra.label}}</view>
					<view class="title">{{`(${i+1})`}}</view>
				</view>
				<view class="u-flex child" v-for="(child,c) in extra.children" :key="c">
					<view class="txt u-m-b-8">{{child.title}}</view>
					<view class="txt val u-line-1">
						{{extraObj[extra.value][i][child.dataIndex] }}
					</view>
				</view>
			</view>
			<view class="u-text-center" v-if="!extraObj[extra.value]?.length">
				暂无数据
			</view>
		</view>
		<view class="u-flex extra" v-else>
			<view class="txt">{{extra.label}}</view>
			<view class="txt val u-line-1" v-if="extraObj">{{extraObj[extra.value]}}</view>
		</view>
	</view>
</template>

<script>
	export default {
		props: {
			extraOptions: {
				type: Array,
				default: () => []
			},
			extraObj: {
				type: Object,
				default: () => {}
			}
		},
		computed: {
			getExtraList() {
				const extraOptions = this.extraOptions.filter(o => !!o.value);
				let list = [];
				for (let i = 0; i < extraOptions.length; i++) {
					const e = extraOptions[i];
					if (!e.value.includes('-')) {
						list.push(e);
					} else {
						let value = e.value.split('-')[0];
						let childValue = e.value.split('-')[1];
						let label = e.label.split('-')[0];
						let childLabel = e.label.replace(label + '-', '');
						let newItem = {
							jnpfKey: 'table',
							value,
							label,
							title: label,
							dataIndex: value,
							children: [],
							tableName: value
						};
						e.dataIndex = childValue;
						e.title = childLabel;
						if (!list.some(o => o.value === value)) list.push(newItem);
						for (let i = 0; i < list.length; i++) {
							if (list[i].value === value) {
								list[i].children.push(e);
								break;
							}
						}
					}
				}
				return list;
			}
		}
	}
</script>

<style lang="scss">
	.extra-val {
		min-height: 58rpx;
		margin-bottom: 0.8rem;
		justify-content: space-between;

		&:last-child {
			margin-bottom: 0;
		}

		.extra-child {
			width: 100%;
			border: 1rpx solid #f0f2f6;
			border-radius: 8rpx 8rpx 0 0;
			margin-bottom: 20rpx;

			.title {
				padding-left: 10rpx;
				background-color: #f0f2f6;
			}

			.extra-child-item {
				.extra-child-item-hd {
					background-color: #f0f2f6;
					border-radius: 8rpx 8rpx 0 0;
					padding-left: 10rpx;

					.title {
						height: 58rpx;
					}
				}

				.child {
					justify-content: space-between;
					// padding-left: 10rpx;
					min-height: 58rpx;
					border-bottom: 2rpx solid #f0f2f6;
					padding: 14rpx;
				}
			}
		}

		.extra {
			width: 100%;
			justify-content: space-between;
			padding-left: 10rpx;
			min-height: 2.18rem;
		}

		.txt {
			width: 160rpx;
			line-height: 1.2rem;
			font-size: 28rpx;
		}

		.val {
			text-align: right;
			width: 300rpx;
			overflow: hidden;
		}

	}
</style>