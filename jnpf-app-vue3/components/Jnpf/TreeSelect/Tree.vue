<template>
	<view class="u-select">
		<u-popup :maskCloseAble="maskCloseAble" mode="bottom" :popup="false" v-model="showPopup" length="auto"
			:safeAreaInsetBottom="safeAreaInsetBottom" @close="close" :z-index="uZIndex">
			<view class="u-select">
				<view class="u-select__header" @touchmove.stop.prevent="">
					<view class="u-select__header__cancel u-select__header__btn" :style="{ color: cancelColor }"
						hover-class="u-hover-class" :hover-stay-time="150" @tap="close()">
						{{cancelText}}
					</view>
					<view class="u-select__header__title">{{title}}</view>
					<view class="u-select__header__confirm u-select__header__btn"
						:style="{ color: moving ? cancelColor : confirmColor }" hover-class="u-hover-class"
						:hover-stay-time="150" @touchmove.stop="" @tap.stop="handleConfirm()">
						{{confirmText}}
					</view>
				</view>
				<view class="search-box_sticky" v-if="filterable">
					<view class="search-box">
						<u-search :placeholder="$t('app.apply.pleaseKeyword')" height="72" :show-action="false" bg-color="#f0f2f6"
							shape="square" v-model="filterText">
						</u-search>
					</view>
				</view>
				<view class="u-select__body u-select__body__treeSelect">
					<view class="tree-box">
						<scroll-view :scroll-y="true" style="height: 100%">
							<ly-tree ref="tree" :node-key="realProps.value" :tree-data="options" :props="realProps"
								:show-node-icon="true" :filter-node-method="filterNode" child-visible-for-filter-node
								check-on-click-node :expand-on-click-node="false" default-expand-all
								:show-radio="!multiple" :show-checkbox="multiple" />
						</scroll-view>
					</view>
				</view>
			</view>
		</u-popup>
	</view>
</template>

<script>
	const defaultProps = {
		label: 'fullName',
		value: 'id',
		icon: 'icon',
		children: 'children'
	}
	import LyTree from '@/components/ly-tree/ly-tree.vue'
	export default {
		name: "tree-select",
		components: {
			LyTree
		},
		props: {
			options: {
				type: Array,
				default () {
					return [];
				}
			},
			filterable: {
				type: Boolean,
				default: false
			},
			// 是否显示边框
			border: {
				type: Boolean,
				default: true
			},
			// 通过双向绑定控制组件的弹出与收起
			modelValue: {
				type: Boolean,
				default: false
			},
			// "取消"按钮的颜色
			cancelColor: {
				type: String,
				default: '#606266'
			},
			// "确定"按钮的颜色
			confirmColor: {
				type: String,
				default: '#2979ff'
			},
			// 弹出的z-index值
			zIndex: {
				type: [String, Number],
				default: 0
			},
			safeAreaInsetBottom: {
				type: Boolean,
				default: false
			},
			// 是否允许通过点击遮罩关闭Picker
			maskCloseAble: {
				type: Boolean,
				default: true
			},
			defaultValue: {
				type: Array,
				default: () => []
			},
			props: {
				type: Object,
				default: () => ({
					label: 'fullName',
					value: 'id',
					icon: 'icon',
					children: 'children'
				})
			},
			// 只能选择最后一层的数值
			lastLevel: {
				type: Boolean,
				default: false
			},
			// 只能选择最后一层的数值时，需要根据lastLevelKey来判断是否最后一层
			lastLevelKey: {
				type: String,
				default: "hasChildren"
			},
			lastLevelValue: {
				default: false
			},
			multiple: {
				type: Boolean,
				default: false
			},
			// 顶部标题
			title: {
				type: String,
				default: ''
			},
			// 取消按钮的文字
			cancelText: {
				type: String,
				default: '取消'
			},
			// 确认按钮的文字
			confirmText: {
				type: String,
				default: '确认'
			}
		},
		data() {
			return {
				filterText: '',
				moving: false,
				showPopup: false
			};
		},
		watch: {
			// 在select弹起的时候，重新初始化所有数据
			modelValue: {
				immediate: true,
				handler(val) {
					this.showPopup = val
					if (val) setTimeout(() => this.init(), 10);
				}
			},
			filterText(val) {
				this.$refs.tree.filter(val);
			}
		},
		computed: {
			uZIndex() {
				// 如果用户有传递z-index值，优先使用
				return this.zIndex ? this.zIndex : this.$u.zIndex.popup;
			},
			realProps() {
				return {
					...defaultProps,
					...this.props
				}
			}
		},
		methods: {
			// 标识滑动开始，只有微信小程序才有这样的事件
			pickstart() {
				// #ifdef MP-WEIXIN
				this.moving = true;
				// #endif
			},
			// 标识滑动结束
			pickend() {
				// #ifdef MP-WEIXIN
				this.moving = false;
				// #endif
			},
			filterNode(value, data) {
				if (!value) return true;
				return data[this.realProps.label].indexOf(value) !== -1;
			},
			init() {
				this.filterText = ''
				this.setSelectValue();
			},
			// 获取默认选中的值
			setSelectValue() {
				this.$nextTick(() => {
					this.$refs.tree.setCheckedKeys(this.defaultValue)
				})
			},
			close() {
				this.$emit('close');
			},
			handleConfirm() {
				// #ifdef MP-WEIXIN
				if (this.moving) return;
				// #endif
				let selectValue = this.$refs.tree.getCheckedNodes()
				if (this.lastLevel) {
					selectValue = selectValue.filter(o => o[this.lastLevelKey] == this.lastLevelValue)
				}
				if (!selectValue.length) return
				this.$emit('confirm', selectValue);
				this.close();
			}
		}
	};
</script>

<style scoped lang="scss">
	.u-select {

		&__action {
			position: relative;
			line-height: $u-form-item-height;
			height: $u-form-item-height;

			&__icon {
				position: absolute;
				right: 20rpx;
				top: 50%;
				transition: transform .4s;
				transform: translateY(-50%);
				z-index: 1;

				&--reverse {
					transform: rotate(-180deg) translateY(50%);
				}
			}
		}

		&__hader {
			&__title {
				color: $u-content-color;
			}
		}

		&--border {
			border-radius: 6rpx;
			border-radius: 4px;
			border: 1px solid $u-form-item-border-color;
		}

		&__header {
			display: flex;
			align-items: center;
			justify-content: space-between;
			height: 80rpx;
			padding: 0 40rpx;
		}

		&__body {
			width: 100%;
			height: 500rpx;
			overflow: hidden;
			background-color: #fff;

			.tree-box {
				height: 100%;
			}

			&__picker-view {
				height: 100%;
				box-sizing: border-box;

				&__item {
					display: flex;
					align-items: center;
					justify-content: center;
					font-size: 32rpx;
					color: $u-main-color;
					padding: 0 8rpx;
				}
			}
		}
	}
</style>