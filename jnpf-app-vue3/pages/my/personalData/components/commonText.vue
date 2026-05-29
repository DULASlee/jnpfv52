<template>
	<view class="common_v">
		<mescroll-uni ref="mescrollRef" @init="mescrollInit" @down="downCallback" @up="upCallback" :up="upOption"
			:bottombar="false" top="120">
			<uni-swipe-action ref="swipeAction">
				<uni-swipe-action-item v-for="(item, index) in commonWordsList" :key="index" :threshold="0"
					:right-options="actionData" :auto-close="false" @click="bindClick(item,$event)">
					<view class="action-item">
						{{item.commonWordsText}}
					</view>
				</uni-swipe-action-item>
			</uni-swipe-action>
		</mescroll-uni>
		<view class="flowBefore-actions">
			<u-button class="buttom-btn" type="primary" @click='editCommonWord'>添加常用语</u-button>
		</view>
	</view>
	<uni-popup ref="inputDialog" type="dialog">
		<uni-popup-dialog ref="inputClose" @confirm="confirm" mode="input" class="popup-dialog"
			borderRadius="20px 20px 20px 20px" beforeClose @close="close" title="审批常用语">
			<!-- #ifndef MP-WEIXIN -->
			<u-input v-model="commonWordsText" type="textarea" placeholder="请输入内容" :auto-height="false"
				maxlength="99999" height="150" />
			<!-- #endif -->
			<!-- #ifdef MP-WEIXIN -->
			<textarea v-model="commonWordsText" :maxlength="99999" placeholder="请输入内容"
				style="padding: 20rpx 0; "></textarea>
			<!-- #endif -->
		</uni-popup-dialog>
	</uni-popup>
</template>
<script>
	import {
		commonWords,
		Create,
		Update,
		Delete
	} from "@/api/commonWords.js";
	import NoData from "@/components/noData";
	import MescrollMixin from "@/uni_modules/mescroll-uni/components/mescroll-uni/mescroll-mixins.js";
	import resources from '@/libs/resources.js'
	export default {
		mixins: [MescrollMixin],
		components: {
			NoData
		},
		props: {
			showCommonWords: {
				type: Boolean,
				default: false
			}
		},
		data() {
			return {
				downOption: {
					use: true,
					auto: true
				},
				upOption: {
					page: {
						num: 0,
						size: 30,
						time: null
					},
					empty: {
						use: true,
						icon: resources.message.nodata,
						tip: this.$t('common.noData'),
						fixed: true,
						top: "360rpx"
					},
					textNoMore: this.$t('app.apply.noMoreData')
				},
				actionData: [{
						style: {
							backgroundColor: '#1890ff'
						},
						text: '编辑'
					},
					{
						style: {
							backgroundColor: '#F56C6C'
						},
						text: '删除'
					}
				],
				commonWordsText: '',
				commonWordsData: {},
				commonWordsList: [],
				showAdd: false
			}
		},
		methods: {
			upCallback(page) {
				const query = {
					currentPage: page.num,
					pageSize: page.size,
					commonWordsType: 1
				}
				commonWords(query).then(res => {
					const curPageData = res.data.list || [] // 当前页数据
					if (page.num == 1) this.commonWordsList = []; // 第一页需手动制空列表
					this.mescroll.endSuccess(res.data.list.length);
					this.commonWordsList = this.commonWordsList.concat(curPageData); //追加新数据
				}).catch(() => {
					this.mescroll.endErr();
				})
			},
			bindClick(item, e) {
				if (e.index == 0) this.editCommonWord(item)
				if (e.index == 1) this.delCommonWord(item)
			},
			editCommonWord(item) {
				this.$refs.inputDialog.open()
				let data = {
					commonWordsText: "",
					enabledMark: 1,
					id: 0,
					sortCode: 0,
					systemIds: [],
					systemNames: [],
				};
				if (item.id) {
					this.commonWordsText = item.commonWordsText;
					this.commonWordsData = {
						...item,
						systemIds: [],
						systemNames: []
					};
				} else {
					this.commonWordsText = "";
					this.commonWordsData = data;
				}
			},
			delCommonWord(item) {
				Delete(item.id).then(res => {
					this.$u.toast(res.msg)
					this.mescroll.resetUpScroll()
				})
			},
			close() {
				this.$refs.inputDialog.close()
			},
			confirm() {
				this.commonWordsData.commonWordsText = this.commonWordsText;
				this.commonWordsData.commonWordsType = 1
				if (!this.commonWordsText) return this.$u.toast(`审批常用语不能为空`);
				let funs = this.commonWordsData.id === 0 ? Create : Update;
				funs(this.commonWordsData).then((res) => {
					this.close()
					this.commonWordsText = "";
					uni.showToast({
						title: res.msg,
						icon: "none",
						complete: () => {
							this.mescroll.resetUpScroll()
						},
					});
				}).catch(() => {
					this.close()
					this.mescroll.resetUpScroll()
				});
			}
		}
	}
</script>

<style lang="scss">
	.action-item {
		width: 100%;
		min-height: 3.6rem;
		background-color: #fff;
		display: flex;
		align-items: center;
		border-bottom: 1rpx solid #eee;
		padding: 10rpx 20rpx;
		word-break: break-all;
	}
</style>