<template>
	<u-popup class="jnpf-select" :maskCloseAble="maskCloseAble" mode="bottom" v-model="showPopup"
		:safeAreaInsetBottom="safeAreaInsetBottom" @close="close" :height="height" :mask-close-able="false">
		<view class="u-select">
			<view class="u-select__header" @touchmove.stop.prevent="">
				<view class="u-select__header__cancel u-select__header__btn" :style="{ color: cancelColor }"
					hover-class="u-hover-class" :hover-stay-time="150" @tap="close()"
					style="width: 60rpx;text-align: center;">
					<text v-if="cancelBtn">{{cancelText}}</text>
				</view>
				<view class="u-select__header__title" style="flex: 1;text-align: center;">
					{{title}}
				</view>
				<view class="u-select__header__confirm u-select__header__btn" :style="{ color: confirmColor }"
					style="width: 60rpx;text-align: center;" hover-class="u-hover-class" :hover-stay-time="150"
					@touchmove.stop="" @tap.stop="handleConfirm()">
					<text v-if="confirmBtn">{{confirmText}}</text>
				</view>
			</view>
			<view class="search-box_sticky" v-if=" isFlow || filterable">
				<view class="search-box">
					<u-search :placeholder="$t('app.apply.pleaseKeyword')" height="72" :show-action="false"
						bg-color="#f0f2f6" shape="square" v-model="searchValue">
					</u-search>
				</view>
			</view>
			<view class="u-select__body u-select__body__multiple">
				<scroll-view :scroll-y="true" style="height: 100%">
					<u-checkbox-group v-model="innerValue" v-if="multiple">
						<u-checkbox v-model="item.checked" v-for="(item, index) in columnList" :key="index"
							:name="item[valueName]">
							{{item[labelName]}}
						</u-checkbox>
					</u-checkbox-group>
					<u-radio-group wrap v-model="checkedValue" v-else>
						<u-radio @change="radioGroupChange(item,i)" :name="item[valueName]"
							v-for="(item,i) in columnList" :key="i">
							{{item[labelName]}}
						</u-radio>
					</u-radio-group>
					<view v-if="!columnList.length" class="notData-box u-flex-col">
						<view class="u-flex-col notData-inner">
							<image :src="icon" class="iconImg"></image>
						</view>
						<text class="notData-inner-text">{{$t('common.noData')}}</text>
					</view>
				</scroll-view>
			</view>
		</view>
	</u-popup>
</template>
<script>
	import resources from '@/libs/resources.js'
	export default {
		name: 'JnpfMultSelect',
		props: {
			list: {
				type: Array,
				default: () => []
			},
			height: {
				type: [Number, String],
				default: ''
			},
			multiple: {
				type: Boolean,
				default: false
			},
			filterable: {
				type: Boolean,
				default: false
			},
			cancelBtn: {
				type: Boolean,
				default: true
			},
			confirmBtn: {
				type: Boolean,
				default: true
			},
			show: {
				type: Boolean,
				default: false
			},
			cancelColor: {
				type: String,
				default: '#606266'
			},
			confirmColor: {
				type: String,
				default: '#2979ff'
			},
			safeAreaInsetBottom: {
				type: Boolean,
				default: false
			},
			maskCloseAble: {
				type: Boolean,
				default: true
			},
			defaultValue: {
				type: Array,
				default: () => []
			},
			labelName: {
				type: String,
				default: 'fullName'
			},
			valueName: {
				type: String,
				default: 'id'
			},
			title: {
				type: String,
				default: ''
			},
			cancelText: {
				type: String,
				default: '取消'
			},
			confirmText: {
				type: String,
				default: '确认'
			},
			isFlow: {
				type: Boolean,
				default: false
			}
		},
		data() {
			return {
				columnData: [],
				innerValue: [],
				lastSelectIndex: [],
				showPopup: false,
				checkedValue: '',
				searchValue: '',
				icon: resources.message.nodata,
			}
		},
		watch: {
			show: {
				handler(val) {
					this.showPopup = val
					if (val) setTimeout(() => this.init(), 10);
				},
				immediate: true,
			},
		},
		computed: {
			columnList() {
				return this.columnData.filter((o) => (o[this.labelName] && o[this.labelName].match(this.searchValue)))
			}
		},
		methods: {
			init() {
				this.setColumnData();
				this.setDefault();
			},
			setColumnData() {
				this.columnData = this.list.map((o, i) => ({
					...o,
					checked: false
				}))
			},
			// 获取默认选中的值
			setDefault() {
				this.searchValue = ''
				this.checkedValue = ''
				if (this.multiple) {
					this.innerValue = this.defaultValue
					outer: for (let i = 0; i < this.innerValue.length; i++) {
						inner: for (let j = 0; j < this.columnData.length; j++) {
							if (this.innerValue[i] === this.columnData[j][this.valueName]) {
								this.columnData[j].checked = true
								break inner
							}
						}
					}
				} else {
					for (let j = 0; j < this.columnData.length; j++) {
						if (this.defaultValue[0] === this.columnData[j][this.valueName]) {
							this.checkedValue = this.columnData[j][this.valueName]
							this.innerValue = this.columnData[j]
						}
					}
				}
			},
			radioGroupChange(e) {
				this.innerValue = [{
					...e,
					checked: true
				}]
			},
			handleConfirm() {
				if (this.multiple) {
					let data = {
						indexs: [],
						list: [],
						label: '',
						value: uni.$u.deepClone(this.innerValue)
					}
					if (!this.isFlow) {
						for (let i = 0; i < this.columnData.length; i++) {
							const item = this.columnData[i]
							if (this.columnData[i].checked) {
								data.list.push(uni.$u.deepClone(item))
								data.indexs.push(i)
								if (!data.label) {
									data.label += item[this.labelName]
								} else {
									data.label += ',' + item[this.labelName]
								}
							}
						}
					}
					this.$emit('confirm', data);
				} else {
					if (this.isFlow && !this.innerValue.length) return this.$u.toast("请选择流程");
					this.$emit('confirm', this.innerValue);
				}
				this.close()
			},
			close() {
				this.$emit('close');
			},
		}
	}
</script>
<style scoped lang="scss">
	.notData-box {
		width: 100%;
		height: 100%;
		justify-content: center;
		align-items: center;
		margin-top: -50px;

		.notData-inner {
			width: 286rpx;
			height: 222rpx;
			align-items: center;

			.iconImg {
				width: 100% !important;
				height: 100% !important;
			}
		}

		.notData-inner-text {
			color: #909399;
		}
	}

	.u-select {
		&__header {
			display: flex;
			flex-direction: row;
			align-items: center;
			justify-content: space-between;
			height: 40px;
			padding: 0 20px;
			position: relative;

			::after {
				content: "";
				position: absolute;
				border-bottom: 0.5px solid #eaeef1;
				transform: scaleY(0.5);
				bottom: 0;
				right: 0;
				left: 0;
			}
		}

		&__body {
			width: 100%;
			height: 500rpx;
			overflow: hidden;
			background-color: #fff;

			&__picker-view {
				height: 100%;
				box-sizing: border-box;

				&__item {
					display: flex;
					align-items: center;
					justify-content: center;
					font-size: 32rpx;
					padding: 0 8rpx;
				}
			}

			.u-checkbox-group {
				padding: 0 30rpx;

				.u-checkbox {
					height: 25px;
					line-height: 25px;
					width: 100% !important;
					display: flex;
					margin: 6rpx 0;

					:deep(uni-text) {
						flex: 1;
						overflow: hidden;
						white-space: nowrap;
						text-overflow: ellipsis;
					}
				}
			}
		}
	}
</style>