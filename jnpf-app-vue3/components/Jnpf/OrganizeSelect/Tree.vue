<template>
	<u-popup class="jnpf-tree-select-popup" mode="right" v-model="showPopup" width="100%" @close="close">
		<view class="jnpf-tree-select-body">
			<view class="jnpf-tree-select-title">
				<text class="icon-ym icon-ym-report-icon-preview-pagePre backIcon" @tap="close()"></text>
				<view class="title">组织选择</view>
			</view>
			<view class="jnpf-tree-select-search">
				<u-search :placeholder="$t('app.apply.pleaseKeyword')" v-model="keyword" height="72"
					:show-action="false" @change="handleSearch" bg-color="#f0f2f6" shape="square">
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
						<u-tag closeable @close="delSelect(index)" v-for="(list,index) in selectList" :key="index"
							:text="list" class="u-selectTag" />
					</scroll-view>
				</view>
			</view>
			<view class="jnpf-tree-select-tree">
				<scroll-view :scroll-y="true" style="height: 100%">
					<ly-tree v-if="selectType !== 'all'" :tree-data="options" :node-key="realProps.value"
						default-expand-all :props="realProps" :filter-node-method="filterNode"
						child-visible-for-filter-node @node-click="handleNodeClick" ref="tree">
					</ly-tree>
					<ly-tree ref="tree" v-if="selectType === 'all'" :props="realProps" :node-key="realProps.value"
						:load="loadNode" lazy :tree-data="lazyOptions" show-node-icon :defaultExpandAll='false'
						@node-click="handleNodeClick" />
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
	import {
		getOrgByOrganizeCondition,
		selectAsyncList
	} from '@/api/common.js'
	export default {
		props: {
			options: {
				type: Array,
				default () {
					return [];
				}
			},
			selectedData: {
				type: Array,
				default () {
					return [];
				}
			},
			selectType: {
				type: String,
				default: 'all'
			},
			ids: {
				type: [Array, String],
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
			}
		},
		data() {
			return {
				moving: false,
				selectList: [],
				selectListId: [],
				keyword: '',
				showPopup: false,
				lazyOptions: [],
				level: 0
			};
		},
		watch: {
			// 在select弹起的时候，重新初始化所有数据
			modelValue: {
				handler(val) {
					this.showPopup = val
					if (val) setTimeout(() => this.init(), 10);
				},
				immediate: true
			}
		},
		created() {
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
				this.keyword = ""
				this.selectListId = Array.isArray(this.ids) ? JSON.parse(JSON.stringify(this.ids)) : []
				this.selectList = JSON.parse(JSON.stringify(this.selectedData))
			},
			filterNode(value, data) {
				if (!value) return true;
				return data[this.realProps.label].indexOf(value) !== -1;
			},
			loadNode(node, resolve) {
				let id = node.key === null ? 0 : node.key
				this.level = node.level
				let data = {
					keyword: '',
					currOrgId: 0
				}
				selectAsyncList(data, id).then(res => {
					resolve(res.data?.list)
				})
			},
			selectAsyncList() {
				let data = {
					keyword: this.keyword,
					currOrgId: 0
				}
				this.lazyOptions = []
				selectAsyncList(data, 0).then(res => {
					this.lazyOptions = res.data.list || []
				})
			},
			handleNodeClick(obj) {
				let selectList;
				if (this.multiple) {
					this.selectList.push(obj.data.organize)
					this.selectList = [...new Set(this.selectList)];
					if (obj.data.organizeIds.length) this.selectListId.push(obj.data.organizeIds)
					this.selectListId = [...new Set(this.selectListId)];
					this.selectListId = this.selectListId.filter(o => Array.isArray(o))
				} else {
					this.selectList = []
					this.selectList.push(obj.data.organize)
					this.selectList = [...new Set(this.selectList)];
					this.selectListId = obj.data.organizeIds
				}
			},
			delSelect(index) {
				this.selectList.splice(index, 1);
				this.selectListId.splice(index, 1);
			},
			setCheckAll() {
				this.selectListId = []
				this.selectList = [];
			},
			handleSearch(val) {
				this.keyword = val
				if (this.selectType === 'all') return this.$u.debounce(this.selectAsyncList, 600)
				this.$refs.tree && this.$refs.tree.filter(val)
			},
			handleConfirm() {
				this.$emit('confirm', this.selectList, this.selectListId);
				this.close();
			},
			close() {
				this.$emit('close', false);
			}
		}
	};
</script>