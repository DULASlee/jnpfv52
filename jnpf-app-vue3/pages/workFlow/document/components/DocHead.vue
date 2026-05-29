<template>
	<view class="notice-warp">
		<view class="search-box" style="background-color: #fff;">
			<u-search :placeholder="$t('app.apply.pleaseKeyword')" v-model="keyword" height="72" :show-action="false"
				@change="search" bg-color="#f0f2f6" shape="square" />
		</view>
		<CommonTabs :list="categoryList" @change="change" :current="current" ref="CommonTabs" :icon="icon" type="doc"
			@iconClick="iconClick">
		</CommonTabs>
	</view>
</template>

<script>
	import mixin from "../mixin.js"
	import CommonTabs from '@/components/CommonTabs'
	export default {
		mixins: [mixin],
		components: {
			CommonTabs
		},
		data() {
			return {
				icon: 'icon-ym icon-ym-thumb-mode',
				keyword: '',
				current: 0,
				categoryList: [{
					fullName: '我的文档',

				}, {
					fullName: '我的共享'
				}, {
					fullName: '共享给我'
				}, {
					fullName: '回收站'
				}],
			}
		},
		methods: {
			search() {
				this.searchTimer && clearTimeout(this.searchTimer);
				this.searchTimer = setTimeout(() => {
					this.$emit('search', this.keyword)
				}, 300);
			},
			change(e) {
				this.current = e;
				this.keyword = ''
				this.$emit('change', this.current)
			},
			iconClick(e) {
				this.$emit('iconClick')
			},
		}
	}
</script>

<style lang="scss">
	.notice-warp {
		z-index: 9;
		position: fixed;
		top: var(--window-top);
		left: 0;
		width: 100%;
		height: 200rpx;
		/*对应mescroll-body的top值*/
		font-size: 26rpx;
		text-align: center;
		background-color: #fff;
	}
</style>