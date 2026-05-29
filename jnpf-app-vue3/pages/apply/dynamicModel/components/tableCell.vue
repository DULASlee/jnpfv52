<template>
	<uni-collapse class='collapse' accordion ref="collapse" @change="collapseChange" @click.stop>
		<uni-collapse-item :key="key">
			<template v-slot:title>
				<view class="u-font-24 u-flex">
					<view style="width: 124rpx;text-align: right;">
						<text>{{label+':'}}</text>
					</view>
					<text style="color: #606266;" class="u-m-l-28">{{$t('app.apply.expandData')}}</text>
				</view>
			</template>
			<view class="collapse-item" v-for="(item,d) in dataList" :key="d">
				<view v-if="d<allPageLen" class="item-cell-children">
					<view class="item-cell" v-for="(cld,c) in children" :key="c">
						<text
							class="item-cell-label">{{cld.labelI18nCode ? $t(cld.labelI18nCode, cld.label) : cld.label}}:</text>
						<text class="item-cell-content"
							v-if="['calculate','inputNumber'].includes(cld.__config__.jnpfKey)">
							{{toThousands(item[cld.vModel],cld) }}
						</text>
						<text class="item-cell-content text-primary"
							v-else-if="cld.__config__.jnpfKey === 'relationForm'"
							@click.stop="relationFormClick(item,cld)">
							{{item[cld.vModel]}}
						</text>
						<view class="item-cell-content" v-else-if="cld.jnpfKey == 'sign'">
							<JnpfSign v-model="item[cld.vModel]" align="left" detailed />
						</view>
						<view class="item-cell-content" v-else-if="cld.jnpfKey == 'signature'">
							<JnpfSignature v-model="item[cld.vModel]" align="left" detailed />
						</view>
						<view class="item-cell-content" v-else-if="cld.jnpfKey == 'uploadImg'" @click.stop>
							<JnpfUploadImg v-model="item[cld.vModel]" detailed simple
								v-if="item[cld.vModel]&&item[cld.vModel].length" />
						</view>
						<!-- #ifndef APP-HARMONY -->
						<view class="item-cell-content" v-else-if="cld.jnpfKey == 'uploadFile'" @click.stop>
							<JnpfUploadFile v-model="item[cld.vModel]" detailed
								v-if="item[cld.vModel]&&item[cld.vModel].length" align="left" />
						</view>
						<!-- #endif -->
						<view class="item-cell-content" v-else-if="cld.jnpfKey == 'rate'">
							<JnpfRate v-model="item[cld.vModel]" :max="cld.count" :allowHalf="cld.allowHalf" disabled />
						</view>
						<view class="item-cell-content item-cell-slider" v-else-if="cld.jnpfKey == 'slider'">
							<JnpfSlider v-model="item[cld.vModel]" :min="cld.min" :max="cld.max" :step="cld.step"
								disabled />
						</view>
						<view class="item-cell-content" v-else-if="cld.jnpfKey == 'input'">
							<JnpfInput v-model="item[cld.vModel]" detailed showOverflow :useMask="cld.useMask"
								:maskConfig="cld.maskConfig" align='left' />
						</view>
						<text class="item-cell-content" v-else>{{item[cld.vModel]}}</text>
					</view>
				</view>
			</view>
			<view class="loadMore" @click.stop="loadMore" v-if="!isAllData&&this.dataList.length>allPageLen">
				加载更多
			</view>
		</uni-collapse-item>
	</uni-collapse>
</template>
<script>
	export default {
		props: ['childList', 'label', 'children', 'pageLen', 'thousands', 'thousandsField'],
		data() {
			return {
				dataList: [],
				isAllData: false,
				key: +new Date(),
				allPageLen: 3
			}
		},
		watch: {
			childList: {
				handler(val) {
					this.dataList = val || []
					this.allPageLen = this.pageLen
					this.children.map(o => {
						if (o.childLabel.length > 4) o.childLabel = o.childLabel.substring(0, 4)
					})
				},
				immediate: true,
			}
		},
		methods: {
			toThousands(val, column) {
				if (val) {
					let valList = val.toString().split('.')
					let num = Number(valList[0])
					let newVal = column.thousands ? num.toLocaleString() : num
					return valList[1] ? newVal + '.' + valList[1] : newVal
				}
			},
			relationFormClick(item, cld) {
				this.$emit('cRelationForm', item, cld)
			},
			loadMore() {
				this.allPageLen = this.childList.length
				this.isAllData = true
				this.resizeCollapse()
			},
			collapseChange(e) {
				if (!e) {
					this.isAllData = false
					setTimeout(() => {
						this.allPageLen = this.pageLen
					}, 500)
				}
				this.resizeCollapse()
			},
			resizeCollapse() {
				setTimeout(() => {
					this.$refs.collapse && this.$refs.collapse.resize()
				}, 50)
			}
		}
	}
</script>