<template>
	<view class="dynamicModel-list-v">
		<view class="head-warp com-dropdown">
			<u-dropdown class="u-dropdown" ref="uDropdown">
				<u-dropdown-item title="筛选">
					<view class="screen-box">
						<view class="screen-list" v-if="showParser && columnCondition.length">
							<view class="u-p-l-20 u-p-r-20 list">
								<scroll-view scroll-y="true" style="height: 100%;">
									<Parser :formConf="columnCondition" :searchFormData="searchFormData"
										:webType="config.webType" ref="searchForm" @submit="sumbitSearchForm" />
								</scroll-view>
							</view>
						</view>
						<view class="notData-box u-flex-col">
							<view class="u-flex-col notData-inner">
								<image :src="icon" class="iconImg"></image>
								<text class="notData-inner-text">{{$t('app.apply.noMoreData')}}</text>
							</view>
						</view>
						<view class="buttom-actions" v-if="showParser && columnCondition.length" style="z-index: 1;">
							<u-button class="buttom-btn" @click="reset">{{$t('common.resetText')}}</u-button>
							<u-button class="buttom-btn" type="primary"
								@click="closeDropdown">{{$t('common.queryText')}}</u-button>
						</view>
					</view>
				</u-dropdown-item>
			</u-dropdown>
		</view>
		<view class="list-warp">
			<mescroll-uni ref="mescrollRef" @init="mescrollInit" @down="downCallback" @up="upCallback" :up="upOption"
				top="164">
				<view class="list u-p-b-20 u-p-l-20 u-p-r-20" ref="tableRef">
					<view class="list-box">
						<uni-swipe-action ref="swipeAction">
							<uni-swipe-action-item v-for="(item, index) in list" :key="item.id" :threshold="0"
								:disabled="true">
								<view class="item" @click="goDetail(item)">
									<view class="item-cell" v-for="(column,i) in columnList" :key="i">
										<template v-if="column.jnpfKey != 'table'">
											<text class="item-cell-label">{{column.label}}:</text>
											<text class="item-cell-content"
												v-if="['calculate','inputNumber'].includes(column.jnpfKey)">
												{{toThousands(item[column.prop],column)}}
											</text>
											<view class="item-cell-content" v-else-if="column.jnpfKey == 'sign'">
												<JnpfSign v-model="item[column.prop]" align="left" detailed />
											</view>
											<view class="item-cell-content" v-else-if="column.jnpfKey == 'rate'">
												<JnpfRate v-model="item[column.prop]" :count="column.count"
													:allowHalf="column.allowHalf" disabled />
											</view>
											<view class="item-cell-content item-cell-slider"
												v-else-if="column.jnpfKey == 'slider'">
												<JnpfSlider v-model="item[column.prop]" :min="column.min"
													:max="column.max" :step="column.step" disabled />
											</view>
											<view class="item-cell-content" v-else-if="column.jnpfKey == 'input'">
												<JnpfInput v-model="item[column.prop]" detailed showOverflow
													:useMask="column.useMask" :maskConfig="column.maskConfig"
													align='left' />
											</view>
											<text class="item-cell-content" v-else>{{item[column.prop]}}</text>
										</template>
										<tableCell v-else @click.stop class="tableCell" ref="tableCell"
											:label="column.label" :childList="item[column.prop]"
											:children="column.children" :pageLen="3">
										</tableCell>
									</view>
								</view>
							</uni-swipe-action-item>
						</uni-swipe-action>
					</view>
				</view>
			</mescroll-uni>
		</view>
	</view>
</template>

