<template>
	<u-popup class="jnpf-tree-select-popup" mode="right" :popup="false" v-model="showPopup" length="auto"
		:z-index="uZIndex" width="100%" @close="close">
		<view class="jnpf-tree-select-body">
			<view class="jnpf-tree-select-title">
				<text class="icon-ym icon-ym-report-icon-preview-pagePre backIcon" @tap="close()"></text>
				<view class="title">{{title}}</view>
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
							:text="!!list.lastFullName?list.lastFullName:list.fullName" class="u-selectTag" />
					</scroll-view>
				</view>
			</view>
			<view class="jnpf-tree-select-tree">
				<scroll-view :scroll-y="true" style="height: 100%">
					<ly-tree ref="tree" :node-key="realProps.value" :tree-data="options" :show-checkbox="false"
						@node-click="handleNodeClick" :props="realProps" :show-node-icon="true" :show-radio="false"
						:filter-node-method="filterNode" :highlight-current="true" />
				</scroll-view>
			</view>
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
		getDepartmentSelector
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
			ids: {
				default: ''
			},
			title: {
				default: '选择'
			},
			modelValue: {
				type: Boolean,
				default: false
			},
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
				newListId: [],
				keyword: '',
				showPopup: false
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
			},
		},
		computed: {
			uZIndex() {
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
				this.selectListId = JSON.parse(JSON.stringify(this.ids))
				if (!Array.isArray(this.ids)) this.selectListId = []
				this.selectList = JSON.parse(JSON.stringify(this.selectedData))
			},
			handleNodeClick(obj) {
				if (obj.data.type != 'position') return
				let isExist = false;
				if (!this.multiple) {
					this.selectList = [];
					this.selectListId = [];
				}
				for (var i = 0; i < this.selectList.length; i++) {
					if (this.selectList[i].id == obj.data.id) {
						isExist = true;
						break;
					}
				};
				!isExist && this.selectList.push(obj.data);
				!isExist && this.selectListId.push(obj.data[this.props.value]);
			},
			delSelect(index) {
				this.selectList.splice(index, 1);
				this.selectListId.splice(index, 1);
			},
			setCheckAll() {
				this.selectListId = [];
				this.selectList = [];
				this.$refs.tree.setCheckAll(false);
			},
			filterNode(value, data) {
				if (!value) return true;
				return data[this.realProps.label].indexOf(value) !== -1;
			},
			handleSearch(val) {
				this.$refs.tree && this.$refs.tree.filter(val)
			},
			handleConfirm() {
				this.$emit('confirm', this.selectList, this.selectListId);
				this.close();
			},
			close() {
				this.$emit('close', false);
			},
		}
	};
</script>