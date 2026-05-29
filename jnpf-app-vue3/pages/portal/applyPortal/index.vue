<template>
	<view class="scan-v">
		<mescroll-body ref="mescrollRef" @down="downCallback" :down="downOption" :sticky="true" @up="upCallback"
			:up="upOption" :bottombar="false" style="min-height: 100%" @init="mescrollInit">
			<view class="portal-v">
				<view v-if="formData.length">
					<view class="portal-box" v-for="(item,index) in formData" :key="index">
						<portalItem :item='item' ref="portalItem"></portalItem>
					</view>
				</view>
				<view v-else class="portal-v portal-nodata">
					<view class="u-flex-col" style="align-items: center;">
						<u-image width="280rpx" height="280rpx" :src="emptyImg"></u-image>
						<text class="u-m-t-20" style="color: #909399;">{{$t('common.noData')}}</text>
					</view>
				</view>
			</view>
		</mescroll-body>
	</view>
</template>
<script>
	import portalItem from '@/pages/portal//components/index.vue'
	import MescrollMixin from "@/uni_modules/mescroll-uni/components/mescroll-uni/mescroll-mixins.js"
	import {
		getPreviewPortal,
		auth
	} from '@/api/portal/portal.js'
	import resources from '@/libs/resources.js'
	export default {
		mixins: [MescrollMixin],
		components: {
			portalItem
		},
		data() {
			return {
				id: '',
				show: false,
				formData: [],
				dataList: [],
				emptyImg: resources.message.nodata,
				fullName: '',
				downOption: {
					use: true,
					auto: true
				},
				upOption: {
					page: {
						num: 0,
						size: 50,
						time: null
					},
					empty: {
						use: false,
					},
					textNoMore: this.$t('app.apply.noMoreData'),
				}
			}
		},
		onLoad(e) {
			uni.setStorageSync('token', e.token)
			this.id = e.id
			uni.$on('refresh', () => {
				this.formData = [];
				this.mescroll.resetUpScroll();
			})
		},
		methods: {
			upCallback(keyword) {
				this.mescroll.lockDownScroll(true);
				auth(this.id).then(res => {
					let data = res.data.formData ? JSON.parse(res.data.formData) : {};
					this.formData = data.layout ? JSON.parse(JSON.stringify(data.layout)) : [];
					this.mescroll.endSuccess(this.formData.length);
					if (!this.formData.length) return
					this.handelFormData(data)
				}).catch(() => {
					this.mescroll.endSuccess(0);
					this.mescroll.endErr();
				})
			},
			handelFormData(data) {
				const loop = (list) => {
					list.forEach(o => {
						o.allRefresh = data.refresh
						o.show = false
						if (o.visibility && o.visibility.length && o.visibility.includes('app')) o.show =
							true
						if (o.children && o.children.length) loop(o.children)
					})
					this.key = +new Date()
				}
				loop(this.formData)
				this.dataList = this.formData.filter(o => o.show)
				if (this.dataList.length < 1) {
					this.formData = this.dataList
				}
			},
		}
	}
</script>

<style lang="scss">
	page {
		background-color: #f0f2f6;
	}

	.portal-nodata {
		position: absolute;
		top: 450rpx;
		width: 100%;
		text-align: center;
		z-index: 100;
		background-color: #f0f2f6;
	}
</style>