<template>
	<view class="personalData-v">
		<view class="notice-warp">
			<u-tabs :list="tabBars" :is-scroll="false" v-model="current" @change="tabChange" height="100">
			</u-tabs>
		</view>
		<view class="content">
			<accountData ref="accountData" v-if="current==0" :accountData="baseInfo"></accountData>
			<personalData ref="personalData" v-if="current == 1" :personalData="baseInfo"></personalData>
			<signList ref="signList" v-if="current == 2"></signList>
			<commonText ref="commonText" v-if="current == 3"></commonText>
		</view>
	</view>
</template>
<script>
	import personalData from './components/personalData.vue';
	import accountData from './components/accountInformation.vue';
	import signList from './components/signList.vue';
	import commonText from './components/commonText.vue';
	export default {
		components: {
			personalData,
			accountData,
			signList,
			commonText
		},
		data() {
			return {
				tabBars: [{
					name: '账户信息'
				}, {
					name: '个人资料'
				}, {
					name: '个人签名'
				}, {
					name: '审批常用语'
				}],
				current: 0,
				baseInfo: {}
			};
		},
		onLoad(e) {
			this.baseInfo = JSON.parse(decodeURIComponent(e.baseInfo))
		},
		methods: {
			tabChange(index) {
				this.current = index;
			}
		}
	}
</script>

<style lang="scss">
	page {
		background-color: #f0f2f6;
		height: 100%;
	}

	.notice-warp {
		height: 100rpx;

		.search-box {
			padding: 20rpx;
		}
	}

	.content {
		margin-top: 120rpx;
	}

	.personalData-v {
		display: flex;
		flex-direction: column;
		padding-bottom: 100rpx;

		::v-deep .buttom-btn {
			width: 100% !important;
		}
	}
</style>