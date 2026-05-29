<template>
	<u-popup class="jnpf-tree-select-popup" :maskCloseAble="maskCloseAble" mode="right" v-model="showPopup"
		:safeAreaInsetBottom="safeAreaInsetBottom" @close="close" :z-index="uZIndex" width="100%">
		<view class="jnpf-tree-select-body">
			<view class="jnpf-tree-select-title">
				<view class="icon-ym icon-ym-report-icon-preview-pagePre u-font-40 backIcon" @tap="close()"></view>
				<view class="title">选择提醒</view>
			</view>
			<view class="jnpf-tree-select-search">
				<u-search :placeholder="$t('app.apply.pleaseKeyword')" v-model="keyword" height="72"
					:show-action="false" @change="search()" bg-color="#f0f2f6" shape="square">
				</u-search>
			</view>
			<view class="jnpf-tree-select-tree">
				<scroll-view style="height: 100%" :refresher-enabled="false" :refresher-threshold="100"
					:scroll-with-animation='true' :refresher-triggered="triggered" @scrolltolower="handleScrollToLower"
					:scroll-y="true">
					<view class="list-box" v-if="list.length">
						<radio-group class="list-item" :value="innerValue" v-for="(item,index) in list" :key="index"
							@change="radioChange(item)">
							<label class="radio-label">
								<radio class="u-radio" :value="item.id" :checked="item.id === selectId" />
								<view class="list-item-content u-line-1">{{item.fullName}}</view>
							</label>
						</radio-group>
					</view>
					<view v-else class="nodata u-flex-col">
						<image :src="noDataIcon" mode="widthFix" class="noDataIcon" />
						{{$t('common.noData')}}
					</view>
				</scroll-view>
			</view>
			<!-- 底部按钮 -->
			<view class="jnpf-bottom-actions">
				<u-button class="buttom-btn" @click="close()">取消</u-button>
				<u-button class="buttom-btn" type="primary" @click.stop="handleConfirm()">确定</u-button>
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
		getMsgTemplate
	} from '@/api/portal/portal.js'
	export default {
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
				default: 99999
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
			// 取消按钮的文字
			cancelText: {
				type: String,
				default: '取消'
			},
			// 确认按钮的文字
			confirmText: {
				type: String,
				default: '确认'
			},
			selectedId: {
				type: String,
				default: ''
			},
		},
		data() {
			return {
				noDataIcon: resources.message.nodata,
				keyword: '',
				selectList: [],
				list: [],
				pagination: {
					currentPage: 1,
					pageSize: 20,
					messageSource: 4
				},
				total: 0,
				triggered: false,
				innerValue: '',
				selectId: this.selectedId,
				showPopup: false
			};
		},
		watch: {
			modelValue: {
				immediate: true,
				handler(val) {
					this.showPopup = val
				}
			},
		},
		computed: {
			uZIndex() {
				// 如果用户有传递z-index值，优先使用
				return this.zIndex ? this.zIndex : this.$u.zIndex.popup;
			},
			realProps() {
				return {
					...defaultProps,
				}
			}
		},
		methods: {
			cleanAll() {
				this.selectList = [];
			},
			radioChange(item) {
				this.selectId = item.id;
				this.innerValue = item.id;
				this.selectList = item
			},
			search() {
				this.searchTimer && clearTimeout(this.searchTimer)
				this.searchTimer = setTimeout(() => {
					this.resetData()
				}, 300)
			},
			resetData() {
				this.list = []
				this.pagination = {
					currentPage: 1,
					pageSize: 20,
					messageSource: 4
				}
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
					messageSource: this.pagination.messageSource
				}
				this.loading = false
				getMsgTemplate(query).then(res => {
					const list = res.data.list;
					this.list = this.list.concat(list);
					this.pagination = res.data.pagination
					this.total = this.pagination.total
					let item = this.list.filter(o => o.id == this.selectId)[0]
					if (item) this.selectList = item
				})
			},
			handleConfirm() {
				this.keyword = '';
				this.$emit('confirm', this.selectList);
				this.close();
			},
			close() {
				this.$emit('close', false);
			},
		}
	};
</script>