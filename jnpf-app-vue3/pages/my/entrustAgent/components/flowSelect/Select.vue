<template>
	<u-popup class="jnpf-tree-select-popup" :maskCloseAble="maskCloseAble" mode="right" :popup="false"
		v-model="showPopup" :safeAreaInsetBottom="safeAreaInsetBottom" :z-index="uZIndex" width="100%">
		<view class="jnpf-tree-select-body">
			<view class="jnpf-tree-select-title">
				<text class="icon-ym icon-ym-report-icon-preview-pagePre u-font-40 backIcon"
					@click.stop="handleConfirm"></text>
				<view class="title">{{$t('app.my.flowSelect')}}</view>
			</view>
			<view class="jnpf-tree-select-search">
				<u-search :placeholder="$t('app.apply.pleaseKeyword')" v-model="keyword" height="72"
					:show-action="false" @change="search(swiperCurrent)" bg-color="#f0f2f6" shape="square">
				</u-search>
			</view>
			<view class="jnpf-tree-selected">
				<view class="jnpf-tree-selected-head">
					<view>{{$t('component.jnpf.common.selected')}}</view>
					<view v-if="multiple" class="clear-btn" @click="cleanAll">{{$t('component.jnpf.common.clearAll')}}
					</view>
				</view>
				<view class="jnpf-tree-selected-box">
					<scroll-view scroll-y="true" style="max-height: 240rpx;">
						<view class="jnpf-tree-selected-list">
							<view class="u-selectTag u-selectTag-flow" v-for="(list,index) in selectList" :key="index">
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
			<view class="sticky-tabs">
				<u-tabs ref="tabs" :list="tabsList" :current="tabIndex" @change="tabChange" :is-scroll="true"
					name="fullName">
				</u-tabs>
			</view>
			<view class="jnpf-tree-select-tree">
				<scroll-view :style="{height: '100%'}" :refresher-enabled="false" :refresher-threshold="100"
					:scroll-with-animation='true' :refresher-triggered="triggered" @scrolltolower="handleScrollToLower"
					:scroll-y="true">
					<view class="lists_box list_top">
						<view class="list-cell-txt" v-for="(list,index) in list" :key="index"
							@click="handleNodeClick(list)">
							<view class="u-font-30 content">
								<view class="nameSty">{{list.fullName}}
								</view>
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
				<u-button class="buttom-btn" @click.stop="handleConfirm">{{$t('common.cancelText')}}</u-button>
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
	}
	import resources from '@/libs/resources.js'
	import {
		getFlowSelector
	} from '@/api/workFlow/flowEngine.js'
	import {
		useBaseStore
	} from '@/store/modules/base'

	const baseStore = useBaseStore()
	export default {
		props: {
			selectType: {
				type: String,
				default: 'all'
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
			isAuthority: {
				type: [String, Number],
				default: 0
			}
		},
		data() {
			return {
				noDataIcon: resources.message.nodata,
				tabWidth: 150,
				tabIndex: 0,
				tabsList: [],
				keyword: '',
				selectList: [],
				list: [],
				// 因为内部的滑动机制限制，请将tabs组件和swiper组件的current用不同变量赋值
				current: 0, // tabs组件的current值，表示当前活动的tab选项
				swiperCurrent: 0, // swiper组件的current值，表示当前那个swiper-item是活动的
				pagination: {
					currentPage: 1,
					pageSize: 20
				},
				total: 0,
				categoryId: '',
				triggered: false,
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
		},
		computed: {
			uZIndex() {
				// 如果用户有传递z-index值，优先使用
				return this.zIndex ? this.zIndex : this.$u.zIndex.popup;
			},
		},
		created() {
			setTimeout(() => {
				this.triggered = true;
			}, 1000)
			baseStore.getDictionaryData({
				sort: 'businessType'
			}).then(res => {
				this.tabsList.push({
					id: 0,
					encode: "all",
					fullName: "全部流程",
				})
				this.tabsList.push(...res)
			})
		},
		methods: {
			init() {
				this.upCallback()
				this.selectList = JSON.parse(JSON.stringify(this.selectedData)) || []
				// #ifdef MP-WEIXIN
				this.$nextTick(() => {
					this.$refs.tabs.init()
				})
				// #endif
			},
			delSelect(index) {
				this.selectList.splice(index, 1);
			},
			cleanAll() {
				this.selectList = [];
			},
			handleNodeClick(obj) {
				if (!this.multiple) this.selectList = []
				var isExist = false;
				for (var i = 0; i < this.selectList.length; i++) {
					if (this.selectList[i].id == obj.id) {
						isExist = true;
						break;
					}
				};
				!isExist && this.selectList.push(obj);
			},
			//父组件调用
			resetData() {
				this.list = []
				this.pagination = {
					currentPage: 1,
					pageSize: 20
				}
			},
			search(index) {
				this.pagination.currentPage = 1
				this.searchTimer && clearTimeout(this.searchTimer)
				this.searchTimer = setTimeout(() => {
					this.list = [];
					this.upCallback()
				}, 300)
			},
			// 切换菜单
			tabChange(index) {
				this.tabIndex = index
				this.pagination.currentPage = 1
				this.fullName = this.tabsList[this.tabIndex].fullName
				this.categoryId = !this.tabsList[this.tabIndex].id ? '' : this.tabsList[this.tabIndex].id
				this.list = [];
				this.upCallback()
			},
			handleScrollToLower() {
				if (this.pagination.pageSize * this.pagination.currentPage < this.total) {
					this.pagination.currentPage = this.pagination.currentPage + 1;
					this.upCallback()
				} else {
					uni.showToast({
						title: '没有更多信息啦！',
						icon: 'none'
					});
				}
			},
			upCallback() {
				let query = {
					currentPage: this.pagination.currentPage,
					pageSize: this.pagination.pageSize,
					keyword: this.keyword,
					category: this.categoryId ? this.categoryId : "",
					isAuthority: this.isAuthority == 2 ? 0 : 1,
					isDelegate: 1
				}
				this.loading = false
				getFlowSelector(query).then(res => {
					const list = res.data.list || [];
					list.map((o) => {
						o.fullName = o.fullName + '/' + o.enCode
					})
					this.list = this.list.concat(list);
					this.pagination = res.data.pagination
					this.total = this.pagination.total
				})
			},
			handleConfirm() {
				// #ifdef MP-WEIXIN
				if (this.moving) return;
				// #endif
				this.keyword = '';
				this.$emit('confirm', this.selectList);
			}
		}
	};
</script>
<style scoped lang="scss">
	.lists_box {
		height: 100%;

		.list-cell-txt {
			display: flex;
			box-sizing: border-box;
			width: 100%;
			padding: 20rpx 32rpx;
			overflow: hidden;
			color: $u-content-color;
			font-size: 28rpx;
			line-height: 48rpx;
			background-color: #fff;
		}
	}
</style>