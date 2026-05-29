<template>
	<view class="candidateForm-v">
		<mescroll-body ref="mescrollRef" @init="mescrollInit" @down="downCallback" @up="upCallback" :sticky="true"
			:down="downOption" :up="upOption" :bottombar="false">
			<view class="treeSelect-search search-box_sticky">
				<u-search :placeholder="$t('app.apply.pleaseKeyword')" v-model="keyword" height="72"
					:show-action="false" @change="search" bg-color="#f0f2f6" shape="square">
				</u-search>
				<view class="alreadySelect">
					<view class="alreadySelect__box u-flex-col">
						<view class="alreadySelect_hd u-flex">
							<view>{{$t('component.jnpf.common.selected')}}</view>
							<view v-if="multiple" @click="setCheckAll" style="color: #2979ff;">
								{{$t('component.jnpf.common.clearAll')}}
							</view>
						</view>
						<view class="select__box u-flex-col">
							<scroll-view scroll-y="true" style="max-height: 200px;">
								<view class="u-flex select__list">
									<view class="u-selectTag u-flex" v-for="(list,index) in selectList" :key="index">
										<view class="avatar">
											<u-avatar :src="baseURL+list.headIcon" mode="circle" size="mini">
											</u-avatar>
										</view>
										<view class="u-font-24 content">
											<view class="nameSty u-flex">
												<view class="nameUp">
													{{list.fullName}}
												</view>
												<u-icon name="close" class="close" @click='delSelect(index)'>
												</u-icon>
											</view>
											<view class="organizeSty">{{list.organize}}</view>
										</view>
									</view>
								</view>
							</scroll-view>
						</view>
					</view>
				</view>
				<view class="listTitle">全部数据</view>
			</view>
			<view class="mescroll_body">
				<view style="" class="lists_box">
					<view class="list-cell-txt u-border-bottom" v-for="(list,index) in list" :key="index"
						@click="onSelect(list)">
						<view class="avatar">
							<u-avatar :src="baseURL+list.headIcon" mode="circle" size="default"></u-avatar>
						</view>
						<view class="u-font-30 content">
							<view class="nameSty">{{list.fullName}}</view>
							<view class="organizeSty">{{list.organize}}</view>
						</view>
					</view>
				</view>
			</view>
		</mescroll-body>
		<!-- 底部按钮 -->
		<view class="flowBefore-actions">
			<u-button class="buttom-btn" @click="getResult('cancel')">{{$t('common.cancelText')}}</u-button>
			<u-button class="buttom-btn" type="primary"
				@click.stop="getResult('confirm')">{{$t('common.okText')}}</u-button>
		</view>
	</view>
</template>

<script>
	import {
		CandidateUser
	} from '@/api/workFlow/flowBefore'
	import resources from '@/libs/resources.js'
	import MescrollMixin from "@/uni_modules/mescroll-uni/components/mescroll-uni/mescroll-mixins.js";
	export default {
		mixins: [MescrollMixin],
		data() {
			return {
				downOption: {
					use: true,
					auto: true
				},
				upOption: {
					page: {
						num: 0,
						size: 20,
						time: null
					},
					empty: {
						use: true,
						icon: resources.message.nodata,
						tip: this.$t('common.noData'),
						fixed: true,
						top: "300rpx",
					},
					textNoMore: this.$t('app.apply.noMoreData'),
				},
				onLoadData: {},
				safeAreaInsetBottom: true,
				list: [],
				show: false,
				keyword: '',
				selectList: [],
				multiple: true
			}
		},
		onLoad(e) {
			this.show = true
			this.onLoadData = JSON.parse(decodeURIComponent(e.data));
			this.selectList = this.onLoadData.selectList
			this.delegateUser = this.onLoadData.delegateUser
		},
		computed: {
			baseURL() {
				return this.define.baseURL
			}
		},
		methods: {
			upCallback(page) {
				let query = {
					currentPage: page.num,
					pageSize: page.size,
					keyword: this.keyword,
					...this.onLoadData.formData,
					nodeCode: this.onLoadData.nodeCode,
					delegateUser: this.delegateUser
				}
				CandidateUser(this.onLoadData.taskId || 0, query, {
					load: page.num == 1
				}).then(res => {
					this.mescroll.endSuccess(res.data.list.length);
					if (page.num == 1) this.list = [];
					const list = res.data.list;
					this.list = this.list.concat(list);
				}).catch(() => {
					this.mescroll.endErr();
				})
			},
			search() {
				// 节流,避免输入过快多次请求
				this.searchTimer && clearTimeout(this.searchTimer)
				this.searchTimer = setTimeout(() => {
					this.list = [];
					this.mescroll.resetUpScroll();
				}, 300)
			},
			onSelect(list) {
				let flag = false;
				for (let i = 0; i < this.selectList.length; i++) {
					if (this.selectList[i].id === list.id) {
						flag = true;
						return
					}
				};
				!flag && this.selectList.push(list)
				this.selectList = this.selectList.map(o => ({
					nodeCode: this.onLoadData.nodeCode,
					...o
				}))
			},
			delSelect(index) {
				this.selectList.splice(index, 1);
			},
			close() {
				this.list = []
				this.$emit('input', false);
			},
			getResult(type) {
				uni.$emit(type, this.selectList, this.onLoadData.nodeCode);
				uni.navigateBack()
			},
			setCheckAll() {
				this.selectList = []
			}
		}
	}
</script>

<style lang="scss" scoped>
	.candidateForm-v {
		padding-bottom: 88rpx;

		.treeSelect-search {
			padding: 20rpx 30rpx 20rpx;

			.alreadySelect {
				width: 100%;
				padding: 30rpx 0rpx 0;
				border-bottom: 1rpx solid #c0c4cc;

				.alreadySelect__box {
					.alreadySelect_hd {
						width: 100%;
						height: 60rpx;
						justify-content: space-between;
					}

					.select__box {
						width: 100%;
						justify-content: center;

						.select__list {
							justify-content: flex-start;
							flex-wrap: wrap;

							.u-selectTag {
								width: 310rpx;
								border: 1px solid #2194fa;
								background-color: #e8f4fe;
								line-height: 40rpx;
								margin: 10rpx;
								padding-left: 10rpx;
								align-items: center;
								border-radius: 8rpx;

								.content {
									width: 74%;
									margin-left: 10rpx;

									.nameSty {
										color: #353535;
										flex: 1;
										display: flex;

										.nameUp {
											white-space: nowrap;
											overflow: hidden;
											text-overflow: ellipsis;
											flex: 1;
										}

										.close {
											width: 26px;
											padding-right: 8rpx;
											justify-content: flex-end;
											color: #2194fa;
											flex-shrink: 0;
										}
									}

									.organizeSty {
										color: #a0a1a1;
										white-space: nowrap;
										overflow: hidden;
										text-overflow: ellipsis;
									}
								}
							}

							.u-size-default {
								padding: 6rpx 12rpx;
							}
						}
					}
				}
			}

			.listTitle {
				// height: 100rpx;
				padding: 22rpx 0;
				font-size: 36rpx;
			}
		}

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
				line-height: 24px;
				background-color: #fff;

				.content {
					width: 85%;
					margin-left: 15rpx;

					.nameSty {}

					.organizeSty {
						white-space: nowrap;
						overflow: hidden;
						text-overflow: ellipsis;
					}
				}

				.department {
					color: #9A9A9A;
				}
			}
		}
	}
</style>