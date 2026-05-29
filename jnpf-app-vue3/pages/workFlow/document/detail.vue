<template>
	<view class="document-v" :style="{'padding-bottom': show ? '88rpx' : '0','overflow':showAddSelect?'hidden':''}">
		<view class="u-flex top-btn" :class="slide2" v-show="show && current !== 1">
			<view class="button-left" @click.stop="bottomfun('cancel')">
				<p class="u-m-t-10 u-font-28">取消</p>
			</view>
			<view class="button-center" @click.stop="bottomfun('select')">
				<p class="u-m-t-10 u-font-28">已选中{{this.selectFiles.length}}文件</p>
			</view>
			<view class="button-right u-m-t-12" @click.stop="bottomfun('checkAll')">
				<p class="icon-ym icon-ym-app-checkAll " :style="{'color':this.checkedAll ? '#0293fc' : '#303133'}">
				</p>
			</view>
		</view>
		<NaviGation @inF="navigationInt" @clickItem="backTree" :slabel="props.label" ref="NaviGation"></NaviGation>
		<mescroll-body ref="mescrollRef" @down="downCallback" :down="downOption" :sticky="false" @up="upCallback"
			:up="upOption" :bottombar="false" @init="mescrollInit" top="20">
			<DocList v-model="changeStyle" :documentList="documentList" @goDetail="goDetail"
				@checkboxChange="checkboxChange"></DocList>
		</mescroll-body>
		<view class="com-addBtn" @click="addFolder()" v-if="!selectFiles.length && current == 0">
			<u-icon name="plus" size="48" color="#fff" />
		</view>
		<view class="u-flex bottom-btn" :class="slide" v-show="show && current !== 1">
			<template v-if="current == 0">
				<view class="button-preIcon" @click.stop="bottomfun('down')">
					<p class="icon-ym icon-ym-app-download u-m-b-8"></p>
					<p class="u-m-t-10 u-font-24">下载</p>
				</view>
				<view class="button-preIcon" @click.stop="bottomfun('share')">
					<p class="icon-ym icon-ym-app-share u-m-b-8"></p>
					<p class="u-m-t-10 u-font-24">共享</p>
				</view>
				<view class="button-preIcon" @click.stop="bottomfun('delete')">
					<p class="icon-ym icon-ym-app-delete u-m-b-8"></p>
					<p class="u-m-t-10 u-font-24">删除</p>
				</view>
				<view class="button-preIcon" @click.stop="bottomfun('move')">
					<p class="icon-ym icon-ym-app-move u-m-b-8"></p>
					<p class="u-m-t-10 u-font-24">移动到</p>
				</view>
				<view class="button-preIcon" @click.stop="bottomfun('restName')" v-if="this.selectFiles.length === 1">
					<p class="icon-ym icon-ym-app-rename u-m-b-8"></p>
					<p class="u-m-t-10 u-font-24">重命名</p>
				</view>
			</template>
			<template v-if="current == 2">
				<view class="button-preIcon" @click.stop="bottomfun('down')">
					<p class="icon-ym icon-ym-app-download u-m-b-8"></p>
					<p class="u-m-t-10 u-font-24">下载</p>
				</view>
			</template>
			<template v-if="current == 1">
				<view class="button-preIcon" @click.stop="bottomfun('shareCancel')">
					<p class="icon-ym icon-ym-app-share u-m-b-8"></p>
					<p class="u-m-t-10 u-font-24">取消共享</p>
				</view>
				<view class="button-preIcon" @click.stop="bottomfun('share')">
					<p class="icon-ym icon-ym-app-share u-m-b-8"></p>
					<p class="u-m-t-10 u-font-24">共享</p>
				</view>
			</template>
			<template v-if="current == 3">
				<view class="button-preIcon" @click.stop="bottomfun('revert')">
					<p class="icon-ym icon-ym-recovery u-m-b-8"></p>
					<p class="u-m-t-10 u-font-24">还原</p>
				</view>
				<view class="button-preIcon" @click.stop="bottomfun('delete')">
					<p class="icon-ym icon-ym-app-delete u-m-b-8"></p>
					<p class="u-m-t-10 u-font-24">删除</p>
				</view>
			</template>
		</view>
		<!-- 重命名弹窗 -->
		<uni-popup ref="inputDialog" type="dialog">
			<uni-popup-dialog ref="inputClose" mode="input" :title="modalTitle" v-model="modalValue" placeholder="请输入内容"
				before-close @confirm="restName" @close="closeDialog"></uni-popup-dialog>
		</uni-popup>
		<treeCollapse :show="showApply" v-if="showApply" :treeData="folderTreeList" @change="handelClick" mode="right"
			@close="close" width="100%" type="doc">
			<view class="u-flex u-p-l-32 u-p-r-32" style="justify-content: space-between;height: 88rpx;">
				<text style="color: #2979ff;" @click="close()">取消</text>
				<text>移动到</text>
				<text style="color: #2979ff;" v-if="selectFiles.length" @click="folderMove">移动到此</text>
			</view>
		</treeCollapse>
		<JnpfUserSelect ref="JnpfUsersSelect" multiple @change="shareSubmit" v-model="usersSelectValue"
			:isInput="false" />
		<AddFilePopup v-if="showAddFilePopup" :show="showAddSelect" @confirm="addSelect" @close="showAddSelect = false"
			title="新建" :confirmBtn="false" @onCallback="onCallback" :parentId="parentId"></AddFilePopup>
	</view>
