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
			<selectedBox :clearable="clearable" :selectList="selectList" ref="selectedBox"
				@setSelectList="setSelectList"></selectedBox>
			<view class="u-p-l-32 u-p-r-32" v-if="selectType !== 'all'">全部数据</view>
			<view class="jnpf-user-content" v-if="selectType === 'all'">
				<!-- tabs切换 -->
				<view class="search-box_sticky u-userSelect_sticky">
					<!-- #ifdef MP-WEIXIN -->
					<u-tabs-swiper activeColor="#1890ff" ref="tabs" :list="!multiple?[tabsList[0]]:tabsList"
						:current="current" @change="change" :is-scroll="!multiple" :show-bar="false">
					</u-tabs-swiper>
					<!-- #endif -->
					<!-- #ifndef MP-WEIXIN -->
					<u-tabs-swiper activeColor="#1890ff" ref="tabs" :list="!multiple?[tabsList[0]]:tabsList"
						:current="current" @change="change" :is-scroll="!multiple"></u-tabs-swiper>
					<!-- #endif -->
				</view>
				<swiper :current="swiperCurrent" @transition="transition" @animationfinish="animationfinish"
					class="swiper-box">
					<swiper-item>
						<scroll-view :scroll-y="true" class="scroll-view">
							<ly-tree ref="tree" :node-key="realProps.value" :tree-data="options0"
								:highlight-current="true" @node-click="handleNodeClick" :props="realProps"
								:show-node-icon="true" :load="loadNode" :default-expand-all="false" lazy
								v-if="swiperCurrent == 0" :expandOnClickNode="multiple?false:true" />
						</scroll-view>
					</swiper-item>
					<swiper-item v-if="multiple">
						<scroll-view :scroll-y="true" class="scroll-view">
							<ly-tree ref="tree" :node-key="realProps.value" :tree-data="roleOption"
								:highlight-current="true" @node-click="handleNodeClick" :props="realProps"
								:ready="!!roleOption.length" :filter-node-method="filterNode" v-if="swiperCurrent == 1"
								:expandOnClickNode="false" :show-node-icon="true" />
						</scroll-view>
					</swiper-item>
					<swiper-item v-if="multiple">
						<scroll-view :scroll-y="true" class="scroll-view">
							<ly-tree ref="tree" :node-key="realProps.value" :tree-data="posOption"
								:highlight-current="true" @node-click="handleNodeClick" :props="realProps"
								:ready="!!posOption.length" :filter-node-method="filterNode" v-if="swiperCurrent == 2"
								:expandOnClickNode="false" :show-node-icon="true" />
						</scroll-view>
					</swiper-item>
					<swiper-item v-if="multiple">
						<scroll-view :scroll-y="true" class="scroll-view">
							<ly-tree ref="tree" :node-key="realProps.value" :tree-data="groupOption"
								:highlight-current="true" @node-click="handleNodeClick" :props="realProps"
								:ready="!!groupOption.length" :filter-node-method="filterNode" v-if="swiperCurrent == 3"
								:expandOnClickNode="false" :show-node-icon="true" />
						</scroll-view>
					</swiper-item>
				</swiper>
			</view>
			<view v-else class="jnpf-tree-select-tree">
				<scroll-view id="scroll-view-h" class="scroll-view" :refresher-enabled="false"
					:refresher-threshold="100" :scroll-with-animation='true' :refresher-triggered="triggered"
					@scrolltolower="handleScrollToLower" :scroll-y="true">
					<view class="lists_box">
						<view class="list-cell-txt u-border-bottom" v-for="(list,index) in list" :key="index"
							@click="onSelect(list)">
							<view class="avatar">
								<u-avatar :src="baseURL+list.headIcon" mode="circle" size="default"></u-avatar>
							</view>
							<view class="u-font-30 content">
								<view>{{list.fullName}}</view>
								<view class="organize">{{list.organize}}</view>
							</view>
						</view>
						<view v-if="!list.length" class="nodata u-flex-col">
							<image :src="noDataIcon" mode="widthFix" class="noDataIcon"></image>
							{{$t('common.noData')}}
						</view>
					</view>
				</scroll-view>
			</view>
			<!-- 底部按钮 -->
			<view class="jnpf-tree-select-actions">
				<u-button class="buttom-btn" @click="close">{{$t('common.cancelText')}}</u-button>
				<u-button class="buttom-btn" type="primary"
					@click.stop="handleConfirm">{{$t('common.okText')}}</u-button>
			</view>
		</view>
	</u-popup>
