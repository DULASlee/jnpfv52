<template>
	<u-popup class="jnpf-tree-select-popup" :maskCloseAble="maskCloseAble" mode="right" v-model="showPopup"
		:safeAreaInsetBottom="safeAreaInsetBottom" @close="close" :z-index="uZIndex" width="100%">
		<view class="jnpf-tree-select-body">
			<view class="jnpf-tree-select-title">
				<text class="icon-ym icon-ym-report-icon-preview-pagePre u-font-40 backIcon" @tap="close"></text>
				<view class="title">选择用户</view>
			</view>
			<view class="jnpf-tree-select-search">
				<u-search :placeholder="$t('app.apply.pleaseKeyword')" v-model="keyword" height="72"
					:show-action="false" @change="search(swiperCurrent)" bg-color="#f0f2f6" shape="square">
				</u-search>
			</view>
			<view class="jnpf-tree-selected">
				<view class="jnpf-tree-selected-head">
					<view>{{$t('component.jnpf.common.selected')}}</view>
					<view v-if="multiple" class="clear-btn" @click="cleanAll">
						{{$t('component.jnpf.common.clearAll')}}
					</view>
				</view>
				<view class="jnpf-tree-selected-box">
					<scroll-view scroll-y="true" style="max-height: 240rpx;">
						<view class="jnpf-tree-selected-list">
							<view class="u-selectTag" v-for="(list,index) in selectList" :key="index">
								<u-avatar class="avatar" :src="baseURL+list.headIcon" mode="circle"
									size="mini"></u-avatar>
								<view class="jnpf-tree-selected-content">
									<view class="name-box">
										<view class="name">{{list.fullName}}</view>
										<u-icon name="close" class="close" @click='delSelect(index)'></u-icon>
									</view>
									<view class="organize">{{list.organize}}</view>
								</view>
							</view>
						</view>
					</scroll-view>
				</view>
			</view>
			<view class="listTitle" v-if="selectType !== 'all'">全部数据</view>
			<view class="jnpf-user-content" v-if="selectType === 'all'">
				<!-- #ifdef MP-WEIXIN -->
				<u-tabs-swiper activeColor="#1890ff" ref="tabs" :list="tabsList" :current="current" @change="change"
					:is-scroll="false" :show-bar="false"></u-tabs-swiper>
				<!-- #endif -->
				<!-- #ifndef MP-WEIXIN -->
				<u-tabs-swiper activeColor="#1890ff" ref="tabs" :list="tabsList" :current="current" @change="change"
					:is-scroll="false"></u-tabs-swiper>
				<!-- #endif -->
				<swiper :current="swiperCurrent" @transition="transition" @animationfinish="animationfinish"
					class="swiper-box">
					<swiper-item>
						<scroll-view :scroll-y="true" class="scroll-view">
							<ly-tree ref="tree" :node-key="realProps.value" :expand-on-click-node="true"
								:tree-data="options0" check-on-click-node :show-checkbox="false"
								:default-expand-all="false" :highlight-current="true" @node-click="handleNodeClick"
								:props="realProps" :show-node-icon="true" :show-radio="false" :load="loadNode" lazy />
						</scroll-view>
					</swiper-item>

					<swiper-item>
						<scroll-view :scroll-y="true" class="scroll-view">
							<ly-tree ref="tree" :node-key="realProps.value" :expand-on-click-node="true"
								check-on-click-node :show-checkbox="false" :default-expand-all="false"
								:highlight-current="true" @node-click="handleNodeClick" :props="realProps"
								:show-node-icon="true" :show-radio="false" :tree-data="options" />
						</scroll-view>
					</swiper-item>

					<swiper-item>
						<scroll-view :scroll-y="true" class="scroll-view">
							<ly-tree ref="tree" :node-key="realProps.value" :expand-on-click-node="true"
								check-on-click-node :show-checkbox="false" :default-expand-all="false"
								:highlight-current="true" @node-click="handleNodeClick" :props="realProps"
								:show-node-icon="true" :show-radio="false" :tree-data="options" />
						</scroll-view>
					</swiper-item>
				</swiper>
			</view>
			<view v-else class="jnpf-tree-select-tree">
				<scroll-view class="scroll-view" :refresher-enabled="false" :refresher-threshold="100"
					:scroll-with-animation='true' :refresher-triggered="triggered" @scrolltolower="handleScrollToLower"
					:scroll-y="true">
					<view class="lists_box">
						<view class="list-cell-txt u-border-bottom" v-for="(list,index) in list" :key="index"
							@click="onSelect(list)">
							<u-avatar class="avatar" :src="baseURL+list.headIcon" mode="circle"
								size="default"></u-avatar>
							<view class="u-font-30 content">
								<view>{{list.fullName}}</view>
								<view class="organize">{{list.organize}}</view>
							</view>
						</view>
						<view v-if="list.length<1" class="nodata u-flex-col">
							<image :src="noDataIcon" mode="widthFix" class="noDataIcon"></image>
							{{$t('common.noData')}}
						</view>
					</view>
				</scroll-view>
			</view>
			<!-- 底部按钮 -->
			<view class="jnpf-tree-select-actions">
				<u-button class="buttom-btn" @click="close()">{{$t('common.cancelText')}}</u-button>
				<u-button class="buttom-btn" type="primary"
					@click.stop="handleConfirm">{{$t('common.okText')}}</u-button>
			</view>
		</view>
	</u-popup>