</template>
<script>
	import {
		getDocumentList,
		trash,
		shareFolder,
		shareTome
	} from "@/api/workFlow/document";
	import treeCollapse from '@/components/treeCollapse'
	import AddFilePopup from './components/AddFilePopup.vue'
	import NaviGation from './components/navigation/NaviGation.vue'
	import DocList from './components/DocList.vue'
	import mixin from "./mixin.js"
	export default {
		mixins: [mixin],
		components: {
			NaviGation,
			DocList,
			treeCollapse,
			AddFilePopup,
		},
		data() {
			return {
				showAddFilePopup: false,
				showModal: false,
				show: false,
				detailList: [],
				keyword: '',
				documentList: [],
				changeStyle: true,
				parentId: 0,
				props: {
					id: 'id',
					label: 'fullName',
					children: 'children',
					multiple: false,
					checkStrictly: false, //不关联
					nodes: false, // nodes为false时，可以选择任意一级选项；nodes为true时只能选择叶子节点
				}
			}
		},
		onLoad(e) {
			let config = JSON.parse(e.config)
			this.config = JSON.parse(JSON.stringify(config))
			this.parentId = this.config.id
			this.$nextTick(() => {
				this.$refs.NaviGation.pushTreeStack(this.config);
			})
			this.init()
		},
		watch: {
			// 在select弹起的时候，重新初始化所有数据
			selectFiles: {
				handler(val) {
					if (!val.length) {
						setTimeout(() => {
							this.show = false
						}, 500)
					}
					if (val.length === this.documentList.length) {
						this.checkedAll = true
					} else {
						this.checkedAll = false
					}
				},
				immediate: true
			},
		},
		methods: {
			init() {
				this.isDetail = true
				this.current = this.config.current
				this.changeStyle = this.config.changeStyle
				this.setTitle(this.config.fullName)
				this.showAddFilePopup = true
			},
			navigationInt(e) {
				this.pushTreeStack = e.pushTreeStack;
			},
			backTree(item, index) {
				if (index === -1) {
					this.isDetail = false
					uni.navigateBack()
					return
				}
				if (item.id === this.parentId) return
				this.parentId = item.id
				this.setTitle(item.fullName)
				this.resetList()
			},
			setTitle(fullName) {
				uni.setNavigationBarTitle({
					title: fullName
				})
			}
		}
	}
</script>

<style lang="scss">
	page {
		background-color: #f0f2f6;
	}

	.document-v {
		padding-bottom: 88rpx;
		height: calc(100vh - 288rpx);

		::v-deep .mescroll-empty {
			padding-top: 400rpx;
		}

		::v-deep .u-model__title {
			padding: 20rpx 0;
		}

		::v-deep .u-model__footer__button {
			border-right: 1rpx solid #e4e7ed;
			height: 76rpx;
			line-height: 76rpx;
			font-size: 28rpx;
		}

		.top-btn {
			height: 80rpx;
			position: fixed;
			width: 100%;
			top: 0;
			left: 0;
			background-color: #fff;
			z-index: 9999;
			justify-content: space-between;
			padding: 0 20rpx;

			.button-left {
				color: #0293fc;
			}

			.button-right {
				width: 30rpx;
				height: 30rpx;
			}
		}

		.slide-down2 {
			animation: slide-down2 0.5s forwards;
			opacity: 1;
			transform: translateY(0);
		}

		.slide-up2 {
			animation: slide-up2 0.5s forwards;
			opacity: 0;
			transform: translateY(-100%);
		}

		.slide-down {
			animation: slide-down 0.5s forwards;
			opacity: 1;
			transform: translateY(0);
		}

		.slide-up {
			animation: slide-up 0.5s forwards;
			opacity: 0;
			transform: translateY(100%);
		}

		.bottom-btn {
			height: 100rpx;
			position: fixed;
			width: 100%;
			bottom: 0;
			left: 0;
			background-color: #0293fc;
			z-index: 9;
			justify-content: space-around;

			.button-preIcon {
				color: #fff;
				text-align: center;
				width: 20%;
			}
		}

		@keyframes slide-up {
			to {
				transform: translateY(0);
				opacity: 1;
			}
		}

		@keyframes slide-down {
			to {
				transform: translateY(100%);
				opacity: 0;
			}
		}

		@keyframes slide-up2 {
			to {
				transform: translateY(0);
				opacity: 1;
			}
		}

		@keyframes slide-down2 {
			to {
				transform: translateY(-100%);
				opacity: 0;
			}
		}

		.com-addBtn {
			bottom: 320rpx;
			right: 66rpx;
		}
	}
</style>