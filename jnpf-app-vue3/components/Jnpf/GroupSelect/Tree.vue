<template>
	<u-popup class="jnpf-tree-select-popup" mode="right" v-model="showPopup" @close="close" :z-index="uZIndex"
		width="100%">
		<view class="jnpf-tree-select-body">
			<view class="jnpf-tree-select-title">
				<text class="icon-ym icon-ym-report-icon-preview-pagePre backIcon" @tap="close()"></text>
				<view class="title">分组选择</view>
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
							:text="list.fullName" class="u-selectTag" />
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
			selectId: {
				default: ""
			},
			// 通过双向绑定控制组件的弹出与收起
			modelValue: {
				type: Boolean,
				default: false
			},
			// 弹出的z-index值
			zIndex: {
				type: [String, Number],
				default: 99999
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
			modelValue: {
				handler(val) {
					this.showPopup = val
					if (val) setTimeout(() => this.init(), 10);
				},
				immediate: true
			},
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
				this.keyword = ''
				this.selectListId = JSON.parse(JSON.stringify(this.selectId))
				if (!Array.isArray(this.selectId)) this.selectListId = []
				this.selectList = JSON.parse(JSON.stringify(this.selectedData))
				this.handleSearch()
			},
			handleNodeClick(obj) {
				if (obj.level == 1) return
				let isExist = false;
				if (!this.multiple) {
					// 单选
					this.selectList = [];
					this.selectListId = [];
				}
				if (!this.selectList.some(item => item.id === obj.data.id)) {
					this.selectList.push(obj.data);
					this.selectListId.push(obj.data.id);
				}
			},
			delSelect(index) {
				this.selectList.splice(index, 1);
				this.selectListId.splice(index, 1);
			},
			setCheckAll() {
				this.selectList = [];
				this.selectListId = [];
				this.$refs.tree.setCheckAll(false);
			},
			handleConfirm() {
				this.$emit('confirm', this.selectList, this.selectListId);
				this.close();
			},
			close() {
				this.$emit('close');
			},
			filterNode(value, data) {
				if (!value) return true;
				return data[this.realProps.label].indexOf(value) !== -1;
			},
			handleSearch(val) {
				this.$refs.tree && this.$refs.tree.filter(this.keyword)
			},
		}
	};
</script>