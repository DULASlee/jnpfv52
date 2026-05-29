<template>
	<view class="list u-p-b-20 u-p-l-20 u-p-r-20" ref="tableRef">
		<view class="list-box">
			<u-swipe-action :show="item.show" :index="index" v-for="(item, index) in list" :key="index"
				@click="actionClick" @open="open" :options="options" :btnWidth="160" class="u-m-t-20">
				<view class="item" @tap.stop="goDetail(item)">
					<u-checkbox v-if="showCheckbox" @change="checkboxChange($event,item,index)" v-model="item.checked"
						class="checkbox" @tap.stop shape="circle"></u-checkbox>
					<view class="item-cell" v-for="(column,i) in columnList" :key="i">
						<template v-if="column.jnpfKey != 'table'">
							<text class="item-cell-label">{{column.label}}:</text>
							<text class="item-cell-content" v-if="['calculate','inputNumber'].includes(column.jnpfKey)">
								{{toThousands(item[column.prop],column)}}
							</text>
							<text class="item-cell-content text-primary" v-else-if="column.jnpfKey == 'relationForm'"
								@click.stop="relationFormClick(item,column)">
								{{item[column.prop]}}
							</text>
							<view class="item-cell-content" v-else-if="column.jnpfKey == 'sign'">
								<JnpfSign v-model="item[column.prop]" align="left" detailed />
							</view>
							<view class="item-cell-content" v-else-if="column.jnpfKey == 'signature'">
								<JnpfSignature v-model="item[column.prop]" align="left" detailed />
							</view>
							<view class="item-cell-content" v-else-if="column.jnpfKey == 'uploadImg'" @click.stop>
								<JnpfUploadImg v-model="item[column.prop]" detailed simple
									v-if="item[column.prop]&&item[column.prop].length" />
							</view>
							<!-- #ifndef APP-HARMONY -->
							<view class="item-cell-content" v-else-if="column.jnpfKey == 'uploadFile'" @click.stop>
								<JnpfUploadFile v-model="item[column.prop]" detailed
									v-if="item[column.prop]&&item[column.prop].length" align="left" />
							</view>
							<!-- #endif -->
							<view class="item-cell-content" v-else-if="column.jnpfKey == 'rate'">
								<JnpfRate v-model="item[column.prop]" :max="column.count" :allowHalf="column.allowHalf"
									disabled />
							</view>
							<view class="item-cell-content item-cell-slider" v-else-if="column.jnpfKey == 'slider'">
								<JnpfSlider v-model="item[column.prop]" :min="column.min" :max="column.max"
									:step="column.step" disabled />
							</view>
							<view class="item-cell-content" v-else-if="column.jnpfKey == 'input'">
								<JnpfInput v-model="item[column.prop]" detailed showOverflow :useMask="column.useMask"
									:maskConfig="column.maskConfig" align='left' />
							</view>
							<text class="item-cell-content" v-else>{{item[column.prop]}}</text>
						</template>
						<tableCell v-else @click.stop class="tableCell" ref="tableCell" :label="column.label"
							:childList="item[column.prop]" :children="column.children" :pageLen="3"
							@cRelationForm="relationFormClick">
						</tableCell>
					</view>
					<view class="item-cell" v-if="config.enableFlow==1">
						<text class="item-cell-label">审批状态:</text>
						<text :style="{color:useDefine.getFlowStatusColor(item.flowState)}">
							{{useDefine.getFlowStatusContent(item.flowState)}}
						</text>
					</view>
				</view>
			</u-swipe-action>
		</view>
	</view>
</template>

<script>
	import {
		useDefineSetting
	} from '@/utils/useDefineSetting';
	import tableCell from '../tableCell.vue'
	export default {
		emits: ['selectCheckbox', 'handleClick', 'handleMoreClick', 'goDetail', 'relationFormClick', 'update:modelValue'],
		components: {
			tableCell
		},
		props: ['config', 'list', 'columnList', 'actionOptions', 'showSelect', 'checkedAll', 'modelValue', 'isMoreBtn',
			'customBtnsList'
		],
		data() {
			return {
				selectData: [],
				useDefine: useDefineSetting()
			}
		},
		watch: {
			checkedAll: {
				handler(val) {
					this.handleCheckAll()
				},
				immediate: true
			}
		},
		computed: {
			options() {
				if (!this.customBtnsList?.length) return this.actionOptions;
				return [{
						text: this.$t('common.moreText'),
						value: 'more',
						style: {
							backgroundColor: '#007aff'
						}
					},
					...this.actionOptions,
				];
			},
			showCheckbox() {
				return this.showSelect
			}
		},
		methods: {
			open(index) {
				this.list[index].show = true;
				this.list.map((val, idx) => {
					if (index != idx) this.list[idx].show = false;
				})
			},
			/* 关联表单操作 */
			relationFormClick(item, column) {
				this.$emit('relationFormClick', item, column)
			},
			/* 跳转详情 */
			goDetail(item) {
				this.$emit('goDetail', item)
			},
			actionClick(itemIndex, btnIndex) {
				if (this.options[btnIndex].value === 'remove') return this.$emit('handleClick', itemIndex)
				if (this.options[btnIndex].value === 'more') return this.$emit('handleMoreClick', itemIndex)
			},
			/* 列表选择框 */
			checkboxChange(e, item) {
				const isSelected = e.value;
				const itemIndex = this.list.indexOf(item);
				if (itemIndex === -1) return;
				const selectedItemsSet = new Set(this.selectData.map(selectedItem => {
					return selectedItem.id;
				}));
				if (isSelected) {
					selectedItemsSet.add(item.id);
				} else {
					selectedItemsSet.delete(item.id);
				}
				this.selectData = [...selectedItemsSet.values()].map(id => {
					return this.list.find(listItem => listItem.id === id);
				});
				this.$emit('selectCheckbox', this.selectData);
			},
			/* 全部选中 */
			handleCheckAll() {
				this.selectData = []
				if (this.checkedAll) this.selectData = this.list.filter(o => o.checked)
				this.$emit('selectCheckbox', this.selectData)
			},
			/* 千分位操作 */
			toThousands(val, column) {
				if (val) {
					let valList = val.toString().split('.')
					let num = Number(valList[0])
					let newVal = column.thousands ? num.toLocaleString() : num
					return valList[1] ? newVal + '.' + valList[1] : newVal
				} else {
					return val
				}
			}
		}
	}
</script>

<style lang="scss">
	.item {
		padding: 0 !important;

		.checkbox {
			position: absolute;
			top: 6rpx;
			left: 8rpx;
			z-index: 9999;
		}
	}

	.right-option-box {
		display: flex;
		width: max-content;

		.right-option {
			width: 144rpx;
			height: 100%;
			font-size: 16px;
			color: #fff;
			background-color: #dd524d;
			display: flex;
			align-items: center;
			justify-content: center;
		}

		.more-option {
			background-color: #1890ff;
		}
	}
</style>