<script>
	import tableCell from '@/pages/apply/dynamicModel/components/tableCell.vue'
	import resources from '@/libs/resources.js'
	import MescrollMixin from "@/uni_modules/mescroll-uni/components/mescroll-uni/mescroll-mixins.js";
	import Parser from '@/pages/apply/dynamicModel/components/parser/index.vue'
	import {
		listLink
	} from '@/api/apply/webDesign'
	export default {
		mixins: [MescrollMixin],
		props: ['config', 'modelId', 'columnCondition', 'columnText', 'encryption'],
		components: {
			Parser,
			tableCell
		},
		data() {
			return {
				show: false,
				icon: resources.message.nodata,
				upOption: {
					page: {
						num: 0,
						size: 10,
						time: null
					},
					empty: {
						icon: resources.message.nodata,
						tip: this.$t('common.noData'),
						top: "300rpx"
					},
					textNoMore: this.$t('app.apply.noMoreData'),
					toTop: {
						bottom: 250
					}
				},
				list: [],
				listQuery: {
					sidx: '',
					keyword: '',
					queryJson: ''
				},
				options: [{
					text: '删除',
					style: {
						backgroundColor: '#dd524d'
					}
				}],
				showParser: false,
				columnList: {},
				searchList: [],
				searchFormConf: [],
				searchFormData: {},
				key: +new Date()
			}
		},
		created() {
			this.init()
		},
		methods: {
			init() {
				this.columnList = this.transformColumnList(this.columnText)
				this.columnList.map((o) => {
					if (o.jnpfKey != 'table' && o.label.length > 4) {
						o.label = o.label.substring(0, 4)
					}
				})
				let config = JSON.parse(this.config.appColumnData)
				this.setDefaultQuery(config.defaultSortConfig)
				this.$nextTick(() => {
					this.key = +new Date()
				})
			},
			setDefaultQuery(defaultSortList) {
				const defaultSortConfig = (defaultSortList || []).map(o =>
					(o.sort === 'desc' ? '-' : '') + o.field);
				this.listQuery.sidx = defaultSortConfig.join(',')
			},
			toThousands(val, column) {
				if (val) {
					let valList = val.toString().split('.')
					let num = Number(valList[0])
					let newVal = column.thousands ? num.toLocaleString() : num
					return valList[1] ? newVal + '.' + valList[1] : newVal
				} else {
					return val
				}
			},
			transformColumnList(columnList) {
				let list = []
				for (let i = 0; i < columnList.length; i++) {
					const e = columnList[i];
					if (!e.prop.includes('-')) {
						e.option = null
						list.push(e)
					} else {
						let prop = e.prop.split('-')[0]
						let vModel = e.prop.split('-')[1]
						let label = e.label.split('-')[0]
						let childLabel = e.label.replace(label + '-', '');
						let newItem = {
							align: "center",
							jnpfKey: "table",
							prop,
							label,
							children: []
						}
						e.vModel = vModel
						e.childLabel = childLabel
						if (!list.some(o => o.prop === prop)) list.push(newItem)
						for (let i = 0; i < list.length; i++) {
							if (list[i].prop === prop) {
								e.option = null
								list[i].children.push(e)
								break
							}
						}
					}
				}
				return list
			},
			upCallback(page) {
				if (this.isPreview == '1') return this.mescroll.endSuccess(0, false);
				const query = {
					currentPage: page.num,
					pageSize: page.size,
					menuId: this.modelId,
					...this.listQuery
				}
				listLink(this.modelId, query, this.encryption, {
					load: page.num == 1
				}, this.encryption).then(res => {
					this.showParser = true
					if (page.num == 1) this.list = [];
					this.mescroll.endSuccess(res.data.list.length);
					const list = res.data.list.map((o, i) => ({
						show: false,
						...o
					}));
					this.list = this.list.concat(list);
					uni.$off('refresh')
				}).catch((err) => {
					this.mescroll.endByPage(0, 0);
					this.mescroll.endErr();
					uni.$off('refresh')
				})
			},
			goDetail(item) {
				if (!item.id) return
				let config = {
					modelId: this.modelId,
					id: item.id,
					formTitle: '详情',
					noShowBtn: 1,
					encryption: this.encryption
				}
				this.$nextTick(() => {
					const url = `./detail?config=${this.jnpf.base64.encode(JSON.stringify(config),"UTF-8")}`
					uni.navigateTo({
						url: url
					})
				})
			},
			reset() {
				this.searchFormData = {}
				const list = ['datePicker', 'timePicker', 'inputNumber', 'calculate', 'cascader', 'organizeSelect']
				for (let i = 0; i < this.searchList.length; i++) {
					const item = this.searchList[i]
					const config = item.__config__
					let defaultValue = item.searchMultiple || list.includes(config.jnpfKey) ? [] : undefined
					config.defaultValue = defaultValue
					this.searchFormData[item.__vModel__] = defaultValue
				}
				this.searchFormConf = JSON.parse(JSON.stringify(this.searchList))
			},
			closeDropdown() {
				if (this.isPreview == '1') return this.$u.toast('功能预览不支持检索')
				this.$refs.searchForm && this.$refs.searchForm.submitForm()
			},
			fillFormData(list, data) {
				for (let i = 0; i < list.length; i++) {
					let item = list[i]
					const val = data.hasOwnProperty(item.__vModel__) ? data[item.__vModel__] : item.__config__
						.defaultValue
					if (!item.__config__.custom && item.__config__.defaultCurrent && item.__config__
						.jnpfKey === 'timePicker') val = this.jnpf.toDate(new Date(), item.format)
					if (!item.__config__.custom && item.__config__.defaultCurrent && item.__config__
						.jnpfKey === 'datePicker') val = new Date().getTime()
					item.__config__.defaultValue = val
				}
			},
			sumbitSearchForm(data) {
				const queryJson = data || {}
				this.searchFormData = data
				this.listQuery.queryJson = JSON.stringify(queryJson) !== '{}' ? JSON.stringify(queryJson) : ''
				this.$refs.uDropdown.close();
				this.$nextTick(() => {
					this.list = [];
					this.mescroll.resetUpScroll();
				})
			}
		}
	}
</script>

<style lang="scss">
	page {
		background-color: #f0f2f6;
		height: 100%;
		/* #ifdef MP-ALIPAY */
		position: absolute;
		top: 0;
		left: 0;
		width: 100%;
		/* #endif */
	}

	.item {
		padding: 0 !important;
	}

	.notData-box {
		width: 100%;
		height: 100%;
		justify-content: center;
		align-items: center;
		padding-bottom: 200rpx;

		.notData-inner {
			width: 280rpx;
			height: 308rpx;
			align-items: center;


			.iconImg {
				width: 100%;
				height: 100%;
			}

			.notData-inner-text {
				padding: 30rpx 0;
				color: #909399;
			}
		}
	}

	.screen-box {
		background-color: #fff;
		height: 100%;

		.screen-list {
			width: 100%;
			height: 100%;

			.list {
				height: calc(100% - 88rpx);
				overflow-y: scroll;
			}
		}
	}
</style>