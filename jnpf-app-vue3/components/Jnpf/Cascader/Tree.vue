<template>
	<u-popup class="jnpf-tree-select-popup" width="100%" v-model="showPopup" length="auto" mode="right" :popup="false"
		:safeAreaInsetBottom="safeAreaInsetBottom" :maskCloseAble="maskCloseAble" :z-index="uZIndex" @close="close">
		<view class="jnpf-tree-select-body">
			<view class="jnpf-tree-select-title">
				<text class="icon-ym icon-ym-report-icon-preview-pagePre backIcon" @tap="close"></text>
				<view class="title">级联选择</view>
			</view>
			<view class="jnpf-tree-select-search" v-if="filterable">
				<u-search :placeholder="$t('app.apply.pleaseKeyword')" v-model="filterText" height="72"
					:show-action="false" bg-color="#f0f2f6" shape="square">
				</u-search>
			</view>
			<view class="jnpf-tree-selected">
				<view class="jnpf-tree-selected-head">
					<view>{{$t('component.jnpf.common.selected')}}</view>
					<view v-if="multiple" class="clear-btn" @click="setCheckAll">
						{{$t('component.jnpf.common.clearAll')}}
					</view>
				</view>
				<view class="jnpf-tree-selected-box">
					<scroll-view scroll-y="true" class="select-list">
						<u-tag closeable @close="delSelect(index)" v-for="(list,index) in selectListText" :key="index"
							:text="list" class="u-selectTag" />
					</scroll-view>
				</view>
			</view>
			<view class="jnpf-tree-select-tree">
				<scroll-view :scroll-y="true" style="height: 100%">
					<ly-tree ref="tree" :tree-data="options" check-on-click-node default-expand-all
						:node-key="realProps.value" highlight-current :props="realProps" @node-click="handleNodeClick"
						:filter-node-method="filterNode" />
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
		getProvinceSelector
	} from '@/api/common.js'
	let _self;
	export default {
		name: "tree-select",
		props: {
			selectList: {
				type: Array,
				default () {
					return [];
				}
			},
			selectedId: {
				type: Array,
				default () {
					return [];
				}
			},
			selectData: {
				type: Array,
				default () {
					return [];
				}
			},
			options: {
				type: Array,
				default: () => []
			},
			// 是否显示边框
			border: {
				type: Boolean,
				default: true
			},
			filterable: {
				type: Boolean,
				default: false
			},
			showAllLevels: {
				type: Boolean,
				default: true
			},
			clearable: {
				type: Boolean,
				default: false
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
			level: {
				type: Number,
				default: 0
			}
		},
		data() {
			return {
				moving: false,
				selectListText: [],
				selectListId: [],
				selectListData: [],
				newListId: [],
				filterText: '',
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
		created() {
			_self = this
			this.init()
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
			init() {
				this.selectListText = this.$u.deepClone(this.selectList)
				this.selectListId = this.$u.deepClone(this.selectedId)
				this.selectListData = this.$u.deepClone(this.selectData)
			},
			filterNode(value, options) {
				if (!value) return true;
				return options[this.props.label].indexOf(value) !== -1;
			},
			handleNodeClick(obj) {
				if (!obj.parentId && !obj.isLeaf) return
				let allPath = this.$refs.tree.getNodePath(obj)
				let list = []
				let listId = []
				let currentNode = obj.data
				if (!this.multiple) {
					this.selectListText = [];
					this.selectListId = [];
					this.selectListData = [];
				}
				let txt = ''
				let ids = ''
				for (let i = 0; i < allPath.length; i++) {
					listId.push(allPath[i][this.props.value])
					ids += (i ? ',' : '') + allPath[i][this.props.value]
					txt += (i ? '/' : '') + allPath[i][this.props.label]
				}
				if (this.showAllLevels) {
					this.selectListText.push(txt)
				} else {
					this.selectListText.push(currentNode[this.props.label])
				}
				this.selectListText = [...new Set(this.selectListText)]
				var isExist = false;
				for (var i = 0; i < this.selectListId.length; i++) {
					if (this.selectListId[i].join(',') === ids) {
						isExist = true;
						break;
					}
				};
				!isExist && this.selectListId.push(listId);
				this.selectListData = allPath
			},
			delSelect(index) {
				this.selectListText.splice(index, 1);
				this.selectListId.splice(index, 1);
				this.selectListData.splice(index, 1);
			},
			setCheckAll() {
				this.selectListText = [];
				this.selectListId = [];
				this.selectListData = [];
				this.$refs.tree.setCheckAll(false);
			},
			handleConfirm() {
				this.$emit('confirm', this.selectListText, this.selectListId, this.selectListData);
				this.close();
			},
			close() {
				this.$emit('close', false);
			}
		}
	};
</script>