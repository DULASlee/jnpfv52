<template>
	<u-popup class="jnpf-tree-select-popup" :maskCloseAble="maskCloseAble" mode="right" v-model="showPopup"
		:safeAreaInsetBottom="safeAreaInsetBottom" @close="close" :z-index="uZIndex" width="100%">
		<view class="jnpf-tree-select-body">
			<view class="jnpf-tree-select-title">
				<text class="icon-ym icon-ym-report-icon-preview-pagePre backIcon" @tap="close()"></text>
				<view class="title">省市区</view>
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
						<u-tag closeable @close="delSelect(index)" v-for="(list,index) in selectList" :key="index"
							:text="list" class="u-selectTag" />
					</scroll-view>
				</view>
			</view>
			<view class="jnpf-tree-select-tree">
				<scroll-view :scroll-y="true" style="height: 100%">
					<ly-tree ref="tree" :node-key="realProps.value" :tree-data="options" :show-checkbox="false"
						:defaultExpandAll='false' @node-click="handleNodeClick" :props="realProps"
						:show-node-icon="true" :show-radio="false" :load="loadNode" lazy />
				</scroll-view>
			</view>
			<!-- 底部按钮 -->
			<view class="jnpf-tree-select-actions">
				<u-button class="buttom-btn" @click="close()">{{$t('common.cancelText')}}</u-button>
				<u-button class="buttom-btn" type="primary"
					@click.stop="handleConfirm()">{{$t('common.okText')}}</u-button>
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
	var _self;
	import {
		getProvinceSelector
	} from '@/api/common.js'
	export default {
		name: "tree-select",
		props: {
			selectedData: {
				type: Array,
				default () {
					return [];
				}
			},
			ids: {
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
				default: 2
			}
		},
		data() {
			return {
				moving: false,
				selectList: [],
				selectListId: [],
				newListId: [],
				options: [],
				selectData: []
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
				this.selectList = JSON.parse(JSON.stringify(this.selectedData))
				this.selectListId = !!this.ids ? this.ids : []
			},
			loadNode(node, resolve) {
				let id = node.key === null ? -1 : node.key
				let level = node.level
				getProvinceSelector(id).then(res => {
					const list = res.data.list.map((value, i) => ({
						id: value.id,
						fullName: value.fullName,
						isLeaf: level >= _self.level ? true : value.isLeaf
					}));
					resolve(list)
				})
			},
			handleNodeClick(obj) {
				if (!obj.isLeaf) return
				let getNodePath = this.$refs.tree.getNodePath(obj)
				let list = []
				let listId = []
				let selectList = []
				for (let i = 0; i < getNodePath.length; i++) {
					list.push(getNodePath[i].fullName)
					listId.push(getNodePath[i].id)
					let obj = {
						fullName: getNodePath[i].fullName,
						id: getNodePath[i].id
					}
					selectList.push(obj)
				}
				if (listId.length !== this.level + 1) return;
				if (!this.multiple) {
					this.selectList = [];
					this.selectListId = [];
					this.selectData = [];
				}
				var isExist = false;
				for (var i = 0; i < this.selectList.length; i++) {
					if (this.selectList[i] == list.join('/')) {
						isExist = true;
						break;
					}
				};
				!isExist && this.selectListId.push(listId);
				!isExist && this.selectList.push(list.join('/'));
				!isExist && this.selectData.push(selectList);
			},
			delSelect(index) {
				this.selectList.splice(index, 1);
				if (!this.multiple) {
					this.selectListId = [];
					this.selectData = []
				}
				this.selectListId.splice(index, 1);
			},
			setCheckAll() {
				this.selectList = [];
				this.selectListId = [];
				this.$refs.tree.setCheckAll(false);
			},
			handleConfirm() {
				this.$emit('confirm', this.selectList, this.selectListId, this.selectData);
				this.close();
			},
			close() {
				this.$emit('close');
			}
		}
	};
</script>