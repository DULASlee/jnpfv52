<template>
	<view class="jnpf-pop-select">
		<mescroll-body ref="mescrollRef" @init="mescrollInit" @down="downCallback" @up="upCallback" :sticky="true"
			:down="downOption" :up="upOption">
			<view class="search-box search-box_sticky">
				<u-search :placeholder="$t('app.apply.pleaseKeyword')" v-model="keyword" height="72"
					:show-action="false" @change="search" bg-color="#f0f2f6" shape="square">
				</u-search>
			</view>
			<view class="u-flex-col tableList">
				<view class="u-flex list-card" v-for="(item,index) in list" :key="index">
					<u-radio-group v-model="selectId[0]" v-if="!onLoadData.multiple">
						<u-radio :name="item[publicField]" @change="radioChange(item)">
							<view class="u-flex-col fieldContent u-m-l-10">
								<view v-for="(column,c) in onLoadData.columnOptions" :key="c"
									class="fieldList u-line-1 u-flex">
									<view class="val">{{column.label+':'}} {{item[column.value]}}</view>
								</view>
							</view>
						</u-radio>
					</u-radio-group>
				</view>
			</view>
		</mescroll-body>
		<!-- 底部按钮 -->
		<view class="flowBefore-actions">
			<u-button class="buttom-btn" @click.stop="handleClose()">{{$t('common.cancelText')}}</u-button>
			<u-button class="buttom-btn" type="primary" @click.stop="handleConfirm()">{{$t('common.okText')}}</u-button>
		</view>
	</view>
</template>
<script>
	import {
		getRelationSelect,
		getPopSelect
	} from '@/api/common.js'
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
				list: [],
				type: '',
				onLoadData: {},
				keyword: '',
				innerValue: '',
				listQuery: {
					keyword: ''
				},
				modelId: '',
				cur: null,
				firstId: 0,
				selectId: [],
				publicField: '',
				selectRow: [],
				columnOptions: []
			}
		},
		onLoad(e) {
			try {
				this.onLoadData = JSON.parse(decodeURIComponent(e.data));
				this.columnOptions = this.onLoadData.columnOptions.map(option => option.value);
				this.innerValue = this.onLoadData.innerValue;
				this.type = this.onLoadData.type;
				this.publicField = this.onLoadData.propsValue;
				this.selectId = [this.onLoadData?.id] || []
				this.modelId = this.onLoadData.modelId;
				// 更新导航栏标题
				uni.setNavigationBarTitle({
					title: this.onLoadData.popupTitle
				});
			} catch (error) {
				console.error('Error processing data:', error);
			}
		},
		methods: {
			upCallback(page) {
				const method = this.type === 'popup' ? getPopSelect : getRelationSelect
				const paramList = this.onLoadData.paramList
				let query = {
					...this.listQuery,
					currentPage: page.num,
					pageSize: this.onLoadData.hasPage ? this.onLoadData.pageSize : 10000,
					interfaceId: this.onLoadData.modelId,
					propsValue: this.onLoadData.propsValue,
					relationField: this.onLoadData.relationField,
					columnOptions: this.columnOptions.join(','),
					paramList
				}
				if (this.type === 'relation') query = {
					...query,
					queryType: this.onLoadData.queryType
				}
				method(this.modelId, query, {
					load: page.num == 1
				}).then(res => {
					if (!this.onLoadData.hasPage) {
						this.mescroll.endBySize(res.data.list.length, res.data.pagination.total)
					} else {
						this.mescroll.endSuccess(res.data.list.length);
					}
					if (page.num == 1) this.list = [];
					this.list = this.list.concat(res.data.list);
					if (this.onLoadData.multiple) {
						this.list = this.list.map((o, i) => ({
							...o,
							checked: false
						}))
						if (this.selectId.length) this.setSelectValue()
					} else {
						var index = this.list.findIndex((item) => {
							return item[this.publicField] == this.selectId
						})
						if (index >= 0) this.selectRow = [this.list[index]]
					}
				}).catch(() => {
					this.mescroll.endErr();
				})
			},
			setSelectValue() {
				outer: for (let i = 0; i < this.selectId.length; i++) {
					inner: for (let j = 0; j < this.list.length; j++) {
						if (this.selectId[i] === this.list[j][this.publicField]) {
							this.list[j].checked = true
							break inner
						}
					}
				}
			},
			radioChange(item) {
				this.selectId = []
				this.selectRow = []
				this.selectId.push(item[this.publicField]);
				this.selectRow.push(item)
			},
			handleConfirm() {
				this.list.map((o, i) => {
					if (this.selectId == o[this.publicField]) {
						this.firstId = o[this.publicField];
						const val = this.type == 'popup' ? o[this.onLoadData.propsValue] : o[this.publicField];
						const emit = this.type == 'popup' ? 'popConfirm' : 'relationConfirm'
						uni.$emit(emit, val, this.innerValue, this.onLoadData.vModel, this.selectRow[0])
					}
				})
				uni.navigateBack();
			},
			handleClose() {
				this.selectId = ""
				uni.navigateBack();
			},
			search() {
				// 节流,避免输入过快多次请求
				this.searchTimer && clearTimeout(this.searchTimer)
				this.searchTimer = setTimeout(() => {
					this.list = [];
					this.listQuery.keyword = this.keyword
					this.listQuery.currentPage = 1
					this.mescroll.resetUpScroll();
				}, 300)
			},
		}
	}
</script>

<style lang="scss">
	page {
		background-color: #f0f2f6;
	}

	.jnpf-pop-select {
		width: 100%;
		height: 100%;
		padding-bottom: 106rpx;

		.tableList {
			overflow: hidden auto;
			padding: 0 20rpx;

			.list-card {
				display: flex;
				flex-direction: row;
				align-items: center;
				background-color: #fff;
				width: 100%;
				border-radius: 8rpx;
				margin-top: 20rpx;
				padding: 0rpx 20rpx;
				min-height: 88rpx;

				.fieldContent {
					width: 100%;

					.fieldList {
						width: 752rpx;

						.key {
							width: 136rpx;
							margin-right: 10rpx;
							text-align: right;
							overflow: hidden;
							white-space: nowrap;
							text-overflow: ellipsis;
							line-height: 60rpx;
						}

						.val {
							flex: 0.85;
							overflow: hidden;
							white-space: nowrap;
							text-overflow: ellipsis;
						}
					}
				}
			}
		}

		.nodata {
			margin-top: 258rpx;
			justify-content: center;
			align-items: center;

			image {
				width: 280rpx;
				height: 215rpx;
			}
		}
	}
</style>