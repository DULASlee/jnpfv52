<template>
	<view class="search-popup-v">
		<u-popup v-model="showPopup" width="100%" height="100vh" mode="right" :mask="false" @close="close">
			<view class="search-popup-b">
				<view class="search-popup-h">
					<view class="search-popup-h-txt">
						<u-icon name="close" @click="showPopup=false" class="search-popup-h-icon" />
					</view>
					<u-input type="text" v-model="value" placeholder="请输入" @input="onInput" :clearable="clearable" />
				</view>
			</view>
			<view class="search-popup-item" v-if="showList.length>0">
				<view v-for="(item, index) in showList" :key="index" @tap="selectThisItem(item)"
					class="u-p-l-20 u-p-r-20">{{item[relationField]}}</view>
			</view>
			<view class="search-notData" v-if="showList.length<1">
				<view class="notData-box u-flex-col">
					<view class="u-flex-col notData-inner">
						<image :src="icon" class="iconImg"></image>
						<text class="notData-inner-text">{{$t('common.noData')}}</text>
					</view>
				</view>
			</view>
		</u-popup>
	</view>
</template>

<script>
	import resources from '@/libs/resources.js'
	import {
		getPopSelect
	} from '@/api/common.js'
	export default {
		props: {
			interfaceId: {
				type: String,
				default: ''
			},
			clearable: {
				type: Boolean,
				default: true
			},
			relationField: {
				type: String,
				default: 'fullName'
			},
			total: {
				type: [String, Number],
				default: 50
			},
			formData: {
				type: Object
			},
			templateJson: {
				type: Array,
				default: () => []
			},
			rowIndex: {
				default: null
			},
		},
		data() {
			return {
				istQuery: {
					keyword: '',
					pageSize: 1000
				},
				icon: resources.message.nodata,
				showPopup: false,
				value: '',
				showList: [],
				timer: ''
			}
		},
		methods: {
			init(val) {
				this.showPopup = true
				this.value = val
				this.getDataInterfaceList()
			},
			getDataInterfaceList() {
				this.showList = []
				const paramList = this.getParamList()
				let query = {
					interfaceId: this.interfaceId,
					relationField: this.relationField,
					pageSize: 10000,
					paramList
				}
				getPopSelect(this.interfaceId, query).then(res => {
					let list = JSON.parse(JSON.stringify(res.data.list)) || []
					if (list.length) list = this.unique(list, this.relationField)
					this.showList = list.splice(0, this.total)
				})
			},
			unique(arr, attrName) {
				const res = new Map();
				// 根据对象的某个属性值去重
				return arr.filter(o => !res.has(o[attrName]) && res.set(o[attrName], 1));
			},
			getParamList() {
				let templateJson = this.templateJson
				for (let i = 0; i < templateJson.length; i++) {
					if (templateJson[i].relationField && this.formData && templateJson[i].sourceType == 1) {
						if (templateJson[i].relationField.includes('-')) {
							let tableVModel = templateJson[i].relationField.split('-')[0]
							let childVModel = templateJson[i].relationField.split('-')[1]
							templateJson[i].defaultValue = this.formData[tableVModel] && this.formData[tableVModel][this
								.rowIndex
							] && this.formData[tableVModel][this.rowIndex][childVModel] || ''
						} else {
							templateJson[i].defaultValue = this.formData[templateJson[i].relationField] || ''
						}
					}
					if (templateJson[i].relationField == '@keyword') templateJson[i].defaultValue = this.value
				}
				return templateJson
			},
			onInput(e) {
				this.value = e
				this.$emit('confirm', this.value);
				this.timer && clearTimeout(this.timer);
				this.timer = setTimeout(() => {
					this.list = [];
					this.getDataInterfaceList()
				}, 300);
			},
			close() {
				this.showPopup = false
			},
			selectThisItem(item) {
				this.value = item[this.relationField];
				this.$emit('confirm', this.value);
				this.close()
			}
		}
	}
</script>

<style lang="scss">
	.search-popup-v {
		.search-popup-b {
			height: 158rpx;

			.search-popup-h {
				padding: 0 20rpx;
				border-bottom: 1rpx solid #cbcbcb;
				position: fixed;
				width: 100%;
				background-color: #fff;
				z-index: 9;
				text-align: center;

				.search-popup-h-txt {
					height: 86rpx;
					width: 100%;
					padding: 15rpx 0;
					text-align: center;
					line-height: 54rpx;
					box-sizing: border-box;
					font-size: 32rpx;
					font-weight: 700;
					letter-spacing: 2rpx;

					.search-popup-h-icon {
						float: right;
						margin-top: 12rpx;
					}
				}
			}
		}



		.search-popup-item {
			width: 100%;
			height: 100%;
			z-index: 9997;
		}

		.search-notData {
			width: 100%;
			height: calc(100% - 160rpx);
			background-color: #fff;

			.notData-box {
				width: 100%;
				height: 100%;
				justify-content: center;
				align-items: center;

				.notData-inner {
					width: 280rpx;
					height: 340rpx;
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
		}


	}
</style>