</template>
<script>
	const defaultProps = {
		label: 'fullName',
		value: 'id',
		icon: 'icon',
		children: 'children'
	}
	import {
		getUserSelectorNew,
		getSelectedUserList,
		getGroupSelector,
		getPositionSelector,
		getRoleSelector
	} from '@/api/common.js'
	import resources from '@/libs/resources.js'
	import selectedBox from './selected-box.vue'
	import LyTree from '@/components/ly-tree/ly-tree.vue'
	export default {
		name: "tree-select",
		components: {
			LyTree,
			selectedBox
		},
		props: {
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
			// 通过双向绑定控制组件的弹出与收起
			modelValue: {
				type: Boolean,
				default: false
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
			multiple: {
				type: Boolean,
				default: false
			},
			title: {
				type: String,
				default: ''
			},
		},
		data() {
			return {
				noDataIcon: resources.message.nodata,
				triggered: false,
				moving: false,
				selectList: [],
				keyword: '',
				tabsList: [{
						name: '部门'
					},
					{
						name: '角色'
					},
					{
						name: '岗位'
					},
					{
						name: '分组'
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
				showPopup: false,
				roleOption: [],
				posOption: [],
				groupOption: []
			};
		},
		watch: {
			// 在select弹起的时候，重新初始化所有数据
			modelValue: {
				immediate: true,
				handler(val) {
					this.current = 0
					this.swiperCurrent = 0
					this.showPopup = val
					if (val) setTimeout(() => this.init(), 10);
				}
			}
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
			init() {
				this.pagination.currentPage = 1
				this.list = []
				if (this.selectType !== 'all') this.getSelectedUserList()
				if (this.multiple && this.selectType === 'all') this.initData()
				this.selectList = JSON.parse(JSON.stringify(this.selectedData))
			},
			initData() {
				getGroupSelector().then(res => {
					this.groupOption = res.data
				})
				getPositionSelector().then(res => {
					this.posOption = res.data.list
				})
				getRoleSelector().then(res => {
					this.roleOption = res.data.list
				})
			},
			filterNode(value, data) {
				if (!value) return true;
				return data[this.props.label].indexOf(value) !== -1;
			},
			setSelectList(e) {
				this.selectList = e
			},
			handleScrollToLower() {
				this.getSelectedUserList()
			},
			getSelectedUserList() {
				let query = this.query;
				delete(query.ableDepIds)
				this.pagination.keyword = this.keyword
				query.pagination = this.pagination
				getSelectedUserList(query).then(res => {
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
			// tab栏切换
			change(index) {
				this.swiperCurrent = index;
				this.keyword = ''
			},
			search(index) {
				this.searchTimer && clearTimeout(this.searchTimer)
				this.searchTimer = setTimeout(() => {
					this.pagination = {
						currentPage: 1,
						pageSize: 20
					}
					if (this.selectType === 'all') {
						if (this.swiperCurrent == 0 && this.current == 0) {
							getUserSelectorNew(0, this.keyword).then(res => {
								this.options0 = res.data.list
							})
						} else {
							this.$nextTick(() => {
								this.$refs.tree.filter(this.keyword);
							})
						}
					} else {
						let query = this.query;
						this.pagination.keyword = this.keyword
						query.pagination = this.pagination
						getSelectedUserList(query).then(res => {
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
			},
			handleNodeClick(obj) {
				if ((!this.multiple && this.swiperCurrent == 0 && obj.data.type !== 'user') || (this
						.swiperCurrent == 3 && obj.data.type == 0) || !obj.data.type) return;
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
				let id = '';
				let userId = []
				for (let i = 0; i < this.selectList.length; i++) {
					id = this.selectList[i].id + '--' + this.selectList[i].type;
					userId.push(id)
				}
				this.$emit('confirm', userId, this.selectList);
				this.close();
			},
			close() {
				this.keyword = ""
				this.$emit('close');
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