</template>
<script>
	/**
	 * tree-select 树形选择器
	 * @property {Boolean} v-model 布尔值变量，用于控制选择器的弹出与收起
	 * @property {Boolean} safe-area-inset-bottom 是否开启底部安全区适配(默认false)
	 * @property {String} cancel-color 取消按钮的颜色（默认#606266）
	 * @property {String} confirm-color 确认按钮的颜色(默认#2979ff)
	 * @property {String} confirm-text 确认按钮的文字
	 * @property {String} cancel-text 取消按钮的文字
	 * @property {Boolean} mask-close-able 是否允许通过点击遮罩关闭Picker(默认true)
	 * @property {String Number} z-index 弹出时的z-index值(默认10075)
	 * @event {Function} confirm 点击确定按钮，返回当前选择的值
	 */
	const defaultProps = {
		label: 'fullName',
		value: 'id',
		icon: 'icon',
		children: 'children'
	}
	import {
		getUserSelectorNew,
		getSubordinates,
		getOrganization,
	} from '@/api/common.js'
	import {
		getTaskUserList
	} from '@/api/workFlow/flowBefore'
	import resources from '@/libs/resources.js'
	import LyTree from '@/components/ly-tree/ly-tree.vue'
	export default {
		name: "comment-tree-select",
		components: {
			LyTree
		},
		props: {
			taskId: {
				type: String,
				default: ''
			},
			selectType: {
				type: String,
				default: 'all'
			},
			clearable: {
				type: Boolean,
				default: false
			},
			query: {
				type: Object,
				default: () => ({})
			},
			selectedData: {
				type: Array,
				default () {
					return [];
				}
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
				default: 999
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
			props: {
				type: Object,
				default: () => ({
					label: 'fullName',
					value: 'id',
					icon: 'icon',
					children: 'children',
					isLeaf: 'isLeaf'
				})
			},
			//多选
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
				noDataIcon: resources.message.nodata,
				triggered: false,
				moving: false,
				selectList: [],
				keyword: '',
				tabsList: [{
						name: '全部数据'
					},
					{
						name: '当前组织'
					},
					{
						name: '我的下属'
					}
				],
				current: 0,
				swiperCurrent: 0,
				options: [],
				options0: [],
				list: [],
				pagination: {
					currentPage: 1,
					pageSize: 20
				},
				total: 0,
				height: 0,
				showPopup: false
			};
		},
		watch: {
			// 在select弹起的时候，重新初始化所有数据
			modelValue: {
				immediate: true,
				handler(val) {
					this.showPopup = val
					if (val) setTimeout(() => this.getInfoList(), 10);
				}
			},
		},
		computed: {
			baseURL() {
				return this.define.baseURL
			},
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
		created() {
			this._freshing = false;
			setTimeout(() => {
				this.triggered = true;
			}, 1000)
		},
		methods: {
			handleScrollToLower() {
				this.getInfoList()
			},
			getInfoList() {
				this.pagination.keyword = this.keyword
				getTaskUserList(this.taskId, this.pagination).then(res => {
					const list = res.data.list;
					if (!list.length && this.pagination.currentPage != 1) return uni.showToast({
						title: '没有更多信息啦！',
						icon: 'none'
					});
					this.list = this.list.concat(list);
					this.pagination.currentPage++
				})
			},
			onSelect(list) {
				if (!this.multiple) this.selectList = []
				let flag = false;
				for (let i = 0; i < this.selectList.length; i++) {
					if (this.selectList[i].id === list.id) {
						flag = true;
						return
					}
				};
				!flag && this.selectList.push(list)
			},
			loadNode(node, resolve) {
				if (node.level === 0) {
					getUserSelectorNew(node.level).then(res => {
						resolve(res.data.list)
					})
				} else {
					getUserSelectorNew(node.data.id).then(res => {
						const data = res.data.list
						resolve(data)
					})
				}
			},
			change(index) {
				this.swiperCurrent = index;
				this.keyword = ''
				if (this.swiperCurrent !== 0) this.handOff(this.swiperCurrent)
			},
			handOff(swiperCurrent) {
				let method = swiperCurrent == 1 ? getOrganization : getSubordinates;
				method(this.keyword).then(res => {
					this.options = res.data
				})
			},
			search(index) {
				this.searchTimer && clearTimeout(this.searchTimer)
				this.searchTimer = setTimeout(() => {
					this.pagination = {
						currentPage: 1,
						pageSize: 20
					}
					if (this.selectType === 'all') {
						if (index !== 0) this.handOff(index)
						getUserSelectorNew(0, this.keyword).then(res => {
							this.options0 = res.data.list
						})
					} else {
						this.pagination.keyword = this.keyword
						getTaskUserList(this.taskId, this.pagination).then(res => {
							const list = res.data.list;
							this.list = list
							this.pagination = res.data.pagination
							this.total = this.pagination.total
						})
					}
				}, 300)
			},
			transition({
				detail: {
					dx
				}
			}) {
				this.$refs.tabs.setDx(dx);
			},
			animationfinish({
				detail: {
					current
				}
			}) {
				this.$refs.tabs.setFinishCurrent(current);
				this.swiperCurrent = current;
				this.current = current;
				if (this.swiperCurrent !== 0) this.handOff(this.swiperCurrent)
			},
			handleNodeClick(obj) {
				if (this.swiperCurrent === 0 && obj.data.type !== 'user') return
				if (!this.multiple) this.selectList = []
				var isExist = false;
				for (var i = 0; i < this.selectList.length; i++) {
					if (this.selectList[i].id == obj.data.id) {
						isExist = true;
						break;
					}
				};
				!isExist && this.selectList.push(obj.data);
			},
			delSelect(index) {
				this.selectList.splice(index, 1);
			},
			cleanAll() {
				this.selectList = [];
			},
			handleConfirm() {
				// #ifdef MP-WEIXIN
				if (this.moving) return;
				// #endif
				this.keyword = '';
				this.$emit('confirm', this.selectList);
				this.close();
			},
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
			close() {
				this.$emit('close');
			},
			resetData() {
				this.list = []
				this.pagination = {
					currentPage: 1,
					pageSize: 20
				}
			}
		}
	};
</script>
<style scoped lang="scss">
	.jnpf-user-content {
		flex: 1;
		display: flex;
		flex-direction: column;

		.swiper-box {
			flex: 1;

		}
	}

	.listTitle {
		padding: 10rpx 30rpx;
		font-size: 32rpx;
	}

	.scroll-view {
		height: 100%;
	}

	.lists_box {
		height: 100%;

		.nodata {
			height: 100%;
			margin: auto;
			align-items: center;
			justify-content: center;
			color: #909399;

			.noDataIcon {
				width: 300rpx;
				height: 210rpx;
			}
		}

		.list-cell-txt {
			display: flex;
			box-sizing: border-box;
			width: 100%;
			padding: 20rpx 32rpx;
			overflow: hidden;
			color: $u-content-color;
			font-size: 28rpx;
			line-height: 24px;
			background-color: #fff;

			.content {
				width: 85%;
				margin-left: 15rpx;

				.organize {
					white-space: nowrap;
					overflow: hidden; //超出的文本隐藏
					text-overflow: ellipsis
				}
			}

			.department {
				color: #9A9A9A;
			}
		}
	}
</style>